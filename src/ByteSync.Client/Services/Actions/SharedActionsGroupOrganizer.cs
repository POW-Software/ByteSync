using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Actions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;

namespace ByteSync.Services.Actions;

public class SharedActionsGroupOrganizer : ISharedActionsGroupOrganizer
{
    private readonly IConnectionService _connectionService;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;

    public SharedActionsGroupOrganizer(IConnectionService connectionService, ISharedActionsGroupRepository sharedActionsGroupRepository)
    {
        _connectionService = connectionService;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
    }
    
    public Task OrganizeSharedActionGroups()
    {
        return Task.Run(() =>
        {
            // On met en premier les actions de synchro de contenu dont je suis la source
            // En les groupant par targets
            // Puis on met les autres actions
            List<SharedActionsGroup> sourceCopyActions = new List<SharedActionsGroup>();
            List<SharedActionsGroup> otherActions = new List<SharedActionsGroup>();
            foreach (SharedActionsGroup sharedActionsGroup in _sharedActionsGroupRepository.Elements)
            {
                if (sharedActionsGroup.NeedsOperatingOnSourceAndTargets &&
                    sharedActionsGroup.Source != null && sharedActionsGroup.Source.ClientInstanceId.Equals(_connectionService.ClientInstanceId))
                {
                    sourceCopyActions.Add(sharedActionsGroup);
                }
                else if (!sharedActionsGroup.NeedsOperatingOnSourceAndTargets && 
                         sharedActionsGroup.Targets.Any(t => t.ClientInstanceId.Equals(_connectionService.ClientInstanceId)))
                {
                    otherActions.Add(sharedActionsGroup);
                }
            }

            // On groupe par targets, comme ça, lors des uploads, on pourra zipper les fichiers par cibles
            Dictionary<string, List<SharedActionsGroup>> sourceCopyActionsDictionary = new Dictionary<string, List<SharedActionsGroup>>();
            foreach (var sharedActionsGroup in sourceCopyActions)
            {

                string key = sharedActionsGroup.Key;

                if (!sourceCopyActionsDictionary.ContainsKey(key))
                {
                    sourceCopyActionsDictionary.Add(key, new List<SharedActionsGroup>());
                }
                
                sourceCopyActionsDictionary[key].Add(sharedActionsGroup);
            }

            List<SharedActionsGroup> sortedSharedActionGroups = new List<SharedActionsGroup>();
            foreach (var sourceCopySharedActionsGroups in sourceCopyActionsDictionary.Values)
            {
                // On trie par taille croissante pour obtenir un meilleur rendement de la compression
                // On trie aussi par type de synchronisation. Ce n'est pas forcément utile
                var list = sourceCopySharedActionsGroups
                    .OrderBy(sag => sag.SynchronizationType)
                    .ThenBy(sag => sag.Size);

                foreach (var sharedActionsGroup in list)
                {
                    sortedSharedActionGroups.Add(sharedActionsGroup);
                }
            }
            
            sortedSharedActionGroups.AddAll(otherActions);

            _sharedActionsGroupRepository.SetOrganizedSharedActionsGroups(sortedSharedActionGroups);
        });
    }
}