using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Announcements;
using ReactiveUI;

namespace ByteSync.Views.Announcements;

public partial class AnnouncementView : ReactiveUserControl<AnnouncementViewModel>
{
    public AnnouncementView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}
