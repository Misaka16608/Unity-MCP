/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.   |
+------------------------------------------------------------------+
*/

#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Represents a NuGet package dependency with its ID and version.
    /// </summary>
    readonly struct NuGetPackage
    {
        /// <summary>NuGet package ID (e.g., "System.Text.Json").</summary>
        public readonly string Id;

        /// <summary>Package version (e.g., "10.0.3").</summary>
        public readonly string Version;

        /// <summary>
        /// If true, this DLL is included in game builds (all platforms).
        /// If false, the DLL is editor-only (excluded from builds via PluginImporter).
        /// </summary>
        public readonly bool IncludeInBuild;

        public NuGetPackage(string id, string version, bool includeInBuild = false)
        {
            Id = id;
            Version = version;
            IncludeInBuild = includeInBuild;
        }

        /// <summary>
        /// The .nupkg download URL from NuGet v3 flat container API.
        /// </summary>
        public string DownloadUrl
        {
            get
            {
                if (HostedDependencyRelease.IsHostedPackage(Id, Version))
                    return HostedDependencyRelease.AssetUrl(CacheFileName);
                var lowerId = Id.ToLowerInvariant();
                var lowerVersion = Version.ToLowerInvariant();
                return $"{NuGetConfig.NuGetBaseUrl}/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg";
            }
        }

        /// <summary>
        /// Cached .nupkg filename.
        /// </summary>
        public string CacheFileName => $"{Id}.{Version}.nupkg";

        public override string ToString() => $"{Id} {Version}";
    }

    /// <summary>The paired fork dependencies are immutable assets of one pinned public release.</summary>
    public static class HostedDependencyRelease
    {
        public const string Repository = "Misaka16608/Unity-MCP";
        public const string PackageVersion = "8.9.0-request-safety.4";
        public const string ServerVersion = "9.2.12-request-safety.4";

        public static bool IsHostedPackage(string id, string version)
            => version == PackageVersion &&
               (string.Equals(id, "com.IvanMurzak.McpPlugin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "com.IvanMurzak.McpPlugin.Common", StringComparison.OrdinalIgnoreCase));

        public static string AssetUrl(string name, string? serverVersion = null)
        {
            var version = (serverVersion ?? ServerVersion).Trim();
            var tag = version.StartsWith("v", StringComparison.Ordinal) ? version : "v" + version;
            return $"https://github.com/{Repository}/releases/download/{tag}/{name}";
        }

        /// <summary>Reject missing, ambiguous or mismatched digests before installing cached/downloaded packages.</summary>
        public static void VerifyFile(string path, string assetName, string manifest)
        {
            string? expected = null;
            foreach (var raw in manifest.Split('\n'))
            {
                var match = Regex.Match(raw.Trim(), @"^([0-9a-fA-F]{64})\s+\*?(.+)$");
                if (!match.Success || match.Groups[2].Value != assetName)
                    continue;
                if (expected != null)
                    throw new InvalidDataException($"Duplicate checksum for {assetName}");
                expected = match.Groups[1].Value;
            }
            if (expected == null)
                throw new InvalidDataException($"Missing checksum for {assetName}");
            using var input = File.OpenRead(path);
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SHA256 mismatch for {assetName}");
        }
    }
}
