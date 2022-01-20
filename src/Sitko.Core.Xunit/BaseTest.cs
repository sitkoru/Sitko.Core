using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit;

[PublicAPI]
public abstract class BaseTest : IAsyncDisposable, IAsyncLifetime
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

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    public virtual Task InitializeAsync() => Task.CompletedTask;

    protected async Task<T> GetScopeAsync<T>([CallerMemberName] string name = "") where T : IBaseTestScope
    {
        T scope;

        if (!Scopes.ContainsKey(name))
        {
            scope = Activator.CreateInstance<T>();
            await scope.BeforeConfiguredAsync();
            await scope.ConfigureAsync(name, TestOutputHelper);
            await scope.OnCreatedAsync();
            Scopes.Add(name, scope);
        }
        else
        {
            if (Scopes[name] is T typedScope)
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
        if (!Scopes.ContainsKey(name))
        {
            throw new InvalidOperationException("No scope exists");
        }

        var scope = (T)Scopes[name];
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
