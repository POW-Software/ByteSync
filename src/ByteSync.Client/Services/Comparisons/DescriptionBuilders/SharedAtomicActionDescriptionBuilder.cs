using System.Text;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Actions;
using ByteSync.Interfaces;

namespace ByteSync.Services.Comparisons.DescriptionBuilders;

public class SharedAtomicActionDescriptionBuilder : AbstractDescriptionBuilder<SharedAtomicAction>
{
    public SharedAtomicActionDescriptionBuilder(ILocalizationService? localizationService = null) : base(localizationService)
    {

    }
        
    public override void AppendDescription(StringBuilder stringBuilder, SharedAtomicAction sharedAtomicAction)
    {
        if (sharedAtomicAction.Operator == ActionOperatorTypes.DoNothing)
        {
            stringBuilder.Append($"{GetAction(sharedAtomicAction)}");
        }
        else if (sharedAtomicAction.Operator.In(ActionOperatorTypes.Create, ActionOperatorTypes.Delete))
        {
            stringBuilder.Append($"{GetAction(sharedAtomicAction)} {GetOn()} ");
            stringBuilder.Append($"{sharedAtomicAction.Target?.Name}"); // protection nullRef a piori inutile
        }
        else
        {
            stringBuilder.Append($"{GetAction(sharedAtomicAction)} {GetFrom()} ");
            stringBuilder.Append($"{sharedAtomicAction.Source?.Name} {GetTo()} ");
            stringBuilder.Append($"{sharedAtomicAction.Target?.Name}"); // protection nullRef a piori inutile
        }
    }
}