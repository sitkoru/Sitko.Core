using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Blazor;
using Sitko.Core.Configuration.Vault;
using Sitko.Core.IdProvider.SonyFlake;

namespace Sitko.Core.Apps.Vault
{
    public class Program
    {
        public static async Task Main(string[] args) => await CreateApplication(args).RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            CreateApplication(args).GetHostBuilder();

        private static VaultApplication CreateApplication(string[] args) => new(args);
    }

    public class VaultApplication : BlazorApplication<Startup>
    {
        public VaultApplication(string[] args) : base(args)
        {
            this
                .AddVaultConfiguration()
                .AddSonyFlakeIdProvider();
            ConfigureServices((_, hostBuilderContext, services) =>
            {
                services.Configure<TestOptions>(hostBuilderContext.Configuration.GetSection("Test"));
            });
        }
    }

    public class TestOptions
    {
        public string Foo { get; set; }
        public int Bar { get; set; }
    }


    public class ReportSender
    {
        public void Send()
        {
            var report = new Report();
            var senderType = GetSenderType();
            switch (senderType)
            {
                case "mail":
                    var mailSender = new MailSender();
                    mailSender.Send(report);
                    break;
                case "telegram":
                    var telegramSender = new TelegramSender();
                    telegramSender.Send(report);
                    break;
            }
        }

        private static string GetSenderType() => new List<string>() {"email", "telegram"}.OrderBy(_ => Guid.NewGuid()).First();
    }

    public class MailSender
    {
        public void Send(Report report)
        {
            // send to telegram
        }
    }

    public class Report
    {
    }

    public class TelegramSender
    {
        public void Send(Report report)
        {
            // send to telegram
        }
    }
}
