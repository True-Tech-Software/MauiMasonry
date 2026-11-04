namespace MauiMasonry.Core;

/// <summary>
/// Represents the complete computed result of a masonry grid layout pass.
/// </summary>
public sealed class MasonryLayoutResult
{
    private readonly MasonryItemPlacement[][] _placementsByColumn;

    /// <summary>
    /// Gets an empty layout result with zero items and zero height.
    /// </summary>
    public static MasonryLayoutResult Empty { get; } = new([], 0, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="MasonryLayoutResult"/> class.
    /// </summary>
    /// <param name="placements">The computed placements for each item in index order.</param>
    /// <param name="totalWidth">The total available width used for the layout.</param>
    /// <param name="totalHeight">The total vertical height of the tallest column.</param>
    public MasonryLayoutResult(
        IReadOnlyList<MasonryItemPlacement> placements,
        double totalWidth,
        double totalHeight)
    {
        ArgumentNullException.ThrowIfNull(placements);
        Placements = placements;
        TotalWidth = totalWidth;
        TotalHeight = totalHeight;
        _placementsByColumn = BuildColumnIndex(placements);
    }

    /// <summary>
    /// Gets the computed placements for all items in the layout.
    /// </summary>
    public IReadOnlyList<MasonryItemPlacement> Placements { get; }

    /// <summary>
    /// Gets the total width of the masonry layout in device-independent units.
    /// </summary>
    public double TotalWidth { get; }

    /// <summary>
    /// Gets the total height of the masonry layout (the height of the tallest column) in device-independent units.
    /// </summary>
    public double TotalHeight { get; }

    /// <summary>
    /// Returns all item placements that intersect the visible viewport expanded by the specified buffer multiplier.
    /// Uses per-column binary search so scrolling visits only the intersecting items.
    /// </summary>
    /// <param name="scrollY">The current vertical scroll offset.</param>
    /// <param name="viewportHeight">The height of the visible viewport.</param>
    /// <param name="bufferScreens">
    /// The number of viewport heights to include above and below the visible area (defaults to <c>1.0</c>).
    /// </param>
    /// <returns>A read-only list of placements intersecting the buffered viewport, ordered by <see cref="MasonryItemPlacement.Index"/>.</returns>
    public IReadOnlyList<MasonryItemPlacement> GetVisiblePlacements(
        double scrollY,
        double viewportHeight,
        double bufferScreens = 1.0)
    {
        if (Placements.Count == 0 || viewportHeight <= 0 || double.IsNaN(viewportHeight))
        {
            return [];
        }

        double clampedBuffer = Math.Max(0, bufferScreens) * viewportHeight;
        double rangeTop = Math.Max(0, scrollY - clampedBuffer);
        double rangeBottom = Math.Max(rangeTop, scrollY + viewportHeight + clampedBuffer);

        var visible = new List<MasonryItemPlacement>();
        for (int col = 0; col < _placementsByColumn.Length; col++)
        {
            MasonryItemPlacement[] columnPlacements = _placementsByColumn[col];
            if (columnPlacements.Length == 0)
            {
                continue;
            }

            int startIdx = FindFirstIntersectingIndex(columnPlacements, rangeTop);
            for (int i = startIdx; i < columnPlacements.Length; i++)
            {
                MasonryItemPlacement placement = columnPlacements[i];
                if (placement.Y > rangeBottom)
                {
                    break;
                }

                visible.Add(placement);
            }
        }

        visible.Sort(static (a, b) => a.Index.CompareTo(b.Index));
        return visible;
    }

    private static MasonryItemPlacement[][] BuildColumnIndex(IReadOnlyList<MasonryItemPlacement> placements)
    {
        if (placements.Count == 0)
        {
            return [];
        }

        int maxColumn = 0;
        for (int i = 0; i < placements.Count; i++)
        {
            if (placements[i].ColumnIndex > maxColumn)
            {
                maxColumn = placements[i].ColumnIndex;
            }
        }

        var buckets = new List<MasonryItemPlacement>[maxColumn + 1];
        for (int col = 0; col <= maxColumn; col++)
        {
            buckets[col] = [];
        }

        for (int i = 0; i < placements.Count; i++)
        {
            MasonryItemPlacement placement = placements[i];
            buckets[placement.ColumnIndex].Add(placement);
        }

        var result = new MasonryItemPlacement[maxColumn + 1][];
        for (int col = 0; col <= maxColumn; col++)
        {
            result[col] = [.. buckets[col]];
        }

        return result;
    }

    private static int FindFirstIntersectingIndex(MasonryItemPlacement[] columnPlacements, double rangeTop)
    {
        int low = 0;
        int high = columnPlacements.Length - 1;
        int result = columnPlacements.Length;

        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            if (columnPlacements[mid].Bottom >= rangeTop)
            {
                result = mid;
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        return result;
    }
}
