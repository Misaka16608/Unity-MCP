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
using System.Collections;
using com.IvanMurzak.Unity.MCP.Runtime.API;
using AIGD;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace com.IvanMurzak.Unity.MCP.Tests
{
    public class RuntimeInputPlayModeTests
    {
        GameObject _eventSystem = null!;
        GameObject _canvas = null!;
        TestClickHandler _handler = null!;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _eventSystem = new GameObject("McpPlayModeEventSystem", typeof(EventSystem));
            _canvas = new GameObject("McpPlayModeCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            _canvas.GetComponent<Canvas>()!.renderMode = RenderMode.ScreenSpaceOverlay;

            var button = new GameObject("McpPlayModeButton", typeof(RectTransform), typeof(Image), typeof(TestClickHandler));
            button.transform.SetParent(_canvas.transform, false);
            var rect = button.GetComponent<RectTransform>()!;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(100f, 100f);
            rect.sizeDelta = new Vector2(200f, 100f);
            button.GetComponent<Image>()!.raycastTarget = true;
            _handler = button.GetComponent<TestClickHandler>()!;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_eventSystem);
            Object.Destroy(_canvas);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Click_DispatchesUGUIPointerSequence()
        {
            var tool = new Tool_RuntimeInput();
            var result = tool.Click(
                new RuntimeInputPoint { X = 150f, Y = 150f },
                clickCount: 2,
                eventSystemInstanceId: _eventSystem.GetComponent<EventSystem>()!.GetInstanceID());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsTrue(result.Dispatched);
            Assert.AreEqual(2, _handler.ClickCount);
            Assert.Contains("pointerClickHandler", result.DispatchedEvents);
            yield return null;
        }

        sealed class TestClickHandler : MonoBehaviour, IPointerClickHandler
        {
            public int ClickCount { get; private set; }

            public void OnPointerClick(PointerEventData eventData)
                => ClickCount++;
        }
    }
}
