using Application.Finance;
using Application.Users;
using Application.Dto.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class InstallmentsController : BaseController
{
    private readonly InstallmentPlanService _service;

    public InstallmentsController(INotificationHandler notification, UserHandler userHandler, InstallmentPlanService service)
        : base(notification, userHandler)
    {
        _service = service;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] InstallmentDto dto)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        dto.Id = id;
        return HandleResponse(await _service.UpdateInstallmentAsync(userId, id, dto));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        var ok = await _service.DeleteInstallmentAsync(userId, id);
        return ok ? HandleSuccess("Parcela removida") : HandleResponse<InstallmentDto?>(null);
    }
}
