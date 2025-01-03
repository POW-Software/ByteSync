﻿using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;

namespace ByteSync.Views.Sessions.Inventories;

public class InventoryBeforeStartView : ReactiveUserControl<InventoryBeforeStartViewModel>
{
    public InventoryBeforeStartView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}