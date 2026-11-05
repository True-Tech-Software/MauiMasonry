# MauiMasonry

[![CI & Publish NuGet Packages](https://github.com/True-Tech-Software/MauiMasonry/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/True-Tech-Software/MauiMasonry/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/TrueTechSoftware.MauiMasonry.svg?label=TrueTechSoftware.MauiMasonry)](https://www.nuget.org/packages/TrueTechSoftware.MauiMasonry)
[![NuGet Core](https://img.shields.io/nuget/v/TrueTechSoftware.MauiMasonry.Core.svg?label=TrueTechSoftware.MauiMasonry.Core)](https://www.nuget.org/packages/TrueTechSoftware.MauiMasonry.Core)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

A lightweight, performant .NET MAUI control for building Pinterest-style staggered masonry grids on Android and iOS. Features deterministic aspect-ratio sizing, shortest-column-first placement, configurable column counts (2–4), smooth scrolling, and viewport item virtualization with view recycling.

## Packages

| Package | Target Frameworks | Description |
| --- | --- | --- |
| **`TrueTechSoftware.MauiMasonry.Core`** | `net10.0` | Pure .NET library containing `IMasonryViewItem` and `MasonryLayoutCalculator` so DTOs and ViewModels can implement item contracts without a .NET MAUI UI dependency. |
| **`TrueTechSoftware.MauiMasonry`** | `net10.0` | The `.NET MAUI` `MasonryView` control with `DataTemplate` support, empty-state templates, and viewport virtualization. |

## Quick Start

### 1. Implement `IMasonryViewItem` on your data model

```csharp
using MauiMasonry.Core;

public class PhotoDto : IMasonryViewItem
{
    public required string Title { get; set; }
    public string? ThumbnailUrl { get; set; }

    // Width / Height ratio (must be finite and > 0)
    public required double AspectRatio { get; set; }
}
```

### 2. Use `MasonryView` in XAML

```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:masonry="http://schemas.truetechsoftware.com/dotnet/2026/mauimasonry">

    <masonry:MasonryView
        ItemsSource="{Binding Results}"
        ColumnCount="2"
        ColumnSpacing="10"
        RowSpacing="10">

        <masonry:MasonryView.ItemTemplate>
            <DataTemplate x:DataType="models:PhotoDto">
                <Border Padding="0">
                    <Image Aspect="AspectFill" Source="{Binding ThumbnailUrl}" />
                </Border>
            </DataTemplate>
        </masonry:MasonryView.ItemTemplate>

        <masonry:MasonryView.EmptyViewTemplate>
            <DataTemplate>
                <Label Text="No items found." HorizontalOptions="Center" VerticalOptions="Center" />
            </DataTemplate>
        </masonry:MasonryView.EmptyViewTemplate>

    </masonry:MasonryView>
</ContentPage>
```

## Bindable Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ItemsSource` | `IEnumerable` | `null` | Collection of `IMasonryViewItem` items (supports `INotifyCollectionChanged`). |
| `ItemTemplate` | `DataTemplate` | `null` | `DataTemplate` or `DataTemplateSelector` used to render each item inside its aspect-ratio box. |
| `EmptyView` | `object` | `null` | View or data displayed when `ItemsSource` is null or empty. |
| `EmptyViewTemplate` | `DataTemplate` | `null` | `DataTemplate` used to render the empty state. |
| `ColumnCount` | `int` | `2` | Number of columns (`2` to `4` inclusive; throws `ArgumentOutOfRangeException` outside range). |
| `ColumnSpacing` | `double` | `10.0` | Horizontal spacing between columns in device-independent units. |
| `RowSpacing` | `double` | `10.0` | Vertical spacing between stacked items in a column in device-independent units. |

