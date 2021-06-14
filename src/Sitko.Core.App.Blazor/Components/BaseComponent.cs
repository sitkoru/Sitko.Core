using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Blazor.Components
{
    public abstract class BaseComponent : OwningComponentBase
    {
        protected bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        protected ILogger<BaseComponent> Logger { get; private set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Logger = GetService<ILogger<BaseComponent>>();
        }

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
            StateHasChanged();
        }

        protected void StopLoading()
        {
            IsLoading = false;
            StateHasChanged();
        }

        protected Task StartLoadingAsync()
        {
            IsLoading = true;
            return NotifyStateChangeAsync();
        }

        protected Task StopLoadingAsync()
        {
            IsLoading = false;
            return NotifyStateChangeAsync();
        }

        public Task NotifyStateChangeAsync()
        {
            return InvokeAsync(StateHasChanged);
        }
    }
}
