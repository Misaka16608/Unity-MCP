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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common.Execution;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    /// <summary>Claims a queued callback once, including under nested editor updates.</summary>
    public sealed class QueuedExecution<T>
    {
        private readonly Func<T> _body;
        private readonly ToolExecutionContext? _context;
        private readonly TaskCompletionSource<T> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenRegistration _registration;
        private int _state; // 0 queued, 1 started, 2 canceled before start
        public Task<T> Task => _completion.Task;

        public QueuedExecution(Func<T> body, ToolExecutionContext? context)
        {
            _body = body;
            _context = context;
            _registration = context?.CancellationToken.Register(CancelQueued) ?? default;
        }
        private void CancelQueued()
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
                _completion.TrySetCanceled();
        }
        public void Execute()
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            {
                _registration.Dispose();
                return;
            }
            var previous = ToolExecutionContext.Current;
            try
            {
                ToolExecutionContext.Current = _context;
                _context?.ThrowIfExpired();
                _context?.MarkMainThreadStarted();
                _completion.TrySetResult(_body());
            }
            catch (OperationCanceledException) { _completion.TrySetCanceled(); }
            catch (Exception ex) { _completion.TrySetException(ex); }
            finally
            {
                ToolExecutionContext.Current = previous;
                _registration.Dispose();
            }
        }
    }
}
