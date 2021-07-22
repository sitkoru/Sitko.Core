using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.Apps.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args) => await CreateApplication(args).RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            CreateApplication(args).GetHostBuilder();

        private static TestBlazorApplication CreateApplication(string[] args) => new(args);
    }
}
