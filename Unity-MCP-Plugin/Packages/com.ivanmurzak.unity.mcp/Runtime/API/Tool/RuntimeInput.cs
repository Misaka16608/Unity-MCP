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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.API
{
    [AiToolType]
    public partial class Tool_RuntimeInput
    {
        public const string AdapterName = "ugui-event-system";
        public const string CoordinateSpace = "screenPixelsBottomLeft";

        static readonly BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        static readonly BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        static readonly BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        static Type? FindType(string fullName)
        {
            var type = Type.GetType(fullName + ", UnityEngine.UI");
            if (type != null)
                return type;

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);
        }

        static Type? EventSystemType => FindType("UnityEngine.EventSystems.EventSystem");
        static Type? PointerEventDataType => FindType("UnityEngine.EventSystems.PointerEventData");
        static Type? RaycastResultType => FindType("UnityEngine.EventSystems.RaycastResult");
        static Type? ExecuteEventsType => FindType("UnityEngine.EventSystems.ExecuteEvents");
        static Type? PointerClickHandlerType => FindType("UnityEngine.EventSystems.IPointerClickHandler");
        static Type? GraphicType => FindType("UnityEngine.UI.Graphic");
        static Type? SelectableType => FindType("UnityEngine.UI.Selectable");

        static bool IsAvailable
            => EventSystemType != null && PointerEventDataType != null && RaycastResultType != null
                && ExecuteEventsType != null && PointerClickHandlerType != null && GraphicType != null;

        static object? ReadMember(object? instance, string name)
        {
            if (instance == null)
                return null;

            var type = instance is Type staticType ? staticType : instance.GetType();
            var flags = instance is Type ? PublicStatic : AnyInstance;
            var property = type.GetProperty(name, flags);
            if (property != null)
                return property.GetValue(instance is Type ? null : instance);

            var field = type.GetField(name, flags);
            return field?.GetValue(instance is Type ? null : instance);
        }

        static bool TryReadBool(object? instance, string name, out bool value)
        {
            var raw = ReadMember(instance, name);
            if (raw is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            value = false;
            return false;
        }

        static GameObject? GetGameObject(object? component)
            => component is Component unityComponent ? unityComponent.gameObject : null;

        static EventSystemResolution ResolveEventSystem(string? eventSystemName, int eventSystemInstanceId)
        {
            var type = EventSystemType;
            if (type == null)
                return EventSystemResolution.Fail("UGUI_UNAVAILABLE", "UnityEngine.EventSystems is not available. Install/enable the UGUI package.");

            var candidates = Resources.FindObjectsOfTypeAll(type)
                .OfType<UnityEngine.Object>()
                .Where(candidate => candidate is Component component
                    && component.gameObject.activeInHierarchy
                    && component.gameObject.scene.IsValid()
                    && component.gameObject.scene.isLoaded)
                .ToList();

            if (eventSystemInstanceId != 0)
            {
                var match = candidates.FirstOrDefault(candidate => candidate.GetInstanceID() == eventSystemInstanceId);
                if (match == null)
                    return EventSystemResolution.Fail("EVENT_SYSTEM_NOT_FOUND", $"No active EventSystem has instance ID {eventSystemInstanceId}.");
                return EventSystemResolution.Success(match, type);
            }

            if (!string.IsNullOrWhiteSpace(eventSystemName))
            {
                var matches = candidates.Where(candidate => ((Component)candidate).gameObject.name == eventSystemName).ToList();
                if (matches.Count == 0)
                    return EventSystemResolution.Fail("EVENT_SYSTEM_NOT_FOUND", $"No active EventSystem named '{eventSystemName}' was found.");
                if (matches.Count > 1)
                    return EventSystemResolution.Fail("EVENT_SYSTEM_AMBIGUOUS", $"Multiple active EventSystems are named '{eventSystemName}'. Use eventSystemInstanceId.");
                return EventSystemResolution.Success(matches[0], type);
            }

            var current = ReadMember(type, "current") as UnityEngine.Object;
            if (current != null && candidates.Any(candidate => candidate.GetInstanceID() == current.GetInstanceID()))
                return EventSystemResolution.Success(current, type);

            if (candidates.Count == 1)
                return EventSystemResolution.Success(candidates[0], type);
            if (candidates.Count == 0)
                return EventSystemResolution.Fail("EVENT_SYSTEM_NOT_FOUND", "No active EventSystem was found in a loaded scene.");
            return EventSystemResolution.Fail("EVENT_SYSTEM_REQUIRED", "Multiple active EventSystems exist. Specify eventSystemName or eventSystemInstanceId.");
        }

        static RuntimeInputObject BuildObject(GameObject gameObject, string? role = null, string? module = null,
            int? sortingOrder = null, int? depth = null, float? distance = null, Camera? eventCamera = null,
            bool? interactableOverride = null)
        {
            var handler = GetClickHandler(gameObject) ?? gameObject;
            var selectable = SelectableType == null ? null : gameObject.GetComponentInParent(SelectableType);
            var interactable = interactableOverride ?? true;
            if (selectable != null)
            {
                var method = SelectableType!.GetMethod("IsInteractable", PublicInstance);
                if (method != null && method.Invoke(selectable, null) is bool selectableState)
                    interactable = selectableState;
            }

            var screenRect = GetScreenRect(handler, eventCamera);
            return new RuntimeInputObject
            {
                Name = handler.name,
                HierarchyPath = GetHierarchyPath(handler.transform),
                ComponentType = handler.GetComponent<MonoBehaviour>()?.GetType().FullName,
                InstanceId = handler.GetInstanceID(),
                Active = handler.activeInHierarchy,
                Interactable = interactable,
                ScreenRect = screenRect,
                RecommendedPoint = screenRect == null ? null : new RuntimeInputPoint
                {
                    X = screenRect.X + screenRect.Width * 0.5f,
                    Y = screenRect.Y + screenRect.Height * 0.5f
                },
                RaycastModule = module,
                SortingOrder = sortingOrder,
                Depth = depth,
                Distance = distance,
                Role = role
            };
        }

        static string GetHierarchyPath(Transform transform)
        {
            var parts = new List<string>();
            for (var current = transform; current != null; current = current.parent)
                parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        static RuntimeInputRect? GetScreenRect(GameObject gameObject, Camera? eventCamera)
        {
            var rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
                return null;

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(eventCamera, corner);
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }

            return new RuntimeInputRect { X = min.x, Y = min.y, Width = max.x - min.x, Height = max.y - min.y };
        }

        static Camera? GetEventCamera(object? graphic)
        {
            if (graphic is Camera camera)
                return camera;
            if (graphic == null)
                return Camera.main;

            var canvas = ReadMember(graphic, "canvas") as Canvas;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;
            return canvas.worldCamera ?? Camera.main;
        }

        static GameObject? GetClickHandler(GameObject gameObject)
        {
            if (ExecuteEventsType == null || PointerClickHandlerType == null)
                return null;

            var method = ExecuteEventsType.GetMethods(PublicStatic)
                .FirstOrDefault(candidate => candidate.Name == "GetEventHandler"
                    && candidate.IsGenericMethodDefinition
                    && candidate.GetGenericArguments().Length == 1
                    && candidate.GetParameters().Length == 1);
            if (method == null)
                return null;

            var resolved = method.MakeGenericMethod(PointerClickHandlerType).Invoke(null, new object[] { gameObject });
            return resolved as GameObject;
        }

        // PointerEventData's constructor has changed visibility/binding behavior across Unity
        // versions. Do not rely on Activator.CreateInstance(Type, object), whose binder can
        // incorrectly look for a parameterless constructor when the EventSystem value is held
        // through a UnityEngine.Object reference. Resolve the one-argument constructor explicitly.
        static object? CreatePointerEventData(object eventSystem, out string? error)
        {
            error = null;
            if (PointerEventDataType == null || EventSystemType == null)
            {
                error = "PointerEventData/EventSystem types are unavailable.";
                return null;
            }

            var constructors = PointerEventDataType.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var constructor = constructors.FirstOrDefault(candidate =>
            {
                var parameters = candidate.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(eventSystem);
            });

            if (constructor == null)
            {
                error = $"No PointerEventData constructor accepting '{EventSystemType.FullName}' was found.";
                return null;
            }

            try
            {
                return constructor.Invoke(new[] { eventSystem });
            }
            catch (Exception exception)
            {
                error = $"PointerEventData constructor failed: {exception.GetBaseException().Message}";
                return null;
            }
        }

        static List<object> Raycast(object eventSystem, Vector2 point)
        {
            var pointerData = CreatePointerEventData(eventSystem, out var pointerError);
            if (pointerData == null)
                throw new InvalidOperationException(pointerError ?? "Failed to create PointerEventData.");
            PointerEventDataType!.GetProperty("position", PublicInstance)?.SetValue(pointerData, point);
            PointerEventDataType.GetProperty("pressPosition", PublicInstance)?.SetValue(pointerData, point);

            var resultListType = typeof(List<>).MakeGenericType(RaycastResultType!);
            var results = Activator.CreateInstance(resultListType)!;
            var method = EventSystemType!.GetMethods(PublicInstance)
                .First(candidate => candidate.Name == "RaycastAll" && candidate.GetParameters().Length == 2);
            method.Invoke(eventSystem, new[] { pointerData, results });
            return ((IEnumerable)results).Cast<object>().ToList();
        }

        static GameObject? GetRaycastGameObject(object raycast)
            => ReadMember(raycast, "gameObject") as GameObject;

        static string? GetRaycastModuleName(object raycast)
            => (ReadMember(raycast, "module") as Component)?.GetType().FullName;

        static T? GetNullable<T>(object raycast, string member) where T : struct
        {
            var value = ReadMember(raycast, member);
            return value is T typed ? typed : null;
        }

        static RuntimeInputRaycast BuildRaycast(object raycast, string? role, Camera? eventCamera)
        {
            var raycastObject = GetRaycastGameObject(raycast);
            if (raycastObject == null)
                return new RuntimeInputRaycast();

            var handler = GetClickHandler(raycastObject);
            var moduleName = GetRaycastModuleName(raycast);
            var sortingOrder = GetNullable<int>(raycast, "sortingOrder");
            var depth = GetNullable<int>(raycast, "depth");
            var distance = GetNullable<float>(raycast, "distance");
            return new RuntimeInputRaycast
            {
                RaycastObject = BuildObject(raycastObject, role ?? "raycast", moduleName, sortingOrder, depth, distance, eventCamera),
                ClickHandler = handler == null ? null : BuildObject(handler, role, moduleName, sortingOrder, depth, distance, eventCamera)
            };
        }

        static List<GameObject> FindClickableObjects()
        {
            if (GraphicType == null)
                return new List<GameObject>();

            var result = new Dictionary<int, GameObject>();
            foreach (var rawGraphic in Resources.FindObjectsOfTypeAll(GraphicType))
            {
                if (rawGraphic is not Component graphic || !graphic.gameObject.activeInHierarchy
                    || !graphic.gameObject.scene.IsValid() || !graphic.gameObject.scene.isLoaded)
                    continue;
                if (ReadMember(rawGraphic, "raycastTarget") is bool raycastTarget && !raycastTarget)
                    continue;

                var handler = GetClickHandler(graphic.gameObject);
                if (handler == null)
                    continue;
                result[handler.GetInstanceID()] = handler;
            }
            return result.Values.ToList();
        }

        static object? FindGraphicForHandler(GameObject handler)
        {
            if (GraphicType == null)
                return null;

            foreach (var rawGraphic in Resources.FindObjectsOfTypeAll(GraphicType))
            {
                if (rawGraphic is not Component graphic || !graphic.gameObject.activeInHierarchy)
                    continue;
                if (GetClickHandler(graphic.gameObject) == handler)
                    return rawGraphic;
            }
            return null;
        }

        static RuntimeInputObject BuildCandidate(GameObject gameObject, string role = "candidate")
            => BuildObject(gameObject, role, eventCamera: GetEventCamera(FindGraphicForHandler(gameObject)));

        static RuntimeInputObject? FirstClickHandler(List<object> raycasts, Camera? eventCamera,
            out List<RuntimeInputRaycast> entries, out List<RuntimeInputObject> occluders)
        {
            entries = new List<RuntimeInputRaycast>();
            occluders = new List<RuntimeInputObject>();
            RuntimeInputObject? primary = null;
            var topmostResolved = false;
            foreach (var raycast in raycasts)
            {
                var raw = GetRaycastGameObject(raycast);
                if (raw == null)
                    continue;
                var handler = GetClickHandler(raw);
                var entry = BuildRaycast(raycast, handler == null ? "occluder" : "raycast", eventCamera);
                entries.Add(entry);
                if (topmostResolved)
                    continue;

                if (handler == null)
                {
                    entry.RaycastObject.Role = "occluder";
                    occluders.Add(entry.RaycastObject);
                    topmostResolved = true;
                    continue;
                }

                primary = entry.ClickHandler;
                if (primary != null)
                    primary.Role = "primaryHit";
                topmostResolved = true;
            }
            return primary;
        }

        static bool SetPointerProperty(object pointerData, string propertyName, object value)
        {
            var property = PointerEventDataType!.GetProperty(propertyName, PublicInstance);
            if (property == null || !property.CanWrite)
                return false;
            property.SetValue(pointerData, value);
            return true;
        }

        static bool DispatchExecuteHierarchy(string eventFunctionName, GameObject target, object pointerData)
        {
            if (ExecuteEventsType == null)
                return false;

            var eventFunctionProperty = ExecuteEventsType.GetProperty(eventFunctionName, PublicStatic);
            if (eventFunctionProperty == null)
                return false;
            var eventFunction = eventFunctionProperty.GetValue(null);
            if (eventFunction == null)
                return false;

            var handlerType = eventFunctionProperty.PropertyType.GetGenericArguments().FirstOrDefault();
            if (handlerType == null)
                return false;

            var executeMethod = ExecuteEventsType.GetMethods(PublicStatic)
                .FirstOrDefault(candidate => candidate.Name == "ExecuteHierarchy"
                    && candidate.IsGenericMethodDefinition
                    && candidate.GetGenericArguments().Length == 1
                    && candidate.GetParameters().Length == 3);
            if (executeMethod == null)
                return false;

            try
            {
                executeMethod.MakeGenericMethod(handlerType).Invoke(null, new[] { target, pointerData, eventFunction });
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool DispatchPointerSequence(UnityEngine.Object eventSystemValue, GameObject target, Vector2 point,
            string mouseButton, int clickCount,
            out List<string> dispatchedEvents, out string? error)
        {
            dispatchedEvents = new List<string>();
            error = null;
            if (PointerEventDataType == null)
            {
                error = "PointerEventData is unavailable.";
                return false;
            }

            var pointerData = CreatePointerEventData(eventSystemValue, out var pointerError);
            if (pointerData == null)
            {
                error = pointerError ?? "Failed to create PointerEventData.";
                return false;
            }

            SetPointerProperty(pointerData, "position", point);
            SetPointerProperty(pointerData, "pressPosition", point);
            SetPointerProperty(pointerData, "clickTime", Time.unscaledTime);
            SetPointerProperty(pointerData, "pointerId", -1);

            var buttonProperty = PointerEventDataType.GetProperty("button", PublicInstance);
            if (buttonProperty != null && buttonProperty.PropertyType.IsEnum)
                buttonProperty.SetValue(pointerData, Enum.Parse(buttonProperty.PropertyType, mouseButton, true));

            for (var currentClick = 1; currentClick <= clickCount; currentClick++)
            {
                SetPointerProperty(pointerData, "clickCount", currentClick);
                var eventNames = currentClick == 1
                    ? new[] { "pointerEnterHandler", "pointerDownHandler", "pointerUpHandler", "pointerClickHandler" }
                    : new[] { "pointerDownHandler", "pointerUpHandler", "pointerClickHandler" };
                foreach (var eventName in eventNames)
                {
                    if (!DispatchExecuteHierarchy(eventName, target, pointerData))
                    {
                        error = $"Failed to dispatch EventSystem event '{eventName}'.";
                        return false;
                    }
                    dispatchedEvents.Add(eventName);
                }
            }

            if (!DispatchExecuteHierarchy("pointerExitHandler", target, pointerData))
            {
                error = "Failed to dispatch EventSystem event 'pointerExitHandler'.";
                return false;
            }
            dispatchedEvents.Add("pointerExitHandler");
            return true;
        }

        static bool Intersects(RuntimeInputRect? first, RuntimeInputRect second)
        {
            if (first == null)
                return false;
            return first.X < second.X + second.Width && first.X + first.Width > second.X
                && first.Y < second.Y + second.Height && first.Y + first.Height > second.Y;
        }

        static RuntimeInputPoint ToPoint(Vector2 value) => new() { X = value.x, Y = value.y };
        static Rect ToUnityRect(RuntimeInputRect value) => new(value.X, value.Y, value.Width, value.Height);

        sealed class EventSystemResolution
        {
            public bool IsSuccess { get; private set; }
            public string? ErrorCode { get; private set; }
            public string? ErrorMessage { get; private set; }
            public UnityEngine.Object? Value { get; private set; }
            public Type? Type { get; private set; }

            public static EventSystemResolution Success(UnityEngine.Object value, Type type)
                => new() { IsSuccess = true, Value = value, Type = type };
            public static EventSystemResolution Fail(string code, string message)
                => new() { IsSuccess = false, ErrorCode = code, ErrorMessage = message };
        }
    }
}
