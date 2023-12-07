using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Blazor.Server;

public abstract class
    BaseRevalidatingServerAuthenticationStateProvider<TUserInfo> : RevalidatingServerAuthenticationStateProvider
{
    private readonly PersistentComponentState state;

    private readonly PersistingComponentStateSubscription subscription;

    private Task<AuthenticationState>? authenticationStateTask;

    protected BaseRevalidatingServerAuthenticationStateProvider(ILoggerFactory loggerFactory,
        PersistentComponentState persistentComponentState) : base(loggerFactory)
    {
        state = persistentComponentState;

        AuthenticationStateChanged += OnAuthenticationStateChanged;
        subscription = state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) => authenticationStateTask = task;

    private async Task OnPersistingAsync()
    {
        if (authenticationStateTask is null)
        {
            throw new UnreachableException($"Authentication state not set in {nameof(OnPersistingAsync)}().");
        }

        var authenticationState = await authenticationStateTask;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            var userInfo = GetUserInfo(principal);
            if (userInfo is not null)
            {
                state.PersistAsJson(typeof(TUserInfo).FullName!, userInfo);
            }
        }
    }

    protected abstract TUserInfo? GetUserInfo(ClaimsPrincipal principal);

    protected override void Dispose(bool disposing)
    {
        subscription.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
        base.Dispose(disposing);
    }
}
