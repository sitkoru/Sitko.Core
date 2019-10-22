using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Sitko.Core.Xunit
{
    public abstract class BaseTest
    {
        protected ITestOutputHelper TestOutputHelper { get; }

        protected BaseTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }
    }

    public abstract class BaseTest<T> : BaseTest, IDisposable where T : BaseTestScope
    {
        protected BaseTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        private readonly Dictionary<string, BaseTestScope> _scopes = new Dictionary<string, BaseTestScope>();

        protected T GetScope([CallerMemberName] string name = "")
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

        protected IServiceScope? CreateServiceScope([CallerMemberName] string name = "")
        {
            if (!_scopes.ContainsKey(name))
            {
                throw new Exception("No scope exists");
            }

            var scope = _scopes[name] as T;
            return scope?.Get<IServiceScopeFactory>().CreateScope();
        }

        public void Dispose()
        {
            foreach (var testScope in _scopes)
            {
                testScope.Value.Dispose();
            }
        }
    }
}
