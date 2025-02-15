﻿using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Home;

namespace ByteSync.Views.Home;

public class CreateCloudSessionView : ReactiveUserControl<CreateCloudSessionViewModel>
{
    public CreateCloudSessionView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}