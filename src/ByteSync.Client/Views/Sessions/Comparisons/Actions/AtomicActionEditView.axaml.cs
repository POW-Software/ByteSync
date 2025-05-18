using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;

namespace ByteSync.Views.Sessions.Comparisons.Actions;

public partial class AtomicActionEditView : ReactiveUserControl<AtomicActionEditViewModel>
{
    public AtomicActionEditView()
    {
        InitializeComponent();
    }
}