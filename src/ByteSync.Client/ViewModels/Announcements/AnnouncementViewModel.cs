using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using ByteSync.Common.Business.Announcements;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Repositories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Announcements;

public class AnnouncementViewModel : ActivatableViewModelBase
{
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly ILocalizationService _localizationService;

    public AnnouncementViewModel()
    {
        Announcements = new ObservableCollection<string>();
    }

    public AnnouncementViewModel(IAnnouncementRepository announcementRepository, ILocalizationService localizationService) : this()
    {
        _announcementRepository = announcementRepository;
        _localizationService = localizationService;

        this.WhenActivated(disposables =>
        {
            Refresh();

            _announcementRepository.ObservableCache
                .Connect()
                .Subscribe(_ => Refresh())
                .DisposeWith(disposables);

            _localizationService.CurrentCultureObservable
                .Subscribe(_ => Refresh())
                .DisposeWith(disposables);
        });
    }

    public ObservableCollection<string> Announcements { get; }

    [Reactive]
    public bool IsVisible { get; private set; }

    private void Refresh()
    {
        if (_announcementRepository == null || _localizationService == null)
        {
            return;
        }

        var cultureCode = _localizationService.CurrentCultureDefinition.Code;
        var messages = _announcementRepository.Elements
            .Select(a => a.Message.TryGetValue(cultureCode, out var msg)
                ? msg
                : a.Message.Values.FirstOrDefault() ?? string.Empty)
            .ToList();

        Announcements.Clear();
        foreach (var message in messages)
        {
            Announcements.Add(message);
        }

        IsVisible = Announcements.Count > 0;
    }
}
