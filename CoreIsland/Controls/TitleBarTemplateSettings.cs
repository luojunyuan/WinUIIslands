// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CoreIsland.Controls;

public partial class TitleBarTemplateSettings : DependencyObject
{
    public static readonly DependencyProperty IconElementProperty = DependencyProperty.Register(
        nameof(IconElement),
        typeof(IconElement),
        typeof(TitleBarTemplateSettings),
        new PropertyMetadata(null));

    public IconElement? IconElement
    {
        get => (IconElement?)GetValue(IconElementProperty);
        internal set => SetValue(IconElementProperty, value);
    }
}
