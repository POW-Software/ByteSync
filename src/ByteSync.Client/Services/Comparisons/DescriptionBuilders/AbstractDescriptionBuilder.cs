using System.Text;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Actions;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Comparisons.DescriptionBuilders;

public abstract class AbstractDescriptionBuilder<T>
{
    protected AbstractDescriptionBuilder(ILocalizationService localizationService)
    {
        LocalizationService = localizationService;
    }
    
    protected ILocalizationService LocalizationService { get; }
    
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
            case ActionOperatorTypes.CopyContentOnly:
                result = LocalizationService[nameof(Resources.SynchronizationActionDescription_Action_CopyContent)];
                
                break;
            case ActionOperatorTypes.Copy:
                result = LocalizationService[nameof(Resources.SynchronizationActionDescription_Action_Copy)];
                
                break;
            case ActionOperatorTypes.CopyDatesOnly:
                result = LocalizationService[nameof(Resources.SynchronizationActionDescription_Action_CopyDate)];
                
                break;
            case ActionOperatorTypes.Create:
                result = LocalizationService[nameof(Resources.SynchronizationActionDescription_Action_Create)];
                
                break;
            case ActionOperatorTypes.Delete:
                result = LocalizationService[nameof(Resources.SynchronizationActionDescription_Action_Delete)];
                
                break;
            case ActionOperatorTypes.DoNothing:
                result = LocalizationService[nameof(Resources.SynchronizationActionDescription_Action_DoNothing)];
                
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
        return LocalizationService[nameof(Resources.SynchronizationActionDescription_From)];
    }
    
    protected string GetTo()
    {
        return LocalizationService[nameof(Resources.SynchronizationActionDescription_To)];
    }
    
    protected string GetOn()
    {
        return LocalizationService[nameof(Resources.SynchronizationActionDescription_On)];
    }
}