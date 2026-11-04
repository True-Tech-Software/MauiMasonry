using System.Collections.ObjectModel;
using MauiMasonry.Core;

namespace MauiMasonry.Tests;

public class MasonryViewTests
{
    private sealed record SampleItem(string Title, double AspectRatio) : IMasonryViewItem;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void ColumnCount_OutOfRange_ThrowsArgumentOutOfRangeException(int invalidColumnCount)
    {
        var view = new MasonryView();

        Assert.Throws<ArgumentOutOfRangeException>(() => view.ColumnCount = invalidColumnCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SetValue(MasonryView.ColumnCountProperty, invalidColumnCount));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ColumnCount_ValidValues_UpdatesProperty(int columnCount)
    {
        var view = new MasonryView { ColumnCount = columnCount };

        Assert.Equal(columnCount, view.ColumnCount);
    }

    [Fact]
    public void ItemsSource_ItemWithInvalidAspectRatio_ThrowsArgumentOutOfRangeException()
    {
        var view = new MasonryView();
        var items = new[] { new SampleItem("Bad", 0) };

        Assert.Throws<ArgumentOutOfRangeException>(() => view.ItemsSource = items);
    }

    [Fact]
    public void Virtualization_RealizesOnlyBufferedViewportAndRecyclesOnScroll()
    {
        int templateCreationCount = 0;
        var items = Enumerable.Range(0, 60)
            .Select(i => new SampleItem($"Item {i}", 1.0))
            .ToList();

        var view = new MasonryView
        {
            ColumnCount = 2,
            ColumnSpacing = 10,
            RowSpacing = 10,
            ItemTemplate = new DataTemplate(() =>
            {
                templateCreationCount++;
                return new Border();
            }),
            ItemsSource = items
        };

        // Viewport width = 210 -> columnWidth = 100 -> each item is 100x100, row step = 110.
        // Viewport height = 200, scrollY = 0, buffer = 1.0x (range 0..400) -> rows 0..3 (8 items).
        view.UpdateViewport(width: 210, height: 200, scrollY: 0);

        Assert.Equal(60, view.CurrentLayout.Placements.Count);
        Assert.Equal(8, view.RealizedViewCount);
        Assert.Equal(8, templateCreationCount);

        // Scroll deep into the list (scrollY = 1500, buffered range = 1300..1900 -> rows 11..17 = 14 items).
        // The 8 views from the top are recycled and only 6 additional views are created (14 total instead of 60).
        view.UpdateViewport(width: 210, height: 200, scrollY: 1500);

        Assert.Equal(14, view.RealizedViewCount);
        Assert.Equal(14, templateCreationCount);

        // Scroll further down (scrollY = 2200, buffered range = 2000..2600 -> rows 18..23 = 12 items).
        // All 12 views come from the recycle pool -> zero new template instantiations!
        view.UpdateViewport(width: 210, height: 200, scrollY: 2200);

        Assert.Equal(12, view.RealizedViewCount);
        Assert.Equal(14, templateCreationCount);
    }

    [Fact]
    public void ObservableCollection_AddAndClear_UpdatesLayoutAutomatically()
    {
        var collection = new ObservableCollection<SampleItem>
        {
            new("One", 1.0),
            new("Two", 0.5)
        };

        var view = new MasonryView
        {
            ColumnCount = 2,
            ColumnSpacing = 10,
            RowSpacing = 10,
            ItemTemplate = new DataTemplate(() => new Border()),
            ItemsSource = collection
        };

        view.UpdateViewport(width: 210, height: 400, scrollY: 0);
        Assert.Equal(2, view.CurrentLayout.Placements.Count);
        Assert.Equal(2, view.RealizedViewCount);

        collection.Add(new SampleItem("Three", 1.0));
        Assert.Equal(3, view.CurrentLayout.Placements.Count);
        Assert.Equal(3, view.RealizedViewCount);

        collection.Clear();
        Assert.Empty(view.CurrentLayout.Placements);
        Assert.Equal(0, view.RealizedViewCount);
    }
}
