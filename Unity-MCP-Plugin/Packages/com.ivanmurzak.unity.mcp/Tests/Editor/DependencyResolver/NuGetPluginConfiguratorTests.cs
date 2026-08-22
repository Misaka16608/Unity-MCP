/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.  |
+------------------------------------------------------------------+
*/

#nullable enable
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    [TestFixture]
    public class NuGetPluginConfiguratorTests
    {
        [TestCase("com.IvanMurzak.McpPlugin")]
        [TestCase("System.Text.Json")]
        [TestCase("Some.Transitive.Package")]
        public void ShouldIncludeInBuild_RuntimeDisabled_ExcludesEveryPackage(string packageId)
        {
            Assert.IsFalse(NuGetPluginConfigurator.ShouldIncludeInBuild(
                packageId,
                runtimeMcpEnabledForPlayer: false));
        }

        [Test]
        public void ShouldIncludeInBuild_RuntimeEnabled_IncludesConfiguredRuntimePackage()
        {
            Assert.IsTrue(NuGetPluginConfigurator.ShouldIncludeInBuild(
                "com.IvanMurzak.McpPlugin",
                runtimeMcpEnabledForPlayer: true));
        }

        [Test]
        public void ShouldIncludeInBuild_RuntimeEnabled_ExcludesConfiguredEditorOnlyPackage()
        {
            Assert.IsFalse(NuGetPluginConfigurator.ShouldIncludeInBuild(
                "Microsoft.CodeAnalysis.CSharp",
                runtimeMcpEnabledForPlayer: true));
        }

        [Test]
        public void ShouldIncludeInBuild_RuntimeEnabled_IncludesTransitiveDependency()
        {
            Assert.IsTrue(NuGetPluginConfigurator.ShouldIncludeInBuild(
                "Some.Transitive.Package",
                runtimeMcpEnabledForPlayer: true));
        }

        [Test]
        public void ApplyCompatibilityState_DisablingAnyPlatform_ClearsStaleExcludeEditor()
        {
            var anyPlatform = true;
            var excludeEditor = true;
            var editor = false;

            NuGetPluginConfigurator.ApplyCompatibilityState(
                value => anyPlatform = value,
                value => excludeEditor = value,
                value => editor = value,
                anyPlatform: false,
                excludeEditor: false,
                editor: false);

            Assert.IsFalse(anyPlatform);
            Assert.IsFalse(excludeEditor,
                "A stale Exclude Editor flag would cause ConfigureDll to reimport on every domain reload.");
            Assert.IsFalse(editor);
        }
    }
}
