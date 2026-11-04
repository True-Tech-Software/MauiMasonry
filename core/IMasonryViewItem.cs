namespace MauiMasonry.Core;

/// <summary>
/// Represents an item displayed within a <c>MasonryView</c> with a known width-to-height aspect ratio.
/// </summary>
public interface IMasonryViewItem
{
    /// <summary>
    /// Gets the width-to-height aspect ratio (<c>Width / Height</c>) of the item.
    /// Must be a finite number strictly greater than zero.
    /// </summary>
    double AspectRatio { get; }
}
