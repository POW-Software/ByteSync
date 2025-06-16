using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Themes;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Services.Filtering;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Views.Misc;

public class TagItem : ReactiveObject, IDisposable
{
    private readonly IFilterParser _filterParser;
    private readonly IThemeService _themeService;
    
    private string _text = string.Empty;
    private ParseResult? _parseResult;
    private CompositeDisposable _disposables;

    public TagItem(IFilterParser filterParser, IThemeService themeService, string tagText)
    {
        _filterParser = filterParser;
        _themeService = themeService;
        
        _disposables = new CompositeDisposable();
        
        _themeService.SelectedTheme
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                InitializeBrush();
            })
            .DisposeWith(_disposables);
        
        Text = tagText;
    }

    public string Text
    {
        get => _text;
        set
        {
            this.RaiseAndSetIfChanged(ref _text, value);
            UpdateParseResult();
        }
    }   

    public ParseResult? ParseResult
    {
        get => _parseResult;
        private set
        {
            this.RaiseAndSetIfChanged(ref _parseResult, value);
            InitializeBrush();
        }
    }

    public bool IsValid => ParseResult?.IsComplete == true;

    private void UpdateParseResult()
    {
        ParseResult = _filterParser.TryParse(Text);
    }
    
    private void InitializeBrush()
    {
        var brush = IsValid 
            ? _themeService.GetBrush("SystemBaseLowColor")
            : _themeService.GetBrush("CurrentMemberSecondaryBackGround");
        
        TagBackground = brush;
    }
    
    [Reactive]
    public IBrush? TagBackground { get; set; }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
