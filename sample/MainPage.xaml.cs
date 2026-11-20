using MauiMasonry.Sample.ViewModels;

﻿namespace MauiMasonry.Sample;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }
}
