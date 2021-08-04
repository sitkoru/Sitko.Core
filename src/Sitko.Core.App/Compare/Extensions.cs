using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sitko.Core.App.Compare
{
    [PublicAPI]
    public static class Extensions
    {
        public static IServiceCollection AddCompareLogicConfigurator<TConfigurator>(
            this IServiceCollection serviceCollection) where TConfigurator : class, ICompareLogicConfigurator
        {
            serviceCollection.TryAddScoped<ICompareLogicConfigurator, TConfigurator>();
            return serviceCollection;
        }

        public static Application AddCompareLogicConfigurator<TConfigurator>(this Application application)
            where TConfigurator : class, ICompareLogicConfigurator =>
            application.ConfigureServices(collection =>
            {
                collection.AddCompareLogicConfigurator<TConfigurator>();
            });
    }
}
