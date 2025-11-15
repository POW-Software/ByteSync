using ByteSync.Services.Comparisons;
using ByteSync.Services.Comparisons.ConditionMatchers;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

public static class SynchronizationRuleMatcherTestHelper
{
    public static ConditionMatcherFactory CreateConditionMatcherFactory()
    {
        var extractor = new ContentIdentityExtractor();
        var matchers = new IConditionMatcher[]
        {
            new ContentConditionMatcher(extractor),
            new SizeConditionMatcher(extractor),
            new DateConditionMatcher(extractor),
            new PresenceConditionMatcher(extractor),
            new NameConditionMatcher()
        };
        
        return new ConditionMatcherFactory(matchers);
    }
}