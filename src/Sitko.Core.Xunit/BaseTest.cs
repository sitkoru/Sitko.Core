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

        protected readonly Dictionary<string, BaseTestScope> _scopes = new Dictionary<string, BaseTestScope>();

        protected T GetScope<T>([CallerMemberName] string name = "") where T : BaseTestScope
        {
            T scope;

            if (!_scopes.ContainsKey(name))
            {
                scope = Activator.CreateInstance<T>();
                scope.Configure(name, TestOutputHelper);
                scope.OnCreated();
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

        protected IServiceScope? CreateServiceScope<T>([CallerMemberName] string name = "") where T : BaseTestScope
        {
            if (!_scopes.ContainsKey(name))
            {
                throw new Exception("No scope exists");
            }

            var scope = _scopes[name] as T;
            return scope?.Get<IServiceScopeFactory>().CreateScope();
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

    public abstract class BaseTest<T> : BaseTest where T : BaseTestScope
    {
        protected BaseTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }


        protected T GetScope([CallerMemberName] string name = "")
        {
            return GetScope<T>(name);
        }


        protected IServiceScope? CreateServiceScope([CallerMemberName] string name = "")
        {
            return CreateServiceScope<T>(name);
        }
    }
}
