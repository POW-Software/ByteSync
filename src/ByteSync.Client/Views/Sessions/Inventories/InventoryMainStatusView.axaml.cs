﻿using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryMainStatusView : ReactiveUserControl<InventoryMainStatusViewModel>
{
    public InventoryMainStatusView()
    {
        InitializeComponent();
    }
}