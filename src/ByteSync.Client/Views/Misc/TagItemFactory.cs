using Autofac;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Services;

namespace ByteSync.Views.Misc;

public class TagItemFactory : ITagItemFactory
{
    private readonly IComponentContext _context;
    
    public TagItemFactory()
    {
        _context = ContainerProvider.Container.Resolve<IComponentContext>();
    }
    
    public TagItem CreateTagItem(string tagText)
    {
        var filterParser = _context.Resolve<IFilterParser>();
        var themeService = _context.Resolve<IThemeService>();

        var tagItem = new TagItem(filterParser, themeService, tagText);
        
        return tagItem;
    }
}