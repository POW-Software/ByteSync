using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Localizations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Misc;

public class MessageBoxViewModel : FlyoutElementViewModel
{
    private MessageBoxResult? _result;
    
    public MessageBoxViewModel()
    {
        OkButtonText = "";
        YesButtonText = "";
        NoButtonText = "";
        CancelButtonText = "";
    }

    public MessageBoxViewModel(string titleKey, string? messageKey, List<string>? messageArguments, ILocalizationService localizationService)
    {
        TitleKey = titleKey;
        MessageKey = messageKey;

        SyncRoot = new object();
        
        MessageArguments = messageArguments;
        if (MessageKey != null)
        {
            if (MessageArguments == null || MessageArguments.Count == 0)
            {
                Message = localizationService[MessageKey];
            }
            else
            {
                // ReSharper disable once CoVariantArrayConversion
                Message = string.Format(localizationService[MessageKey], MessageArguments!.ToArray());
            }
        }
        
        OkButtonText = localizationService[nameof(Resources.MessageBox_OK)];
        YesButtonText = localizationService[nameof(Resources.MessageBox_Yes)];
        NoButtonText = localizationService[nameof(Resources.MessageBox_No)];
        CancelButtonText = localizationService[nameof(Resources.MessageBox_Cancel)];

        ResultSelected = new ManualResetEvent(false);

        CanExecuteOK = new BehaviorSubject<bool>(true);
        OKButtonCommand = ReactiveCommand.Create(() =>
        {
            lock (SyncRoot)
            {
                Result ??= MessageBoxResult.OK;
            }
            
            ResultSelected.Set();
            
            RaiseCloseFlyoutRequested();
            
        }, CanExecuteOK);

        AnyButtonCommand = ReactiveCommand.Create((MessageBoxResult mbr) =>
        {
            lock (SyncRoot)
            {
                Result ??= mbr;
            }

            ResultSelected.Set();
            
            RaiseCloseFlyoutRequested();
        });
    }

    private object SyncRoot { get; set; }

    private MessageBoxResult? Result
    {
        get
        {
            lock (SyncRoot)
            {
                return _result;
            }
        }
        set
        {
            lock (SyncRoot)
            {
                _result = value;
            }
        }
    }

    public ManualResetEvent ResultSelected { get; set; }

    public ReactiveCommand<Unit, Unit> OKButtonCommand { get; set; }
    
    public ReactiveCommand<MessageBoxResult, Unit> AnyButtonCommand { get; set; }
    
    public string TitleKey { get; }
    
    private string? MessageKey { get; }
    
    public string? Message { get; set; }

    [Reactive] 
    public bool ShowOK { get; set; }
    
    [Reactive] 
    public bool ShowYesNo { get; set; }
    
    [Reactive] 
    public bool ShowCancel { get; set; }

    [Reactive]
    public string OkButtonText { get; set; }

    [Reactive]
    public string YesButtonText { get; set; }

    [Reactive]
    public string NoButtonText { get; set; }

    [Reactive]
    public string CancelButtonText { get; set; }

    public List<string>? MessageArguments { get; }
    
    [Reactive]
    public ViewModelBase? MessageContent { get; set; }
    
    public BehaviorSubject<bool> CanExecuteOK { get; }

    public async Task<MessageBoxResult?> WaitForResponse()
    {
        return await Task.Run(() =>
        {
            ResultSelected.WaitOne();

            return Result;
        });
    }

    public override async Task CancelIfNeeded()
    {
        await Task.Run(() =>
        {
            lock (SyncRoot)
            {
                Result ??= MessageBoxResult.Cancel;
            }

            ResultSelected.Set();
        });
    }
}
