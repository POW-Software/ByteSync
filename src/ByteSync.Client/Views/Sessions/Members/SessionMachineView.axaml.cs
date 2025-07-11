﻿using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Members;

namespace ByteSync.Views.Sessions.Members;

public partial class SessionMachineView : ReactiveUserControl<SessionMachineViewModel>
{
    public SessionMachineView()
    {
        InitializeComponent();
    }
}