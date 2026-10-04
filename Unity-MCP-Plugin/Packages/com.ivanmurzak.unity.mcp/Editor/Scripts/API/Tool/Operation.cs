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

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [InitializeOnLoad]
    [AiToolType]
    public static partial class Tool_Operation
    {
        private static readonly string LedgerPath;
        private static readonly Lazy<DurableOperationStore> Store;

        static Tool_Operation()
        {
            // Unity invokes InitializeOnLoad on its main thread; later RPCs do no Unity API I/O.
            LedgerPath = Path.Combine(UnityMcpPluginEditor.ProjectRootPath, "Library", "mcp-operations");
            Store = new Lazy<DurableOperationStore>(() => new DurableOperationStore(LedgerPath));
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }
        private static void Dispose() { if (Store.IsValueCreated) Store.Value.Dispose(); }
        internal static int ActiveExecutionCount => Store.IsValueCreated ? Store.Value.ActiveExecutionCount : 0;
        private static string Identity => ToolExecutionContext.Current?.IdentityScope ?? "local";

        [AiTool("tool-operation-start", Title = "Tool / Start durable operation", IdempotentHint = true)]
        [Description("Accepts a long-running tool and returns a durable operation handle immediately. " +
            "Choose and retain operationId BEFORE calling; reuse the same ID and arguments after timeout. " +
            "Use tool-operation-status to poll. No completion notification is promised. " +
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
                // Detached from the accepting MCP request's timeout. Only explicit operation
                // cancellation or editor shutdown cancels this execution.
                ToolExecutionContext.Current = new ToolExecutionContext(identity, token, null,
                    () => store.MarkMainThreadStarted(identity, operationId), allowDuringMaintenance: true);
                var request = new RequestCallTool(operationId, toolName, arguments)
                {
                    IdentityScope = identity,
                    ExecutionScope = "durable:" + DurableOperationStore.Digest(LedgerPath + identity)
                };
                var response = await manager.RunCallTool(request, token).ConfigureAwait(false);
                var resultJson = JsonSerializer.Serialize(response);
                if (response.Status == ResponseStatus.Processing || response.Value?.Status == ResponseStatus.Processing)
                    throw new OperationOutcomeUnknownException("Target uses legacy completion notifications; its final outcome is unknown to this durable wrapper. Inspect outputs; do not resubmit under a new ID.");
                if (response.Status == ResponseStatus.Error || response.Value?.Status == ResponseStatus.Error)
                {
                    token.ThrowIfCancellationRequested();
                    throw new InvalidOperationException(resultJson);
                }
                return resultJson;
            });
        }

        [AiTool("tool-operation-status", Title = "Tool / Query durable operation", ReadOnlyHint = true, IdempotentHint = true)]
        [Description("Queries operation state and saved result by stable ID in the caller's identity and project. " +
            "running may include waiting for Unity's main thread; MainThreadStarted records entry into a Unity callback. " +
            "unknown requires inspecting output artifacts before submitting a different ID.")]
        public static OperationSnapshot Status(string operationId) => Store.Value.Get(Identity, operationId);

        [AiTool("tool-operation-cancel", Title = "Tool / Cancel durable operation", IdempotentHint = true)]
        [Description("Requests cancellation. A queued Unity callback will not run once cancellation is observed. " +
            "Already started synchronous builds can continue to completion. Check status; cancellation does not roll back side effects.")]
        public static OperationSnapshot Cancel(string operationId) => Store.Value.Cancel(Identity, operationId);
    }
}
