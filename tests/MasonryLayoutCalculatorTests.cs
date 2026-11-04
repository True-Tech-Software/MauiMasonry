using MauiMasonry.Core;

namespace MauiMasonry.Tests;

public class MasonryLayoutCalculatorTests
{
    private sealed record TestMasonryItem(double AspectRatio) : IMasonryViewItem;

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Calculate_InvalidColumnCount_ThrowsArgumentOutOfRangeException(int invalidColumnCount)
    {
        var items = new[] { new TestMasonryItem(1.0) };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MasonryLayoutCalculator.Calculate(items, availableWidth: 200, columnCount: invalidColumnCount));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ValidateColumnCount_ValidRange_DoesNotThrow(int validColumnCount)
    {
        Exception? exception = Record.Exception(() => MasonryLayoutCalculator.ValidateColumnCount(validColumnCount));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_InvalidItemAspectRatio_ThrowsArgumentOutOfRangeException(double invalidAspectRatio)
    {
        var items = new[] { new TestMasonryItem(invalidAspectRatio) };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MasonryLayoutCalculator.Calculate(items, availableWidth: 200, columnCount: 2));
    }

    [Fact]
    public void Calculate_PlacesItemsIntoShortestColumnFirstWithLeftToRightTieBreak()
    {
        // AvailableWidth = 210, ColumnCount = 2, ColumnSpacing = 10 -> ColumnWidth = 100
        // Item 0: AR = 0.5 -> Height = 200 -> Col 0 (Y = 0..200)
        // Item 1: AR = 1.0 -> Height = 100 -> Col 1 (Y = 0..100)
        // Item 2: AR = 2.0 -> Height = 50  -> Col 1 is shorter (100 < 200) -> Col 1 (Y = 110..160)
        // Item 3: AR = 1.0 -> Height = 100 -> Col 1 is still shorter (160 < 200) -> Col 1 (Y = 170..270)
        // Item 4: AR = 1.0 -> Height = 100 -> Col 0 is now shorter (200 < 270) -> Col 0 (Y = 210..310)
        var items = new[]
        {
            new TestMasonryItem(0.5),
            new TestMasonryItem(1.0),
            new TestMasonryItem(2.0),
            new TestMasonryItem(1.0),
            new TestMasonryItem(1.0),
        };

        MasonryLayoutResult result = MasonryLayoutCalculator.Calculate(
            items,
            availableWidth: 210,
            columnCount: 2,
            columnSpacing: 10,
            rowSpacing: 10);

        Assert.Equal(5, result.Placements.Count);
        Assert.Equal(210, result.TotalWidth);
        Assert.Equal(310, result.TotalHeight);

        Assert.Equal(new MasonryItemPlacement(0, ColumnIndex: 0, X: 0, Y: 0, Width: 100, Height: 200), result.Placements[0]);
        Assert.Equal(new MasonryItemPlacement(1, ColumnIndex: 1, X: 110, Y: 0, Width: 100, Height: 100), result.Placements[1]);
        Assert.Equal(new MasonryItemPlacement(2, ColumnIndex: 1, X: 110, Y: 110, Width: 100, Height: 50), result.Placements[2]);
        Assert.Equal(new MasonryItemPlacement(3, ColumnIndex: 1, X: 110, Y: 170, Width: 100, Height: 100), result.Placements[3]);
        Assert.Equal(new MasonryItemPlacement(4, ColumnIndex: 0, X: 0, Y: 210, Width: 100, Height: 100), result.Placements[4]);
    }

    [Fact]
    public void Calculate_EqualColumnHeights_TieBreaksLeftToRight()
    {
        var items = new[]
        {
            new TestMasonryItem(1.0),
            new TestMasonryItem(1.0),
            new TestMasonryItem(1.0),
            new TestMasonryItem(1.0),
        };

        MasonryLayoutResult result = MasonryLayoutCalculator.Calculate(
            items,
            availableWidth: 320,
            columnCount: 3,
            columnSpacing: 10,
            rowSpacing: 10);

        Assert.Equal(0, result.Placements[0].ColumnIndex);
        Assert.Equal(1, result.Placements[1].ColumnIndex);
        Assert.Equal(2, result.Placements[2].ColumnIndex);
        Assert.Equal(0, result.Placements[3].ColumnIndex);
    }

    [Fact]
    public void GetVisiblePlacements_ReturnsOnlyPlacementsIntersectingBufferedViewport()
    {
        // 20 square items in 2 columns (width 100 each, spacing 0, rowSpacing 10):
        // Each row is 100px tall + 10px spacing -> row r starts at r * 110
        var items = Enumerable.Range(0, 20).Select(_ => new TestMasonryItem(1.0)).ToList();
        MasonryLayoutResult layout = MasonryLayoutCalculator.Calculate(items, availableWidth: 200, columnCount: 2, columnSpacing: 0, rowSpacing: 10);

        // Viewport at scrollY = 300, height = 100, bufferScreens = 0 (strictly 300..400):
        // Row 2: Y = 220..320 (intersects 300..400) -> indices 4, 5
        // Row 3: Y = 330..430 (intersects 300..400) -> indices 6, 7
        IReadOnlyList<MasonryItemPlacement> visible = layout.GetVisiblePlacements(scrollY: 300, viewportHeight: 100, bufferScreens: 0);

        Assert.Equal([4, 5, 6, 7], visible.Select(p => p.Index).ToArray());
    }
}
