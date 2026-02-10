using SignalRDemo.Server.Hubs;
using SignalRDemo.Server.Services;

public class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();
        
        // 注册聊天服务
        builder.Services.AddSingleton<IChatRepository, InMemoryChatRepository>();
        builder.Services.AddSingleton<IUserConnectionManager, UserConnectionManager>();
        
        // SignalR 配置：添加 MessagePack 协议和优化选项
        builder.Services.AddSignalR()
            .AddMessagePackProtocol()
            .AddHubOptions<ChatHub>(options =>
            {
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
                options.StreamBufferCapacity = 50;
            });
        
        // 添加健康检查
        builder.Services.AddHealthChecks()
            .AddCheck<SignalRHealthCheck>("signalr_hub");
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazorClient",
                builder => builder
                    .WithOrigins(
                    "https://localhost:7002",
                    "http://localhost:5293"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseCors("AllowBlazorClient");

        app.MapHub<ChatHub>("/chathub");
        app.MapRazorPages();
        app.MapControllers();
        
        // 添加健康检查端点
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    Status = report.Status.ToString(),
                    TotalDuration = report.TotalDuration.TotalMilliseconds,
                    Checks = report.Entries.Select(e => new
                    {
                        Name = e.Key,
                        Status = e.Value.Status.ToString(),
                        Duration = e.Value.Duration.TotalMilliseconds,
                        Data = e.Value.Data
                    })
                };
                await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, result);
            }
        });
        
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
