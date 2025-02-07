using System.Reactive.Linq;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using DynamicData;

namespace ByteSync.Repositories;

public class SessionInvalidationCachePolicy<TObject, TKey> : ISessionInvalidationSourceCachePolicy<TObject, TKey> where TKey : notnull
{
    private readonly ISessionService _sessionService;
    private IDisposable? _sessionSubscription;
    private IDisposable? _sessionStatusSubscription;

    public SessionInvalidationCachePolicy(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }
    
    public void Initialize(SourceCache<TObject, TKey> sourceCache, bool clearOnSessionNull, bool clearOnSessionStatus)
    {
        if (clearOnSessionNull)
        {
            _sessionSubscription = _sessionService.SessionObservable            
                .Where(session => session == null)
                .Subscribe(_ => sourceCache.Clear());
        }
        
        if (clearOnSessionStatus)
        {
            _sessionStatusSubscription = _sessionService.SessionStatusObservable
                .Where(x => x == SessionStatus.Preparation)
                .Subscribe(_ => sourceCache.Clear());
        }
    }

    public void Dispose()
    {
        _sessionSubscription?.Dispose();
        _sessionStatusSubscription?.Dispose();
    }
}