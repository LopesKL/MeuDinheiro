using Application.Finance;
using Application.Users;
using Application.Dto.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class ExpensesController : BaseController
{
    private readonly ExpenseService _service;
    private readonly OcrService _ocr;
    private readonly IWebHostEnvironment _env;

    public ExpensesController(
        INotificationHandler notification,
        UserHandler userHandler,
        ExpenseService service,
        OcrService ocr,
        IWebHostEnvironment env)
        : base(notification, userHandler)
    {
        _service = service;
        _ocr = ocr;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? month)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetAllAsync(userId, year, month));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.GetByIdAsync(userId, id));
    }

    [HttpPost("parse")]
    public async Task<IActionResult> Parse([FromBody] ParseExpenseRequestDto dto)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.ParseQuickInputAsync(userId, dto.Text));
    }

    [HttpPost("upload-receipt")]
    public async Task<IActionResult> UploadReceipt(IFormFile file)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        if (file == null || file.Length == 0)
            return HandleError("Arquivo obrigatório");

        var uploads = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", userId);
        Directory.CreateDirectory(uploads);
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var name = $"{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(uploads, name);
        await using (var stream = System.IO.File.Create(full))
            await file.CopyToAsync(stream);

        var relative = $"/uploads/{userId}/{name}";
        var ocr = await _ocr.AnalyzeAsync(file.FileName, file.Length);
        return HandleResponse(new { imagePath = relative, ocr });
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] ExpenseDto dto)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        return HandleResponse(await _service.UpsertAsync(userId, dto));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        var ok = await _service.DeleteAsync(userId, id);
        return ok ? HandleSuccess("Removido") : HandleResponse<ExpenseDto?>(null);
    }
}
