namespace WASMDemo.Server;

public class Program
{
    public static async Task Main(string[] args) => await CreateApplication(args).RunAsync();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        CreateApplication(args).GetHostBuilder();

    private static ServerApplication CreateApplication(string[] args) => new(args);
}

