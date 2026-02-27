using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SignalRDemo.Client;
using SignalRDemo.Client.Services;
using SignalRDemo.Client.Services.Abstractions;
using SignalRDemo.Client.Infrastructure;

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
        
        // 原有服务（保持兼容）
        builder.Services.AddScoped<ChatService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<DialogService>();
        
        // 新架构服务
        builder.Services.AddScoped<ISignalRConnectionService, SignalRConnectionService>();
        builder.Services.AddScoped<IUserStateService, UserStateService>();
        builder.Services.AddScoped<IRoomService, RoomService>();
        builder.Services.AddScoped<IMessageService, MessageService>();

        var host = builder.Build();
        
        // 初始化用户状态服务（从 localStorage 恢复）
        var userStateService = host.Services.GetRequiredService<IUserStateService>();
        await userStateService.InitializeAsync();
        
        await host.RunAsync();
    }
}
