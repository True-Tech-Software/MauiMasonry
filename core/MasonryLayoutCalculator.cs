namespace MauiMasonry.Core;

/// <summary>
/// Deterministic layout calculator for staggered masonry grids using a greedy shortest-column-first algorithm.
/// </summary>
public static class MasonryLayoutCalculator
{
    /// <summary>
    /// The minimum allowed column count (<c>2</c>).
    /// </summary>
    public const int MinColumnCount = 2;

    /// <summary>
    /// The maximum allowed column count (<c>4</c>).
    /// </summary>
    public const int MaxColumnCount = 4;

    /// <summary>
    /// Validates that the specified column count is within the allowed range (<see cref="MinColumnCount"/> to <see cref="MaxColumnCount"/>).
    /// </summary>
    /// <param name="columnCount">The column count to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="columnCount"/> is less than 2 or greater than 4.</exception>
    public static void ValidateColumnCount(int columnCount)
    {
        if (columnCount < MinColumnCount || columnCount > MaxColumnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columnCount),
                columnCount,
                $"ColumnCount must be between {MinColumnCount} and {MaxColumnCount} (inclusive).");
        }
    }

    /// <summary>
    /// Validates that the specified aspect ratio is finite and strictly greater than zero.
    /// </summary>
    /// <param name="aspectRatio">The aspect ratio (<c>Width / Height</c>) to validate.</param>
    /// <param name="paramName">The parameter name to include in the exception if validation fails.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="aspectRatio"/> is less than or equal to 0, NaN, or infinite.</exception>
    public static void ValidateAspectRatio(double aspectRatio, string paramName = "aspectRatio")
    {
        if (double.IsNaN(aspectRatio) || double.IsInfinity(aspectRatio) || aspectRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                aspectRatio,
                "AspectRatio must be a finite number strictly greater than zero.");
        }
    }

    /// <summary>
    /// Computes the masonry layout placements for a sequence of <see cref="IMasonryViewItem"/> items.
    /// Places each item into the currently shortest column, breaking ties from left to right.
    /// </summary>
    /// <param name="items">The items to arrange.</param>
    /// <param name="availableWidth">The total horizontal width available for the grid.</param>
    /// <param name="columnCount">The number of columns (must be between 2 and 4).</param>
    /// <param name="columnSpacing">The horizontal spacing between adjacent columns.</param>
    /// <param name="rowSpacing">The vertical spacing between stacked items in the same column.</param>
    /// <returns>A <see cref="MasonryLayoutResult"/> containing all item placements and total dimensions.</returns>
    public static MasonryLayoutResult Calculate(
        IEnumerable<IMasonryViewItem>? items,
        double availableWidth,
        int columnCount,
        double columnSpacing = 0,
        double rowSpacing = 0)
    {
        ValidateColumnCount(columnCount);

        if (double.IsNaN(columnSpacing) || double.IsInfinity(columnSpacing) || columnSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnSpacing), columnSpacing, "ColumnSpacing must be a finite non-negative number.");
        }

        if (double.IsNaN(rowSpacing) || double.IsInfinity(rowSpacing) || rowSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowSpacing), rowSpacing, "RowSpacing must be a finite non-negative number.");
        }

        if (items is null || availableWidth <= 0 || double.IsNaN(availableWidth) || double.IsInfinity(availableWidth))
        {
            return MasonryLayoutResult.Empty;
        }

        double totalColumnSpacing = columnSpacing * (columnCount - 1);
        double columnWidth = Math.Max(0, (availableWidth - totalColumnSpacing) / columnCount);
        if (columnWidth <= 0)
        {
            return MasonryLayoutResult.Empty;
        }

        var columnHeights = new double[columnCount];
        var placements = new List<MasonryItemPlacement>();

        int index = 0;
        foreach (IMasonryViewItem? item in items)
        {
            if (item is null)
            {
                throw new ArgumentException($"Item at index {index} cannot be null.", nameof(items));
            }

            ValidateAspectRatio(item.AspectRatio, $"{nameof(items)}[{index}].{nameof(IMasonryViewItem.AspectRatio)}");

            // Find the shortest column, breaking ties left-to-right (strict '<' keeps earlier column on tie).
            int targetColumn = 0;
            double minColumnHeight = columnHeights[0];
            for (int col = 1; col < columnCount; col++)
            {
                if (columnHeights[col] < minColumnHeight)
                {
                    minColumnHeight = columnHeights[col];
                    targetColumn = col;
                }
            }

            double x = targetColumn * (columnWidth + columnSpacing);
            double y = minColumnHeight > 0 ? minColumnHeight + rowSpacing : 0;
            double itemHeight = columnWidth / item.AspectRatio;

            placements.Add(new MasonryItemPlacement(
                Index: index,
                ColumnIndex: targetColumn,
                X: x,
                Y: y,
                Width: columnWidth,
                Height: itemHeight));

            columnHeights[targetColumn] = y + itemHeight;
            index++;
        }

        if (placements.Count == 0)
        {
            return MasonryLayoutResult.Empty;
        }

        double totalHeight = 0;
        for (int col = 0; col < columnCount; col++)
        {
            if (columnHeights[col] > totalHeight)
            {
                totalHeight = columnHeights[col];
            }
        }

        return new MasonryLayoutResult(placements, availableWidth, totalHeight);
    }
}
