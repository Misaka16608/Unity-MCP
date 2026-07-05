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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Prefab
    {
        public const string AssetsPrefabGetDataToolId = "assets-prefab-get-data";
        [AiTool
        (
            AssetsPrefabGetDataToolId,
            Title = "Assets / Prefab / Get Data",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [AiSkillDescription("Retrieve the full GameObject hierarchy of a prefab asset in a single call — " +
            "the prefab equivalent of '" + Tool_Scene.SceneGetDataToolId + "'. " +
            "Loads the prefab directly, no need to open/close the prefab stage. " +
            "Use '" + Tool_Assets.AssetsFindToolId + "' to locate the prefab first.")]
        [AiSkillBody("Retrieve the full GameObject hierarchy of a prefab asset. " +
            "Use '" + Tool_Assets.AssetsFindToolId + "' to find prefabs.\n\n" +
            "## Toggles (all default `false` to keep responses small)\n\n" +
            "- `includeChildrenDepth` (default 3) — depth of the hierarchy to include.\n" +
            "- `includeBounds` — include 3D bounds for GameObjects.\n" +
            "- `includeData` — include serialized component data for GameObjects.\n\n" +
            "## Path-scoped reads (token-saving)\n\n" +
            "Supply `paths` to read only the listed fields from the prefab root GameObject's data via " +
            "`Reflector.TryReadAt`, or `viewQuery` to navigate/filter via `Reflector.View`. " +
            "The result populates `Data` on the returned `PrefabData`. These two parameters are mutually exclusive.\n\n" +
            "## Path syntax\n\n" +
            "`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`. Leading `#/` is stripped. " +
            "Example: `paths=['m_Name']` reads the root GameObject's name.")]
        [Description("Retrieve the full GameObject hierarchy of a prefab asset. " +
            "Use '" + Tool_Assets.AssetsFindToolId + "' to find prefabs.\n\n" +
            "Path-scoped reads (token-saving): supply '" + "paths" + "' (a list of paths) to read only the listed " +
            "fields/elements from the prefab root GameObject's data via Reflector.TryReadAt, or '" + "viewQuery" +
            "' (a ViewQuery) to navigate/filter via Reflector.View. The result populates 'Data' on the " +
            "returned PrefabData. These two parameters are mutually exclusive.\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.")]
        public PrefabData GetData
        (
            [Description("Prefab asset path, e.g. 'Assets/Prefabs/MyPrefab.prefab'. " +
                "Use '" + Tool_Assets.AssetsFindToolId + "' to locate. Mutually exclusive with 'gameObjectRef'.")]
            string? prefabAssetPath = null,
            [Description("Scene prefab instance reference. Resolves to its source prefab asset. " +
                "Mutually exclusive with 'prefabAssetPath'.")]
            GameObjectRef? gameObjectRef = null,
            [Description("Hierarchy depth to include (default 3). Use a high value like 99 for all descendants.")]
            int includeChildrenDepth = 3,
            [Description("Include 3D bounds for each GameObject.")]
            bool includeBounds = false,
            [Description("Include serialized component data for each GameObject.")]
            bool includeData = false,
            [Description("Optional. Token-saving — read only the listed paths from the root GameObject's data " +
                "via Reflector.TryReadAt. Mutually exclusive with 'viewQuery'.")]
            List<string>? paths = null,
            [Description("Optional. Token-saving — navigate/filter the root GameObject's data " +
                "via Reflector.View. Mutually exclusive with 'paths'.")]
            ViewQuery? viewQuery = null
        )
        {
            var hasPaths = paths != null && paths.Count > 0;
            var hasViewQuery = viewQuery != null;
            if (hasPaths && hasViewQuery)
                throw new ArgumentException(
                    $"'{nameof(paths)}' and '{nameof(viewQuery)}' are mutually exclusive — supply at most one.");

            var hasPrefabPath = !string.IsNullOrEmpty(prefabAssetPath);
            var hasGameObjectRef = gameObjectRef?.IsValid() == true;

            if (!hasPrefabPath && !hasGameObjectRef)
                throw new ArgumentException(
                    $"Either '{nameof(prefabAssetPath)}' or '{nameof(gameObjectRef)}' must be provided.");

            if (hasPrefabPath && hasGameObjectRef)
                throw new ArgumentException(
                    $"'{nameof(prefabAssetPath)}' and '{nameof(gameObjectRef)}' are mutually exclusive — supply at most one.");

            if (hasPrefabPath)
            {
                var path = prefabAssetPath!;
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Prefab asset path must start with 'Assets/'. Got: '{path}'");
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Prefab asset path must end with '.prefab'. Got: '{path}'");
            }

            return MainThread.Instance.Run(() =>
            {
                var reflector = UnityMcpPluginEditor.Instance.Reflector
                    ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Assets_Prefab>();

                // Resolve the prefab root GameObject
                UnityEngine.GameObject prefabRoot;
                string resolvedAssetPath;
                string resolvedAssetGuid;

                if (hasPrefabPath)
                {
                    resolvedAssetPath = prefabAssetPath!;
                    prefabRoot = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(resolvedAssetPath);
                    if (prefabRoot == null)
                        throw new ArgumentException(Error.NotFoundPrefabAtPath(resolvedAssetPath));
                }
                else
                {
                    // Resolve from GameObjectRef
                    var go = gameObjectRef!.FindGameObject();
                    if (go == null)
                        throw new ArgumentException(
                            $"GameObject not found for the provided reference. " +
                            $"Use '{Tool_Assets.AssetsFindToolId}' to locate prefab instances first.");

                    resolvedAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    if (string.IsNullOrEmpty(resolvedAssetPath))
                        throw new ArgumentException(
                            $"The provided GameObject '{go.name}' is not a prefab instance.");

                    prefabRoot = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(resolvedAssetPath);
                    if (prefabRoot == null)
                        throw new ArgumentException(Error.NotFoundPrefabAtPath(resolvedAssetPath));
                }

                resolvedAssetGuid = AssetDatabase.AssetPathToGUID(resolvedAssetPath);

                // Count total GameObjects in the hierarchy
                var totalCount = prefabRoot.GetComponentsInChildren<UnityEngine.Transform>(true).Length;

                // Build the root GameObject data with full hierarchy
                var rootData = new GameObjectData(
                    reflector: reflector,
                    go: prefabRoot,
                    includeData: includeData,
                    includeComponents: false,
                    includeBounds: includeBounds,
                    includeHierarchy: includeChildrenDepth > 0,
                    hierarchyDepth: includeChildrenDepth,
                    logger: logger
                );

                var prefabData = new PrefabData
                {
                    AssetPath = resolvedAssetPath,
                    AssetGuid = resolvedAssetGuid,
                    Name = prefabRoot.name,
                    RootGameObject = rootData,
                    IsValid = true,
                    TotalGameObjectCount = totalCount
                };

                // Path-scoped reads / view query against the serialized root GameObject data
                if (hasPaths || hasViewQuery)
                {
                    if (hasPaths)
                    {
                        prefabData.Data = PathReadHelper.BuildPathReadAggregate(
                            reflector, prefabRoot, prefabRoot.name, paths!, logger);
                    }
                    else
                    {
                        prefabData.Data = reflector.View(prefabRoot, viewQuery, logs: null, logger: logger)
                            ?? new SerializedMember
                            {
                                name = prefabRoot.name,
                                typeName = prefabRoot.GetType().FullName ?? string.Empty
                            };
                    }
                }

                return prefabData;
            });
        }
    }
}
