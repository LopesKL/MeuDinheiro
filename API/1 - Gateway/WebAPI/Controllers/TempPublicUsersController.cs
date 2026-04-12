using Application.Dto.ResponsePatterns;
using Application.Dto.Users;
using Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Endpoints públicos temporários para listar/consultar utilizadores. Remover ou proteger antes de produção.
/// </summary>
[ApiController]
[Route("api/temp/users")]
[AllowAnonymous]
public class TempPublicUsersController : ControllerBase
{
    private readonly UserHandler _handler;

    public TempPublicUsersController(UserHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public ActionResult<ApiResponse<IReadOnlyList<UserDto>>> GetAll()
    {
        var users = _handler.GetAllUsers();
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.SuccessResult(users));
    }

    /// <summary>Procura por nome de utilizador ou email (o mesmo critério do sign-in).</summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Lookup([FromQuery] string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResult(
                "Parâmetro login é obrigatório",
                new List<string> { "Use ?login=nomeOuEmail" }));
        }

        var user = await _handler.GetUserByLoginAsync(login);
        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResult(
                "Utilizador não encontrado",
                new List<string>()));
        }

        return Ok(ApiResponse<UserDto>.SuccessResult(user));
    }
}
