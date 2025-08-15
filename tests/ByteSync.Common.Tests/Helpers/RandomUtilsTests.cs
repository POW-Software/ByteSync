using ByteSync.Common.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace TestingCommon.Helpers;

[TestFixture]
public class RandomUtilsTests
{
	[Test]
	public void GetRandomLetters_WithUppercaseTrue_ReturnsOnlyUppercaseLetters()
	{
		var result = RandomUtils.GetRandomLetters(32, true);

		result.Should().HaveLength(32);
		result.All(char.IsUpper).Should().BeTrue();
	}

	[Test]
	public void GetRandomLetters_WithUppercaseFalse_ReturnsOnlyLowercaseLetters()
	{
		var result = RandomUtils.GetRandomLetters(32, false);

		result.Should().HaveLength(32);
		result.All(char.IsLower).Should().BeTrue();
	}

	[Test]
	public void GetRandomLetters_WithNull_YieldsBothCasesOverMultipleSamples()
	{
		var result = RandomUtils.GetRandomLetters(1024, null);

		result.Should().HaveLength(1024);
		result.Any(char.IsUpper).Should().BeTrue();
		result.Any(char.IsLower).Should().BeTrue();
	}

	[Test]
	public void GetRandomLetters_WithCountZero_ReturnsEmptyString()
	{
		var result = RandomUtils.GetRandomLetters(0, true);

		result.Should().BeEmpty();
	}

	[Test]
	public void GetRandomLetter_WithNull_ProducesUpperAndLowerWithinReasonableTries()
	{
		bool seenUpper = false;
		bool seenLower = false;

		for (int i = 0; i < 8192 && (!seenUpper || !seenLower); i++)
		{
			char c = RandomUtils.GetRandomLetter(null);
			if (char.IsUpper(c)) seenUpper = true;
			if (char.IsLower(c)) seenLower = true;
		}

		seenUpper.Should().BeTrue();
		seenLower.Should().BeTrue();
	}

	[Test]
	public void GetRandomLetter_WithUppercaseTrue_ReturnsUppercase()
	{
		char c = RandomUtils.GetRandomLetter(true);
		char.IsUpper(c).Should().BeTrue();
	}

	[Test]
	public void GetRandomLetter_WithUppercaseFalse_ReturnsLowercase()
	{
		char c = RandomUtils.GetRandomLetter(false);
		char.IsLower(c).Should().BeTrue();
	}

	[Test]
	public void GetRandomNumber_WithOneDigit_ReturnsSingleDigitString()
	{
		for (int i = 0; i < 50; i++)
		{
			var result = RandomUtils.GetRandomNumber(1);
			result.Should().MatchRegex("^\\d$");
		}
	}

	[Test]
	public void GetRandomNumber_WithThreeDigits_ReturnsThreeDigitString()
	{
		for (int i = 0; i < 50; i++)
		{
			var result = RandomUtils.GetRandomNumber(3);
			result.Should().MatchRegex("^\\d{3}$");
		}
	}

	[Test]
	public void GetRandomElement_WithEmptyCollection_ReturnsDefault()
	{
		var empty = new List<string>();
		var result = RandomUtils.GetRandomElement(empty);
		result.Should().BeNull();
	}

	[Test]
	public void GetRandomElement_WithIListCollection_ReturnsAnExistingElement()
	{
		var list = new List<int> { 10, 20, 30, 40 };
		var element = RandomUtils.GetRandomElement(list);
		list.Should().Contain(element);
	}

	[Test]
	public void GetRandomElement_WithNonListCollection_ReturnsAnExistingElement()
	{
		var set = new HashSet<string> { "alpha", "beta", "gamma" };
		var element = RandomUtils.GetRandomElement(set);
		set.Should().Contain(element);
	}
}


