using System.Text;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using Splat;

namespace ByteSync.Services.Comparisons.DescriptionBuilders;

public abstract class AbstractDescriptionBuilder<T>
{
    protected AbstractDescriptionBuilder(ILocalizationService? localizationService)
    {
        TranslationSource = localizationService ?? Locator.Current.GetService<ILocalizationService>()!;
    }

    protected ILocalizationService TranslationSource { get; }

    public string GetDescription(T element)
    {
        var stringBuilder = new StringBuilder();
            
        AppendDescription(stringBuilder, element);
        var description = stringBuilder.ToString();
            
        return description;
    }

    public abstract void AppendDescription(StringBuilder stringBuilder, T element);
    
    protected string GetAction(AbstractAction action)
    {
        return GetAction(action.Operator);
    }
    
    protected string GetAction(ActionOperatorTypes operatorType)
    {
        var result = "";

        switch (operatorType)
        {
            case ActionOperatorTypes.SynchronizeContentOnly:
                result = TranslationSource[nameof(Resources.SynchronizationActionDescription_Action_SynchronizeContent)];
                break;
            case ActionOperatorTypes.SynchronizeContentAndDate:
                result = TranslationSource[nameof(Resources.SynchronizationActionDescription_Action_SynchronizeContentAndDate)];
                break;
            case ActionOperatorTypes.SynchronizeDate:
                result = TranslationSource[nameof(Resources.SynchronizationActionDescription_Action_SynchronizeDate)];
                break;
            case ActionOperatorTypes.Create:
                result = TranslationSource[nameof(Resources.SynchronizationActionDescription_Action_Create)];
                break;
            case ActionOperatorTypes.Delete:
                result = TranslationSource[nameof(Resources.SynchronizationActionDescription_Action_Delete)];
                break;
            case ActionOperatorTypes.DoNothing:
                result = TranslationSource[nameof(Resources.SynchronizationActionDescription_Action_DoNothing)];
                break;
        }

        if (result.IsEmpty())
        {
            throw new ApplicationException("Unknown actionsGroup.Operator " + operatorType);
        }

        return result;
    }
    
    protected string GetFrom()
    {
        return TranslationSource[nameof(Resources.SynchronizationActionDescription_From)];
    }
    
    protected string GetTo()
    {
        return TranslationSource[nameof(Resources.SynchronizationActionDescription_To)];
    }
    
    protected string GetOn()
    {
        return TranslationSource[nameof(Resources.SynchronizationActionDescription_On)];
    }
}