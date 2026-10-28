using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.Db.InMemory;
using Sitko.Core.Identity;
using Xunit;

namespace Sitko.Core.Auth.Tests
{
    public sealed class IdentityConfirmationTests
    {
        [Fact]
        public async Task PasswordSignInRequiresConfirmedAccount()
        {
            const string userName = "alice";
            const string email = "alice@example.test";
            const string password = "Identity1!";

            var builder = Host.CreateApplicationBuilder();
            _ = builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            var app = builder.AddSitkoCore();
            _ = app.AddInMemoryDatabase<IdentityTestDbContext>(static options =>
                options.Database = $"identity-confirmation-{Guid.NewGuid():N}");
            _ = app.AddIdentity<IdentityUser, IdentityRole, string, IdentityTestDbContext>();

            using var host = builder.Build();
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                Assert.True(services.GetRequiredService<IOptions<IdentityOptions>>().Value.SignIn.RequireConfirmedAccount);
                var dbContext = services.GetRequiredService<IdentityTestDbContext>();
                _ = await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

                var users = services.GetRequiredService<UserManager<IdentityUser>>();
                var user = new IdentityUser { UserName = userName, Email = email };
                var created = await users.CreateAsync(user, password);
                Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(static error => error.Description)));

                var signIn = services.GetRequiredService<SignInManager<IdentityUser>>();
                Assert.True((await signIn.CheckPasswordSignInAsync(user, password, false)).IsNotAllowed);

                var token = await users.GenerateEmailConfirmationTokenAsync(user);
                var confirmed = await users.ConfirmEmailAsync(user, token);
                Assert.True(confirmed.Succeeded, string.Join(", ", confirmed.Errors.Select(static error => error.Description)));
            }

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var users = services.GetRequiredService<UserManager<IdentityUser>>();
                var user = await users.FindByNameAsync(userName);
                Assert.NotNull(user);
                Assert.True(await users.IsEmailConfirmedAsync(user));
                var signIn = services.GetRequiredService<SignInManager<IdentityUser>>();
                Assert.True((await signIn.CheckPasswordSignInAsync(user, password, false)).Succeeded);
            }
        }

        private sealed class IdentityTestDbContext(DbContextOptions<IdentityTestDbContext> options)
            : IdentityDbContext<IdentityUser, IdentityRole, string>(options);
    }
}
