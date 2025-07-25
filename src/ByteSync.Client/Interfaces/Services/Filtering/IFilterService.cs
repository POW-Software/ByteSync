﻿using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IFilterService
{
    Func<ComparisonItem, bool> BuildFilter(string filterText);
    
    Func<ComparisonItem, bool> BuildFilter(List<string> filterTexts);
}