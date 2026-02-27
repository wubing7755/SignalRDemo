using Microsoft.AspNetCore.Mvc;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Queries.Rooms;
using MediatR;

namespace SignalRDemo.Server.Controllers;

/// <summary>
/// 统计控制器 - 提供房间和用户统计数据
/// </summary>
[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StatsController> _logger;

    public StatsController(IMediator mediator, ILogger<StatsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// 获取房间统计数据 - 使用DDD Query
    /// </summary>
    [HttpGet("rooms")]
    public async Task<IActionResult> GetRoomStats()
    {
        try
        {
            var rooms = await _mediator.Send(new GetPublicRoomsQuery());
            var stats = new RoomStatsDto
            {
                RoomCount = rooms.Count,
                UserCount = rooms.Sum(r => r.MemberCount)
            };
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取房间统计失败");
            return StatusCode(500, new { Error = "获取统计数据失败" });
        }
    }

    /// <summary>
    /// 获取所有公共房间列表 - 使用DDD Query
    /// </summary>
    [HttpGet("rooms/list")]
    public async Task<IActionResult> GetPublicRooms()
    {
        try
        {
            var rooms = await _mediator.Send(new GetPublicRoomsQuery());
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取公共房间列表失败");
            return StatusCode(500, new { Error = "获取房间列表失败" });
        }
    }
}
