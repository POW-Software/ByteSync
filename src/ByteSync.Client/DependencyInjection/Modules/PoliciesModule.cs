using System.Net.Http;
using Autofac;
using ByteSync.Helpers;
using Polly;
using Polly.Extensions.Http;

namespace ByteSync.DependencyInjection.Modules;

public class PoliciesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(context =>
        {
            var logger = context.Resolve<ILogger<RetryPolicyLogger>>();
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, _) =>
                    {
                        var request = outcome.Result?.RequestMessage;
                        var requestInfo = $"{request?.Method} {request?.RequestUri}";

                        if (outcome.Exception != null)
                        {
                            logger.LogWarning(outcome.Exception, 
                                "Retry attempt {RetryCount} for {Request} after {Delay} seconds due to exception",
                                retryAttempt, requestInfo, timespan.TotalSeconds);
                        }
                        else
                        {
                            logger.LogWarning(
                                "Retry attempt {RetryCount} for {Request} after {Delay} seconds due to HTTP status code {StatusCode}",
                                retryAttempt, requestInfo, timespan.TotalSeconds, outcome.Result?.StatusCode);
                        }
                    });
        }).As<IAsyncPolicy<HttpResponseMessage>>().SingleInstance();
    }
}