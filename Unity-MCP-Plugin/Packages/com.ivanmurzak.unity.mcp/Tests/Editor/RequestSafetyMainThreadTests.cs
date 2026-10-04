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
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common.Execution;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class RequestSafetyMainThreadTests
    {
        [UnityTest]
        public IEnumerator BusyEditor_CancelPostedCallbackBeforeUpdate_ZeroExecutions()
        {
            using var cancellation = new CancellationTokenSource();
            var posted = new TaskCompletionSource<Task<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            int writes = 0;
            var mainThread = new UnityMainThread();
            _ = Task.Run(() =>
            {
                ToolExecutionContext.Current = new ToolExecutionContext("isolated-test", cancellation.Token, null);
                posted.SetResult(mainThread.RunAsync(() => Interlocked.Increment(ref writes)));
            });
            // Hold the editor's main thread until the callback is queued, modeling a busy
            // editor without running a build or pumping updates while awaiting the worker.
            Assert.IsTrue(SpinWait.SpinUntil(() => posted.Task.IsCompleted, 3000), "worker did not post a callback");
            cancellation.Cancel();
            var task = posted.Task.Result;
            Assert.IsTrue(task.IsCanceled);
            yield return null; // actual EditorApplication.update now runs
            yield return null;
            Assert.AreEqual(0, writes);
        }

        [UnityTest]
        public IEnumerator EditorUpdate_ExecutesCallbackOnce_AcrossSynchronousAssetRefresh()
        {
            var posted = new TaskCompletionSource<Task<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            int writes = 0;
            var mainThread = new UnityMainThread();
            _ = Task.Run(() => posted.SetResult(mainThread.RunAsync(() =>
            {
                Interlocked.Increment(ref writes);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                return writes;
            })));
            Assert.IsTrue(SpinWait.SpinUntil(() => posted.Task.IsCompleted, 3000));
            var task = posted.Task.Result;
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!task.IsCompleted && DateTime.UtcNow < deadline) yield return null;
            Assert.IsTrue(task.IsCompleted, "editor callback did not run");
            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            Assert.AreEqual(1, task.Result);
            yield return null;
            Assert.AreEqual(1, writes);
        }
    }
}
