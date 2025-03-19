using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace ByteSync.TestsCommon.TestHelpers;

public class TestableHubConnection : HubConnection
{
    private static readonly TestConnectionContext ConnectionContext = new();
    private static readonly TestHubProtocol Protocol = new();
    private static readonly TestEndPoint EndPoint = new();
    private static readonly TestServiceProvider ServiceProvider = new();
    private static readonly TestLoggerFactory LoggerFactory = new();

    public TestableHubConnection()
        : base(new TestConnectionFactory(), Protocol, EndPoint, ServiceProvider, LoggerFactory)
    {
    }

    // Mock implementations to provide constructor parameters
    private class TestConnectionFactory : Microsoft.AspNetCore.Connections.IConnectionFactory
    {
        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<ConnectionContext>(ConnectionContext);
        }
    }

    private class TestConnectionContext : ConnectionContext
    {
        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override IDictionary<object, object?> Items { get; set; }
        public override IDuplexPipe Transport { get; set; } = null!;
    }

    private class TestHubProtocol : IHubProtocol
    {
        public string Name => "test-protocol";
        public int Version => 1;
        public TransferFormat TransferFormat => TransferFormat.Binary;

        public bool IsVersionSupported(int version) => true;
        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message) => new byte[0];
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            message = null!;
            return false;
        }
        public void WriteMessage(HubMessage message, IBufferWriter<byte> output) { }
    }

    private class TestEndPoint : EndPoint { }

    private class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private class TestLoggerFactory : ILoggerFactory
    {
        public void Dispose() { }
        public ILogger CreateLogger(string categoryName) => new TestLogger();
        public void AddProvider(ILoggerProvider provider) { }
    }

    private class TestLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new TestDisposable();
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }

    // Override the abstract/virtual methods we need
    public override Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public override Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}