using System.Collections.Concurrent;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Moq;

namespace ByteSync.TestsCommon.Mocking;

public class MoqRegistrationSource : IRegistrationSource
{
    private readonly ConcurrentDictionary<Type, object> _mocks = new();

    public bool IsAdapterForIndividualComponents => false;

    public IEnumerable<IComponentRegistration> RegistrationsFor(
        Service service,
        Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        if (service is TypedService typedService)
        {
            var serviceType = typedService.ServiceType;

            // Mock<T> resolution
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(Mock<>))
            {
                var mockType = serviceType;
                var mockServiceType = mockType.GetGenericArguments()[0];

                if (!mockServiceType.IsInterface && !mockServiceType.IsAbstract)
                    yield break;

                yield return RegistrationBuilder
                    .ForDelegate((c, p) =>
                    {
                        return _mocks.GetOrAdd(mockServiceType, t => Activator.CreateInstance(mockType));
                    })
                    .As(serviceType)
                    .InstancePerLifetimeScope()
                    .CreateRegistration();
            }
            // Resolve T as Mock<T>.Object
            else if (serviceType.IsInterface || serviceType.IsAbstract)
            {
                var mockType = typeof(Mock<>).MakeGenericType(serviceType);

                yield return RegistrationBuilder
                    .ForDelegate((c, p) =>
                    {
                        var mock = (Mock)_mocks.GetOrAdd(serviceType, t => Activator.CreateInstance(mockType));
                        return mock.Object;
                    })
                    .As(serviceType)
                    .InstancePerLifetimeScope()
                    .CreateRegistration();
            }
        }
    }

    public override string ToString()
    {
        return "MoqRegistrationSource";
    }
}