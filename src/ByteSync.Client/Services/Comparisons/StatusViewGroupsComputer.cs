using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Services.Comparisons;

class StatusViewGroupsComputer : IDisposable
{
    public StatusViewGroupsComputer(StatusViewModel statusViewModel)
    {
        StatusViewModel = statusViewModel;
    }

    public StatusViewModel StatusViewModel { get; }

    private Status Status
    {
        get
        {
            return StatusViewModel.Status;
        }
    }

    private List<Inventory> AllInventories { get; set; }

    public void Compute()
    {
        StatusViewModel.FingerPrintGroups!.Clear();
        StatusViewModel.LastWriteTimeGroups!.Clear();
        StatusViewModel.PresenceGroups!.Clear();

        ComputeInventories();

        if (StatusViewModel.FileSystemType == FileSystemTypes.File)
        {
            var fingerPrintMembers = ComputeMembers(Status.FingerPrintGroups);
            var fingerPrintGroups = ComputeGroups(fingerPrintMembers);
            SetStatusViewGroups(fingerPrintGroups, StatusViewModel.FingerPrintGroups);

            var lastWriteTimeMembers = ComputeMembers(Status.LastWriteTimeGroups);
            var lastWriteTimeGroups = ComputeGroups(lastWriteTimeMembers);
            SetStatusViewGroups(lastWriteTimeGroups, StatusViewModel.LastWriteTimeGroups);
        }
        else
        {
            var presenceMembers = ComputePresenceMembers(Status.FingerPrintGroups);
            var presenceGroups = ComputeGroups(presenceMembers);
            SetStatusViewGroups(presenceGroups, StatusViewModel.PresenceGroups);
        }
    }

    private void SetStatusViewGroups(List<StatusGroup> groups, ICollection<StatusItemViewModel> targetGroup)
    {
        var cpt = 0;
        foreach (var group in groups)
        {
            StatusViewModel.BrushColors backBrushColor;
            StatusViewModel.BrushColors foreBrushColor;

            if (group.IsMissing)
            {
                backBrushColor = StatusViewModel.BrushColors.MahAppsGray10;
                foreBrushColor = StatusViewModel.BrushColors.Gray;
            }
            else
            {
                if (cpt % 2 == 0)
                {
                    backBrushColor = StatusViewModel.BrushColors.MainBackground;
                }
                else
                {
                    backBrushColor = StatusViewModel.BrushColors.OppositeBackground;
                }

                foreBrushColor = StatusViewModel.BrushColors.MainForeColor;

                cpt += 1;
            }

            foreach (var member in group.Members.OrderBy(m => m.Letter))
            {
                var statusItemViewModel = new StatusItemViewModel();

                statusItemViewModel.Letter = member.Letter;

                statusItemViewModel.BackBrushColor = backBrushColor;
                statusItemViewModel.ForeBrushColor = foreBrushColor;

                targetGroup.Add(statusItemViewModel);
            }
        }
    }

    private List<StatusGroupMember> ComputeMembers<T>(Dictionary<T, HashSet<InventoryPart>> dictionary)
    {
        var result = new List<StatusGroupMember>();

        foreach (var inventory in Status.MissingInventories)
        {
            var member = new StatusGroupMember();
            member.Letter = inventory.Letter;
            member.IsMissing = true;

            member.Inventory = inventory;

            result.Add(member);
        }

        foreach (var pair in dictionary)
        {
            foreach (var inventoryPart in pair.Value)
            {
                var inventory = AllInventories.Single(i => i.Equals(inventoryPart.Inventory));

                if (inventory.InventoryParts.Count == 1)
                {
                    var member = new StatusGroupMember();
                    member.Letter = inventory.Letter;
                    member.Inventory = inventory;
                    member.Link = pair.Key;

                    result.Add(member);
                }
                else
                {
                    var member = new StatusGroupMember();
                    member.Letter = inventoryPart.Code;
                    member.InventoryPart = inventoryPart;
                    member.Link = pair.Key;

                    result.Add(member);
                }
            }
        }
            
        return result;
    }
        
    private List<StatusGroupMember> ComputePresenceMembers(Dictionary<ContentIdentityCore, HashSet<InventoryPart>> statusFingerPrintGroups)
    {
        var result = new List<StatusGroupMember>();

        foreach (var inventory in StatusViewModel.AllInventories)
        {
            if (!Status.MissingInventories.Contains(inventory))
            {
                var member = new StatusGroupMember();
                member.Letter = inventory.Letter;
                member.Inventory = inventory;
                member.IsMissing = false;
                member.Link = "present";

                result.Add(member);
            }
        }

        foreach (var inventory in Status.MissingInventories)
        {
            var member = new StatusGroupMember();
            member.Letter = inventory.Letter;
            member.IsMissing = true;

            member.Inventory = inventory;

            result.Add(member);
        }

        return result;
    }

    private List<StatusGroup> ComputeGroups(List<StatusGroupMember> groupMembers)
    {
        var result = new List<StatusGroup>();

        // Définition des groupes
        foreach (var groupMember in groupMembers)
        {
            if (groupMember.IsMissing)
            {
                var statusGroup = new StatusGroup(groupMember);
                result.Add(statusGroup);
            }
            else
            {
                var statusGroup = result.SingleOrDefault(sg => Equals(sg.Link, groupMember.Link));

                if (statusGroup == null)
                {
                    statusGroup = new StatusGroup(groupMember);
                    result.Add(statusGroup);
                }
                else
                {
                    statusGroup.Members.Add(groupMember);
                }
            }
        }

        // Ordonnancement des groupes
        result = result.OrderBy(sg => sg.MinimalLetter).ToList();

        return result;
    }

    private void ComputeInventories()
    {
        var allInventories = new HashSet<Inventory>();

        foreach (var pair in StatusViewModel.Status.FingerPrintGroups)
        {
            HashSet<Inventory> inventories = new HashSet<Inventory>(pair.Value.Select(ip => ip.Inventory));

            allInventories.AddAll(inventories);
        }

        foreach (var pair in StatusViewModel.Status.LastWriteTimeGroups)
        {
            HashSet<Inventory> inventories = new HashSet<Inventory>(pair.Value.Select(ip => ip.Inventory));

            allInventories.AddAll(inventories);
        }

        allInventories.AddAll(StatusViewModel.Status.MissingInventories);

        AllInventories = allInventories.OrderBy(i => i.Letter).ToList();
    }

    public void Dispose()
    {
        AllInventories = null;
    }
}