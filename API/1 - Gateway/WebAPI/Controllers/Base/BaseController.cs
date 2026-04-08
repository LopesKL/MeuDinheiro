using System.Security.Claims;
using Application.Dto.ResponsePatterns;
using Application.Dto.Users;
using Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;

namespace WebAPI.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected readonly INotificationHandler _notification;
    protected readonly UserHandler _userHandler;
    private UserDto? _currentUser;

    protected BaseController(INotificationHandler notification, UserHandler userHandler)
    {
        _notification = notification;
        _userHandler = userHandler;
    }

    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected UserDto? CurrentUser
    {
        get
        {
            if (_currentUser != null)
                return _currentUser;

            var id = CurrentUserId;
            if (string.IsNullOrEmpty(id))
                return null;

            _currentUser = _userHandler.GetUserByIdAsync(id).GetAwaiter().GetResult();
            return _currentUser;
        }
    }

    protected IActionResult HandleResponse<T>(T? data, string? message = null)
    {
        if (_notification.HasNotification())
        {
            var errors = _notification.GetNotifications().Select(n => n.Message).ToList();
            return BadRequest(ApiResponse<T>.ErrorResult(
                message ?? "Validation errors occurred",
                errors));
        }

        return Ok(ApiResponse<T>.SuccessResult(data, message ?? "Success"));
    }

    protected IActionResult HandleResponseAll<T>(ResponseAllDto<T> response)
    {
        if (_notification.HasNotification())
        {
            var errors = _notification.GetNotifications().Select(n => n.Message).ToList();
            response.Success = false;
            response.Errors = errors;
            response.Message = "Validation errors occurred";
            return BadRequest(response);
        }

        return Ok(response);
    }

    protected IActionResult HandleError(string message, List<string>? errors = null)
    {
        return BadRequest(ApiResponse<object>.ErrorResult(message, errors ?? new List<string>()));
    }

    protected IActionResult HandleSuccess(string message = "Success")
    {
        return Ok(ApiResponse<object?>.SuccessResult(null, message));
    }
}
