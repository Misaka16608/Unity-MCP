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
using UnityEditor.PackageManager;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    public static class PackageDelivery
    {
        private static string? _directory;
        private static PairedReleaseManifest? _manifest;
        public static string DirectoryPath => _directory ??= Path.Combine(
            PackageInfo.FindForAssembly(typeof(PackageDelivery).Assembly)?.resolvedPath
                ?? throw new InvalidOperationException("Cannot locate the installed Unity MCP package."),
            "Editor", "Delivery~");
        public static PairedReleaseManifest Manifest
        {
            get
            {
                if (_manifest != null) return _manifest;
                var path = Path.Combine(DirectoryPath, "manifest.json");
                var manifest = JsonUtility.FromJson<PairedReleaseManifest>(File.ReadAllText(path))
                    ?? throw new InvalidDataException("Paired plugin release manifest missing.");
                manifest.Validate();
                foreach (var package in manifest.packages)
                {
                    if (!Array.Exists(NuGetConfig.Packages, p => p.Id == package.id && p.Version == package.version)
                        && package.id != "com.IvanMurzak.McpPlugin.Common")
                        throw new InvalidDataException("Bundled package does not match the plugin's dependency pins: " + package.id);
                }
                var plugin = Array.Find(NuGetConfig.Packages, p => p.Id == "com.IvanMurzak.McpPlugin");
                if (manifest.Package(plugin.Id, plugin.Version) == null
                    || manifest.Package("com.IvanMurzak.McpPlugin.Common", plugin.Version) == null)
                    throw new InvalidDataException("The bundled framework pair is incomplete.");
                _manifest = manifest;
                return manifest;
            }
        }
        internal static void Prepare()
        {
            foreach (var file in Manifest.packages)
                PairedReleaseManifest.InstallBundledPackage(DirectoryPath, NuGetConfig.CachePath, file);
        }
        internal static string? TryInstall(NuGetPackage package)
        {
            var bundled = Manifest.Package(package.Id, package.Version);
            return bundled == null ? null : PairedReleaseManifest.InstallBundledPackage(DirectoryPath, NuGetConfig.CachePath, bundled);
        }
    }
}
