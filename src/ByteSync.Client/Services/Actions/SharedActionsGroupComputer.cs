﻿using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Actions;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Actions;

public class SharedActionsGroupComputer : ISharedActionsGroupComputer
{
    private readonly ISharedAtomicActionRepository _sharedAtomicActionRepository;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;

    private List<SharedActionsGroup> _buffer;
    private int _counter; 
    private readonly object _lock = new();

    public SharedActionsGroupComputer(ISharedAtomicActionRepository sharedAtomicActionRepository, ISharedActionsGroupRepository sharedActionsGroupRepository)
    {
        _sharedAtomicActionRepository = sharedAtomicActionRepository;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _buffer = new List<SharedActionsGroup>();
            
        _counter = 0;
    }

    public async Task ComputeSharedActionsGroups()
    {
        var sharedAtomicActions = _sharedAtomicActionRepository.Elements;
        
        // var tasks = new List<Task>();
        var dictionary = sharedAtomicActions.GroupBy(saa => saa.PathIdentity)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        // foreach (KeyValuePair<PathIdentity, List<SharedAtomicAction>> pair in dictionary)
        // {
        //     tasks.Add(Task.Run(() => ComputeGroups_CopyContentAndDate(pair.Value)));
        //     tasks.Add(Task.Run(() => ComputeGroups_CopyContent(pair.Value)));
        //     tasks.Add(Task.Run(() => ComputeGroups_CopyDate(pair.Value)));
        //     tasks.Add(Task.Run(() => ComputeGroups_Create(pair.Value)));
        //     tasks.Add(Task.Run(() => ComputeGroups_Delete(pair.Value)));
        // }
        //
        // await Task.WhenAll(tasks);
        
        // await Task.Run(() => {
        //     Parallel.ForEach(
        //         dictionary,
        //         new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
        //         pair => {
        //             ComputeGroups_CopyContentAndDate(pair.Value).Wait();
        //             ComputeGroups_CopyContent(pair.Value).Wait();
        //             ComputeGroups_CopyDate(pair.Value).Wait();
        //             ComputeGroups_Create(pair.Value).Wait();
        //             ComputeGroups_Delete(pair.Value).Wait();
        //         }
        //     );
        // });
        
        // await Parallel.ForEachAsync(dictionary, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (pair, ct) =>
        // {
        //     await Task.WhenAll(
        //         Task.Run(() => ComputeGroups_CopyContentAndDate(pair.Value)),
        //         Task.Run(() => ComputeGroups_CopyContent(pair.Value)),
        //         Task.Run(() => ComputeGroups_CopyDate(pair.Value)),
        //         Task.Run(() => ComputeGroups_Create(pair.Value)),
        //         Task.Run(() => ComputeGroups_Delete(pair.Value))
        //     );
        // });
        
        await Parallel.ForEachAsync(dictionary, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, (pair, _) =>
        {
            ComputeGroups_CopyContentAndDate(pair.Value);
            ComputeGroups_CopyContent(pair.Value);
            ComputeGroups_CopyDate(pair.Value);
            ComputeGroups_Create(pair.Value);
            ComputeGroups_Delete(pair.Value);
            return ValueTask.CompletedTask;
        });

        lock (_lock)
        {
            if (_buffer.Count > 0)
            {
                _sharedActionsGroupRepository.AddOrUpdate(_buffer);
                _buffer.Clear();
            }
        }
    }

    private void ComputeGroups_CopyContentAndDate(List<SharedAtomicAction> atomicActions)
    {
        var sharedAtomicActions = GetSharedAtomicActions(atomicActions, ActionOperatorTypes.SynchronizeContentAndDate);
        
        var groups = GetCopyGroups(sharedAtomicActions, true);

        var sharedActionsGroups = new List<SharedActionsGroup>();
        foreach (var group in groups)
        {
            var sharedActionsGroup = BuildSharedActionsGroup(ActionOperatorTypes.SynchronizeContentAndDate);

            sharedActionsGroup.Source = group.First().Source;
            sharedActionsGroup.Targets.AddAll(group.Select(saa => saa.Target!));
            sharedActionsGroup.PathIdentity = group.First().PathIdentity;
            sharedActionsGroup.CreationTimeUtc = group.First().CreationTimeUtc;
            sharedActionsGroup.LastWriteTimeUtc = group.First().LastWriteTimeUtc;
            sharedActionsGroup.Size = group.First().Size;
            sharedActionsGroup.SynchronizationType = group.First().SynchronizationType;
            sharedActionsGroup.IsFromSynchronizationRule = group.First().IsFromSynchronizationRule;

            if (sharedActionsGroup.Targets.All(t => t.SignatureHash != null 
                                                    && t.SignatureHash!.Equals(sharedActionsGroup.Source!.SignatureHash)))
            {
                sharedActionsGroup.AppliesOnlySynchronizeDate = true;
            }

            sharedActionsGroups.Add(sharedActionsGroup);
        }
        
        AddSharedActionsGroups(sharedActionsGroups);
    }

    private void ComputeGroups_CopyContent(List<SharedAtomicAction> atomicActions)
    {
        var sharedAtomicActions = GetSharedAtomicActions(atomicActions, ActionOperatorTypes.SynchronizeContentOnly);

        var groups = GetCopyGroups(sharedAtomicActions, false);

        var sharedActionsGroups = new List<SharedActionsGroup>();
        foreach (var group in groups)
        {
            var sharedActionsGroup = BuildSharedActionsGroup(ActionOperatorTypes.SynchronizeContentOnly);

            sharedActionsGroup.Source = group.First().Source;
            sharedActionsGroup.Targets.AddAll(group.Select(saa => saa.Target!));
            sharedActionsGroup.PathIdentity = group.First().PathIdentity;
            sharedActionsGroup.Size = group.First().Size;
            sharedActionsGroup.SynchronizationType = group.First().SynchronizationType;
            sharedActionsGroup.IsFromSynchronizationRule = group.First().IsFromSynchronizationRule;
            
            AffectSharedActionsGroupId(sharedActionsGroup, group);
            
            sharedActionsGroups.Add(sharedActionsGroup);
        }
        
        AddSharedActionsGroups(sharedActionsGroups);
    }

    private void ComputeGroups_CopyDate(List<SharedAtomicAction> atomicActions)
    {
        var sharedAtomicActions = GetSharedAtomicActions(atomicActions, ActionOperatorTypes.SynchronizeDate);

        var sharedActionsGroups = new List<SharedActionsGroup>();
        
        foreach (KeyValuePair<SharedDataPart, List<SharedAtomicAction>> pair in 
                 sharedAtomicActions.GroupBy(aa => aa.Source!)
                     .ToDictionary(g => g.Key, g => g.ToList()))
        {
            var sharedActionsGroup = BuildSharedActionsGroup(ActionOperatorTypes.SynchronizeDate);

            sharedActionsGroup.Source = pair.Key;
            sharedActionsGroup.Targets.AddAll(pair.Value.Select(saa => saa.Target!));
            sharedActionsGroup.PathIdentity = pair.Value.First().PathIdentity;
            sharedActionsGroup.CreationTimeUtc = pair.Value.First().CreationTimeUtc;
            sharedActionsGroup.LastWriteTimeUtc = pair.Value.First().LastWriteTimeUtc;
            sharedActionsGroup.IsFromSynchronizationRule = pair.Value.First().IsFromSynchronizationRule;
            
            AffectSharedActionsGroupId(sharedActionsGroup, pair.Value);

            sharedActionsGroups.Add(sharedActionsGroup);
        }
        
        AddSharedActionsGroups(sharedActionsGroups);
    }
    
    private void ComputeGroups_Create(List<SharedAtomicAction> atomicActions)
    {
        DoComputeGroups_CreateDelete(ActionOperatorTypes.Create, atomicActions);
    }
    
    private void ComputeGroups_Delete(List<SharedAtomicAction> atomicActions)
    {
        DoComputeGroups_CreateDelete(ActionOperatorTypes.Delete, atomicActions);
    }

    private void DoComputeGroups_CreateDelete(ActionOperatorTypes operatorType, List<SharedAtomicAction> atomicActions)
    {
        if (!operatorType.In(ActionOperatorTypes.Create, ActionOperatorTypes.Delete))
        {
            throw new ArgumentOutOfRangeException(nameof(operatorType));
        }

        var sharedAtomicActions = GetSharedAtomicActions(atomicActions, operatorType).ToList();

        if (sharedAtomicActions.Count == 0)
        {
            return;
        }

        var sharedActionsGroup = BuildSharedActionsGroup(operatorType);

        sharedActionsGroup.Source = null;
        sharedActionsGroup.Targets.AddAll(sharedAtomicActions.Select(saa => saa.Target!));
        sharedActionsGroup.PathIdentity = sharedAtomicActions.First().PathIdentity;
        sharedActionsGroup.IsFromSynchronizationRule = sharedAtomicActions.First().IsFromSynchronizationRule;
        
        AffectSharedActionsGroupId(sharedActionsGroup, sharedAtomicActions);
        
        AddSharedActionsGroup(sharedActionsGroup);
    }

    private IEnumerable<SharedAtomicAction> GetSharedAtomicActions(List<SharedAtomicAction> sharedAtomicActions, ActionOperatorTypes operatorType)
    {
        var automaticActions = sharedAtomicActions
            .Where(vm => vm.Operator == operatorType);
        
        return automaticActions;
    }

    private SharedActionsGroup BuildSharedActionsGroup(ActionOperatorTypes operatorType)
    {
        var group = new SharedActionsGroup();
        
        group.Operator = operatorType;
        group.ActionsGroupId = GenerateUniqueId();
        
        return group;
    }
    
    private string GenerateUniqueId()
    {
        var newCounter = Interlocked.Increment(ref _counter);
        return $"AGID_{newCounter}";
    }
    
    private void AffectSharedActionsGroupId(SharedActionsGroup sharedActionsGroup, List<SharedAtomicAction> sharedAtomicActions)
    {
        foreach (var sharedAtomicAction in sharedAtomicActions)
        {
            sharedAtomicAction.ActionsGroupId = sharedActionsGroup.ActionsGroupId;
        }
    }
    
    private void AddSharedActionsGroups(List<SharedActionsGroup> sharedActionsGroups)
    {
        // if (sharedActionsGroups.Count > 0)
        // {
        //     _sharedActionsGroupRepository.AddOrUpdate(sharedActionsGroups);
        // }
        //
        // AddToBuffer(sharedActionsGroups);
        
        lock (_lock)
        {
            _buffer.AddAll(sharedActionsGroups);

            CheckBuffer();
        }

    }

    // private void AddToBuffer(List<SharedActionsGroup> sharedActionsGroups)
    // {
    //     _buffer.AddAll(sharedActionsGroups);
    // }

    private void AddSharedActionsGroup(SharedActionsGroup sharedActionsGroup)
    {
        lock (_lock)
        {
            _buffer.Add(sharedActionsGroup);
            
            CheckBuffer();
        }

        // _sharedActionsGroupRepository.AddOrUpdate(sharedActionsGroup);
    }

    private void CheckBuffer()
    {
        if (_buffer.Count > 500)
        {
            _sharedActionsGroupRepository.AddOrUpdate(_buffer);
            _buffer.Clear();
        }
    }

    private static List<List<SharedAtomicAction>> GetCopyGroups(IEnumerable<SharedAtomicAction> sharedAtomicActions, bool isContentAndDate)
    {
        var root = sharedAtomicActions
            .Where(saa => saa.Target != null) // La target peut être null dans certains cas avec les règles de synchronisation
            .GroupBy(saa => saa.Source!)
            .Select(x => new
            {
                Source = x.Key,
                Locations = x.ToList().GroupBy(saa => Equals(saa.Target!.ClientInstanceId, saa.Source!.ClientInstanceId))
                    .Select(x2 => new
                    {
                        Location = x2.Key,
                        SignatureHashes = x2.ToList().GroupBy(saa => saa.Target!.SignatureHash)
                            .Select(x3 => new
                            {
                                SignatureHash = x3.Key,
                                Values = x3.ToList(),
                                LastWriteTimes = x3.ToList().GroupBy(saa => new
                                {
                                    saa.LastWriteTimeUtc
                                })
                            })
                    })
            });

        List<List<SharedAtomicAction>> lists = new List<List<SharedAtomicAction>>();
        
        foreach (var perSource in root)
        {
            foreach (var perLocation in perSource.Locations)
            {
                foreach (var perFingerPrint in perLocation.SignatureHashes)
                {
                    if (isContentAndDate)
                    {
                        foreach (var perLastWriteTime in perFingerPrint.LastWriteTimes)
                        {
                            lists.Add(perLastWriteTime.ToList());
                        }
                    }
                    else
                    {
                        lists.Add(perFingerPrint.Values);
                    }
                }
            }
        }

        return lists;
    }
}