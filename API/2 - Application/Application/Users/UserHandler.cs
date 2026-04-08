using Application.Dto.Users;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Notifications.Notifications;
using Project;
using Project.Entities;
using Project.Entities.Finance;
using Repositories.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Users;

public class UserHandler
{
    private readonly IFinanceStore _finance;
    private readonly IMapper _mapper;
    private readonly INotificationHandler _notification;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly RoleManager<AppRole> _roleManager;

    public UserHandler(
        IFinanceStore finance,
        IMapper mapper,
        INotificationHandler notification,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration,
        RoleManager<AppRole> roleManager)
    {
        _finance = finance;
        _mapper = mapper;
        _notification = notification;
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _roleManager = roleManager;
    }

    public async Task<UserSignInResponseDto?> SignInAsync(UserSignInDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            _notification.DefaultBuilder("SignIn_01", "Username e senha são obrigatórios");
            return null;
        }

        var user = await _userManager.FindByNameAsync(dto.Username);
        if (user == null || !user.Active)
        {
            _notification.DefaultBuilder("SignIn_02", "Usuário não encontrado ou inativo");
            return null;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
        {
            _notification.DefaultBuilder("SignIn_03", "Senha incorreta");
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Any())
        {
            _notification.DefaultBuilder("SignIn_04", "Usuário não possui roles atribuídas");
            return null;
        }

        var token = SetToken(user, roles);
        var userDto = _mapper.Map<UserDto>(user);

        return new UserSignInResponseDto
        {
            Token = token,
            User = userDto,
            Roles = roles.ToList()
        };
    }

    public async Task<UserDto?> GetUserByLoginAsync(string login)
    {
        var user = await _userManager.FindByNameAsync(login);
        if (user == null)
            return null;

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserSignInResponseDto?> RegisterAsync(RegisterUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
        {
            _notification.DefaultBuilder("Reg_01", "Usuário e senha são obrigatórios");
            return null;
        }

        if (!IsValidEmail(dto.Email))
        {
            _notification.DefaultBuilder("Reg_02", "Email inválido");
            return null;
        }

        if (await _userManager.FindByNameAsync(dto.UserName) != null)
        {
            _notification.DefaultBuilder("Reg_03", "Nome de usuário já existe");
            return null;
        }

        var user = new AppUser
        {
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim(),
            EmailConfirmed = true,
            Active = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                _notification.DefaultBuilder($"Reg_{err.Code}", err.Description);
            return null;
        }

        if (!await _roleManager.RoleExistsAsync(Roles.ROLE_USER))
        {
            await _roleManager.CreateAsync(new AppRole { Name = Roles.ROLE_USER, NormalizedName = "ROLEUSER" });
        }

        await _userManager.AddToRoleAsync(user, Roles.ROLE_USER);
        await SeedDefaultFinanceDataAsync(user.Id);

        var roles = await _userManager.GetRolesAsync(user);
        var token = SetToken(user, roles);
        return new UserSignInResponseDto
        {
            Token = token,
            User = _mapper.Map<UserDto>(user),
            Roles = roles.ToList()
        };
    }

    private async Task SeedDefaultFinanceDataAsync(string userId)
    {
        var defaults = new[]
        {
            ("Alimentação", true),
            ("Transporte", true),
            ("Moradia", true),
            ("Lazer", true),
            ("Saúde", true),
            ("Educação", true),
            ("Salário", false),
            ("Freelance", false),
            ("Investimentos", false)
        };

        foreach (var (name, isExpense) in defaults)
        {
            await _finance.InsertCategoryAsync(new Category
            {
                UserId = userId,
                Name = name,
                IsExpense = isExpense
            });
        }
    }

    public async Task<UserDto?> InsertAsync(UserDto dto, string password)
    {
        if (!IsValidEmail(dto.Email))
        {
            _notification.DefaultBuilder("Insert_01", "Email inválido");
            return null;
        }

        var existingUser = await _userManager.FindByNameAsync(dto.UserName);
        if (existingUser != null)
        {
            _notification.DefaultBuilder("Insert_02", "Username já existe");
            return null;
        }

        var user = new AppUser
        {
            UserName = dto.UserName,
            Email = dto.Email,
            Active = dto.Active
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                _notification.DefaultBuilder($"Insert_03_{error.Code}", error.Description);
            }
            return null;
        }

        return _mapper.Map<UserDto>(user);
    }

    private string SetToken(AppUser user, IList<string> roles)
    {
        var jwtKey = _configuration["Authentication:JWTIssuerSigningKey"] 
            ?? throw new InvalidOperationException("JWT Key not found.");
        var audience = _configuration["Authentication:Audience"] ?? "ApiAudience";
        var issuer = _configuration["Authentication:Issuer"] ?? "ApiIssuer";

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Id),
            new Claim("id", user.Id),
            new Claim("api_access", "true")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("rol", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var dateCreate = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: dateCreate.AddDays(90),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
