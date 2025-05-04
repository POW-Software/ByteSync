namespace ByteSync.Business.Filtering.Parsing;

public class FilterToken
{
    public required string Token { get; init; }
    
    public required FilterTokenType Type { get; init; }
}