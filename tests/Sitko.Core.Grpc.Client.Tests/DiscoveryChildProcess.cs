using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    internal sealed class DiscoveryChildProcess : IAsyncDisposable
    {
        private readonly Process process;
        private readonly Task<string> errors;
        private Task<string>? output;
        public int Port { get; private set; }

        private DiscoveryChildProcess(Process process)
        {
            this.process = process;
            errors = process.StandardError.ReadToEndAsync();
        }

        public static async Task<DiscoveryChildProcess> Start(Dictionary<string, string?> config,
            string certificate, string replica, CancellationToken token)
        {
            var info = new ProcessStartInfo("dotnet")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            info.ArgumentList.Add(typeof(LifecycleHost.Program).Assembly.Location);
            var childConfig = new Dictionary<string, string?>(config)
            {
                ["Certificate"] = certificate,
                ["Replica"] = replica,
                ["Application:Web:Ports:gRPC:Port"] = "0",
                ["Application:Web:Ports:gRPC:Protocol"] = "Http2",
                ["Application:Web:Ports:gRPC:UseTLS"] = "true",
                ["Application:Web:Ports:gRPC:ExternalAddress"] = "127.0.0.1"
            };
            foreach (var (key, value) in childConfig)
            {
                info.ArgumentList.Add($"--{key}={value}");
            }
            var child = new DiscoveryChildProcess(Process.Start(info)!);
            try
            {
                using var startup = CancellationTokenSource.CreateLinkedTokenSource(token);
                startup.CancelAfter(TimeSpan.FromSeconds(30));
                string? line;
                while ((line = await child.process.StandardOutput.ReadLineAsync(startup.Token)) is not null)
                {
                    if (!line.StartsWith("READY:", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    child.Port = int.Parse(line.AsSpan(6), CultureInfo.InvariantCulture);
                    TestContext.Current.TestOutputHelper?.WriteLine($"Own child PID={child.process.Id}; TLS port={child.Port}; response={replica}");
                    child.output = child.process.StandardOutput.ReadToEndAsync(token);
                    return child;
                }
                throw new InvalidOperationException($"Child did not start: {await child.errors}");
            }
            catch
            {
                await child.DisposeAsync();
                throw;
            }
        }

        public async Task Kill(CancellationToken token)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(token);
        }

        public async Task Stop(CancellationToken token)
        {
            await process.StandardInput.WriteLineAsync("stop".AsMemory(), token);
            await process.StandardInput.FlushAsync(token);
            await process.WaitForExitAsync(token).WaitAsync(TimeSpan.FromSeconds(15), token);
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Child shutdown failed: {await errors}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            _ = await errors;
            if (output is not null)
            {
                _ = await output;
            }
            process.Dispose();
        }
    }
}
