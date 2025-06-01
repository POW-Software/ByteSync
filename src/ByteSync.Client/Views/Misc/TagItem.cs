using ByteSync.Business.Filtering.Parsing;
using ByteSync.Interfaces.Services.Filtering;
using ReactiveUI;

namespace ByteSync.Views.Misc;

public class TagItem : ReactiveObject
{
    private readonly IFilterParser _filterParser;
    
    private string _text = string.Empty;
    private ParseResult? _parseResult;

    public TagItem(IFilterParser filterParser, string tagText)
    {
        _filterParser = filterParser;
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
        private set => this.RaiseAndSetIfChanged(ref _parseResult, value);
    }

    public bool IsValid => ParseResult?.IsComplete == true;

    private void UpdateParseResult()
    {
        ParseResult = _filterParser.TryParse(Text);
    }
}
