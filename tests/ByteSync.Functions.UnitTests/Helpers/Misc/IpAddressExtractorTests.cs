using ByteSync.Functions.Helpers.Misc;
using ByteSync.Functions.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace ByteSync.Functions.UnitTests.Helpers.Misc;

[TestFixture]
public class IpAddressExtractorTests
{
    /// <summary>
    /// Utility method to create a simulated HttpRequestData with a given “x-forwarded-for” header.
    /// </summary>
    /// <param name="headerValue">The value to associate with the header.</param>
    /// <returns>A simulated instance of HttpRequestData.</returns>
    private HttpRequestData CreateHttpRequestDataWithHeader(string? headerValue)
    {
        // var mockRequest = new Mock<HttpRequestData>();
        //
        // // Create a collection of headers and add the “x-forwarded-for” header
        // var headers = new HttpHeadersCollection();
        // headers.Add("x-forwarded-for", headerValue);
        // mockRequest.SetupGet(r => r.Headers).Returns(headers);
        //
        // return mockRequest.Object;

        var functionContext = new Mock<FunctionContext>().Object;
        var request = new FakeHttpRequestData(functionContext);
        if (headerValue != null)
        {
            request.Headers.Add("x-forwarded-for", headerValue);
        }

        return request;
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnIPv4_WhenIPv4WithoutPort()
    {
        // Arrange
        var request = CreateHttpRequestDataWithHeader("192.168.1.1");

        // Act
        var result = request.ExtractIpAddress();

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnIPv4_WhenIPv4WithPort()
    {
        // Arrange
        var request = CreateHttpRequestDataWithHeader("192.168.1.1:1234");

        // Act
        var result = request.ExtractIpAddress();

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnIPv6_WhenIPv6WithoutPort()
    {
        // Arrange
        var request = CreateHttpRequestDataWithHeader("2001:db8::1");

        // Act
        var result = request.ExtractIpAddress();

        // Assert
        result.Should().Be("2001:db8::1");
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnIPv6_WhenIPv6WithPort()
    {
        // Arrange
        var request = CreateHttpRequestDataWithHeader("[2001:db8::1]:1234");

        // Act
        var result = request.ExtractIpAddress();

        // Assert
        result.Should().Be("2001:db8::1");
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnEmpty_WhenHeaderMissing()
    {
        // Arrange
        var request = CreateHttpRequestDataWithHeader(null);

        // Act
        var result = request.ExtractIpAddress();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnEmpty_WhenHeaderEmpty()
    {
        // Arrange
        var request = CreateHttpRequestDataWithHeader("");

        // Act
        var result = request.ExtractIpAddress();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnFirstIp_WhenMultipleIpsProvided()
    {
        // Arrange
        // In the case of several addresses, only the first one should be extracted.
        var request = CreateHttpRequestDataWithHeader("192.168.1.1:1234, 10.0.0.1:4321");

        // Act
        var result = request.ExtractIpAddress();

        // Assert
        result.Should().Be("192.168.1.1");
    }
    
    [Test]
    public void ExtractIpAddress_ShouldReturnEmpty_WhenInputIsLocalhost()
    {
        var request = CreateHttpRequestDataWithHeader("localhost");
        var result = request.ExtractIpAddress();
        result.Should().BeEmpty();
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnIPv6Localhost_WhenInputIsIPv6Localhost()
    {
        var request = CreateHttpRequestDataWithHeader("::1");
        var result = request.ExtractIpAddress();
        result.Should().Be("::1");
    }

    [Test]
    public void ExtractIpAddress_ShouldReturnEmpty_WhenInputIsDumb()
    {
        var request = CreateHttpRequestDataWithHeader("DUMB");
        var result = request.ExtractIpAddress();
        result.Should().BeEmpty();
    }
}