﻿using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface IStatusViewModelFactory
{
    StatusViewModel CreateStatusViewModel(ComparisonItem comparisonItem, List<Inventory> inventories);
}