using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
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
        protected ILogger Logger
        {
            get
            {
                var validatorType = typeof(ILogger<>);
                var formValidatorType = validatorType.MakeGenericType(GetType());
                return (ScopedServices.GetRequiredService(formValidatorType) as ILogger)!;
            }
        }

        protected void MarkAsInitialized()
        {
            IsInitialized = true;
        }

        protected TService GetService<TService>()
        {
#pragma warning disable 8714
            return ScopedServices.GetRequiredService<TService>();
#pragma warning restore 8714
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
