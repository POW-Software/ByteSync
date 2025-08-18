using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Announcements;

public class AnnouncementViewModel : ActivatableViewModelBase
{
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;

    public AnnouncementViewModel()
    {
        Announcements = new ObservableCollection<AnnouncementItemViewModel>();
    }

    public AnnouncementViewModel(IAnnouncementRepository announcementRepository, 
        ILocalizationService localizationService, 
        IApplicationSettingsRepository applicationSettingsRepository) : this()
    {
        _announcementRepository = announcementRepository;
        _localizationService = localizationService;
        _applicationSettingsRepository = applicationSettingsRepository;

        AcknowledgeAnnouncementCommand = ReactiveCommand.Create<string>(AcknowledgeAnnouncement);
        
        this.WhenActivated(disposables =>
        {
            _announcementRepository.ObservableCache
                .Connect()
                .Subscribe(_ => Refresh())
                .DisposeWith(disposables);

            _localizationService.CurrentCultureObservable
                .Subscribe(_ => Refresh())
                .DisposeWith(disposables);
        });
    }

    public ObservableCollection<AnnouncementItemViewModel> Announcements { get; }

    public ReactiveCommand<string, Unit> AcknowledgeAnnouncementCommand { get; }

    [Reactive]
    public bool IsVisible { get; private set; }

    private void Refresh()
    {
        var cultureCode = _localizationService.CurrentCultureDefinition.Code;
        var applicationSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        var acknowledgedIds = applicationSettings.DecodedAcknowledgedAnnouncementIds;
        
        var unacknowledgedAnnouncements = _announcementRepository.Elements
            .Where(a => !acknowledgedIds.Contains(a.Id))
            .Select(a => new AnnouncementItemViewModel
            {
                Id = a.Id,
                Message = a.Message.TryGetValue(cultureCode, out var msg)
                    ? msg
                    : a.Message.Values.FirstOrDefault() ?? string.Empty
            })
            .ToList();

        Announcements.Clear();
        foreach (var announcement in unacknowledgedAnnouncements)
        {
            Announcements.Add(announcement);
        }

        IsVisible = Announcements.Count > 0;
    }

    private void AcknowledgeAnnouncement(string announcementId)
    {
        _applicationSettingsRepository.UpdateCurrentApplicationSettings(settings =>
        {
            settings.AddAcknowledgedAnnouncementId(announcementId);
        }, true);
        
        Refresh();
    }
}

public class AnnouncementItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
