using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.Blazor.Server;
using Sitko.Core.Blazor.Wasm;
using Xunit;

namespace Sitko.Core.App.Blazor.Tests
{
    public class PersistentAuthenticationStateTests
    {
        [Fact]
        public async Task AuthenticatedServerUserIsRestoredWithClaimsOnClient()
        {
            var builder = WebApplication.CreateBuilder();
            _ = builder.Services.AddRazorComponents();
            await using var app = builder.Build();
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var serverState = CreateStateManager(app.Services);
            using var serverProvider = new TestServerAuthenticationStateProvider(loggerFactory, serverState.State);
            serverProvider.SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-42"), new Claim(ClaimTypes.Role, "Editor")],
                    "test")))));

            var store = new MemoryStateStore();
            await using var renderer = new StaticHtmlRenderer(app.Services, loggerFactory);
            await renderer.Dispatcher.InvokeAsync(() => serverState.PersistStateAsync(store, renderer));

            Assert.NotEmpty(store.Keys);

            var clientState = CreateStateManager(app.Services);
            await clientState.RestoreStateAsync(store);
            var clientProvider = new TestClientAuthenticationStateProvider(clientState.State);
            var clientPrincipal = (await clientProvider.GetAuthenticationStateAsync()).User;

            Assert.True(clientPrincipal.Identity?.IsAuthenticated,
                $"Persisted keys: {string.Join(", ", store.Keys)}");
            Assert.Equal("user-42", clientPrincipal.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal("Editor", clientPrincipal.FindFirstValue(ClaimTypes.Role));
        }

        [Fact]
        public async Task MissingPersistedUserIsAnonymousOnClient()
        {
            var builder = WebApplication.CreateBuilder();
            await using var app = builder.Build();
            var clientState = CreateStateManager(app.Services);
            await clientState.RestoreStateAsync(new MemoryStateStore());

            var clientProvider = new TestClientAuthenticationStateProvider(clientState.State);
            var clientPrincipal = (await clientProvider.GetAuthenticationStateAsync()).User;

            Assert.False(clientPrincipal.Identity?.IsAuthenticated);
            Assert.Empty(clientPrincipal.Claims);
        }

        private static ComponentStatePersistenceManager CreateStateManager(IServiceProvider services) =>
            new(services.GetRequiredService<ILogger<ComponentStatePersistenceManager>>(), services);

        private sealed class MemoryStateStore : IPersistentComponentStateStore
        {
            private IDictionary<string, byte[]> state = new Dictionary<string, byte[]>();

            public ICollection<string> Keys => state.Keys;

            public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => Task.FromResult(state);

            public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
            {
                this.state = new Dictionary<string, byte[]>(state);
                return Task.CompletedTask;
            }

            public bool SupportsRenderMode(IComponentRenderMode renderMode) => true;
        }

        private sealed class TestServerAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            PersistentComponentState state) : BaseRevalidatingServerAuthenticationStateProvider<TestUserInfo>(loggerFactory, state)
        {
            protected override TestUserInfo? GetUserInfo(ClaimsPrincipal principal) =>
                new(principal.FindFirstValue(ClaimTypes.NameIdentifier)!, principal.FindFirstValue(ClaimTypes.Role)!);
        }

        private sealed class TestClientAuthenticationStateProvider(PersistentComponentState state)
            : BasePersistentAuthenticationStateProvider<TestUserInfo>(state)
        {
            protected override Claim[] GetClaims(TestUserInfo user) =>
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Role, user.Role)
            ];
        }

        public sealed record TestUserInfo(string UserId, string Role);
    }
}
