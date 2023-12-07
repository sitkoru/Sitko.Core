using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Sitko.Core.Blazor.Wasm;

public abstract class BasePersistentAuthenticationStateProvider<TUser>: AuthenticationStateProvider
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Task<AuthenticationState> DefaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> authenticationStateTask = DefaultUnauthenticatedTask;

    protected BasePersistentAuthenticationStateProvider(PersistentComponentState state)
    {
        if (!state.TryTakeFromJson<TUser>(nameof(TUser), out var userInfo) || userInfo is null)
        {
            return;
        }

        // ReSharper disable once VirtualMemberCallInConstructor
        var claims = GetClaims(userInfo);

        authenticationStateTask = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims,
                authenticationType: nameof(BasePersistentAuthenticationStateProvider<TUser>)))));
    }

    protected abstract Claim[] GetClaims(TUser user);

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => authenticationStateTask;
}
