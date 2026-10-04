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
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.McpPlugin.Common.Execution;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    public class PairedDeliveryTests
    {
        [Fact]
        public async Task RealToolManager_RejectsFreshWrite_ButFinishesAcceptedDurableChild()
        {
            var builder = new McpPluginBuilder(new com.IvanMurzak.McpPlugin.Common.Version());
            builder.WithTools(typeof(MaintenanceTool));
            using var plugin = builder.Build(new Reflector());
            var manager = plugin.McpManager.ToolManager;
            var activity = Assert.IsAssignableFrom<IExecutionActivity>(manager);
            using var lease = activity.BeginMaintenance();
            var blocked = await manager.RunCallTool(Request("blocked"));
            Assert.Equal(ResponseErrorKind.Unavailable, blocked.ErrorKind);
            Assert.Contains("not admitted", blocked.Message);
            Assert.Equal(0, activity.ActiveExecutionCount);
            var previous = ToolExecutionContext.Current;
            try
            {
                // This flag lives in local async execution context, never in RPC input.
                ToolExecutionContext.Current = new ToolExecutionContext("test", default, null, allowDuringMaintenance: true);
                var child = await manager.RunCallTool(Request("accepted-child"));
                Assert.Equal(ResponseStatus.Success, child.Status);
            }
            finally { ToolExecutionContext.Current = previous; }
            var another = await manager.RunCallTool(Request("another-blocked"));
            Assert.Equal(ResponseErrorKind.Unavailable, another.ErrorKind);
        }

        [Fact]
        public void ShippedBundle_MatchesDependencyPinsAndCompiledProtocol()
        {
            var path = RepoFile.Require("Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Delivery~/manifest.json");
            var manifest = JsonSerializer.Deserialize<PairedReleaseManifest>(File.ReadAllText(path), new JsonSerializerOptions { IncludeFields = true })!;
            manifest.Validate();
            Assert.Equal(com.IvanMurzak.McpPlugin.Common.Consts.ApiVersion, manifest.apiVersion);
            foreach (var package in manifest.packages)
                Assert.True(PairedReleaseManifest.VerifyFile(Path.Combine(Path.GetDirectoryName(path)!, package.fileName), package.sha256));
            var pin = Array.Find(NuGetConfig.Packages, p => p.Id == "com.IvanMurzak.McpPlugin")!;
            Assert.NotNull(manifest.Package(pin.Id, pin.Version));
            Assert.NotNull(manifest.Package("com.IvanMurzak.McpPlugin.Common", pin.Version));
            Assert.Equal(7, manifest.servers.Length);
        }

        [Fact]
        public async Task Maintenance_DrainsAcceptedWork_RejectsFreshWork_ReusesSameRequest()
        {
            using var registry = new ExecutionRegistry<int>();
            var finish = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = registry.Run("session", "id", "args", _ => finish.Task);
            using var maintenance = registry.BeginMaintenance();
            Assert.Equal(1, registry.ActiveExecutionCount);
            Assert.Same(request, registry.Run("session", "id", "args", _ => throw new Exception("must not repeat")));
            await Assert.ThrowsAsync<ExecutionMaintenanceException>(() => registry.Run("session", "new", "args", _ => Task.FromResult(2)));
            Assert.Equal(7, await registry.Run("session", "status", "args", _ => Task.FromResult(7), allowDuringMaintenance: true));
            finish.SetResult(1);
            Assert.Equal(1, await request);
            Assert.Equal(0, registry.ActiveExecutionCount);
            Assert.Same(request, registry.Run("session", "id", "args", _ => throw new Exception("must not repeat")));
            maintenance.Dispose();
            maintenance.Dispose();
            Assert.Equal(2, await registry.Run("session", "new", "args", _ => Task.FromResult(2)));
        }

        [Fact]
        public async Task NestedMaintenance_StaysClosedUntilBothLeasesAreReleased()
        {
            using var registry = new ExecutionRegistry<int>();
            var one = registry.BeginMaintenance();
            var two = registry.BeginMaintenance();
            one.Dispose();
            await Assert.ThrowsAsync<ExecutionMaintenanceException>(() => registry.Run("s", "id", "f", _ => Task.FromResult(1)));
            two.Dispose();
            Assert.Equal(1, await registry.Run("s", "id", "f", _ => Task.FromResult(1)));
        }

        [Fact]
        public async Task DurableAdmission_IsCountedBeforeDetachedBodyStarts()
        {
            var directory = Temp();
            try
            {
                using var store = new DurableOperationStore(directory);
                var finish = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                store.Start("user", "id", "args", (_, __) => finish.Task);
                Assert.Equal(1, store.ActiveExecutionCount);
                finish.SetResult("done");
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (store.ActiveExecutionCount != 0 && DateTime.UtcNow < deadline) await Task.Delay(5);
                Assert.Equal(0, store.ActiveExecutionCount);
                Assert.Equal("succeeded", store.Get("user", "id").State);
            }
            finally { Directory.Delete(directory, true); }
        }

        [Fact]
        public void Bundle_RepairsCorruptCache_ThenRejectsCorruptImmutableSource()
        {
            var directory = Temp();
            try
            {
                var cache = Path.Combine(directory, "cache");
                Directory.CreateDirectory(cache);
                var package = FileEntry();
                File.WriteAllText(Path.Combine(directory, package.fileName), "package");
                File.WriteAllText(Path.Combine(cache, package.fileName), "wrong bytes with same version");
                var installed = PairedReleaseManifest.InstallBundledPackage(directory, cache, package);
                Assert.Equal("package", File.ReadAllText(installed));
                File.WriteAllText(Path.Combine(directory, package.fileName), "damaged bundle");
                Assert.Throws<InvalidDataException>(() => PairedReleaseManifest.InstallBundledPackage(directory, cache, package));
                Assert.Equal("package", File.ReadAllText(installed));
                Assert.Empty(Directory.GetFiles(cache, "*.tmp"));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Theory]
        [InlineData("../outside.nupkg")]
        [InlineData("..\\outside.nupkg")]
        [InlineData("C:\\outside.nupkg")]
        [InlineData("/outside.nupkg")]
        public void Manifest_RejectsEscapingArtifactPaths(string path)
        {
            var manifest = Manifest();
            manifest.packages[0].fileName = path;
            Assert.Throws<InvalidDataException>(() => manifest.Validate());
        }

        [Fact]
        public void Manifest_RejectsDuplicateIdentity_AndMismatchedServerVersion()
        {
            var manifest = Manifest();
            manifest.packages = new[] { FileEntry(), FileEntry() };
            Assert.Throws<InvalidDataException>(() => manifest.Validate());
            manifest = Manifest();
            manifest.servers[0].version = "other";
            Assert.Throws<InvalidDataException>(() => manifest.Validate());
        }

        [Theory]
        [InlineData("http://github.com/org/repo/releases/download/pair")]
        [InlineData("https://example.com/org/repo/releases/download/pair")]
        [InlineData("https://github.com/org/repo/releases/latest")]
        [InlineData("https://github.com/org/repo/releases/download/pair?token=secret")]
        public void Manifest_RejectsUnpinnedOrCredentialBearingUrls(string url)
        {
            var manifest = Manifest();
            manifest.releaseBaseUrl = url;
            Assert.Throws<InvalidDataException>(() => manifest.Validate());
        }

        private static string Temp()
        {
            var path = Path.Combine(Path.GetTempPath(), "unity-mcp-delivery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
        private static DeliveryFile FileEntry() => new()
        {
            id = "package", version = "1", fileName = "package.1.nupkg",
            sha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("package")))
        };
        private static RequestCallTool Request(string id) => new(id, "delivery-test-write", new Dictionary<string, JsonElement>())
            { IdentityScope = "test", ExecutionScope = "session" };
        [AiToolType]
        public sealed class MaintenanceTool
        {
            [AiTool("delivery-test-write")]
            public int Write() => 1;
        }
        private static PairedReleaseManifest Manifest() => new()
        {
            schema = 1, apiVersion = "2", pluginVersion = "0.1", serverVersion = "9",
            releaseBaseUrl = "https://github.com/org/repo/releases/download/pair",
            packages = new[] { FileEntry() },
            servers = new[] { new DeliveryFile { id = "win-x64", version = "9", fileName = "server.zip",
                sha256 = new string('a', 64), executableSha256 = new string('b', 64) } }
        };
    }
}
