﻿using ByteSync.Business.Comparisons;
using Prism.Mvvm;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

class ConditionOperatorViewModel : BindableBase
{
    private string _description;

    public ConditionOperatorViewModel()
    {

    }

    public ConditionOperatorViewModel(ConditionOperatorTypes conditionOperator, string description)
    {
        ConditionOperator = conditionOperator;
        Description = description;
    }

    public ConditionOperatorTypes ConditionOperator { get; set; }

    public string Description
    {
        get => _description;
        set
        {
            SetProperty(ref _description, value);
        }
    }

    protected bool Equals(ConditionOperatorViewModel other)
    {
        return Equals(ConditionOperator, other.ConditionOperator);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ConditionOperatorViewModel) obj);
    }

    public override int GetHashCode()
    {
        return ConditionOperator.GetHashCode();
    }
}