using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Blazor.Components
{
    public interface IBaseComponent
    {
        Task NotifyStateChangeAsync();
    }

    public abstract class BaseComponent : OwningComponentBase, IBaseComponent
    {
        protected bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
        protected ILogger<BaseComponent> Logger => GetService<ILogger<BaseComponent>>();

        protected void MarkAsInitialized()
        {
            IsInitialized = true;
        }

        protected TService GetService<TService>()
        {
            return ScopedServices.GetRequiredService<TService>();
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

        public Task NotifyStateChangeAsync()
        {
            return InvokeAsync(StateHasChanged);
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
    }
}
