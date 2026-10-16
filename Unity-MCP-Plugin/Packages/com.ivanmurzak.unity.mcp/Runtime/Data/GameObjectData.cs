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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AIGD
{
    public class GameObjectData
    {
        public GameObjectRef? Reference { get; set; }

        [Description("GameObject editable data (tag, layer, etc).")]
        public SerializedMember? Data { get; set; } = null;

        [Description("Bounds of the GameObject.")]
        public Bounds? Bounds { get; set; }

        [Description("Hierarchy metadata of the GameObject.")]
        public GameObjectMetadata? Hierarchy { get; set; } = null;

        [Description("Attached components shallow data of the GameObject (Read-only, use Component modification tool for modification).")]
        public ComponentDataShallow[]? Components { get; set; } = null;

        internal static Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        public GameObjectData() { }
        public GameObjectData(
            Reflector reflector,
            GameObject go,
            bool includeData = false,
            bool includeComponents = false,
            bool includeBounds = false,
            bool includeHierarchy = false,
            int hierarchyDepth = 0,
            ILogger? logger = null)
        {
            Reference = new GameObjectRef(go);

            if (includeData)
            {
                Data = reflector.Serialize(
                    obj: go,
                    fallbackType: typeof(GameObject),
                    name: go.name,
                    recursive: true,
                    logger: logger);
            }

            if (includeComponents)
            {
                Components = go.GetComponents<UnityEngine.Component>()
                    .Select(c => new ComponentDataShallow(c))
                    .ToArray();
            }

            if (includeBounds)
                Bounds = CalculateBounds(go);

            if (includeHierarchy)
                Hierarchy = GameObjectMetadata.FromGameObject(go, hierarchyDepth);
        }
    }

    public static class GameObjectDataExtensions
    {
        public static GameObjectData ToGameObjectData(
            this GameObject go,
            Reflector reflector,
            bool includeData = false,
            bool includeComponents = false,
            bool includeBounds = false,
            bool includeHierarchy = false,
            int hierarchyDepth = 0,
            ILogger? logger = null)
        {
            return new GameObjectData(
                reflector: reflector,
                go: go,
                includeData: includeData,
                includeComponents: includeComponents,
                includeBounds: includeBounds,
                includeHierarchy: includeHierarchy,
                hierarchyDepth: hierarchyDepth,
                logger: logger
            );
        }
    }
}