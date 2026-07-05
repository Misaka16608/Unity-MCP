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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Model;

namespace AIGD
{
    public class PrefabData
    {
        [Description("Asset path, starts with 'Assets/'.")]
        public string AssetPath { get; set; } = string.Empty;

        [Description("Asset GUID.")]
        public string AssetGuid { get; set; } = string.Empty;

        [Description("Prefab name.")]
        public string Name { get; set; } = string.Empty;

        [Description("Root GameObject with full hierarchy, components, bounds, and serialized data.")]
        public GameObjectData? RootGameObject { get; set; } = null;

        [Description("Whether the prefab loaded successfully.")]
        public bool IsValid { get; set; }

        [Description("Total GameObject count in the hierarchy.")]
        public int TotalGameObjectCount { get; set; }

        [Description("Path-scoped read or view-query result. Populated when 'paths' or 'viewQuery' is supplied.")]
        public SerializedMember? Data { get; set; } = null;

        public PrefabData() { }
    }
}
