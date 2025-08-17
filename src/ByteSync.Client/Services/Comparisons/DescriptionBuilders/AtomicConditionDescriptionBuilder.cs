using System.Text;
using ByteSync.Assets.Resources;
using ByteSync.Business.Comparisons;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Business.Actions;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Comparisons.DescriptionBuilders;

public class AtomicConditionDescriptionBuilder : AbstractDescriptionBuilder<IAtomicCondition>
{
    private readonly ISizeUnitConverter _sizeUnitConverter;

    public AtomicConditionDescriptionBuilder(ILocalizationService localizationService, ISizeUnitConverter sizeUnitConverter) : base(localizationService)
    {
        _sizeUnitConverter = sizeUnitConverter;
    }

    public override void AppendDescription(StringBuilder stringBuilder, IAtomicCondition atomicCondition)
    {
        stringBuilder.Append($"{atomicCondition.SourceName}.{GetComparisonElement(atomicCondition)} {GetOperator(atomicCondition)} ");

        if (atomicCondition.DestinationName != null)
        {
            stringBuilder.Append($"{atomicCondition.DestinationName}");
        }
        else
        {
            if (atomicCondition.Size != null)
            {
                var sizeUnit = _sizeUnitConverter.GetPrintableSizeUnit(atomicCondition.SizeUnit);
                stringBuilder.Append($"{atomicCondition.Size} {sizeUnit}");
            }
            else if (atomicCondition.DateTime != null)
            {
                stringBuilder.Append($"{atomicCondition.DateTime:g}");
            }
            else if (atomicCondition.NamePattern != null)
            {
                stringBuilder.Append($"{atomicCondition.NamePattern}");
            }
        }
    }

    private string GetComparisonElement(IAtomicCondition atomicCondition)
    {
        var result = "";

        switch (atomicCondition.ComparisonProperty)
        {
            case ComparisonProperty.Content:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ComparisonElement_Content)];
                break;
            case ComparisonProperty.Date:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ComparisonElement_Date)];
                break;
            case ComparisonProperty.Size:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ComparisonElement_Size)];
                break;
            case ComparisonProperty.Presence:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ComparisonElement_Presence)];
                break;
            case ComparisonProperty.Name:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ComparisonElement_Name)];
                break;
        }
            
        if (result.IsEmpty())
        {
            throw new ApplicationException("Unknown atomicCondition.ComparisonElement " + atomicCondition.ComparisonProperty);
        }


        return result;
    }

    private string GetOperator(IAtomicCondition atomicCondition)
    {
        var result = "";

        switch (atomicCondition.ConditionOperator)
        {
            case ConditionOperatorTypes.Equals:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ConditionOperator_Equals)];
                break;
            case ConditionOperatorTypes.NotEquals:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ConditionOperator_NotEquals)];
                break;
            case ConditionOperatorTypes.ExistsOn:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ConditionOperator_ExistsOn)];
                break;
            case ConditionOperatorTypes.NotExistsOn:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ConditionOperator_NotExistsOn)];
                break;
            case ConditionOperatorTypes.IsBiggerThan:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ConditionOperator_IsSizeGreaterThan)];
                break;
            case ConditionOperatorTypes.IsSmallerThan:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ConditionOperatort_IsSizeLessThan)];
                break;
            case ConditionOperatorTypes.IsOlderThan:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ConditionOperator_IsDateBefore)];
                break;
            case ConditionOperatorTypes.IsNewerThan:
                result = LocalizationService[nameof(Resources.AtomicConditionDescription_ConditionOperator_IsDateAfter)];
                break;
        }
            
        if (result.IsEmpty())
        {
            throw new ApplicationException("Unknown atomicCondition.ConditionOperator " + atomicCondition.ConditionOperator);
        }

        return result;
    }
}