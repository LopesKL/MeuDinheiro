using Application.Finance;
using Application.Users;
using Application.Dto.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class ProjectionsController : BaseController
{
    private readonly ProjectionService _service;

    public ProjectionsController(INotificationHandler notification, UserHandler userHandler, ProjectionService service)
        : base(notification, userHandler)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<IActionResult> ForMe([FromQuery] int monthsAhead = 12)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.ProjectForUserAsync(userId, monthsAhead));
    }

    /// <summary>Simulação sandbox — não persiste nada.</summary>
    [HttpPost("sandbox")]
    [AllowAnonymous]
    public IActionResult Sandbox([FromBody] SandboxProjectionRequestDto dto)
    {
        return HandleResponse(_service.ProjectSandbox(dto));
    }
}
