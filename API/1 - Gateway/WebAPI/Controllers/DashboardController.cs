using Application.Finance;
using Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly DashboardService _service;

    public DashboardController(INotificationHandler notification, UserHandler userHandler, DashboardService service)
        : base(notification, userHandler)
    {
        _service = service;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] int? year, [FromQuery] int? month)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetSummaryAsync(userId, year, month));
    }
}
