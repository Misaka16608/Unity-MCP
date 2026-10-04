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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common.Execution;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    internal static class PairedServerDelivery
    {
        internal static PairedReleaseManifest Release
        {
            get
            {
                var release = PackageDelivery.Manifest;
                if (release.apiVersion != McpPlugin.Common.Consts.ApiVersion || release.pluginVersion != UnityMcpPlugin.Version)
                    throw new InvalidDataException("Installed plugin DLLs do not match the paired release API. Restore plugin dependencies.");
                return release;
            }
        }
        internal static bool CachedServerMatches(string directory, string executable)
        {
            var release = Release;
            var marker = Path.Combine(directory, "version");
            return File.Exists(marker) && File.ReadAllText(marker).Trim() == release.serverVersion
                && PairedReleaseManifest.VerifyFile(Path.Combine(directory, executable),
                    release.Server(McpServerManager.PlatformName).executableSha256);
        }
        internal static async Task<IDisposable> DrainRequests()
        {
            var plugin = UnityMcpPluginEditor.Instance.McpPluginInstance;
            // A cold install can stage the bundled server before Startup has built the
            // plugin. No request manager exists yet, so no execution can have been admitted.
            // This path contains no await: publish completes before editor updates resume.
            if (plugin == null) return new DrainLease(null, null);
            var managers = plugin.McpManager;
            var tools = managers?.ToolManager as IExecutionActivity;
            var system = managers?.SystemToolManager as IExecutionActivity;
            if (tools == null || system == null)
                throw new InvalidOperationException("Plugin execution managers are not ready for a safe server upgrade.");
            var toolLease = tools.BeginMaintenance();
            DrainLease lease;
            try { lease = new DrainLease(toolLease, system.BeginMaintenance()); }
            catch { toolLease.Dispose(); throw; }
            try
            {
                var deadline = DateTime.UtcNow.AddMinutes(5);
                while (tools.ActiveExecutionCount + system.ActiveExecutionCount + API.Tool_Operation.ActiveExecutionCount != 0)
                {
                    PackageDeliveryStatus.Set("Waiting for accepted tool operations to finish before upgrading server");
                    if (DateTime.UtcNow >= deadline)
                        throw new TimeoutException("Tool operations are still running. Server upgrade was postponed; their work was not canceled or repeated.");
                    await Task.Delay(100);
                }
                PackageDeliveryStatus.Set("Installing verified server");
                return lease;
            }
            catch { lease.Dispose(); throw; }
        }
        private sealed class DrainLease : IDisposable
        {
            private IDisposable? _tools, _system;
            public DrainLease(IDisposable? tools, IDisposable? system) { _tools = tools; _system = system; }
            public void Dispose()
            {
                Interlocked.Exchange(ref _system, null)?.Dispose();
                Interlocked.Exchange(ref _tools, null)?.Dispose();
            }
        }
    }
}
