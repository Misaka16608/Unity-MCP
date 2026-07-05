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
        [Description("Path to the prefab asset within the project. Starts with 'Assets/'.")]
        public string AssetPath { get; set; } = string.Empty;

        [Description("Unique identifier for the prefab asset.")]
        public string AssetGuid { get; set; } = string.Empty;

        [Description("Name of the prefab asset.")]
        public string Name { get; set; } = string.Empty;

        [Description("Root GameObject of the prefab with its full hierarchy, components, bounds, and serialized data " +
            "(depending on the toggles). A prefab always has exactly one root.")]
        public GameObjectData? RootGameObject { get; set; } = null;

        [Description("If true, the prefab was loaded successfully and the root GameObject is valid.")]
        public bool IsValid { get; set; }

        [Description("Total number of GameObjects in the prefab hierarchy.")]
        public int TotalGameObjectCount { get; set; }

        [Description("Path-scoped read or view-query result, populated when 'paths' or 'viewQuery' is supplied " +
            "to the assets-prefab-get-data tool. Null otherwise.")]
        public SerializedMember? Data { get; set; } = null;

        public PrefabData() { }
    }
}
