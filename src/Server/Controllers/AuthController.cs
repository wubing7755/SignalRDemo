using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SignalRDemo.Shared.Models;
using SharedLoginResponse = SignalRDemo.Shared.Models.LoginResponse;
using SignalRDemo.Application.Commands.Users;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Domain.Repositories;
using MediatR;

namespace SignalRDemo.Server.Controllers;

/// <summary>
/// 认证控制器 - 处理用户注册、登录和 Token 管理
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录 - 使用DDD Command
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SignalRDemo.Shared.Models.LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new SharedLoginResponse { Success = false, Message = "用户名和密码不能为空" });
            }

            var command = new LoginCommand(request.UserName, request.Password);
            var result = await _mediator.Send(command);
            
            if (!result.IsSuccess)
            {
                return Unauthorized(new SharedLoginResponse { Success = false, Message = result.Error });
            }

            var user = result.Value;
            // 转换为Shared.Models.User用于JWT生成
            var sharedUser = new User
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName
            };

            var token = GenerateJwtToken(sharedUser);
            var refreshToken = GenerateRefreshToken();

            return Ok(new SharedLoginResponse
            {
                Success = true,
                Message = "登录成功",
                User = sharedUser,
                Token = token,
                RefreshToken = refreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败");
            return StatusCode(500, new SharedLoginResponse { Success = false, Message = "登录失败" });
        }
    }

    /// <summary>
    /// 用户注册 - 使用DDD Command
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignalRDemo.Shared.Models.RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new SharedLoginResponse { Success = false, Message = "用户名和密码不能为空" });
            }

            if (request.UserName.Length < 3 || request.UserName.Length > 20)
            {
                return BadRequest(new SharedLoginResponse { Success = false, Message = "用户名长度必须在3-20个字符之间" });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new SharedLoginResponse { Success = false, Message = "密码长度至少6个字符" });
            }

            var command = new RegisterUserCommand(request.UserName, request.Password, request.DisplayName);
            var result = await _mediator.Send(command);
            
            if (!result.IsSuccess)
            {
                return BadRequest(new SharedLoginResponse { Success = false, Message = result.Error });
            }

            var user = result.Value;
            // 转换为Shared.Models.User用于JWT生成
            var sharedUser = new User
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName
            };

            var token = GenerateJwtToken(sharedUser);
            var refreshToken = GenerateRefreshToken();

            return Ok(new SharedLoginResponse
            {
                Success = true,
                Message = "注册成功",
                User = sharedUser,
                Token = token,
                RefreshToken = refreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册失败");
            return StatusCode(500, new SharedLoginResponse { Success = false, Message = "注册失败" });
        }
    }

    /// <summary>
    /// 刷新 Token - 使用DDD Repository
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new SharedLoginResponse { Success = false, Message = "Refresh token 不能为空" });
            }

            // 验证 Refresh Token 并获取用户信息
            var principal = GetPrincipalFromExpiredToken(request.RefreshToken);
            var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new SharedLoginResponse { Success = false, Message = "无效的 Refresh token" });
            }

            var user = await _userRepository.GetByIdAsync(SignalRDemo.Domain.ValueObjects.UserId.Create(userId), CancellationToken.None);
            if (user == null)
            {
                return Unauthorized(new SharedLoginResponse { Success = false, Message = "用户不存在" });
            }

            var sharedUser = new User
            {
                Id = user.Id.Value,
                UserName = user.UserName.Value,
                DisplayName = user.DisplayName?.Value
            };

            var newToken = GenerateJwtToken(sharedUser);
            var newRefreshToken = GenerateRefreshToken();

            return Ok(new SharedLoginResponse
            {
                Success = true,
                Message = "Token 已刷新",
                User = sharedUser,
                Token = newToken,
                RefreshToken = newRefreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新 token 失败");
            return StatusCode(500, new SharedLoginResponse { Success = false, Message = "刷新 token 失败" });
        }
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // 在实际应用中，这里可能需要清除服务器端的 Refresh Token
        return Ok(new { Success = true, Message = "退出成功" });
    }

    /// <summary>
    /// 获取当前用户信息 - 使用DDD Repository
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(SignalRDemo.Domain.ValueObjects.UserId.Create(userId), CancellationToken.None);
            if (user == null)
            {
                return NotFound();
            }

            var sharedUser = new User
            {
                Id = user.Id.Value,
                UserName = user.UserName.Value,
                DisplayName = user.DisplayName?.Value
            };

            return Ok(new { Success = true, User = sharedUser });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户信息失败");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? "ThisIsASecretKeyForJwtTokenGeneration123456";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("display_name", user.DisplayName ?? user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "SignalRDemo",
            audience: jwtSettings["Audience"] ?? "SignalRDemoClients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 生成 Refresh Token
    /// </summary>
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// 从过期的 Token 中获取 ClaimsPrincipal
    /// </summary>
    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? "ThisIsASecretKeyForJwtTokenGeneration123456";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Refresh Token 请求模型
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
