using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;
using Sitko.Core.App.Localization;

namespace Sitko.Core.Blazor.Components;

public interface IBaseComponent
{
    Task NotifyStateChangeAsync();
}

public abstract class BaseComponent : ComponentBase, IAsyncDisposable
{
    private static readonly FieldInfo? RenderFragment = typeof(ComponentBase).GetField("_renderFragment",
        BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? HasPendingQueuedRender = typeof(ComponentBase).GetField(
        "_hasPendingQueuedRender",
        BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? HasNeverRendered = typeof(ComponentBase).GetField("_hasNeverRendered",
        BindingFlags.NonPublic | BindingFlags.Instance);

    private bool isDisposed;

    private ILocalizationProvider? localizationProvider;

    private ILogger? logger;

    private NavigationManager? navigationManager;
    private AsyncServiceScope? scope;

    protected BaseComponent()
    {
        if (RenderFragment is null || HasPendingQueuedRender is null || HasNeverRendered is null)
        {
            throw new InvalidOperationException("Can't find BaseComponent properties");
        }

        RenderFragment.SetValue(this, (RenderFragment)(builder =>
        {
            HasPendingQueuedRender.SetValue(this, false);
            HasNeverRendered.SetValue(this, false);
            if (IsInitialized) // do not call BuildRenderTree before we initialized
            {
                if (ScopeType == ScopeType.Isolated)
                {
                    builder.OpenComponent<CascadingValue<IServiceScope>>(1);
                    builder.AddAttribute(2, "Value", scope);
                    builder.AddAttribute(3, "Name", "ParentScope");
                    builder.AddAttribute(4, "ChildContent", (RenderFragment)BuildRenderTree);
                    builder.CloseComponent();
                }
                else
                {
                    BuildRenderTree(builder);
                }
            }
        }));
    }

    protected bool IsInitialized { get; private set; }

    [PublicAPI] public Guid ComponentId { get; } = Guid.NewGuid();

    [CascadingParameter(Name = "ParentScope")]
    private IServiceScope? ParentScope { get; set; }

    [Parameter] public virtual ScopeType ScopeType { get; set; } = ScopeType.Parent;

    [Inject] private IServiceProvider GlobalServiceProvider { get; set; } = null!;

    private IServiceProvider ServiceProvider =>
        scope?.ServiceProvider ?? ParentScope?.ServiceProvider ?? GlobalServiceProvider;

    [PublicAPI] public bool IsLoading { get; private set; }

    [PublicAPI]
    protected NavigationManager NavigationManager =>
        navigationManager ??= GlobalServiceProvider.GetRequiredService<NavigationManager>();

    protected ILogger Logger
    {
        get
        {
            if (logger is null)
            {
                var loggerType = typeof(ILogger<>);
                var componentLoggerType = loggerType.MakeGenericType(GetType());
                logger = GetRequiredService<ILogger>(componentLoggerType);
            }

            return logger;
        }
    }

    protected ILocalizationProvider LocalizationProvider
    {
        get
        {
            if (localizationProvider is null)
            {
                var localizationProviderType = typeof(ILocalizationProvider<>);
                var componentLocalizationProviderType = localizationProviderType.MakeGenericType(GetType());
                localizationProvider = GetRequiredService<ILocalizationProvider>(componentLocalizationProviderType);
            }

            return localizationProvider;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!isDisposed)
        {
            NavigationManager.LocationChanged -= HandleLocationChanged;
            Dispose(true);
            await DisposeAsync(true);
            if (scope is not null)
            {
                await scope.Value.DisposeAsync();
            }

            isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }

    public override string ToString() => $"{GetType().Name} {ComponentId}";

    public Task NotifyStateChangeAsync() => InvokeAsync(StateHasChanged);

    protected sealed override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (ScopeType == ScopeType.Isolated)
        {
            scope = ServiceProvider.CreateAsyncScope();
        }

        try
        {
            NavigationManager.LocationChanged += HandleLocationChanged;
            // ReSharper disable once MethodHasAsyncOverload
            Initialize();
            await InitializeAsync();
            IsInitialized = true;
            // ReSharper disable once MethodHasAsyncOverload
            AfterInitialized();
            await AfterInitializedAsync();
        }
        catch (ObjectDisposedException)
        {
            // We got ObjectDisposedException during initialization. It may be caused by cancelled request. In such case
            // ParentScope would be disposed before component initialization is completed.
            var parentDisposed = false;
            if (ParentScope is not null)
            {
                // We have parent scope. Let's check if it is disposed
                try
                {
                    var unused = ParentScope.ServiceProvider.CreateScope();
                }
                catch (ObjectDisposedException)
                {
                    // Scope is disposed
                    parentDisposed = true;
                }
            }

            if (!parentDisposed)
            {
                // Parent scope was not disposed, so problem lies inside initialization code
                // Pass exception
                throw;
            }

            // Parent scope was disposed. Indicate in logs and suppress exception.
            GlobalServiceProvider.GetRequiredService<ILogger<BaseComponent>>()
                .Log(
                    GlobalServiceProvider.GetRequiredService<IApplicationContext>().IsDevelopment()
                        ? LogLevel.Warning
                        : LogLevel.Debug, "Parent scope was disposed in {Component}", GetType());
        }
    }

    protected T? GetQueryString<T>(string key) => TryGetQueryString<T>(key, out var value) ? value : default;

    protected bool TryGetQueryString<T>(string key, [NotNullWhen(true)] out T? value)
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

        return ParseQueryStringHelper.TryGetQueryString(uri.Query, key, out value);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(async () =>
        {
            await Task.Delay(1);
            await OnLocationChangeAsync(e.Location, e.IsNavigationIntercepted);
            StateHasChanged();
        });

    protected virtual Task OnLocationChangeAsync(string location, bool isNavigationIntercepted) =>
        Task.CompletedTask;

    protected sealed override void OnInitialized() => base.OnInitialized();

    protected virtual Task InitializeAsync() => Task.CompletedTask;

    protected virtual void Initialize()
    {
    }

    protected virtual Task AfterInitializedAsync() => Task.CompletedTask;

    protected virtual void AfterInitialized()
    {
    }

    protected override bool ShouldRender() => IsInitialized;

    [PublicAPI]
    protected TService? GetService<TService>() where TService : notnull => ServiceProvider.GetService<TService>();

    [PublicAPI]
    protected TService? GetService<TService>(Type type) where TService : class =>
        ServiceProvider.GetService(type) as TService;

    [PublicAPI]
    protected TService GetRequiredService<TService>() where TService : notnull =>
        ServiceProvider.GetRequiredService<TService>();

    [PublicAPI]
    protected TService GetRequiredService<TService>(Type type) where TService : class =>
        ServiceProvider.GetRequiredService(type) as TService ??
        throw new InvalidOperationException($"Can't resolver service {type}");

    [PublicAPI]
    protected IEnumerable<TService> GetServices<TService>() => ServiceProvider.GetServices<TService>();

    [PublicAPI]
    protected IServiceScope CreateServicesScope() => ServiceProvider.CreateScope();

    protected async Task<TResult> ExecuteServiceOperation<TService, TResult>(
        Func<TService, Task<TResult>> operation) where TService : notnull
    {
        using var serviceScope = CreateServicesScope();
        var repository = serviceScope.ServiceProvider.GetRequiredService<TService>();
        var result = await operation.Invoke(repository);
        return result;
    }

    [PublicAPI]
    protected void StartLoading()
    {
        IsLoading = true;
        OnStartLoading();
        StateHasChanged();
    }


    [PublicAPI]
    protected void StopLoading()
    {
        IsLoading = false;
        OnStopLoading();
        StateHasChanged();
    }

    protected async Task StartLoadingAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        await OnStartLoadingAsync(cancellationToken);
        await NotifyStateChangeAsync();
    }

    protected async Task StopLoadingAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = false;
        await OnStopLoadingAsync(cancellationToken);
        await NotifyStateChangeAsync();
    }

    [PublicAPI]
    protected virtual void OnStartLoading(CancellationToken cancellationToken = default)
    {
    }

    [PublicAPI]
    protected virtual void OnStopLoading(CancellationToken cancellationToken = default)
    {
    }

    [PublicAPI]
    protected virtual Task OnStartLoadingAsync(CancellationToken cancellationToken = default)
    {
        OnStartLoading(cancellationToken);
        return Task.CompletedTask;
    }

    [PublicAPI]
    protected virtual Task OnStopLoadingAsync(CancellationToken cancellationToken = default)
    {
        OnStopLoading(cancellationToken);
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    protected virtual Task DisposeAsync(bool disposing) => Task.CompletedTask;
}

public enum ScopeType
{
    Parent = 0, Isolated = 1
}
