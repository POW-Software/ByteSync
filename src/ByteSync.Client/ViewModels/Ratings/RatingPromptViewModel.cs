using System.Collections.ObjectModel;
using System.Reactive;
using ByteSync.Assets.Resources;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Ratings;

public enum RatingPromptResultType
{
    Rate,
    AskLater,
    DoNotAskAgain
}

public record RatingOption(string Label, string Url);

public record RatingPromptResult(RatingPromptResultType ResultType, string? Url);

public class RatingPromptViewModel : FlyoutElementViewModel
{
    private readonly TaskCompletionSource<RatingPromptResult> _taskCompletionSource;
    
    public RatingPromptViewModel()
    {
    }
    
    public RatingPromptViewModel(ILocalizationService localizationService, IEnumerable<RatingOption> ratingOptions)
    {
        _taskCompletionSource = new TaskCompletionSource<RatingPromptResult>();
        
        Message = localizationService[nameof(Resources.RatingPrompt_Message)];
        AskLaterText = localizationService[nameof(Resources.RatingPrompt_AskLater)];
        DoNotAskAgainText = localizationService[nameof(Resources.RatingPrompt_DoNotAskAgain)];
        
        RatingOptions = new ObservableCollection<RatingOption>(ratingOptions);
        
        RateCommand = ReactiveCommand.Create<string>(SelectRate);
        AskLaterCommand = ReactiveCommand.Create(AskLater);
        DoNotAskAgainCommand = ReactiveCommand.Create(DoNotAskAgain);
    }
    
    public string Message { get; }
    
    public string AskLaterText { get; }
    
    public string DoNotAskAgainText { get; }
    
    public ObservableCollection<RatingOption> RatingOptions { get; }
    
    public ReactiveCommand<string, Unit> RateCommand { get; }
    
    public ReactiveCommand<Unit, Unit> AskLaterCommand { get; }
    
    public ReactiveCommand<Unit, Unit> DoNotAskAgainCommand { get; }
    
    [Reactive]
    public bool IsBusy { get; set; }
    
    public Task<RatingPromptResult> WaitForResultAsync()
    {
        return _taskCompletionSource.Task;
    }
    
    public void SelectRateOption(string url)
    {
        SelectRate(url);
    }
    
    public void SelectAskLater()
    {
        AskLater();
    }
    
    public void SelectDoNotAskAgain()
    {
        DoNotAskAgain();
    }
    
    private void SelectRate(string url)
    {
        SetResult(new RatingPromptResult(RatingPromptResultType.Rate, url));
    }
    
    private void AskLater()
    {
        SetResult(new RatingPromptResult(RatingPromptResultType.AskLater, null));
    }
    
    private void DoNotAskAgain()
    {
        SetResult(new RatingPromptResult(RatingPromptResultType.DoNotAskAgain, null));
    }
    
    private void SetResult(RatingPromptResult result)
    {
        if (_taskCompletionSource.TrySetResult(result))
        {
            RaiseCloseFlyoutRequested();
        }
    }
}