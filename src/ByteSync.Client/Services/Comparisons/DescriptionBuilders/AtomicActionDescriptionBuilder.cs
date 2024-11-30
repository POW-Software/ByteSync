using System.Text;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Helpers;
using ByteSync.Common.Interfaces.Business;
using ByteSync.Interfaces;

namespace ByteSync.Services.Comparisons.DescriptionBuilders;

public class AtomicActionDescriptionBuilder : AbstractDescriptionBuilder<IAtomicAction>
{
    public AtomicActionDescriptionBuilder(ILocalizationService localizationService) : base(localizationService)
    {

    }
        
    public override void AppendDescription(StringBuilder stringBuilder, IAtomicAction atomicAction)
    {
        if (atomicAction.Operator == ActionOperatorTypes.DoNothing)
        {
            stringBuilder.Append($"{GetAction(atomicAction.Operator)}");
        }
        else if (atomicAction.Operator.In(ActionOperatorTypes.Create, ActionOperatorTypes.Delete))
        {
            stringBuilder.Append($"{GetAction(atomicAction.Operator)} {GetOn()} {atomicAction.DestinationName}");
        }
        else
        {
            stringBuilder.Append($"{GetAction(atomicAction.Operator)} {GetFrom()} ");
            stringBuilder.Append($"{atomicAction.SourceName} {GetTo()} {atomicAction.DestinationName}");
        }
    }
}