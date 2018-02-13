using Microsoft.Extensions.DependencyInjection;

namespace ComputerVisionExample
{
    public static class ServiceLocator
    {
        public static IServiceCollection Collection = new ServiceCollection();
        public static ServiceProvider Provider
        {
            get
            {
                if (Provider == null)
                {
                    return Collection.BuildServiceProvider();
                }

                return Provider;
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
    }
}
