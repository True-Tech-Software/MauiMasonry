using System.Collections;
using System.Collections.Specialized;
using MauiMasonry.Core;
using Microsoft.Maui.Graphics;

namespace MauiMasonry;

/// <summary>
/// A lightweight, performant .NET MAUI control for building Pinterest-style staggered masonry grids
/// with deterministic aspect-ratio sizing and viewport item virtualization.
/// </summary>
public class MasonryView : ContentView
{
    private const double DefaultBufferScreens = 1.0;

    /// <summary>
    /// Bindable property for <see cref="ItemsSource"/>.
    /// </summary>
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(MasonryView),
        defaultValue: null,
        propertyChanged: OnItemsSourcePropertyChanged);

    /// <summary>
    /// Bindable property for <see cref="ItemTemplate"/>.
    /// </summary>
    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(MasonryView),
        defaultValue: null,
        propertyChanged: OnItemTemplatePropertyChanged);

    /// <summary>
    /// Bindable property for <see cref="EmptyView"/>.
    /// </summary>
    public static readonly BindableProperty EmptyViewProperty = BindableProperty.Create(
        nameof(EmptyView),
        typeof(object),
        typeof(MasonryView),
        defaultValue: null,
        propertyChanged: OnEmptyViewPropertyChanged);

    /// <summary>
    /// Bindable property for <see cref="EmptyViewTemplate"/>.
    /// </summary>
    public static readonly BindableProperty EmptyViewTemplateProperty = BindableProperty.Create(
        nameof(EmptyViewTemplate),
        typeof(DataTemplate),
        typeof(MasonryView),
        defaultValue: null,
        propertyChanged: OnEmptyViewPropertyChanged);

    /// <summary>
    /// Bindable property for <see cref="ColumnCount"/>. Validated to be between 2 and 4 (inclusive).
    /// </summary>
    public static readonly BindableProperty ColumnCountProperty = BindableProperty.Create(
        nameof(ColumnCount),
        typeof(int),
        typeof(MasonryView),
        defaultValue: 2,
        validateValue: ValidateColumnCountValue,
        propertyChanged: OnLayoutParameterChanged);

    /// <summary>
    /// Bindable property for <see cref="ColumnSpacing"/>.
    /// </summary>
    public static readonly BindableProperty ColumnSpacingProperty = BindableProperty.Create(
        nameof(ColumnSpacing),
        typeof(double),
        typeof(MasonryView),
        defaultValue: 10.0,
        validateValue: ValidateNonNegativeSpacing,
        propertyChanged: OnLayoutParameterChanged);

    /// <summary>
    /// Bindable property for <see cref="RowSpacing"/>.
    /// </summary>
    public static readonly BindableProperty RowSpacingProperty = BindableProperty.Create(
        nameof(RowSpacing),
        typeof(double),
        typeof(MasonryView),
        defaultValue: 10.0,
        validateValue: ValidateNonNegativeSpacing,
        propertyChanged: OnLayoutParameterChanged);

    private readonly Grid _rootGrid;
    private readonly ScrollView _scrollView;
    private readonly MasonryCanvas _canvas;
    private readonly ContentView _emptyViewContainer;

    private readonly List<IMasonryViewItem> _items = [];
    private readonly Dictionary<int, RealizedSlot> _realizedByIndex = [];
    private readonly Dictionary<DataTemplate, Stack<View>> _recyclePoolByTemplate = [];

    private INotifyCollectionChanged? _subscribedCollection;
    private MasonryLayoutResult _currentLayout = MasonryLayoutResult.Empty;
    private double _lastViewportWidth;
    private double _lastViewportHeight;
    private double _currentScrollY;

    /// <summary>
    /// Initializes a new instance of the <see cref="MasonryView"/> class.
    /// </summary>
    public MasonryView()
    {
        _canvas = new MasonryCanvas
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Start
        };

        _scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Content = _canvas
        };
        _scrollView.Scrolled += OnScrollViewScrolled;
        _scrollView.SizeChanged += OnViewportSizeChanged;

        _emptyViewContainer = new ContentView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            IsVisible = false
        };

        _rootGrid = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Children = { _scrollView, _emptyViewContainer }
        };

        base.Content = _rootGrid;
        SizeChanged += OnViewportSizeChanged;
        Loaded += OnMasonryViewLoaded;
        Unloaded += OnMasonryViewUnloaded;

        UpdateEmptyViewContent();
        UpdateEmptyStateVisibility();
    }

    /// <summary>
    /// Gets or sets the collection of <see cref="IMasonryViewItem"/> items to display.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="DataTemplate"/> (or <see cref="DataTemplateSelector"/>) used to render each item.
    /// </summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the content or binding object displayed when <see cref="ItemsSource"/> is null or empty.
    /// </summary>
    public object? EmptyView
    {
        get => GetValue(EmptyViewProperty);
        set => SetValue(EmptyViewProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="DataTemplate"/> used to render the empty state when <see cref="ItemsSource"/> is null or empty.
    /// </summary>
    public DataTemplate? EmptyViewTemplate
    {
        get => (DataTemplate?)GetValue(EmptyViewTemplateProperty);
        set => SetValue(EmptyViewTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of columns in the masonry grid. Must be between 2 and 4 (inclusive).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when set to a value less than 2 or greater than 4.</exception>
    public int ColumnCount
    {
        get => (int)GetValue(ColumnCountProperty);
        set
        {
            MasonryLayoutCalculator.ValidateColumnCount(value);
            SetValue(ColumnCountProperty, value);
        }
    }

    /// <summary>
    /// Gets or sets the horizontal spacing between columns in device-independent units.
    /// </summary>
    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "ColumnSpacing must be a finite non-negative number.");
            }

            SetValue(ColumnSpacingProperty, value);
        }
    }

    /// <summary>
    /// Gets or sets the vertical spacing between stacked items in a column in device-independent units.
    /// </summary>
    public double RowSpacing
    {
        get => (double)GetValue(RowSpacingProperty);
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "RowSpacing must be a finite non-negative number.");
            }

            SetValue(RowSpacingProperty, value);
        }
    }

    /// <summary>
    /// Gets the latest computed <see cref="MasonryLayoutResult"/> for the current items and viewport width.
    /// </summary>
    public MasonryLayoutResult CurrentLayout => _currentLayout;

    /// <summary>
    /// Gets the count of currently realized (instantiated and active) item views in the visual tree.
    /// </summary>
    internal int RealizedViewCount => _realizedByIndex.Count;

    /// <summary>
    /// Gets the internal canvas bounds count for testing cleanup.
    /// </summary>
    internal int CanvasStoredBoundsCount => _canvas.StoredBoundsCount;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is nameof(Padding) or nameof(IsVisible))
        {
            RefreshViewportFromControlSize();
        }
    }

    /// <summary>
    /// Updates the viewport dimensions and scroll position and recalculates layout and realized views.
    /// Exposed internally for deterministic testing without requiring a platform renderer.
    /// </summary>
    internal void UpdateViewport(double width, double height, double scrollY)
    {
        bool widthChanged = Math.Abs(_lastViewportWidth - width) > 0.01;
        _lastViewportWidth = width;
        _lastViewportHeight = height;
        _currentScrollY = Math.Max(0, scrollY);

        if (widthChanged || (_currentLayout.Placements.Count != _items.Count && _items.Count > 0))
        {
            RecalculateLayout();
        }
        else
        {
            UpdateRealizedViews();
        }
    }

    private static bool ValidateColumnCountValue(BindableObject bindable, object value)
    {
        if (value is not int count)
        {
            return false;
        }

        MasonryLayoutCalculator.ValidateColumnCount(count);
        return true;
    }

    private static bool ValidateNonNegativeSpacing(BindableObject bindable, object value)
    {
        if (value is not double spacing)
        {
            return false;
        }

        if (double.IsNaN(spacing) || double.IsInfinity(spacing) || spacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), spacing, "Spacing must be a finite non-negative number.");
        }

        return true;
    }

    private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MasonryView masonryView)
        {
            masonryView.OnItemsSourceChanged(oldValue as IEnumerable, newValue as IEnumerable);
        }
    }

    private static void OnItemTemplatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MasonryView masonryView)
        {
            masonryView.ClearAllRealizedAndPooledViews();
            masonryView.UpdateRealizedViews();
        }
    }

    private static void OnEmptyViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MasonryView masonryView)
        {
            masonryView.UpdateEmptyViewContent();
            masonryView.UpdateEmptyStateVisibility();
        }
    }

    private static void OnLayoutParameterChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MasonryView masonryView)
        {
            masonryView.RecalculateLayout();
        }
    }

    private void OnViewportSizeChanged(object? sender, EventArgs e)
    {
        RefreshViewportFromControlSize();
    }

    private void RefreshViewportFromControlSize()
    {
        double width = _scrollView.Width > 0 ? _scrollView.Width : Width - Padding.HorizontalThickness;
        double height = _scrollView.Height > 0 ? _scrollView.Height : Height - Padding.VerticalThickness;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        UpdateViewport(width, height, _scrollView.ScrollY);
    }

    private void OnMasonryViewLoaded(object? sender, EventArgs e)
    {
        if (ItemsSource is INotifyCollectionChanged notifyCollection && _subscribedCollection is null)
        {
            _subscribedCollection = notifyCollection;
            _subscribedCollection.CollectionChanged += OnCollectionChanged;
            ReloadItemsFromSource();
        }
    }

    private void OnMasonryViewUnloaded(object? sender, EventArgs e)
    {
        if (_subscribedCollection is not null)
        {
            _subscribedCollection.CollectionChanged -= OnCollectionChanged;
            _subscribedCollection = null;
        }
    }

    private void OnScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        _currentScrollY = Math.Max(0, e.ScrollY);
        UpdateRealizedViews();
    }

    private void OnItemsSourceChanged(IEnumerable? oldSource, IEnumerable? newSource)
    {
        if (_subscribedCollection is not null)
        {
            _subscribedCollection.CollectionChanged -= OnCollectionChanged;
            _subscribedCollection = null;
        }

        if (newSource is INotifyCollectionChanged notifyCollection)
        {
            _subscribedCollection = notifyCollection;
            _subscribedCollection.CollectionChanged += OnCollectionChanged;
        }

        ReloadItemsFromSource();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ReloadItemsFromSource();
    }

    private void ReloadItemsFromSource()
    {
        _items.Clear();

        if (ItemsSource is not null)
        {
            int index = 0;
            foreach (object? element in ItemsSource)
            {
                if (element is not IMasonryViewItem masonryItem)
                {
                    throw new ArgumentException(
                        $"All items in {nameof(ItemsSource)} must implement {nameof(IMasonryViewItem)}. Item at index {index} was '{element?.GetType().FullName ?? "null"}'.",
                        nameof(ItemsSource));
                }

                MasonryLayoutCalculator.ValidateAspectRatio(
                    masonryItem.AspectRatio,
                    $"{nameof(ItemsSource)}[{index}].{nameof(IMasonryViewItem.AspectRatio)}");

                _items.Add(masonryItem);
                index++;
            }
        }

        RecycleAllRealizedViews();
        UpdateEmptyStateVisibility();
        RecalculateLayout();
    }

    private void RecalculateLayout()
    {
        if (_items.Count == 0 || _lastViewportWidth <= 0)
        {
            _currentLayout = MasonryLayoutResult.Empty;
            _canvas.TotalContentHeight = 0;
            _canvas.HeightRequest = 0;
            RecycleAllRealizedViews();
            _canvas.RequestRelayout();
            return;
        }

        _currentLayout = MasonryLayoutCalculator.Calculate(
            _items,
            _lastViewportWidth,
            ColumnCount,
            ColumnSpacing,
            RowSpacing);

        _canvas.TotalContentHeight = _currentLayout.TotalHeight;
        _canvas.HeightRequest = _currentLayout.TotalHeight;

        UpdateRealizedViews();
        _canvas.RequestRelayout();
    }

    private void UpdateRealizedViews()
    {
        if (_items.Count == 0 || _currentLayout.Placements.Count == 0 || _lastViewportWidth <= 0 || _lastViewportHeight <= 0)
        {
            RecycleAllRealizedViews();
            return;
        }

        if (ItemTemplate is null)
        {
            RecycleAllRealizedViews();
            return;
        }

        IReadOnlyList<MasonryItemPlacement> visiblePlacements = _currentLayout.GetVisiblePlacements(
            _currentScrollY,
            _lastViewportHeight,
            DefaultBufferScreens);

        var visibleIndices = new HashSet<int>(visiblePlacements.Count);
        for (int i = 0; i < visiblePlacements.Count; i++)
        {
            visibleIndices.Add(visiblePlacements[i].Index);
        }

        // 1. Recycle views that scrolled outside the buffered viewport.
        if (_realizedByIndex.Count > 0)
        {
            var indicesToRecycle = new List<int>();
            foreach (int existingIndex in _realizedByIndex.Keys)
            {
                if (!visibleIndices.Contains(existingIndex))
                {
                    indicesToRecycle.Add(existingIndex);
                }
            }

            for (int i = 0; i < indicesToRecycle.Count; i++)
            {
                RecycleIndex(indicesToRecycle[i]);
            }
        }

        // 2. Realize or update positions for all visible placements.
        bool anyChanged = false;
        for (int i = 0; i < visiblePlacements.Count; i++)
        {
            MasonryItemPlacement placement = visiblePlacements[i];
            IMasonryViewItem item = _items[placement.Index];
            var bounds = new Rect(placement.X, placement.Y, placement.Width, placement.Height);

            if (_realizedByIndex.TryGetValue(placement.Index, out RealizedSlot? existingSlot))
            {
                if (!ReferenceEquals(existingSlot.View.BindingContext, item))
                {
                    existingSlot.View.BindingContext = item;
                }

                if (!_canvas.TryGetLayoutBounds(existingSlot.View, out Rect currentBounds) || currentBounds != bounds)
                {
                    _canvas.SetLayoutBounds(existingSlot.View, bounds);
                    anyChanged = true;
                }
            }
            else
            {
                DataTemplate resolvedTemplate = ResolveTemplate(ItemTemplate, item);
                View view = AcquireView(resolvedTemplate);
                view.BindingContext = item;

                _canvas.SetLayoutBounds(view, bounds);
                if (!_canvas.Contains(view))
                {
                    _canvas.Add(view);
                }

                _realizedByIndex[placement.Index] = new RealizedSlot(view, resolvedTemplate);
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            _canvas.RequestRelayout();
        }
    }

    private DataTemplate ResolveTemplate(DataTemplate template, object item) =>
        template is DataTemplateSelector selector
            ? selector.SelectTemplate(item, this) ?? template
            : template;

    private View AcquireView(DataTemplate template)
    {
        if (_recyclePoolByTemplate.TryGetValue(template, out Stack<View>? pool) && pool.Count > 0)
        {
            return pool.Pop();
        }

        object content = template.CreateContent();
        return content switch
        {
            View view => view,
            _ => throw new InvalidOperationException(
                $"The {nameof(ItemTemplate)} must create a {nameof(View)} instance, but created '{content?.GetType().FullName ?? "null"}'.")
        };
    }

    private void RecycleIndex(int index)
    {
        if (!_realizedByIndex.Remove(index, out RealizedSlot? slot))
        {
            return;
        }

        slot.View.BindingContext = null;
        _canvas.ClearLayoutBounds(slot.View);
        _canvas.Remove(slot.View);

        if (!_recyclePoolByTemplate.TryGetValue(slot.Template, out Stack<View>? pool))
        {
            pool = new Stack<View>();
            _recyclePoolByTemplate[slot.Template] = pool;
        }

        pool.Push(slot.View);
    }

    private void RecycleAllRealizedViews()
    {
        if (_realizedByIndex.Count == 0)
        {
            return;
        }

        var indices = new List<int>(_realizedByIndex.Keys);
        for (int i = 0; i < indices.Count; i++)
        {
            RecycleIndex(indices[i]);
        }
    }

    private void ClearAllRealizedAndPooledViews()
    {
        RecycleAllRealizedViews();
        _recyclePoolByTemplate.Clear();
        _canvas.Reset();
    }

    private void UpdateEmptyViewContent()
    {
        View? emptyContent = null;

        if (EmptyViewTemplate is not null)
        {
            DataTemplate resolved = EmptyViewTemplate is DataTemplateSelector selector
                ? selector.SelectTemplate(EmptyView, this) ?? EmptyViewTemplate
                : EmptyViewTemplate;

            object created = resolved.CreateContent();
            emptyContent = created as View;

            if (emptyContent is not null && EmptyView is not null && EmptyView is not View)
            {
                emptyContent.BindingContext = EmptyView;
            }
        }
        else if (EmptyView is View directView)
        {
            emptyContent = directView;
        }
        else if (EmptyView is not null)
        {
            emptyContent = new Label
            {
                Text = EmptyView.ToString(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
        }

        _emptyViewContainer.Content = emptyContent;
    }

    private void UpdateEmptyStateVisibility()
    {
        bool isEmpty = _items.Count == 0;
        bool hasEmptyPresentation = EmptyView is not null || EmptyViewTemplate is not null;

        _emptyViewContainer.IsVisible = isEmpty && hasEmptyPresentation;
        _scrollView.IsVisible = !isEmpty || !hasEmptyPresentation;
    }

    private sealed record RealizedSlot(View View, DataTemplate Template);
}
