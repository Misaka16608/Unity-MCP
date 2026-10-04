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
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    internal static class DeliveryStatusView
    {
        internal static void Attach(VisualElement root)
        {
            const string name = "package-delivery-status";
            root.Q<Label>(name)?.RemoveFromHierarchy();
            var label = new Label(PackageDeliveryStatus.Text) { name = name };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginLeft = 8;
            label.style.marginRight = 8;
            root.Insert(0, label);
            label.schedule.Execute(() => label.text = PackageDeliveryStatus.Text).Every(250);
        }
    }
}
