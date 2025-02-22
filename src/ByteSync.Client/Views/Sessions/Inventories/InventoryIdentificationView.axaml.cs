﻿using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;

namespace ByteSync.Views.Sessions.Inventories;

public class InventoryIdentificationView : ReactiveUserControl<InventoryIdentificationViewModel>
{
    public InventoryIdentificationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}