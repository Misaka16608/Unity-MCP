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
using System.IO;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public sealed class OperationSnapshot
    {
        public string OperationId { get; set; } = "";
        public string State { get; set; } = "queued";
        public string? CompletionRequestId { get; set; }
        public bool ExecutionStarted { get; set; }
        public bool MainThreadStarted { get; set; }
        public bool CancellationRequested { get; set; }
        public string? ResultJson { get; set; }
        public string? Error { get; set; }
        public string QueryTool { get; set; } = "tool-operation-status";
    }
    /// <summary>
    /// A project-local durable admission ledger. It never resumes side effects after a reload.
    /// Library/mcp-operations must be retained while an operation's outcome is unresolved.
    /// Processing operations may remain awaiting-completion across a reload and are completed
    /// by a later notification carrying their persisted completion request ID.
    /// </summary>
    public sealed class DurableOperationStore : IDisposable
    {
        public sealed class Record
        {
            public string Fingerprint { get; set; } = "";
            public OperationSnapshot Snapshot { get; set; } = new();
        }
        private sealed class Active
        {
            public readonly CancellationTokenSource Cancellation = new();
            private bool _disposed;
            public void Cancel()
            {
                lock (this) { if (!_disposed) Cancellation.Cancel(); }
            }
            public void Dispose()
            {
                lock (this) { _disposed = true; Cancellation.Dispose(); }
            }
        }
        private readonly Dictionary<string, TaskCompletionSource<string?>> _externalCompletions = new(StringComparer.Ordinal);
        private readonly string _directory;
        private readonly object _gate = new();
        private readonly Dictionary<string, Active> _active = new(StringComparer.Ordinal);
        private readonly FileStream _lease;
        private bool _disposed;
        private Exception? _persistenceFailure;

        public DurableOperationStore(string directory)
        {
            _directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(_directory);
            // Prevent two editors/processes from admitting operations into this project ledger.
            _lease = new FileStream(Path.Combine(_directory, "owner.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            try
            {
                var admissions = new HashSet<string>(StringComparer.Ordinal);
                foreach (var file in Directory.GetFiles(_directory, "*.json"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var hash = name.Split('.')[0];
                    if (hash.Length != 64 || hash.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
                        throw new InvalidDataException("Invalid operation ledger filename.");
                    admissions.Add(Path.Combine(_directory, hash + ".json"));
                }
                foreach (var path in admissions)
                {
                    var record = Read(path); // corrupt ledger fails closed
                    if (record.Snapshot.State == "queued" || record.Snapshot.State == "running")
                    {
                        record.Snapshot.State = "unknown";
                        record.Snapshot.Error = "Editor/domain stopped before a durable completion was recorded. Inspect outputs; this operation will not be executed again.";
                        Save(path, record);
                    }
                }
            }
            catch { _lease.Dispose(); throw; }
        }

        public OperationSnapshot Start(string identity, string id, string fingerprint, Func<CancellationToken, Action, Task<string?>> execute)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 200) throw new ArgumentException("Use a stable operation ID of 1–200 characters.");
            var path = PathFor(identity, id);
            lock (_gate)
            {
                ThrowIfDisposed();
                if (File.Exists(path) || Revisions(path).Length != 0)
                {
                    var previous = Read(path);
                    if (previous.Fingerprint != fingerprint) throw new InvalidOperationException("Operation ID already belongs to different tool arguments.");
                    return Copy(previous.Snapshot);
                }
                var record = new Record
                {
                    Fingerprint = fingerprint,
                    Snapshot = new OperationSnapshot
                    {
                        OperationId = id,
                        CompletionRequestId = CompletionRequestId(identity, id)
                    }
                };
                Save(path, record); // commit admission BEFORE scheduling any side effects
                var active = new Active();
                _active.Add(path, active);
                _ = Task.Run(async () =>
                {
                    try { await Execute(path, active, execute).ConfigureAwait(false); }
                    catch (Exception ex) { lock (_gate) { _persistenceFailure = ex; } }
                });
                return Copy(record.Snapshot);
            }
        }

        private async Task Execute(string path, Active active, Func<CancellationToken, Action, Task<string?>> execute)
        {
            try
            {
                lock (_gate)
                {
                    if (_disposed) return;
                    active.Cancellation.Token.ThrowIfCancellationRequested();
                    var record = Read(path);
                    record.Snapshot.State = "running";
                    Save(path, record);
                }
                var result = await execute(active.Cancellation.Token, () => MarkStarted(path, active)).ConfigureAwait(false);
                Finish(path, active, "succeeded", result, null);
            }
            catch (OperationCanceledException)
            {
                Finish(path, active, "canceled", null, "Canceled. Any side effects from already started work must be inspected; cancellation does not roll them back.");
            }
            catch (Exception ex) { Finish(path, active, "failed", null, ex.Message); }
            finally
            {
                lock (_gate) { _active.Remove(path); }
                active.Dispose();
            }
        }
        private void MarkStarted(string path, Active active)
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                active.Cancellation.Token.ThrowIfCancellationRequested();
                var record = Read(path);
                record.Snapshot.ExecutionStarted = true;
                Save(path, record); // write before body; a crash afterwards has unknown outcome
            }
        }
        private void Finish(string path, Active active, string state, string? result, string? error)
        {
            lock (_gate)
            {
                if (_disposed) return; // owner lease released; a new domain owns recovery
                var record = Read(path);
                if (IsTerminal(record.Snapshot.State)) return;
                if (active.Cancellation.IsCancellationRequested && !record.Snapshot.ExecutionStarted)
                    state = "canceled";
                record.Snapshot.State = state;
                record.Snapshot.ResultJson = result;
                record.Snapshot.Error = error;
                Save(path, record);
            }
        }

        /// <summary>Waits for a tool that returned Processing and completes from its later notification.</summary>
        public async Task<string?> WaitForExternalCompletion(string identity, string id, CancellationToken token)
        {
            TaskCompletionSource<string?> completion;
            var path = PathFor(identity, id);
            lock (_gate)
            {
                ThrowIfDisposed();
                var record = Read(path);
                if (IsTerminal(record.Snapshot.State)) return record.Snapshot.ResultJson;
                record.Snapshot.State = "awaiting-completion";
                Save(path, record);
                if (!_externalCompletions.TryGetValue(path, out completion!))
                {
                    completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _externalCompletions.Add(path, completion);
                }
            }

            // Cancellation requests do not prove that detached native work stopped. Only
            // its final notification (or an explicit cooperative acknowledgment) does.
            var result = await completion.Task.ConfigureAwait(false);
            lock (_gate)
            {
                var final = Read(path).Snapshot;
                if (final.State == "failed") throw new InvalidOperationException(final.Error ?? "The target operation failed.");
                if (final.State == "canceled") throw new OperationCanceledException(token);
                return result;
            }
        }

        /// <summary>
        /// Commits a later Processing completion. The request ID is deterministic and persisted in the admission record,
        /// so this also works after a Unity domain reload when no in-memory operation is running.
        /// </summary>
        public bool CompleteExternal(string completionRequestId, string resultJson, bool succeeded, string? error, bool canceled = false)
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                foreach (var path in Directory.GetFiles(_directory, "*.json"))
                {
                    if (Path.GetFileNameWithoutExtension(path).Length != 64) continue;
                    var record = Read(path);
                    if (!string.Equals(record.Snapshot.CompletionRequestId, completionRequestId, StringComparison.Ordinal)) continue;
                    if (IsTerminal(record.Snapshot.State) && record.Snapshot.State != "unknown") return false;
                    record.Snapshot.State = canceled ? "canceled" : succeeded ? "succeeded" : "failed";
                    record.Snapshot.ResultJson = resultJson;
                    record.Snapshot.Error = error;
                    Save(path, record);
                    if (_externalCompletions.Remove(path, out var completion)) completion.TrySetResult(resultJson);
                    return true;
                }
                return false;
            }
        }
        public OperationSnapshot Get(string identity, string id)
        {
            lock (_gate) { ThrowIfDisposed(); return Copy(Read(PathFor(identity, id)).Snapshot); }
        }
        public void MarkMainThreadStarted(string identity, string id)
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                var path = PathFor(identity, id);
                var record = Read(path);
                record.Snapshot.MainThreadStarted = true;
                Save(path, record);
            }
        }
        public OperationSnapshot Cancel(string identity, string id)
        {
            Active? active = null;
            OperationSnapshot snapshot;
            lock (_gate)
            {
                ThrowIfDisposed();
                var path = PathFor(identity, id);
                var record = Read(path);
                if (record.Snapshot.State == "queued" || record.Snapshot.State == "running" || record.Snapshot.State == "awaiting-completion")
                {
                    record.Snapshot.CancellationRequested = true;
                    Save(path, record);
                    _active.TryGetValue(path, out active);
                }
                snapshot = Copy(record.Snapshot);
            }
            // User callbacks must not run under the persistence lock.
            active?.Cancel();
            return snapshot;
        }
        public CancellationToken CompletionCancellationToken(string completionRequestId, CancellationToken fallback)
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                foreach (var pair in _active)
                    if (Read(pair.Key).Snapshot.CompletionRequestId == completionRequestId)
                        return pair.Value.Cancellation.Token;
                return fallback;
            }
        }
        private string PathFor(string identity, string id) => Path.Combine(_directory, Digest(identity.Length + ":" + identity + id) + ".json");
        public static string CompletionRequestId(string identity, string id) => "durable-completion:" + Digest(identity.Length + ":" + identity + id);
        private static bool IsTerminal(string state) => state == "succeeded" || state == "canceled" || state == "failed" || state == "unknown";
        public static string Digest(string value)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        private static string[] Revisions(string path)
        {
            var prefix = Path.GetFileNameWithoutExtension(path) + ".";
            var revisions = Directory.GetFiles(Path.GetDirectoryName(path)!, prefix + "*.json");
            foreach (var revision in revisions)
            {
                var sequence = Path.GetFileNameWithoutExtension(revision).Substring(prefix.Length);
                if (sequence.Length != 20 || !ulong.TryParse(sequence, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number == 0)
                    throw new InvalidDataException("Invalid operation ledger revision.");
            }
            Array.Sort(revisions, StringComparer.Ordinal);
            return revisions;
        }
        private static Record ReadRecord(string path)
        {
            var record = JsonSerializer.Deserialize<Record>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid operation ledger.");
            if (string.IsNullOrEmpty(record.Fingerprint) || record.Snapshot == null || string.IsNullOrWhiteSpace(record.Snapshot.OperationId)
                    || !(record.Snapshot.State == "queued" || record.Snapshot.State == "running" || record.Snapshot.State == "awaiting-completion" || record.Snapshot.State == "unknown"
                    || record.Snapshot.State == "succeeded" || record.Snapshot.State == "canceled" || record.Snapshot.State == "failed"))
                throw new InvalidDataException("Invalid operation ledger record.");
            return record;
        }
        private static Record Read(string path)
        {
            // The immutable admission must exist even when later revisions are present.
            var admission = ReadRecord(path);
            var revisions = Revisions(path);
            if (revisions.Length == 0) return admission;
            var latest = ReadRecord(revisions[revisions.Length - 1]);
            if (latest.Fingerprint != admission.Fingerprint || latest.Snapshot.OperationId != admission.Snapshot.OperationId)
                throw new InvalidDataException("Operation ledger revision does not match its admission.");
            return latest;
        }
        private static OperationSnapshot Copy(OperationSnapshot snapshot) => JsonSerializer.Deserialize<OperationSnapshot>(JsonSerializer.Serialize(snapshot))!;
        private static void Save(string path, Record record)
        {
            var revisions = Revisions(path);
            var destination = path;
            if (File.Exists(path))
            {
                ulong sequence = 1;
                if (revisions.Length != 0)
                {
                    var name = Path.GetFileNameWithoutExtension(revisions[revisions.Length - 1]);
                    sequence = checked(ulong.Parse(name.Substring(name.Length - 20), CultureInfo.InvariantCulture) + 1);
                }
                destination = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path)
                    + "." + sequence.ToString("D20", CultureInfo.InvariantCulture) + ".json");
            }
            else if (revisions.Length != 0) throw new InvalidDataException("Operation ledger admission is missing.");
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(record));
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                // Append an immutable revision using the two-argument rename supported by
                // Unity's BCL. Never delete/replace admission: a crash cannot readmit work.
                File.Move(temporary, destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DurableOperationStore));
            if (_persistenceFailure != null) throw new IOException("Operation ledger update failed; refusing further execution.", _persistenceFailure);
        }
        public void Dispose()
        {
            Active[] active;
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                active = _active.Values.ToArray();
                foreach (var completion in _externalCompletions.Values) completion.TrySetCanceled();
                _externalCompletions.Clear();
                _lease.Dispose();
            }
            foreach (var execution in active) execution.Cancel();
        }
    }
}
