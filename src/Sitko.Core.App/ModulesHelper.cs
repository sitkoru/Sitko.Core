namespace Sitko.Core.App;

public static class ModulesHelper
{
    public static IReadOnlyList<ApplicationModuleRegistration> GetEnabledModuleRegistrations(
        IApplicationContext context, IEnumerable<ApplicationModuleRegistration> moduleRegistrations) =>
        GetEnabledModuleRegistrations<IApplicationModule>(context, moduleRegistrations);

    public static IReadOnlyList<ApplicationModuleRegistration> GetEnabledModuleRegistrations<TModule>(
        IApplicationContext context, IEnumerable<ApplicationModuleRegistration> moduleRegistrations)
        where TModule : IApplicationModule =>
        moduleRegistrations
            .Where(r => r.GetInstance() is TModule && r.IsEnabled(context)).ToList();
}
