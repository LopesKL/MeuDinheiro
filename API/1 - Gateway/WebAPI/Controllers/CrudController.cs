using Application.Crud;
using Application.Dto.Dtos;
using Application.Dto.RequestPatterns;
using Application.Dto.ResponsePatterns;
using Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using Project;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize(Roles = Roles.ROLE_ADMIN)]
public class CrudController : BaseController
{
    private readonly CrudHandler _handler;

    public CrudController(CrudHandler handler, INotificationHandler notification, UserHandler userHandler)
        : base(notification, userHandler)
    {
        _handler = handler;
    }

    [HttpGet("getById/{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _handler.GetByIdAsync(id);
        return HandleResponse(result);
    }

    [HttpPost("getAll")]
    public async Task<IActionResult> GetAll([FromBody] RequestAllDto request)
    {
        var result = await _handler.GetAllAsync(request, CurrentUser?.Id ?? string.Empty);
        return HandleResponseAll(result);
    }

    [HttpPost("upsert")]
    public async Task<IActionResult> Upsert([FromBody] CrudDto dto)
    {
        var result = await _handler.UpsertAsync(dto, CurrentUser?.Id ?? string.Empty);
        return HandleResponse(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _handler.DeleteAsync(id);
        return HandleResponse(result);
    }
}
