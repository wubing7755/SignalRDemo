using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SignalRDemo.Server.Hubs;
using SignalRDemo.Server.Services;
using SignalRDemo.Infrastructure.Services;
using SignalRDemo.Infrastructure.Repositories;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Application.Handlers;
using MediatR;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

public class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();
        
        // JWT 配置
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? "ThisIsASecretKeyForJwtTokenGeneration123456";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"] ?? "SignalRDemo",
                ValidAudience = jwtSettings["Audience"] ?? "SignalRDemoClients",
                IssuerSigningKey = key
            };
            
            // SignalR 通过 Query String 传递 JWT Token
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        builder.Services.AddAuthorization();


        // ========== DDD 新架构服务注册 ==========
        
        // MediatR
        builder.Services.AddMediatR(typeof(RegisterUserHandler).Assembly);
        
        // Redis 连接
        var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(redisConnection));
        
        // 消息存储路径
        var messageStoragePath = builder.Configuration["MessageStorage:Path"] ?? "messages";
        
        // 领域仓储 - 使用 Redis 实现（带日志）
        builder.Services.AddSingleton<IUserRepository>(sp => 
            new RedisUserRepository(
                sp.GetRequiredService<IConnectionMultiplexer>(),
                sp.GetRequiredService<ILogger<RedisUserRepository>>()));
        builder.Services.AddSingleton<IRoomRepository>(sp => 
            new RedisRoomRepository(sp.GetRequiredService<IConnectionMultiplexer>()));
        builder.Services.AddSingleton<IMessageRepository>(sp => 
            new RedisMessageRepository(sp.GetRequiredService<IConnectionMultiplexer>(), messageStoragePath));
        
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
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();

        // 添加认证和授权中间件
        app.UseAuthentication();
        app.UseAuthorization();

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
