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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.API
{
    public partial class Tool_RuntimeInput
    {
        public const string ClickToolId = "runtime-input-click";

        [AiTool
        (
            ClickToolId,
            Title = "Runtime Input / Click",
            DestructiveHint = true,
            Enabled = false
        )]
        [AiSkillDescription("Dispatch a mouse click through a running Unity UGUI/EventSystem target at screen coordinates.")]
        [AiSkillBody("Dispatches a pointer move/press/release/click sequence through Unity's UGUI EventSystem. " +
            "The tool uses in-process Unity events and does not move or click the operating-system mouse. " +
            "Coordinates are Unity screen pixels with a bottom-left origin. The target must be in Play Mode and " +
            "the result includes the topmost EventSystem hit, raycast entries, and any occluding object. " +
            "This first implementation supports UGUI/EventSystem; NGUI and UI Toolkit are not included yet.")]
        [Description("Click a running UGUI/EventSystem target at a screen coordinate. Coordinates use bottom-left Unity screen pixels.")]
        public RuntimeInputClickResult Click(
            RuntimeInputPoint point,
            string mouseButton = "left",
            int clickCount = 1,
            string? eventSystemName = null,
            int eventSystemInstanceId = 0)
        {
            return MainThread.Instance.Run(() =>
            {
                var result = new RuntimeInputClickResult
                {
                    Point = point ?? new RuntimeInputPoint(),
                    MouseButton = mouseButton,
                    ClickCount = clickCount
                };
                if (!IsAvailable)
                    return FailClick(result, "UGUI_UNAVAILABLE", "UGUI/EventSystem is not available in this project.");
                if (!Application.isPlaying)
                    return FailClick(result, "PLAY_MODE_REQUIRED", "runtime-input-click requires Play Mode.");
                if (point == null)
                    return FailClick(result, "POINT_REQUIRED", "point is required.");
                if (clickCount < 1 || clickCount > 16)
                    return FailClick(result, "INVALID_CLICK_COUNT", "clickCount must be between 1 and 16.");
                if (!mouseButton.Equals("left", System.StringComparison.OrdinalIgnoreCase)
                    && !mouseButton.Equals("right", System.StringComparison.OrdinalIgnoreCase)
                    && !mouseButton.Equals("middle", System.StringComparison.OrdinalIgnoreCase))
                    return FailClick(result, "INVALID_MOUSE_BUTTON", "mouseButton must be left, right, or middle.");

                var eventSystem = ResolveEventSystem(eventSystemName, eventSystemInstanceId);
                if (!eventSystem.IsSuccess)
                    return FailClick(result, eventSystem.ErrorCode!, eventSystem.ErrorMessage!);

                result.EventSystemName = ((UnityEngine.Component)eventSystem.Value!).gameObject.name;
                result.EventSystemInstanceId = eventSystem.Value!.GetInstanceID();

                var pointValue = new Vector2(point.X, point.Y);
                List<object> raycasts;
                try
                {
                    raycasts = Raycast(eventSystem.Value!, pointValue);
                }
                catch (System.Exception exception)
                {
                    return FailClick(result, "RAYCAST_FAILED",
                        $"UGUI EventSystem raycast failed: {exception.GetBaseException().Message}");
                }
                var camera = raycasts.Count > 0
                    ? GetEventCamera(ReadMember(ReadMember(raycasts[0], "module"), "eventCamera"))
                    : null;
                result.PrimaryHit = FirstClickHandler(raycasts, camera, out var entries, out var occluders);
                result.Raycasts = entries;
                result.Occluders = occluders;

                if (result.PrimaryHit == null || raycasts.Count == 0 || GetRaycastGameObject(raycasts[0]) == null)
                    return FailClick(result, "CLICK_NOT_HANDLED", "The coordinate did not resolve to a topmost UGUI pointer-click handler. See raycasts and occluders.");

                var topmostObject = GetRaycastGameObject(raycasts[0])!;
                if (!DispatchPointerSequence(eventSystem.Value!, topmostObject, pointValue, mouseButton, clickCount,
                    out var dispatchedEvents, out var dispatchError))
                    return FailClick(result, "DISPATCH_FAILED", dispatchError ?? "Failed to dispatch the UGUI pointer sequence.");

                result.DispatchedEvents = dispatchedEvents;
                result.Dispatched = true;
                result.Success = true;
                return result;
            });
        }

        static RuntimeInputClickResult FailClick(RuntimeInputClickResult result, string code, string message)
        {
            result.Success = false;
            result.Dispatched = false;
            result.ErrorCode = code;
            result.ErrorMessage = message;
            return result;
        }
    }
}
