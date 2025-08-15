using FluentAssertions;
using NUnit.Framework;
using ByteSync.Common.Helpers;

namespace TestingCommon.Helpers
{
    [TestFixture]
    public class DebugUtilsTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public async Task DebugTaskDelay_SingleParameter_ShouldDelay()
        {
            var before = DateTime.UtcNow;
            await DebugUtils.DebugTaskDelay(0.001); // 1 ms
            var after = DateTime.UtcNow;

            (after - before).Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Test]
        public async Task DebugTaskDelay_MinEqualsMax_ShouldDelayExact()
        {
            var before = DateTime.UtcNow;
            await DebugUtils.DebugTaskDelay(0.001, 0.001);
            var after = DateTime.UtcNow;

            (after - before).Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Test]
        public async Task DebugTaskDelay_MinNotEqualMax_ShouldDelayRandom()
        {
            var before = DateTime.UtcNow;
            await DebugUtils.DebugTaskDelay(0.001, 0.002);
            var after = DateTime.UtcNow;

            (after - before).Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Test]
        public void IsRandom_ProbabilityLessThanZero_ShouldThrow()
        {
            Action act = () => DebugUtils.IsRandom(-0.1m);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void IsRandom_ProbabilityGreaterThanOne_ShouldThrow()
        {
            Action act = () => DebugUtils.IsRandom(1.1m);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void IsRandom_ZeroProbability_ShouldReturnFalse()
        {
            var result = DebugUtils.IsRandom(0m);
            result.Should().BeFalse();
        }

        [Test]
        public void IsRandom_OneProbability_ShouldReturnTrue()
        {
            var result = DebugUtils.IsRandom(1m);
            result.Should().BeTrue();
        }

        [Test]
        public void DebugSleep_SingleParameter_ShouldSleep()
        {
            var before = DateTime.UtcNow;
            DebugUtils.DebugSleep(0.001); // 1 ms
            var after = DateTime.UtcNow;

            (after - before).Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Test]
        public void DebugSleep_MinEqualsMax_ShouldSleepExact()
        {
            var before = DateTime.UtcNow;
            DebugUtils.DebugSleep(0.001, 0.001);
            var after = DateTime.UtcNow;

            (after - before).Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Test]
        public void DebugSleep_MinNotEqualMax_ShouldSleepRandom()
        {
            var before = DateTime.UtcNow;
            DebugUtils.DebugSleep(0.001, 0.002);
            var after = DateTime.UtcNow;

            (after - before).Should().BeGreaterThan(TimeSpan.Zero);
        }
    }
}