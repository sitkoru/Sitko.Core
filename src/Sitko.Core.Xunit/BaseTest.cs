using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Sitko.Core.Xunit;

[PublicAPI]
public abstract class BaseTest : IAsyncLifetime
{
    protected BaseTest(ITestOutputHelper testOutputHelper) => TestOutputHelper = testOutputHelper;
    protected ITestOutputHelper TestOutputHelper { get; }

    protected Dictionary<string, IBaseTestScope> Scopes { get; } = new();

    public virtual async ValueTask DisposeAsync()
    {
        foreach (var testScope in Scopes)
        {
            await testScope.Value.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    public virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;

    protected async Task<T> GetScopeAsync<T>([CallerMemberName] string name = "") where T : IBaseTestScope
    {
        T scope;

        if (!Scopes.TryGetValue(name, out var existingScope))
        {
            scope = Activator.CreateInstance<T>();
            await scope.BeforeConfiguredAsync(name);
            await scope.ConfigureAsync(name, TestOutputHelper);
            await scope.OnCreatedAsync();
            Scopes.Add(name, scope);
        }
        else
        {
            if (existingScope is T typedScope)
            {
                scope = typedScope;
            }
            else
            {
                throw new InvalidOperationException($"Can't create scope for {name}");
            }
        }


        return scope;
    }

    protected IServiceScope CreateServiceScope<T>([CallerMemberName] string name = "")
        where T : IBaseTestScope
    {
        if (!Scopes.TryGetValue(name, out var existingScope))
        {
            throw new InvalidOperationException("No scope exists");
        }

        var scope = (T)existingScope;
        return scope.GetService<IServiceScopeFactory>().CreateScope();
    }
}

public abstract class BaseTest<T> : BaseTest where T : IBaseTestScope
{
    protected BaseTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }


    protected Task<T> GetScopeAsync([CallerMemberName] string name = "") => GetScopeAsync<T>(name);


    protected IServiceScope CreateServiceScope([CallerMemberName] string name = "") => CreateServiceScope<T>(name);
}
