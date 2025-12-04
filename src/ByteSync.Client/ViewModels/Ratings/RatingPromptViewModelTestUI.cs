using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace ByteSync.ViewModels.Ratings;

public sealed class RatingPromptViewModelTestUI : RatingPromptViewModel
{
    public RatingPromptViewModelTestUI()
    {
        RatingOptions = new ObservableCollection<RatingOption>
        {
            new("Noter sur le Store", "https://bytesync.app/store"),
            new("Envoyer un feedback", "https://bytesync.app/feedback"),
            new("Découvrir les nouveautés", "https://bytesync.app/releases")
        };
        
        RateCommand = ReactiveCommand.Create<string>(_ => { });
        AskLaterCommand = ReactiveCommand.Create(() => { });
        DoNotAskAgainCommand = ReactiveCommand.Create(() => { });
    }
    
    public new string Message { get; } =
        "Content de vous revoir ! Partagez votre avis pour nous aider à améliorer ByteSync.";
    
    public new string AskLaterText { get; } = "Me le rappeler plus tard";
    
    public new string DoNotAskAgainText { get; } = "Ne plus afficher";
    
    public new ObservableCollection<RatingOption> RatingOptions { get; }
    
    public new ReactiveCommand<string, Unit> RateCommand { get; }
    
    public new ReactiveCommand<Unit, Unit> AskLaterCommand { get; }
    
    public new ReactiveCommand<Unit, Unit> DoNotAskAgainCommand { get; }
}