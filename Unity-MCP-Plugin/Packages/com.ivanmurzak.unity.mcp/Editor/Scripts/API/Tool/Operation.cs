/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Execution;
using com.IvanMurzak.McpPlugin.Common.Hub.Client;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using MainThread = com.IvanMurzak.ReflectorNet.Utils.MainThread;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [InitializeOnLoad]
    [AiToolType]
    public static partial class Tool_Operation
    {
        private static readonly string LedgerPath;
        private static readonly Lazy<DurableOperationStore> Store;
        private static readonly AsyncLocal<bool> DurableExecution = new();
        private static volatile bool _stopping;
        internal static bool IsDurableExecution => DurableExecution.Value;

        static Tool_Operation()
        {
            // Unity invokes InitializeOnLoad on its main thread; later RPCs do no Unity API I/O.
            LedgerPath = Path.Combine(UnityMcpPluginEditor.ProjectRootPath, "Library", "mcp-operations");
            Store = new Lazy<DurableOperationStore>(() => new DurableOperationStore(LedgerPath));
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }
        private static void Dispose() { _stopping = true; if (Store.IsValueCreated) Store.Value.Dispose(); }
        private static string Identity => ToolExecutionContext.Current?.IdentityScope ?? "local";

        [AiTool("tool-operation-start", Title = "Tool / Start durable operation", IdempotentHint = true)]
        [Description("Accepts a long-running tool and returns a durable operation handle immediately. " +
            "Choose and retain operationId BEFORE calling; reuse the same ID and arguments after timeout. " +
            "Use tool-operation-status to poll; Processing targets are completed from their later notification. " +
            "An unknown operation after editor/domain reload is never automatically executed again.")]
        public static OperationSnapshot Start(
            [Description("Stable operation ID chosen by the caller, unique within this identity and project.")] string operationId,
            [Description("Registered tool to execute, for example script-execute. Operation control tools cannot be nested.")] string toolName,
            [Description("JSON object containing the target tool arguments.")] string argumentsJson = "{}")
        {
            if (toolName.StartsWith("tool-operation-", StringComparison.Ordinal))
                throw new ArgumentException("Operation control tools cannot be nested.");
            var manager = UnityMcpPluginEditor.Instance.Tools ?? throw new InvalidOperationException("Tool manager unavailable.");
            if (!manager.HasTool(toolName) || !manager.IsToolEnabled(toolName)) throw new ArgumentException("Target tool is missing or disabled.");
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson)
                ?? throw new ArgumentException("Arguments must be a JSON object.");
            var identity = Identity;
            var fingerprint = DurableOperationStore.Digest(JsonSerializer.Serialize(new { toolName, Arguments = arguments.OrderBy(x => x.Key).ToArray() }));
            var store = Store.Value;
            return store.Start(identity, operationId, fingerprint, async (token, started) =>
            {
                started();
                DurableExecution.Value = true;
                // Detached from the accepting MCP request's timeout. Only explicit operation
                // cancellation or editor shutdown cancels this execution.
                ToolExecutionContext.Current = new ToolExecutionContext(identity, token, null,
                    () => store.MarkMainThreadStarted(identity, operationId));
                var completionRequestId = DurableOperationStore.CompletionRequestId(identity, operationId);
                var request = new RequestCallTool(completionRequestId, toolName, arguments)
                {
                    IdentityScope = identity,
                    ExecutionScope = "durable:" + DurableOperationStore.Digest(LedgerPath + identity)
                };
                var response = await manager.RunCallTool(request, token).ConfigureAwait(false);
                var resultJson = JsonSerializer.Serialize(response);
                if (response.Status == ResponseStatus.Processing || response.Value?.Status == ResponseStatus.Processing)
                    return await store.WaitForExternalCompletion(identity, operationId, token).ConfigureAwait(false);
                if (response.Status == ResponseStatus.Error || response.Value?.Status == ResponseStatus.Error)
                {
                    token.ThrowIfCancellationRequested();
                    throw new InvalidOperationException(resultJson);
                }
                return resultJson;
            });
        }

        /// <summary>Consumes a delayed completion notification before it is forwarded to the MCP hub.</summary>
        public static bool TryCompleteExternal(string requestId, ResponseCallTool result)
        {
            if (string.IsNullOrWhiteSpace(requestId) || result == null
                || !requestId.StartsWith("durable-completion:", StringComparison.Ordinal)) return false;
            if (_stopping) return true;
            // Force the ledger open here as completion notifications can arrive after a
            // domain reload, before the tool manager has enumerated tool types again.
            if (result.Status == ResponseStatus.Processing) return true;
            var store = Store.Value;
            var json = JsonSerializer.Serialize(result);
            var succeeded = result.Status != ResponseStatus.Error;
            store.CompleteExternal(requestId, json, succeeded,
                succeeded ? null : "The target tool returned an error response.");
            return true; // Internal completion IDs must never be forwarded to the hub.
        }

        /// <summary>Observes detached tool failures and retains the durable cancellation token after Processing.</summary>
        public static async Task RunCooperativeAsync(string requestId, Func<CancellationToken, Task> execute, CancellationToken fallback)
        {
            var durable = requestId.StartsWith("durable-completion:", StringComparison.Ordinal);
            var token = durable ? Store.Value.CompletionCancellationToken(requestId, fallback) : fallback;
            try
            {
                await MainThread.Instance.RunAsync(() => execute(token)).Unwrap().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                if (_stopping) return;
                if (durable)
                    Store.Value.CompleteExternal(requestId, "null", false, "The tool acknowledged cancellation before mutation.", canceled: true);
                else
                    await UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData
                    {
                        RequestId = requestId,
                        Result = ResponseCallTool.Error("Canceled before mutation.").SetRequestID(requestId)
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_stopping) return;
                await UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData
                {
                    RequestId = requestId,
                    Result = ResponseCallTool.Error(ex.Message).SetRequestID(requestId)
                }).ConfigureAwait(false);
            }
        }

        [AiTool("tool-operation-status", Title = "Tool / Query durable operation", ReadOnlyHint = true, IdempotentHint = true)]
        [Description("Queries operation state and saved result by stable ID in the caller's identity and project. " +
            "running may include waiting for Unity's main thread; awaiting-completion means the target returned Processing and its later completion notification is being awaited. " +
            "MainThreadStarted records entry into a Unity callback. " +
            "unknown requires inspecting output artifacts before submitting a different ID.")]
        public static OperationSnapshot Status(string operationId) => Store.Value.Get(Identity, operationId);

        [AiTool("tool-operation-cancel", Title = "Tool / Cancel durable operation", IdempotentHint = true)]
        [Description("Requests cancellation. A queued Unity callback will not run once cancellation is observed. " +
            "Scripts can use cancellationToken cooperatively. Package operations check before mutation; started native requests are observed to completion. " +
            "Check status; cancellation does not roll back side effects.")]
        public static OperationSnapshot Cancel(string operationId) => Store.Value.Cancel(Identity, operationId);
    }
}
