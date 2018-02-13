using Microsoft.Extensions.DependencyInjection;
using System;

namespace ComputerVisionExample
{
    public static class ServiceLocator
    {
        private static IServiceProvider serviceProvider;
        public static IServiceCollection Collection = new ServiceCollection();
        public static IServiceProvider Provider
        {
            get
            {
                if (serviceProvider == null)
                {
                    serviceProvider = Collection.BuildServiceProvider();
                }

                return serviceProvider;
            }
        }

        public static T GetRequiredService<T>()
        {
            return Provider.GetRequiredService<T>();
        }

        public static void RegisterService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            Collection.AddScoped<TService, TImplementation>();
        }

        public static void RegisterService<TService, TImplementation>(Func<IServiceProvider, TImplementation> serviceProviderFunction)
            where TService : class
            where TImplementation : class, TService
        {
            Collection.AddScoped<TService, TImplementation>(sp =>
            {
                return serviceProviderFunction(sp);
            });
        }
    }
}
