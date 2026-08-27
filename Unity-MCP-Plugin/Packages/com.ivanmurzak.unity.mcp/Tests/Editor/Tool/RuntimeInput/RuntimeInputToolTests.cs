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
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class RuntimeInputToolTests : BaseTest
    {
        GameObject _eventSystem = null!;
        GameObject _canvas = null!;
        GameObject _button = null!;
        TestClickHandler _handler = null!;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            _eventSystem = new GameObject("McpEventSystem", typeof(EventSystem));
            _canvas = new GameObject("McpCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            _canvas.GetComponent<Canvas>()!.renderMode = RenderMode.ScreenSpaceOverlay;

            _button = new GameObject("McpButton", typeof(RectTransform), typeof(Image), typeof(TestClickHandler));
            _button.transform.SetParent(_canvas.transform, false);
            var buttonRect = _button.GetComponent<RectTransform>()!;
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.zero;
            buttonRect.pivot = Vector2.zero;
            buttonRect.anchoredPosition = new Vector2(100f, 100f);
            buttonRect.sizeDelta = new Vector2(200f, 100f);
            _button.GetComponent<Image>()!.raycastTarget = true;
            _handler = _button.GetComponent<TestClickHandler>()!;

            yield return null;
        }

        [UnityTest]
        public IEnumerator FindClickable_ReturnsScreenRectAndRecommendedPoint()
        {
            var tool = new Tool_RuntimeInput();
            var result = tool.FindClickable(objectName: "McpButton", eventSystemInstanceId: _eventSystem.GetComponent<EventSystem>()!.GetInstanceID());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(1, result.Matches.Count);
            Assert.IsNotNull(result.Matches[0].ScreenRect);
            Assert.IsNotNull(result.Matches[0].RecommendedPoint);
            Assert.AreEqual("McpButton", result.Matches[0].Name);
            Assert.That(result.Matches[0].RecommendedPoint!.X, Is.EqualTo(200f).Within(1f));
            Assert.That(result.Matches[0].RecommendedPoint!.Y, Is.EqualTo(150f).Within(1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HitTest_PointReturnsTopmostClickHandler()
        {
            var tool = new Tool_RuntimeInput();
            var result = tool.HitTest(
                point: new RuntimeInputPoint { X = 150f, Y = 150f },
                eventSystemInstanceId: _eventSystem.GetComponent<EventSystem>()!.GetInstanceID());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsNotNull(result.PrimaryHit);
            Assert.AreEqual("McpButton", result.PrimaryHit!.Name);
            Assert.IsNotEmpty(result.Raycasts);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HitTest_RectReturnsIntersectingCandidates()
        {
            var secondButton = new GameObject("McpSecondButton", typeof(RectTransform), typeof(Image), typeof(TestClickHandler));
            secondButton.transform.SetParent(_canvas.transform, false);
            var secondRect = secondButton.GetComponent<RectTransform>()!;
            secondRect.anchorMin = Vector2.zero;
            secondRect.anchorMax = Vector2.zero;
            secondRect.pivot = Vector2.zero;
            secondRect.anchoredPosition = new Vector2(250f, 100f);
            secondRect.sizeDelta = new Vector2(150f, 100f);
            secondButton.GetComponent<Image>()!.raycastTarget = true;
            yield return null;

            var tool = new Tool_RuntimeInput();
            var result = tool.HitTest(
                rect: new RuntimeInputRect { X = 50f, Y = 50f, Width = 450f, Height = 200f },
                eventSystemInstanceId: _eventSystem.GetComponent<EventSystem>()!.GetInstanceID());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(2, result.Candidates.Count);
            Assert.That(result.Candidates.Exists(candidate => candidate.Name == "McpButton"));
            Assert.That(result.Candidates.Exists(candidate => candidate.Name == "McpSecondButton"));
            Assert.IsNotNull(result.Candidates[0].IntersectionRect);
            Assert.That(result.Candidates[0].OverlapRatio, Is.GreaterThan(0f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HitTest_OverlayIsReportedAsOccluder()
        {
            var overlay = new GameObject("McpOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(_canvas.transform, false);
            var overlayRect = overlay.GetComponent<RectTransform>()!;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.zero;
            overlayRect.pivot = Vector2.zero;
            overlayRect.anchoredPosition = new Vector2(100f, 100f);
            overlayRect.sizeDelta = new Vector2(200f, 100f);
            overlay.GetComponent<Image>()!.raycastTarget = true;
            yield return null;

            var tool = new Tool_RuntimeInput();
            var result = tool.HitTest(
                point: new RuntimeInputPoint { X = 150f, Y = 150f },
                eventSystemInstanceId: _eventSystem.GetComponent<EventSystem>()!.GetInstanceID());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsNull(result.PrimaryHit);
            Assert.IsNotEmpty(result.Occluders);
            Assert.AreEqual("McpOverlay", result.Occluders[0].Name);
        }

        [UnityTest]
        public IEnumerator Click_RequiresPlayMode()
        {
            var tool = new Tool_RuntimeInput();
            var result = tool.Click(
                new RuntimeInputPoint { X = 150f, Y = 150f },
                eventSystemInstanceId: _eventSystem.GetComponent<EventSystem>()!.GetInstanceID());

            Assert.IsFalse(result.Success);
            Assert.AreEqual("PLAY_MODE_REQUIRED", result.ErrorCode);
            Assert.IsFalse(result.Dispatched);
            Assert.AreEqual(0, _handler.ClickCount);
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
