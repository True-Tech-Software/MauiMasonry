using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace MauiMasonry;

/// <summary>
/// Internal layout surface used by <see cref="MasonryView"/> to position only realized (visible + buffered)
/// item views at their pre-calculated coordinates while maintaining the full scrollable content height.
/// </summary>
internal sealed class MasonryCanvas : Layout
{
    private readonly Dictionary<IView, Rect> _boundsByView = [];

    /// <summary>
    /// Gets or sets the total computed height of the masonry layout across all items.
    /// </summary>
    public double TotalContentHeight { get; set; }

    /// <summary>
    /// Assigns the layout rectangle for a realized child view.
    /// </summary>
    public void SetLayoutBounds(View view, Rect bounds)
    {
        _boundsByView[view] = bounds;
    }

    /// <summary>
    /// Removes any stored layout rectangle for a child view.
    /// </summary>
    public void ClearLayoutBounds(View view)
    {
        _boundsByView.Remove(view);
    }

    /// <summary>
    /// Retrieves the currently assigned layout bounds for a realized child view, if any.
    /// </summary>
    public bool TryGetLayoutBounds(IView view, out Rect bounds) =>
        _boundsByView.TryGetValue(view, out bounds);

    /// <summary>
    /// Triggers a measure and arrange pass on the canvas.
    /// </summary>
    public void RequestRelayout() => InvalidateMeasure();

    /// <inheritdoc />
    protected override ILayoutManager CreateLayoutManager() => new MasonryCanvasLayoutManager(this);

    private sealed class MasonryCanvasLayoutManager(MasonryCanvas canvas) : LayoutManager(canvas)
    {
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            for (int i = 0; i < canvas.Count; i++)
            {
                IView child = canvas[i];
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (canvas.TryGetLayoutBounds(child, out Rect bounds))
                {
                    child.Measure(bounds.Width, bounds.Height);
                }
            }

            double width = double.IsInfinity(widthConstraint) || double.IsNaN(widthConstraint)
                ? 0
                : Math.Max(0, widthConstraint);

            return new Size(width, Math.Max(0, canvas.TotalContentHeight));
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            for (int i = 0; i < canvas.Count; i++)
            {
                IView child = canvas[i];
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (canvas.TryGetLayoutBounds(child, out Rect itemBounds))
                {
                    child.Arrange(itemBounds);
                }
            }

            return new Size(bounds.Width, Math.Max(bounds.Height, canvas.TotalContentHeight));
        }
    }
}
