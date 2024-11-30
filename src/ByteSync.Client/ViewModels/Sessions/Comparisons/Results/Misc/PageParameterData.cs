using System.Reactive;
using DynamicData.Operators;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

public class PageParameterData : ReactiveObject
{
    public PageParameterData(int currentPage, int pageSize)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;

        var canGoPreviousPage = this.WhenAnyValue(x => x.CurrentPage, pageIndex => pageIndex > 1);
        FirstPageCommand = ReactiveCommand.Create(() => CurrentPage = 1, canGoPreviousPage);
        PreviousPageCommand = ReactiveCommand.Create(() => CurrentPage = CurrentPage - 1, canGoPreviousPage);
        
        var canGoNextPage = this.WhenAnyValue(x => x.CurrentPage, x => x.PageCount, (pageIndex, pageCount) => pageIndex < pageCount);
        NextPageCommand = ReactiveCommand.Create(() => CurrentPage = CurrentPage + 1, canGoNextPage);
        LastPageCommand = ReactiveCommand.Create(() => CurrentPage = PageCount, canGoNextPage);
    }

    public ReactiveCommand<Unit, int> FirstPageCommand { get; }
    public ReactiveCommand<Unit, int> PreviousPageCommand { get; }
    public ReactiveCommand<Unit, int> NextPageCommand { get; }
    public ReactiveCommand<Unit, int> LastPageCommand { get; }
    
    [Reactive]
    public int TotalCount { get; set; }

    [Reactive]
    public int PageCount { get; set; }

    [Reactive]
    public int CurrentPage { get; set; }

    [Reactive]
    public int PageSize { get; set; }


    public void Update(IPageResponse response)
    {
        CurrentPage = response.Page;
        PageSize = response.PageSize;
        PageCount = response.Pages;
        TotalCount = response.TotalSize;
        // FisrtPageCommand.Refresh();
        // _previousPageCommand.Refresh();
    }
}