using Autofac;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Managing;

namespace ByteSync.Factories.ViewModels;

public class AnalysisModeViewModelFactory : IAnalysisModeViewModelFactory
{
    private readonly IComponentContext _context;

    public AnalysisModeViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public AnalysisModeViewModel CreateAnalysisModeViewModel(AnalysisModes analysisMode)
    {
        var result = _context.Resolve<AnalysisModeViewModel>(
            new TypedParameter(typeof(AnalysisModes), analysisMode));

        return result;
    }
}