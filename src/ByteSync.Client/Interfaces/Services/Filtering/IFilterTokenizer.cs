using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IFilterTokenizer
{
    void Initialize(string filterText);
    
    FilterToken GetNextToken();
}