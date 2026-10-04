/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#if UNITY_EDITOR
#nullable enable
using System;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common.Execution;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class MainThreadInstaller
    {
        [InitializeOnLoadMethod]
        public static void Init() => MainThread.Instance = new UnityMainThread();
    }
    public class UnityMainThread : MainThread
    {
        public override bool IsMainThread => MainThreadDispatcher.IsMainThread;

        public override Task RunAsync(Task task)
            => CanRunInline() ? task : Dispatch(() => { task.Wait(); return true; });

        public override Task<T> RunAsync<T>(Task<T> task)
            => CanRunInline() ? task : Dispatch(() => task.Result);

        public override Task<T> RunAsync<T>(Func<T> func)
            => CanRunInline() ? Task.FromResult(func()) : Dispatch(func);

        public override Task RunAsync(Action action)
        {
            if (CanRunInline())
            {
                action();
                return Task.CompletedTask;
            }
            return Dispatch(() => { action(); return true; });
        }

        static bool CanRunInline()
        {
            ToolExecutionContext.Current?.ThrowIfExpired();
            if (!MainThreadDispatcher.IsMainThread) return false;
            ToolExecutionContext.Current?.MarkMainThreadStarted();
            return true;
        }

        static Task<T> Dispatch<T>(Func<T> body)
        {
            var execution = new QueuedExecution<T>(body, ToolExecutionContext.Current);
            void Execute()
            {
                EditorApplication.update -= Execute;
                execution.Execute();
            }
            EditorApplication.update += Execute;
            return execution.Task;
        }
    }
}
#endif
