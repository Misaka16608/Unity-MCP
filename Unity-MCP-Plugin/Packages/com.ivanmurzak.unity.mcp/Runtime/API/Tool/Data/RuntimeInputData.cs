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

namespace AIGD
{
    [Description("A point in Unity screen coordinates. Origin is the bottom-left and units are pixels.")]
    public class RuntimeInputPoint
    {
        [Description("Horizontal screen coordinate in pixels.")]
        public float X { get; set; }

        [Description("Vertical screen coordinate in pixels. The origin is the bottom-left.")]
        public float Y { get; set; }
    }

    [Description("A rectangle in Unity screen coordinates. Origin is the bottom-left and units are pixels.")]
    public class RuntimeInputRect
    {
        [Description("Left coordinate in pixels.")]
        public float X { get; set; }

        [Description("Bottom coordinate in pixels.")]
        public float Y { get; set; }

        [Description("Width in pixels. Must be greater than zero.")]
        public float Width { get; set; }

        [Description("Height in pixels. Must be greater than zero.")]
        public float Height { get; set; }
    }

    [Description("A Unity object encountered during UGUI/EventSystem hit testing.")]
    public class RuntimeInputObject
    {
        [Description("GameObject name.")]
        public string Name { get; set; } = string.Empty;

        [Description("Full hierarchy path from the scene root.")]
        public string HierarchyPath { get; set; } = string.Empty;

        [Description("Fully qualified component type that handles the click, when available.")]
        public string? ComponentType { get; set; }

        [Description("Unity instance ID of the GameObject.")]
        public int InstanceId { get; set; }

        [Description("Whether the GameObject is active in the hierarchy.")]
        public bool Active { get; set; }

        [Description("Whether a Selectable on the target reports itself interactable.")]
        public bool Interactable { get; set; }

        [Description("Axis-aligned screen rectangle in bottom-left screen coordinates.")]
        public RuntimeInputRect? ScreenRect { get; set; }

        [Description("Recommended point for a click, normally the center of ScreenRect.")]
        public RuntimeInputPoint? RecommendedPoint { get; set; }

        [Description("Intersection with the query rectangle in rectangle hit-test mode.")]
        public RuntimeInputRect? IntersectionRect { get; set; }

        [Description("Intersection area divided by the object's screen rectangle area in rectangle hit-test mode.")]
        public float? OverlapRatio { get; set; }

        [Description("Raycast module name, when the object came from EventSystem.RaycastAll.")]
        public string? RaycastModule { get; set; }

        [Description("Raycast sorting order, when available.")]
        public int? SortingOrder { get; set; }

        [Description("Raycast depth, when available.")]
        public int? Depth { get; set; }

        [Description("Raycast distance, when available.")]
        public float? Distance { get; set; }

        [Description("Why this object is present: primaryHit, raycast, occluder, or candidate.")]
        public string? Role { get; set; }
    }

    [Description("A raw EventSystem raycast entry and its resolved click handler.")]
    public class RuntimeInputRaycast
    {
        public RuntimeInputObject RaycastObject { get; set; } = new();
        public RuntimeInputObject? ClickHandler { get; set; }
    }

    [Description("A result returned by runtime-input-find-clickable.")]
    public class RuntimeInputFindResult
    {
        public bool Success { get; set; }
        public string Adapter { get; set; } = "ugui-event-system";
        public string CoordinateSpace { get; set; } = "screenPixelsBottomLeft";
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? EventSystemName { get; set; }
        public int? EventSystemInstanceId { get; set; }
        public List<RuntimeInputObject> Matches { get; set; } = new();
    }

    [Description("A result returned by runtime-input-hit-test.")]
    public class RuntimeInputHitTestResult
    {
        public bool Success { get; set; }
        public string Adapter { get; set; } = "ugui-event-system";
        public string CoordinateSpace { get; set; } = "screenPixelsBottomLeft";
        public string Mode { get; set; } = string.Empty;
        public RuntimeInputPoint? Point { get; set; }
        public RuntimeInputRect? Rect { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? EventSystemName { get; set; }
        public int? EventSystemInstanceId { get; set; }
        public RuntimeInputObject? PrimaryHit { get; set; }
        public List<RuntimeInputRaycast> Raycasts { get; set; } = new();
        public List<RuntimeInputObject> Occluders { get; set; } = new();
        public List<RuntimeInputObject> Candidates { get; set; } = new();
    }

    [Description("A result returned by runtime-input-click.")]
    public class RuntimeInputClickResult
    {
        public bool Success { get; set; }
        public bool Dispatched { get; set; }
        public string Adapter { get; set; } = "ugui-event-system";
        public string CoordinateSpace { get; set; } = "screenPixelsBottomLeft";
        public RuntimeInputPoint Point { get; set; } = new();
        public string MouseButton { get; set; } = "left";
        public int ClickCount { get; set; } = 1;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? EventSystemName { get; set; }
        public int? EventSystemInstanceId { get; set; }
        public RuntimeInputObject? PrimaryHit { get; set; }
        public List<RuntimeInputRaycast> Raycasts { get; set; } = new();
        public List<RuntimeInputObject> Occluders { get; set; } = new();
        public List<string> DispatchedEvents { get; set; } = new();
    }
}
