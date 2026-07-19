// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Automation.Peers;

namespace CoreIsland.Controls;

public partial class TitleBarAutomationPeer(TitleBar owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TitleBar;

    protected override string GetClassNameCore() => typeof(TitleBar).FullName ?? nameof(TitleBar);

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) && Owner is TitleBar titleBar ? titleBar.Title : name;
    }
}
