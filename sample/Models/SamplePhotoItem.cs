using MauiMasonry.Core;

namespace MauiMasonry.Sample.Models;

public sealed class SamplePhotoItem : IMasonryViewItem
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Category { get; init; }

    public required double AspectRatio { get; init; }

    public required Color CardColor { get; init; }

    public required Color BadgeColor { get; init; }

    public string AspectLabel => $"#{Id} • AR {AspectRatio:0.00}";
}
