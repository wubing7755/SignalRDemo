using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SignalRDemo.Client;
using SignalRDemo.Client.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // 配置日志级别
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddScoped<ChatService>();
        builder.Services.AddScoped<AuthService>();

        await builder.Build().RunAsync();
    }
}
