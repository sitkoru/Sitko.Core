using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit
{
    public abstract class BaseTest : IAsyncDisposable, IAsyncLifetime
    {
        protected ITestOutputHelper TestOutputHelper { get; }

        protected readonly Dictionary<string, IBaseTestScope> _scopes =
            new();

        protected async Task<T> GetScopeAsync<T>([CallerMemberName] string name = "") where T : IBaseTestScope
        {
            T scope;

            if (!_scopes.ContainsKey(name))
            {
                scope = Activator.CreateInstance<T>();
                await scope.ConfigureAsync(name, TestOutputHelper);
                await scope.OnCreatedAsync();
                _scopes.Add(name, scope);
            }
            else
            {
                if (_scopes[name] is T typedScope)
                {
                    scope = typedScope;
                }
                else
                {
                    throw new Exception($"Can't create scope for {name}");
                }
            }


            return scope;
        }

        protected IServiceScope CreateServiceScope<T>([CallerMemberName] string name = "")
            where T : IBaseTestScope
        {
            if (!_scopes.ContainsKey(name))
            {
                throw new Exception("No scope exists");
            }

            var scope = (T)_scopes[name];
            return scope.Get<IServiceScopeFactory>().CreateScope();
        }

        protected BaseTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();
        }

        public virtual async ValueTask DisposeAsync()
        {
            foreach (var testScope in _scopes)
            {
                await testScope.Value.DisposeAsync();
            }
        }

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }

    public abstract class BaseTest<T> : BaseTest where T : IBaseTestScope
    {
        protected BaseTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }


        protected Task<T> GetScopeAsync([CallerMemberName] string name = "")
        {
            return GetScopeAsync<T>(name);
        }


        protected IServiceScope CreateServiceScope([CallerMemberName] string name = "")
        {
            return CreateServiceScope<T>(name);
        }
    }
}
