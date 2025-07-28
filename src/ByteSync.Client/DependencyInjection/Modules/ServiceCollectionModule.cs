using System.Net.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ByteSync.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace ByteSync.DependencyInjection.Modules;

 public class ServiceCollectionModule : Module
    {
        private readonly IServiceCollection _services;

        public ServiceCollectionModule(IServiceCollection services)
        {
            _services = services;
        }

        protected override void Load(ContainerBuilder builder)
        {
            ConfigureServices(_services);
            
            builder.Populate(_services);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient("ApiClient")
                .AddPolicyHandler((serviceProvider, request) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<RetryPolicyLogger>>();
                    return GetRetryPolicy(request, logger);
                });
        }

        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(HttpRequestMessage request, ILogger<RetryPolicyLogger> logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, _) =>
                    {
                        var requestInfo = $"{request.Method} {request.RequestUri}";

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
        }
    }