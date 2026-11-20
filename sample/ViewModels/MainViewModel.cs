using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiMasonry.Sample.Models;

namespace MauiMasonry.Sample.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly Color ActiveButtonColor = Color.FromArgb("#512BD4");
    private static readonly Color InactiveButtonColor = Color.FromArgb("#E2DEFA");
    private static readonly Color ActiveButtonTextColor = Colors.White;
    private static readonly Color InactiveButtonTextColor = Color.FromArgb("#372282");

    private static readonly (string Title, string Category, double AspectRatio, string CardHex, string BadgeHex)[] SeedCatalog =
    [
        ("Alpine Sunrise", "Nature", 0.68, "#264653", "#1B323B"),
        ("Espresso Pour", "Food", 1.00, "#6D4C41", "#4E342E"),
        ("Brutalist Arch", "Architecture", 0.75, "#455A64", "#263238"),
        ("Coastal Cliffs", "Travel", 1.35, "#2A9D8F", "#1D6F65"),
        ("Neon Alleyway", "Street", 0.62, "#5E35B1", "#311B92"),
        ("Botanical Study", "Flora", 0.85, "#388E3C", "#1B5E20"),
        ("Ceramic Vessels", "Craft", 1.15, "#AD6A4C", "#7F4B34"),
        ("Desert Dunes", "Landscape", 1.45, "#C87D32", "#8F571E"),
        ("Rainy Window", "Mood", 0.70, "#3949AB", "#1A237E"),
        ("Nordic Cabin", "Interiors", 0.90, "#5D4037", "#3E2723"),
        ("Mountain Pass", "Adventure", 0.65, "#00796B", "#004D40"),
        ("Vinyl Stack", "Music", 1.05, "#C2185B", "#880E4F")
    ];

    private int _columnCount = 2;
    private bool _isRefreshing;
    private int _generation;

    public MainViewModel()
    {
        SetTwoColumnsCommand = new Command(() => ColumnCount = 2);
        SetThreeColumnsCommand = new Command(() => ColumnCount = 3);
        SetFourColumnsCommand = new Command(() => ColumnCount = 4);
        ToggleEmptyStateCommand = new Command(ToggleEmptyState);
        PopulateCommand = new Command(PopulateSampleItems);
        RefreshCommand = new Command(async () => await RefreshItemsAsync());

        PopulateSampleItems();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SamplePhotoItem> Items { get; } = [];

    public ICommand SetTwoColumnsCommand { get; }

    public ICommand SetThreeColumnsCommand { get; }

    public ICommand SetFourColumnsCommand { get; }

    public ICommand ToggleEmptyStateCommand { get; }

    public ICommand PopulateCommand { get; }

    public ICommand RefreshCommand { get; }

    public int ColumnCount
    {
        get => _columnCount;
        set
        {
            if (_columnCount == value)
            {
                return;
            }

            _columnCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TwoColumnsBackground));
            OnPropertyChanged(nameof(ThreeColumnsBackground));
            OnPropertyChanged(nameof(FourColumnsBackground));
            OnPropertyChanged(nameof(TwoColumnsTextColor));
            OnPropertyChanged(nameof(ThreeColumnsTextColor));
            OnPropertyChanged(nameof(FourColumnsTextColor));
            OnPropertyChanged(nameof(StatusSummary));
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing == value)
            {
                return;
            }

            _isRefreshing = value;
            OnPropertyChanged();
        }
    }

    public Color TwoColumnsBackground => ColumnCount == 2 ? ActiveButtonColor : InactiveButtonColor;

    public Color ThreeColumnsBackground => ColumnCount == 3 ? ActiveButtonColor : InactiveButtonColor;

    public Color FourColumnsBackground => ColumnCount == 4 ? ActiveButtonColor : InactiveButtonColor;

    public Color TwoColumnsTextColor => ColumnCount == 2 ? ActiveButtonTextColor : InactiveButtonTextColor;

    public Color ThreeColumnsTextColor => ColumnCount == 3 ? ActiveButtonTextColor : InactiveButtonTextColor;

    public Color FourColumnsTextColor => ColumnCount == 4 ? ActiveButtonTextColor : InactiveButtonTextColor;

    public string EmptyToggleText => Items.Count == 0 ? "Load Items" : "Show Empty";

    public string StatusSummary => $"{Items.Count} items • {ColumnCount} columns";

    private void ToggleEmptyState()
    {
        if (Items.Count > 0)
        {
            Items.Clear();
            NotifyCollectionSummaryChanged();
        }
        else
        {
            PopulateSampleItems();
        }
    }

    private void PopulateSampleItems()
    {
        Items.Clear();
        int offset = (_generation * 5) % SeedCatalog.Length;

        for (int i = 0; i < 36; i++)
        {
            var template = SeedCatalog[(i + offset) % SeedCatalog.Length];
            Items.Add(new SamplePhotoItem
            {
                Id = i + 1,
                Title = template.Title,
                Category = template.Category,
                AspectRatio = template.AspectRatio,
                CardColor = Color.FromArgb(template.CardHex),
                BadgeColor = Color.FromArgb(template.BadgeHex)
            });
        }

        _generation++;
        NotifyCollectionSummaryChanged();
    }

    private async Task RefreshItemsAsync()
    {
        try
        {
            await Task.Delay(400);
            PopulateSampleItems();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void NotifyCollectionSummaryChanged()
    {
        OnPropertyChanged(nameof(EmptyToggleText));
        OnPropertyChanged(nameof(StatusSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
