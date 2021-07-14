using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Blazor.Components
{
    using JetBrains.Annotations;

    public interface IBaseComponent
    {
        Task NotifyStateChangeAsync();
    }

    public abstract class BaseComponent : IComponent, IHandleEvent, IHandleAfterRender, IBaseComponent, IAsyncDisposable
    {
        private bool isInitialized;

        protected BaseComponent() =>
            renderFragment = builder =>
            {
                hasPendingQueuedRender = false;
                hasNeverRendered = false;
                if (isInitialized) // do not call BuildRenderTree before we initialized
                {
                    BuildRenderTree(builder);
                }
            };

        [PublicAPI] public bool IsLoading { get; private set; }

        [PublicAPI] [Inject] protected NavigationManager NavigationManager { get; set; } = null!;

        protected ILogger Logger
        {
            get
            {
                var validatorType = typeof(ILogger<>);
                var formValidatorType = validatorType.MakeGenericType(GetType());
                return (ScopedServices.GetRequiredService(formValidatorType) as ILogger)!;
            }
        }

        public Task NotifyStateChangeAsync() => InvokeAsync(StateHasChanged);

        private async Task OnInitializedAsync()
        {
            // ReSharper disable once MethodHasAsyncOverload
            Initialize();
            await InitializeAsync();
            isInitialized = true;
        }

        protected virtual Task InitializeAsync() => Task.CompletedTask;

        protected virtual void Initialize()
        {
        }

        protected virtual bool ShouldRender() => isInitialized;

        protected TService GetService<TService>()
        {
#pragma warning disable 8714
            return ScopedServices.GetRequiredService<TService>();
#pragma warning restore 8714
        }

        [PublicAPI]
        protected IEnumerable<TService> GetServices<TService>() => ScopedServices.GetServices<TService>();

        [PublicAPI]
        protected TService GetScopedService<TService>()
        {
#pragma warning disable 8714
            return ScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TService>();
#pragma warning restore 8714
        }

        [PublicAPI]
        protected IEnumerable<TService> GetScopedServices<TService>() =>
            ScopeFactory.CreateScope().ServiceProvider.GetServices<TService>();

        [PublicAPI]
        protected IServiceScope CreateServicesScope() => ScopeFactory.CreateScope();

        protected void StartLoading()
        {
            IsLoading = true;
            OnStartLoading();
            StateHasChanged();
        }


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

        protected virtual void OnStartLoading()
        {
        }

        protected virtual void OnStopLoading()
        {
        }

        protected virtual Task OnStartLoadingAsync()
        {
            OnStartLoading();
            return Task.CompletedTask;
        }

        protected virtual Task OnStopLoadingAsync()
        {
            OnStopLoading();
            return Task.CompletedTask;
        }

        #region BaseComponent and OwningComponentBase stuff

        [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = null!;
        private readonly RenderFragment renderFragment;
        private RenderHandle componentRenderHandle;
        private bool initialized;
        private bool hasNeverRendered = true;
        private bool hasPendingQueuedRender;
        private bool hasCalledOnAfterRender;
        private IServiceScope? scope;
        private bool IsDisposed { get; set; }

        protected virtual void BuildRenderTree(RenderTreeBuilder builder)
        {
        }

        protected IServiceProvider ScopedServices
        {
            get
            {
                if (ScopeFactory == null)
                {
                    throw new InvalidOperationException(
                        "Services cannot be accessed before the component is initialized.");
                }

                if (IsDisposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                scope ??= ScopeFactory.CreateScope();
                return scope.ServiceProvider;
            }
        }

        [PublicAPI]
        protected void StateHasChanged()
        {
            if (hasPendingQueuedRender)
            {
                return;
            }

            if (hasNeverRendered || ShouldRender())
            {
                hasPendingQueuedRender = true;

                try
                {
                    componentRenderHandle.Render(renderFragment);
                }
                catch
                {
                    hasPendingQueuedRender = false;
                    throw;
                }
            }
        }

        [PublicAPI]
        protected Task InvokeAsync(Action workItem) =>
            IsDisposed ? Task.CompletedTask : componentRenderHandle.Dispatcher.InvokeAsync(workItem);

        [PublicAPI]
        protected Task InvokeAsync(Func<Task> workItem) =>
            IsDisposed ? Task.CompletedTask : componentRenderHandle.Dispatcher.InvokeAsync(workItem);

        public async ValueTask DisposeAsync()
        {
            if (!IsDisposed)
            {
                Dispose(true);
                await DisposeAsync(true);
                scope?.Dispose();
                scope = null;
                IsDisposed = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected virtual Task DisposeAsync(bool disposing) => Task.CompletedTask;

        public void Attach(RenderHandle renderHandle)
        {
            if (this.componentRenderHandle.IsInitialized)
            {
                throw new InvalidOperationException(
                    $"The render handle is already set. Cannot initialize a {nameof(ComponentBase)} more than once.");
            }

            this.componentRenderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            if (!initialized)
            {
                initialized = true;

                return RunInitAndSetParametersAsync();
            }

            return CallOnParametersSetAsync();
        }

        private async Task RunInitAndSetParametersAsync()
        {
            var task = OnInitializedAsync();

            if (task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Canceled)
            {
                StateHasChanged();

                try
                {
                    await task;
                }
                catch
                {
                    if (!task.IsCanceled)
                    {
                        throw;
                    }
                }
            }

            await CallOnParametersSetAsync();
        }

        protected virtual void OnParametersSet()
        {
        }

        protected virtual Task OnParametersSetAsync()
            => Task.CompletedTask;

        private Task CallOnParametersSetAsync()
        {
            OnParametersSet();
            var task = OnParametersSetAsync();
            var shouldAwaitTask = task.Status != TaskStatus.RanToCompletion &&
                                  task.Status != TaskStatus.Canceled;
            StateHasChanged();

            return shouldAwaitTask ? CallStateHasChangedOnAsyncCompletion(task) : Task.CompletedTask;
        }

        private async Task CallStateHasChangedOnAsyncCompletion(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                if (task.IsCanceled)
                {
                    return;
                }

                throw;
            }

            StateHasChanged();
        }

        public Task HandleEventAsync(EventCallbackWorkItem item, object? arg)
        {
            var task = item.InvokeAsync(arg);
            var shouldAwaitTask = task.Status != TaskStatus.RanToCompletion &&
                                  task.Status != TaskStatus.Canceled;

            StateHasChanged();

            return shouldAwaitTask ? CallStateHasChangedOnAsyncCompletion(task) : Task.CompletedTask;
        }

        public Task OnAfterRenderAsync()
        {
            var firstRender = !hasCalledOnAfterRender;
            hasCalledOnAfterRender = true;

            OnAfterRender(firstRender);

            return OnAfterRenderAsync(firstRender);
        }

        protected virtual void OnAfterRender(bool firstRender)
        {
        }

        protected virtual Task OnAfterRenderAsync(bool firstRender)
            => Task.CompletedTask;

        #endregion
    }
}
