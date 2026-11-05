using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Pdf.Tests
{
    internal sealed class LocalPageServer : IAsyncDisposable
    {
        private readonly WebApplication app;

        private LocalPageServer(WebApplication app, string url)
        {
            this.app = app;
            Url = url;
        }

        public string Url { get; private set; }
        public string Html => $"""
            <!doctype html><html lang="ru"><head><meta charset="utf-8">
            <link rel="stylesheet" href="{Url}style.css"></head>
            <body><h1>Проверка русского текста</h1><img src="{Url}image.png" width="40" height="40"></body></html>
            """;

        public static async Task<LocalPageServer> StartAsync()
        {
            var builder = WebApplication.CreateSlimBuilder();
            _ = builder.Logging.ClearProviders();
            _ = builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(0));
            var app = builder.Build();
            var server = new LocalPageServer(app, "");
            _ = app.MapGet("/", () => Results.Content(server.Html, "text/html; charset=utf-8"));
            _ = app.MapGet("/style.css", () => Results.Content(
                "html{scrollbar-width:none}body{margin:0;width:320px;height:1100px;background:#00ff00}h1{font:20px sans-serif;margin:0}", "text/css"));
            _ = app.MapGet("/image.png", () => Results.Bytes(Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADUlEQVR4nGP4z8AAAAMBAQDJ/pLvAAAAAElFTkSuQmCC"), "image/png"));
            await app.StartAsync();
            var host = Environment.GetEnvironmentVariable("PDF_TEST_HTTP_HOST") ?? "127.0.0.1";
            server.Url = new UriBuilder(app.Urls.Single()) { Host = host }.Uri.AbsoluteUri;
            return server;
        }

        public ValueTask DisposeAsync() => app.DisposeAsync();
    }
}
