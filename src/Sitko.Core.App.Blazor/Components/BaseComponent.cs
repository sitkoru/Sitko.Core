using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Blazor.Components
{
    public interface IBaseComponent
    {
        Task NotifyStateChangeAsync();
    }

    public abstract class BaseComponent : IComponent, IHandleEvent, IHandleAfterRender, IBaseComponent, IDisposable
    {
        private bool _isInitialized;

        protected BaseComponent()
        {
            _renderFragment = builder =>
            {
                _hasPendingQueuedRender = false;
                _hasNeverRendered = false;
                if (_isInitialized) // do not call BuildRenderTree before we initialized
                {
                    BuildRenderTree(builder);
                }
            };
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public bool IsLoading { get; private set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;

        protected ILogger Logger
        {
            get
            {
                var validatorType = typeof(ILogger<>);
                var formValidatorType = validatorType.MakeGenericType(GetType());
                return (ScopedServices.GetRequiredService(formValidatorType) as ILogger)!;
            }
        }

        public Task NotifyStateChangeAsync()
        {
            return InvokeAsync(StateHasChanged);
        }

        private async Task OnInitializedAsync()
        {
            // ReSharper disable once MethodHasAsyncOverload
            Initialize();
            await InitializeAsync();
            _isInitialized = true;
        }

        protected virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void Initialize()
        {
        }

        protected virtual bool ShouldRender() => _isInitialized;

        protected TService GetService<TService>()
        {
#pragma warning disable 8714
            return ScopedServices.GetRequiredService<TService>();
#pragma warning restore 8714
        }

        protected IEnumerable<TService> GetServices<TService>()
        {
            return ScopedServices.GetServices<TService>();
        }

        protected TService GetScopedService<TService>()
        {
#pragma warning disable 8714
            return ScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TService>();
#pragma warning restore 8714
        }

        protected IEnumerable<TService> GetScopedServices<TService>()
        {
            return ScopeFactory.CreateScope().ServiceProvider.GetServices<TService>();
        }

        protected IServiceScope CreateServicesScope()
        {
            return ScopeFactory.CreateScope();
        }

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
        private readonly RenderFragment _renderFragment;
        private RenderHandle _renderHandle;
        private bool _initialized;
        private bool _hasNeverRendered = true;
        private bool _hasPendingQueuedRender;
        private bool _hasCalledOnAfterRender;
        private IServiceScope? _scope;
        protected bool IsDisposed { get; private set; }

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

                _scope ??= ScopeFactory.CreateScope();
                return _scope.ServiceProvider;
            }
        }

        protected void StateHasChanged()
        {
            if (_hasPendingQueuedRender)
            {
                return;
            }

            if (_hasNeverRendered || ShouldRender())
            {
                _hasPendingQueuedRender = true;

                try
                {
                    _renderHandle.Render(_renderFragment);
                }
                catch
                {
                    _hasPendingQueuedRender = false;
                    throw;
                }
            }
        }

        protected Task InvokeAsync(Action workItem)
            => _renderHandle.Dispatcher.InvokeAsync(workItem);

        void IDisposable.Dispose()
        {
            if (!IsDisposed)
            {
                _scope?.Dispose();
                _scope = null;
                Dispose(disposing: true);
                IsDisposed = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Attach(RenderHandle renderHandle)
        {
            if (_renderHandle.IsInitialized)
            {
                throw new InvalidOperationException(
                    $"The render handle is already set. Cannot initialize a {nameof(ComponentBase)} more than once.");
            }

            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            if (!_initialized)
            {
                _initialized = true;

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

        public Task HandleEventAsync(EventCallbackWorkItem callback, object arg)
        {
            var task = callback.InvokeAsync(arg);
            var shouldAwaitTask = task.Status != TaskStatus.RanToCompletion &&
                                  task.Status != TaskStatus.Canceled;

            StateHasChanged();

            return shouldAwaitTask ? CallStateHasChangedOnAsyncCompletion(task) : Task.CompletedTask;
        }

        public Task OnAfterRenderAsync()
        {
            var firstRender = !_hasCalledOnAfterRender;
            _hasCalledOnAfterRender = true;

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
