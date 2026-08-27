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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;

namespace com.IvanMurzak.Unity.MCP.Runtime.API
{
    public partial class Tool_RuntimeInput
    {
        public const string FindClickableToolId = "runtime-input-find-clickable";

        [AiTool
        (
            FindClickableToolId,
            Title = "Runtime Input / Find Clickable",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [AiSkillDescription("Find active UGUI objects that can receive a pointer click and return their screen-space click areas.")]
        [AiSkillBody("Finds UGUI/EventSystem click targets by exact GameObject name or hierarchy path. " +
            "Returns screen-space rectangles and a recommended coordinate that can be passed to " +
            "'runtime-input-click'. Coordinates use Unity screen pixels with a bottom-left origin. " +
            "This tool is read-only and does not dispatch input. NGUI and UI Toolkit are not included in this adapter.")]
        [Description("Find a UGUI/EventSystem click target by exact name or hierarchy path. Supply at least one of objectName or hierarchyPath.")]
        public RuntimeInputFindResult FindClickable(
            string? objectName = null,
            string? hierarchyPath = null,
            string? eventSystemName = null,
            int eventSystemInstanceId = 0)
        {
            return MainThread.Instance.Run(() =>
            {
                var result = new RuntimeInputFindResult();
                if (!IsAvailable)
                    return Fail(result, "UGUI_UNAVAILABLE", "UGUI/EventSystem is not available in this project.");
                if (string.IsNullOrWhiteSpace(objectName) && string.IsNullOrWhiteSpace(hierarchyPath))
                    return Fail(result, "QUERY_REQUIRED", "Supply objectName or hierarchyPath.");

                var eventSystem = ResolveEventSystem(eventSystemName, eventSystemInstanceId);
                if (!eventSystem.IsSuccess)
                    return Fail(result, eventSystem.ErrorCode!, eventSystem.ErrorMessage!);

                var candidates = FindClickableObjects();
                foreach (var candidate in candidates)
                {
                    var path = GetHierarchyPath(candidate.transform);
                    var nameMatches = !string.IsNullOrWhiteSpace(objectName) && candidate.name == objectName;
                    var pathMatches = !string.IsNullOrWhiteSpace(hierarchyPath) && path == hierarchyPath;
                    if (!nameMatches && !pathMatches)
                        continue;

                    var data = BuildCandidate(candidate);
                    data.Role = "candidate";
                    result.Matches.Add(data);
                }

                result.Success = result.Matches.Count > 0;
                if (!result.Success)
                {
                    result.ErrorCode = "CLICKABLE_NOT_FOUND";
                    result.ErrorMessage = $"No active UGUI click target matched objectName '{objectName}' or hierarchyPath '{hierarchyPath}'.";
                }
                result.EventSystemName = ((UnityEngine.Component)eventSystem.Value!).gameObject.name;
                result.EventSystemInstanceId = eventSystem.Value!.GetInstanceID();
                return result;
            });
        }

        static RuntimeInputFindResult Fail(RuntimeInputFindResult result, string code, string message)
        {
            result.Success = false;
            result.ErrorCode = code;
            result.ErrorMessage = message;
            return result;
        }
    }
}
