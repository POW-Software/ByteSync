using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using ReactiveUI;

namespace ByteSync.ViewModels.Ratings;

[ExcludeFromCodeCoverage]
public sealed class RatingPromptViewModelTestUI : RatingPromptViewModel
{
    public RatingPromptViewModelTestUI()
    {
        RatingOptions = new ObservableCollection<RatingOption>
        {
            new("Noter sur le Store", "https://bytesync.app/store", "RegularStore"),
            new("Envoyer un feedback", "https://bytesync.app/feedback", "LogosGithub"),
            new("Découvrir les nouveautés", "https://bytesync.app/releases", "RegularWorld")
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