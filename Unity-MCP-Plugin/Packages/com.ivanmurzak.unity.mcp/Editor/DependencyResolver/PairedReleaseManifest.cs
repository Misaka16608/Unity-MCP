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
using System.Security.Cryptography;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    [Serializable]
    public sealed class DeliveryFile
    {
        public string id = "";
        public string version = "";
        public string fileName = "";
        public string sha256 = "";
        public string executableSha256 = "";
    }
    [Serializable]
    public sealed class PairedReleaseManifest
    {
        public int schema;
        public string apiVersion = "";
        public string pluginVersion = "";
        public string serverVersion = "";
        public string releaseBaseUrl = "";
        public DeliveryFile[] packages = Array.Empty<DeliveryFile>();
        public DeliveryFile[] servers = Array.Empty<DeliveryFile>();

        public void Validate()
        {
            if (schema != 1 || string.IsNullOrWhiteSpace(apiVersion) || string.IsNullOrWhiteSpace(serverVersion)
                || string.IsNullOrWhiteSpace(pluginVersion) || packages == null || packages.Length == 0 || servers == null || servers.Length == 0)
                throw new InvalidDataException("Incomplete paired plugin release manifest.");
            if (!Uri.TryCreate(releaseBaseUrl, UriKind.Absolute, out var url) || url.Scheme != "https"
                || url.Host != "github.com" || !url.AbsolutePath.Contains("/releases/download/") || url.Query != "" || url.Fragment != "" || url.UserInfo != "")
                throw new InvalidDataException("Paired release must use a pinned GitHub HTTPS asset URL.");
            ValidateFiles(packages, false);
            ValidateFiles(servers, true);
            if (servers.Any(s => s.version != serverVersion)) throw new InvalidDataException("Server version differs from the paired manifest.");
        }
        private static void ValidateFiles(DeliveryFile[] files, bool server)
        {
            if (files.Any(f => f == null)) throw new InvalidDataException("Missing paired artifact metadata.");
            if (files.Select(f => f.id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Length)
                throw new InvalidDataException("Duplicate paired artifact identity.");
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.id) || string.IsNullOrWhiteSpace(file.version) || !SafeName(file.fileName)
                    || !ValidDigest(file.sha256) || server && !ValidDigest(file.executableSha256))
                    throw new InvalidDataException("Invalid paired artifact metadata.");
            }
        }
        private static bool SafeName(string name) => !string.IsNullOrEmpty(name) && name != "." && name != ".."
            && name.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_');
        private static bool ValidDigest(string digest) => digest != null && digest.Length == 64
            && digest.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F');
        public DeliveryFile? Package(string id, string version) => packages.FirstOrDefault(p =>
            string.Equals(p.id, id, StringComparison.OrdinalIgnoreCase) && string.Equals(p.version, version, StringComparison.OrdinalIgnoreCase));
        public DeliveryFile Server(string rid) => servers.FirstOrDefault(p => p.id == rid)
            ?? throw new NotSupportedException("This paired release does not contain a server for " + rid + ".");
        public string ServerUrl(string rid) => releaseBaseUrl.TrimEnd('/') + "/" + Server(rid).fileName;
        public static bool VerifyFile(string path, string expected)
        {
            if (!File.Exists(path) || !ValidDigest(expected)) return false;
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return string.Equals(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""), expected, StringComparison.OrdinalIgnoreCase);
        }
        public static string InstallBundledPackage(string bundleDirectory, string cacheDirectory, DeliveryFile package)
        {
            if (!SafeName(package.fileName)) throw new InvalidDataException("Invalid bundled package path.");
            var source = Path.Combine(bundleDirectory, package.fileName);
            if (!VerifyFile(source, package.sha256)) throw new InvalidDataException("Bundled package missing or checksum mismatch: " + package.id);
            Directory.CreateDirectory(cacheDirectory);
            var destination = Path.Combine(cacheDirectory, package.fileName);
            if (VerifyFile(destination, package.sha256)) return destination;
            var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(source, temporary);
                if (!VerifyFile(temporary, package.sha256)) throw new InvalidDataException("Package changed while copying.");
                // Wrong cached bytes are never trusted just because the version matches.
                // A crash in this cache-only replacement is recoverable from the immutable bundle.
                if (File.Exists(destination)) File.Delete(destination);
                File.Move(temporary, destination);
                return destination;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
    public static class PackageDeliveryStatus
    {
        private static string _text = "Preparing plugin dependencies";
        public static string Text => System.Threading.Volatile.Read(ref _text);
        public static void Set(string text) => System.Threading.Volatile.Write(ref _text, text);
    }
}
