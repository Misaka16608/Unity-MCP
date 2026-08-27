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
        public const string HitTestToolId = "runtime-input-hit-test";

        [AiTool
        (
            HitTestToolId,
            Title = "Runtime Input / Hit Test",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [AiSkillDescription("Analyze which active UGUI/EventSystem objects would receive a click at a point or within a rectangle without dispatching input.")]
        [AiSkillBody("Performs a dry-run UGUI/EventSystem hit test. In point mode it uses EventSystem.RaycastAll " +
            "and returns the topmost click handler, all raycasts, and occluders. In rectangle mode it returns every " +
            "click target whose screen rectangle intersects the supplied range. No pointer event is dispatched and no " +
            "UI state is changed. Coordinates use Unity screen pixels with a bottom-left origin.")]
        [Description("Dry-run UGUI hit test. Supply exactly one of point or rect. It never dispatches pointer events.")]
        public RuntimeInputHitTestResult HitTest(
            RuntimeInputPoint? point = null,
            RuntimeInputRect? rect = null,
            string? eventSystemName = null,
            int eventSystemInstanceId = 0)
        {
            return MainThread.Instance.Run(() =>
            {
                var result = new RuntimeInputHitTestResult
                {
                    Point = point,
                    Rect = rect
                };
                if (!IsAvailable)
                    return FailHit(result, "UGUI_UNAVAILABLE", "UGUI/EventSystem is not available in this project.");
                if ((point == null) == (rect == null))
                    return FailHit(result, "INPUT_SHAPE_REQUIRED", "Supply exactly one of point or rect.");
                if (rect != null && (rect.Width <= 0f || rect.Height <= 0f))
                    return FailHit(result, "INVALID_RECT", "rect.Width and rect.Height must be greater than zero.");

                var eventSystem = ResolveEventSystem(eventSystemName, eventSystemInstanceId);
                if (!eventSystem.IsSuccess)
                    return FailHit(result, eventSystem.ErrorCode!, eventSystem.ErrorMessage!);

                result.EventSystemName = ((UnityEngine.Component)eventSystem.Value!).gameObject.name;
                result.EventSystemInstanceId = eventSystem.Value!.GetInstanceID();

                if (point != null)
                {
                    result.Mode = "point";
                    List<object> raycasts;
                    try
                    {
                        raycasts = Raycast(eventSystem.Value!, new Vector2(point.X, point.Y));
                    }
                    catch (System.Exception exception)
                    {
                        return FailHit(result, "RAYCAST_FAILED",
                            $"UGUI EventSystem raycast failed: {exception.GetBaseException().Message}");
                    }
                    var camera = raycasts.Count > 0
                        ? GetEventCamera(ReadMember(ReadMember(raycasts[0], "module"), "eventCamera"))
                        : null;
                    result.PrimaryHit = FirstClickHandler(raycasts, camera, out var entries, out var occluders);
                    result.Raycasts = entries;
                    result.Occluders = occluders;
                    result.Success = true;
                    return result;
                }

                result.Mode = "rect";
                foreach (var candidate in FindClickableObjects())
                {
                    var data = BuildCandidate(candidate);
                    if (!Intersects(data.ScreenRect, rect!))
                        continue;
                    if (data.ScreenRect != null)
                    {
                        var left = Mathf.Max(data.ScreenRect.X, rect!.X);
                        var bottom = Mathf.Max(data.ScreenRect.Y, rect.Y);
                        var right = Mathf.Min(data.ScreenRect.X + data.ScreenRect.Width, rect.X + rect.Width);
                        var top = Mathf.Min(data.ScreenRect.Y + data.ScreenRect.Height, rect.Y + rect.Height);
                        var intersectionWidth = Mathf.Max(0f, right - left);
                        var intersectionHeight = Mathf.Max(0f, top - bottom);
                        data.IntersectionRect = new RuntimeInputRect
                        {
                            X = left,
                            Y = bottom,
                            Width = intersectionWidth,
                            Height = intersectionHeight
                        };
                        var objectArea = data.ScreenRect.Width * data.ScreenRect.Height;
                        data.OverlapRatio = objectArea > 0f
                            ? intersectionWidth * intersectionHeight / objectArea
                            : 0f;
                    }
                    data.Role = "candidate";
                    result.Candidates.Add(data);
                }
                if (result.Candidates.Count == 1)
                {
                    result.PrimaryHit = result.Candidates[0];
                    result.PrimaryHit.Role = "primaryHit";
                }
                result.Success = true;
                return result;
            });
        }

        static RuntimeInputHitTestResult FailHit(RuntimeInputHitTestResult result, string code, string message)
        {
            result.Success = false;
            result.ErrorCode = code;
            result.ErrorMessage = message;
            return result;
        }
    }
}
