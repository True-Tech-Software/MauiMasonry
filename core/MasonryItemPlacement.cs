namespace MauiMasonry.Core;

/// <summary>
/// Represents the computed position and dimensions of a single item in a masonry layout.
/// </summary>
/// <param name="Index">The zero-based index of the item in the source collection.</param>
/// <param name="ColumnIndex">The zero-based index of the column the item was placed into.</param>
/// <param name="X">The horizontal offset of the item in device-independent units.</param>
/// <param name="Y">The vertical offset of the item in device-independent units.</param>
/// <param name="Width">The width of the item in device-independent units.</param>
/// <param name="Height">The height of the item in device-independent units.</param>
public readonly record struct MasonryItemPlacement(
    int Index,
    int ColumnIndex,
    double X,
    double Y,
    double Width,
    double Height)
{
    /// <summary>
    /// Gets the bottom vertical coordinate (<c>Y + Height</c>) of the item.
    /// </summary>
    public double Bottom => Y + Height;

    /// <summary>
    /// Gets the right horizontal coordinate (<c>X + Width</c>) of the item.
    /// </summary>
    public double Right => X + Width;

    /// <summary>
    /// Determines whether this item intersects the specified vertical viewport range.
    /// </summary>
    /// <param name="top">The top coordinate of the range.</param>
    /// <param name="bottom">The bottom coordinate of the range.</param>
    /// <returns><see langword="true"/> if the item overlaps the vertical range; otherwise, <see langword="false"/>.</returns>
    public bool IntersectsVerticalRange(double top, double bottom) =>
        Bottom >= top && Y <= bottom;
}
