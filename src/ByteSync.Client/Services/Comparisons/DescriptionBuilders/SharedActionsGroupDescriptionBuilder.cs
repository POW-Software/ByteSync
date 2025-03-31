using System.Text;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Actions;
using ByteSync.Interfaces;

namespace ByteSync.Services.Comparisons.DescriptionBuilders;

class SharedActionsGroupDescriptionBuilder : AbstractDescriptionBuilder<SharedActionsGroup>
{
    public SharedActionsGroupDescriptionBuilder(ILocalizationService localizationService) : base(localizationService)
    {

    }
        
    public override void AppendDescription(StringBuilder stringBuilder, SharedActionsGroup sharedActionsGroup)
    {
        if (sharedActionsGroup.Operator == ActionOperatorTypes.DoNothing)
        {
            stringBuilder.Append($"{GetAction(sharedActionsGroup)}");
        }
        else if (sharedActionsGroup.Operator.In(ActionOperatorTypes.Create, ActionOperatorTypes.Delete))
        {
            stringBuilder.Append($"{GetAction(sharedActionsGroup)} {GetOn()} ");
            stringBuilder.Append($"{sharedActionsGroup.Targets.Select(t => t.Name).ToList().JoinToString(", ")}");
        }
        else
        {
            stringBuilder.Append($"{GetAction(sharedActionsGroup)} {GetFrom()} ");
            stringBuilder.Append($"{sharedActionsGroup.Source?.Name} {GetTo()} ");
            stringBuilder.Append($"{sharedActionsGroup.Targets.Select(t => t.Name).ToList().JoinToString(", ")}");
        }
    }
}