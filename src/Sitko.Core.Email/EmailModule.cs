using Sitko.Core.App;

namespace Sitko.Core.Email;

public interface IEmailModule : IApplicationModule;

public abstract class EmailModule<TEmailModuleOptions> : BaseApplicationModule<TEmailModuleOptions>, IEmailModule
    where TEmailModuleOptions : EmailModuleOptions, new();
