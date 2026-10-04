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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common.Execution;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    public class RequestExecutionTests
    {
        [Fact]
        public async Task BusyMainThread_CancelBeforeUpdate_ExecutesNothing()
        {
            using var cancellation = new CancellationTokenSource();
            int count = 0;
            var queued = new QueuedExecution<int>(() => ++count, new ToolExecutionContext("account", cancellation.Token, null));
            Assert.False(queued.Task.IsCompleted);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued.Task);
            queued.Execute();
            queued.Execute();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task BusyMainThread_DeadlineExpires_EvenIfCancelMessageWasLost()
        {
            int count = 0;
            var queued = new QueuedExecution<int>(() => ++count, new ToolExecutionContext("account", default, DateTimeOffset.UtcNow.AddSeconds(-1)));
            queued.Execute();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued.Task);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task NestedEditorUpdate_ClaimsCallbackBeforeBody()
        {
            int count = 0;
            QueuedExecution<int>? queued = null;
            queued = new QueuedExecution<int>(() => { ++count; queued!.Execute(); return count; }, null);
            queued.Execute();
            queued.Execute();
            Assert.Equal(1, await queued.Task);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CancelAfterBodyStarted_DoesNotClaimRollback()
        {
            using var cancellation = new CancellationTokenSource();
            var queued = new QueuedExecution<int>(() => { cancellation.Cancel(); return 42; }, new ToolExecutionContext("account", cancellation.Token, null));
            queued.Execute();
            Assert.Equal(42, await queued.Task);
        }

        [Fact]
        public async Task ConcurrentSameRequest_SharesInFlightAndCompletedResult()
        {
            using var registry = new ExecutionRegistry<int>();
            var finish = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int count = 0;
            Task<int> Run() => registry.Run("account/session/project", "id", "arguments", _ => { Interlocked.Increment(ref count); started.SetResult(true); return finish.Task; });
            var first = Run();
            var all = Enumerable.Range(0, 32).Select(_ => Run()).ToArray();
            Assert.All(all, t => Assert.Same(first, t));
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            finish.SetResult(42);
            Assert.Equal(42, await first);
            Assert.Equal(42, await Run());
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task SameId_DifferentScope_IsIndependent_ConflictingPayloadIsRejected()
        {
            using var registry = new ExecutionRegistry<int>();
            Assert.Equal(1, await registry.Run("account-a/session/project", "1", "a", _ => Task.FromResult(1)));
            Assert.Equal(2, await registry.Run("account-b/session/project", "1", "a", _ => Task.FromResult(2)));
            Assert.Equal(3, await registry.Run("account-a/session/project-b", "1", "a", _ => Task.FromResult(3)));
            Assert.Throws<InvalidOperationException>(() => { _ = registry.Run("account-a/session/project", "1", "b", _ => Task.FromResult(4)); });
        }

        [Fact]
        public async Task CancelBeforeDelivery_LeavesTombstone()
        {
            using var registry = new ExecutionRegistry<int>();
            registry.Cancel("scope", "id");
            int count = 0;
            var task = registry.Run("scope", "id", "args", _ => Task.FromResult(++count));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Same(task, registry.Run("scope", "id", "args", _ => Task.FromResult(++count)));
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task LedgerCapacity_RejectsNewIdWithoutEvictingCompletedId()
        {
            using var registry = new ExecutionRegistry<int>(1);
            Assert.Equal(1, await registry.Run("scope", "id", "args", _ => Task.FromResult(1)));
            Assert.Throws<InvalidOperationException>(() => { _ = registry.Run("scope", "new", "args", _ => Task.FromResult(2)); });
            Assert.Equal(1, await registry.Run("scope", "id", "args", _ => Task.FromResult(3)));
        }

        [Fact]
        public async Task Operation_ReturnsHandleWhileWorkBlocked_AndReusesSavedCompletion()
        {
            using var directory = new TestDirectory();
            using var store = new DurableOperationStore(directory.Path);
            var finish = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            int count = 0;
            Task<string?> Work(CancellationToken token, Action started) { Interlocked.Increment(ref count); started(); return finish.Task; }
            var handle = store.Start("identity", "stable", "args", Work);
            Assert.Equal("queued", handle.State);
            var calls = Enumerable.Range(0, 32).Select(_ => Task.Run(() => store.Start("identity", "stable", "args", Work))).ToArray();
            await Task.WhenAll(calls);
            await WaitFor(store, "identity", "stable", s => s.ExecutionStarted);
            Assert.Equal(1, count);
            finish.SetResult("{\"value\":42}");
            var completed = await WaitFor(store, "identity", "stable", s => s.State == "succeeded");
            Assert.Equal("{\"value\":42}", completed.ResultJson);
            Assert.Equal("succeeded", store.Start("identity", "stable", "args", Work).State);
            Assert.Equal(1, count);
            Assert.Throws<InvalidOperationException>(() => store.Start("identity", "stable", "different", Work));
        }

        [Fact]
        public async Task DomainReplacement_RunningOutcomeUnknown_NeverReexecutesOrAcceptsOldCompletion()
        {
            using var directory = new TestDirectory();
            var finish = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var old = new DurableOperationStore(directory.Path);
            old.Start("identity", "stable", "args", (_, started) => { started(); return finish.Task; });
            await WaitFor(old, "identity", "stable", s => s.ExecutionStarted);
            old.Dispose();
            using var next = new DurableOperationStore(directory.Path);
            int count = 0;
            Assert.Equal("unknown", next.Get("identity", "stable").State);
            Assert.Equal("unknown", next.Start("identity", "stable", "args", (_, _) => Task.FromResult<string?>((++count).ToString())).State);
            finish.SetResult("late old domain completion");
            Assert.Equal("unknown", next.Get("identity", "stable").State);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task OperationCancel_WhileWaitingForMainThread_ZeroBodyExecutions()
        {
            using var directory = new TestDirectory();
            using var store = new DurableOperationStore(directory.Path);
            var posted = new TaskCompletionSource<QueuedExecution<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            int count = 0;
            store.Start("identity", "stable", "args", async (token, started) =>
            {
                started();
                var queued = new QueuedExecution<int>(() => ++count, new ToolExecutionContext("identity", token, null));
                posted.SetResult(queued);
                return (await queued.Task).ToString();
            });
            var work = await posted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(store.Cancel("identity", "stable").CancellationRequested);
            var result = await WaitFor(store, "identity", "stable", s => s.State == "canceled");
            work.Execute();
            Assert.Equal(0, count);
            Assert.False(result.MainThreadStarted);
        }

        [Fact]
        public async Task OperationCancel_StartedNonInterruptibleBody_CanStillSucceed()
        {
            using var directory = new TestDirectory();
            using var store = new DurableOperationStore(directory.Path);
            var finish = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            store.Start("identity", "stable", "args", (_, started) => { started(); store.MarkMainThreadStarted("identity", "stable"); return finish.Task; });
            await WaitFor(store, "identity", "stable", s => s.MainThreadStarted);
            Assert.Equal("running", store.Cancel("identity", "stable").State);
            finish.SetResult("build finished");
            var result = await WaitFor(store, "identity", "stable", s => s.State == "succeeded");
            Assert.True(result.CancellationRequested);
            Assert.Equal("build finished", result.ResultJson);
        }

        [Fact]
        public async Task DurableId_ScopedByIdentityAndProject_AndCompletedStateSurvivesReload()
        {
            using var directory = new TestDirectory();
            using var otherDirectory = new TestDirectory();
            using (var store = new DurableOperationStore(directory.Path))
            {
                store.Start("a", "stable", "args", (_, start) => { start(); return Task.FromResult<string?>("a"); });
                store.Start("b", "stable", "args", (_, start) => { start(); return Task.FromResult<string?>("b"); });
                Assert.Equal("a", (await WaitFor(store, "a", "stable", s => s.State == "succeeded")).ResultJson);
                Assert.Equal("b", (await WaitFor(store, "b", "stable", s => s.State == "succeeded")).ResultJson);
            }
            using var next = new DurableOperationStore(directory.Path);
            Assert.Equal("a", next.Get("a", "stable").ResultJson);
            using var other = new DurableOperationStore(otherDirectory.Path);
            other.Start("a", "stable", "args", (_, start) => { start(); return Task.FromResult<string?>("other project"); });
            Assert.Equal("other project", (await WaitFor(other, "a", "stable", s => s.State == "succeeded")).ResultJson);
        }

        [Fact]
        public void SecondEditorOwner_AndCorruptLedger_FailClosed()
        {
            using var directory = new TestDirectory();
            using (var store = new DurableOperationStore(directory.Path))
                Assert.Throws<IOException>(() => new DurableOperationStore(directory.Path));
            File.WriteAllText(System.IO.Path.Combine(directory.Path, "broken.json"), "broken");
            Assert.ThrowsAny<Exception>(() => new DurableOperationStore(directory.Path));
        }

        [Fact]
        public async Task DurableRevisions_PreserveAdmission_AndIgnoreUncommittedTemporaryWrite()
        {
            using var directory = new TestDirectory();
            string admission;
            using (var store = new DurableOperationStore(directory.Path))
            {
                store.Start("identity", "stable", "args", (_, started) => { started(); return Task.FromResult<string?>("done"); });
                await WaitFor(store, "identity", "stable", s => s.State == "succeeded");
                admission = Directory.GetFiles(directory.Path, "*.json").Single(p => Path.GetFileNameWithoutExtension(p).Length == 64);
                Assert.Equal("queued", System.Text.Json.JsonSerializer.Deserialize<DurableOperationStore.Record>(File.ReadAllText(admission))!.Snapshot.State);
                Assert.True(Directory.GetFiles(directory.Path, "*.json").Length >= 4);
                File.WriteAllText(admission + ".interrupted.tmp", "incomplete write");
            }
            using var recovered = new DurableOperationStore(directory.Path);
            Assert.Equal("done", recovered.Get("identity", "stable").ResultJson);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DurableRevisions_MissingAdmissionOrCorruptLatest_FailClosed(bool removeAdmission)
        {
            using var directory = new TestDirectory();
            using (var store = new DurableOperationStore(directory.Path))
            {
                store.Start("identity", "stable", "args", (_, started) => { started(); return Task.FromResult<string?>("done"); });
                await WaitFor(store, "identity", "stable", s => s.State == "succeeded");
            }
            var files = Directory.GetFiles(directory.Path, "*.json");
            if (removeAdmission)
                File.Delete(files.Single(p => Path.GetFileNameWithoutExtension(p).Length == 64));
            else
                File.WriteAllText(files.Where(p => Path.GetFileNameWithoutExtension(p).Length > 64).OrderBy(p => p, StringComparer.Ordinal).Last(), "{}");
            Assert.ThrowsAny<Exception>(() => new DurableOperationStore(directory.Path));
        }

        private static async Task<OperationSnapshot> WaitFor(DurableOperationStore store, string identity, string id, Func<OperationSnapshot, bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (true)
            {
                var snapshot = store.Get(identity, id);
                if (predicate(snapshot)) return snapshot;
                await Task.Delay(10, timeout.Token);
            }
        }
        private sealed class TestDirectory : IDisposable
        {
            public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unity-mcp-operation-test-" + Guid.NewGuid().ToString("N"));
            public void Dispose() { if (Directory.Exists(Path)) Directory.Delete(Path, true); }
        }
    }
}
