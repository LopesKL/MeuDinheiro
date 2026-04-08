using Application.Dto.Users;
using Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using Project;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class SignInController : BaseController
{
    private readonly UserHandler _handler;

    public SignInController(UserHandler handler, INotificationHandler notification, UserHandler userHandler)
        : base(notification, userHandler)
    {
        _handler = handler;
    }

    [HttpPost("signin")]
    [AllowAnonymous]
    public async Task<IActionResult> Signin([FromBody] UserSignInDto dto)
    {
        var result = await _handler.SignInAsync(dto);
        return HandleResponse(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var result = await _handler.RegisterAsync(dto);
        return HandleResponse(result);
    }

    [HttpPost("seed")]
    [Authorize(Roles = Roles.ROLE_ADMIN)]
    public async Task<IActionResult> Seed()
    {
        // Implementar seed se necessário
        return HandleSuccess("Seed executado com sucesso");
    }
}
