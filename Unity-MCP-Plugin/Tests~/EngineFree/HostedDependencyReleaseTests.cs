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
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests;

public sealed class HostedDependencyReleaseTests
{
    [Theory]
    [InlineData("com.IvanMurzak.McpPlugin")]
    [InlineData("com.IvanMurzak.McpPlugin.Common")]
    public void PairedPackagesUseSameReleaseAsServer(string id)
    {
        var package = new NuGetPackage(id, HostedDependencyRelease.PackageVersion);
        var packageDirectory = package.DownloadUrl[..package.DownloadUrl.LastIndexOf('/')];
        var serverUrl = HostedDependencyRelease.AssetUrl("gamedev-mcp-server-win-x64.zip");
        Assert.StartsWith(packageDirectory + "/", serverUrl);
        Assert.EndsWith("/" + package.CacheFileName, package.DownloadUrl);
        Assert.Contains("/Misaka16608/Unity-MCP/releases/download/v", package.DownloadUrl);
    }

    [Fact]
    public void PublicDependenciesStillUseNuGet()
    {
        Assert.StartsWith(NuGetConfig.NuGetBaseUrl + "/system.text.json/", new NuGetPackage("System.Text.Json", "10.0.3").DownloadUrl);
        Assert.StartsWith(NuGetConfig.NuGetBaseUrl + "/", new NuGetPackage("com.IvanMurzak.McpPlugin", "8.9.0").DownloadUrl);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CachedAndFreshFilesMustMatchPublishedDigest(bool corrupt)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "original package");
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            var sums = hash + "  package.nupkg\r\n";
            if (corrupt)
                File.AppendAllText(path, "tampered");
            if (corrupt)
                Assert.Throws<InvalidDataException>(() => HostedDependencyRelease.VerifyFile(path, "package.nupkg", sums));
            else
                HostedDependencyRelease.VerifyFile(path, "package.nupkg", sums);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MissingAndDuplicateManifestEntriesFailClosed()
    {
        var path = Path.GetTempFileName();
        try
        {
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            Assert.Throws<InvalidDataException>(() => HostedDependencyRelease.VerifyFile(path, "a.nupkg", hash + "  a.nupkg.extra\n"));
            var entry = hash + "  a.nupkg\n";
            Assert.Throws<InvalidDataException>(() => HostedDependencyRelease.VerifyFile(path, "a.nupkg", entry + entry));
        }
        finally { File.Delete(path); }
    }
}
