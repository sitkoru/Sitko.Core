using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace Sitko.Core.App.Blazor.Components
{
    public interface IBaseComponent
    {
        Task NotifyStateChangeAsync();
    }

    public abstract class BaseComponent : ComponentBase, IAsyncDisposable, IDisposable
    {
        private static readonly FieldInfo? RenderFragment = typeof(ComponentBase).GetField("_renderFragment",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo? HasPendingQueuedRender = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo? HasNeverRendered = typeof(ComponentBase).GetField("_hasNeverRendered",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private bool isDisposed;
        private bool isInitialized;

        private ILogger? logger;
#if NET6_0_OR_GREATER
        private AsyncServiceScope? scope;
#else
        private IServiceScope? scope;
#endif

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
                if (isInitialized) // do not call BuildRenderTree before we initialized
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

        [PublicAPI] public Guid ComponentId { get; } = Guid.NewGuid();

        [CascadingParameter(Name = "ParentScope")]
        private IServiceScope? ParentScope { get; set; }

        [Parameter] public virtual ScopeType ScopeType { get; set; } = ScopeType.Parent;

        [Inject] private IServiceProvider GlobalServiceProvider { get; set; } = null!;

        private IServiceProvider ServiceProvider =>
            scope?.ServiceProvider ?? ParentScope?.ServiceProvider ?? GlobalServiceProvider;

        [PublicAPI] public bool IsLoading { get; private set; }

        [PublicAPI]
        protected NavigationManager NavigationManager => GlobalServiceProvider.GetRequiredService<NavigationManager>();

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

        public async ValueTask DisposeAsync()
        {
            if (!isDisposed)
            {
                if (scope is not null)
                {
#if NET6_0_OR_GREATER
                    await scope.Value.DisposeAsync();
#else
                    scope.Dispose();
#endif
                }

                NavigationManager.LocationChanged -= HandleLocationChanged;
                Dispose(true);
                await DisposeAsync(true);
                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                throw new InvalidOperationException(
                    "This class must not be disposed synchronously. This method only here to avoid exception on sync scope dispose in .NET 5");
            }

            GC.SuppressFinalize(this);
        }

        public override string ToString() => $"{GetType().Name} {ComponentId}";

        public Task NotifyStateChangeAsync() => InvokeAsync(StateHasChanged);

        protected sealed override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (ScopeType == ScopeType.Isolated)
            {
#if NET6_0_OR_GREATER
                scope = ServiceProvider.CreateAsyncScope();
#else
                scope = ServiceProvider.CreateScope();
#endif
            }

            NavigationManager.LocationChanged += HandleLocationChanged;
            // ReSharper disable once MethodHasAsyncOverload
            Initialize();
            await InitializeAsync();
            isInitialized = true;
        }

        protected T? GetQueryString<T>(string key) => TryGetQueryString<T>(key, out var value) ? value : default;

        protected bool TryGetQueryString<T>(string key, out T? value)
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(key, out var valueFromQueryString))
            {
                if (typeof(T) == typeof(int) && int.TryParse(valueFromQueryString, out var valueAsInt))
                {
                    value = (T)(object)valueAsInt;
                    return true;
                }

                if (typeof(T) == typeof(string))
                {
                    value = (T)(object)valueFromQueryString.ToString();
                    return true;
                }

                if (typeof(T) == typeof(decimal) && decimal.TryParse(valueFromQueryString, out var valueAsDecimal))
                {
                    value = (T)(object)valueAsDecimal;
                    return true;
                }

                if (typeof(T) == typeof(double) && double.TryParse(valueFromQueryString, out var valueAsDouble))
                {
                    value = (T)(object)valueAsDouble;
                    return true;
                }

                if (typeof(T) == typeof(Guid) && Guid.TryParse(valueFromQueryString, out var valueAsGuid))
                {
                    value = (T)(object)valueAsGuid;
                    return true;
                }
            }

            value = default;
            return false;
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

        protected override bool ShouldRender() => isInitialized;

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

        protected async Task StartLoadingAsync()
        {
            IsLoading = true;
            await OnStartLoadingAsync();
            await NotifyStateChangeAsync();
        }

        protected async Task StopLoadingAsync()
        {
            IsLoading = false;
            await OnStopLoadingAsync();
            await NotifyStateChangeAsync();
        }

        [PublicAPI]
        protected virtual void OnStartLoading()
        {
        }

        [PublicAPI]
        protected virtual void OnStopLoading()
        {
        }

        [PublicAPI]
        protected virtual Task OnStartLoadingAsync()
        {
            OnStartLoading();
            return Task.CompletedTask;
        }

        [PublicAPI]
        protected virtual Task OnStopLoadingAsync()
        {
            OnStopLoading();
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
}
