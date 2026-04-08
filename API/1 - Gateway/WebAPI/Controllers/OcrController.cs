using Application.Finance;
using Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Notifications;
using WebAPI.Controllers.Base;

namespace WebAPI.Controllers;

[Authorize]
public class OcrController : BaseController
{
    private readonly OcrService _ocr;

    public OcrController(INotificationHandler notification, UserHandler userHandler, OcrService ocr)
        : base(notification, userHandler)
    {
        _ocr = ocr;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return HandleError("Arquivo obrigatório");
        var result = await _ocr.AnalyzeAsync(file.FileName, file.Length);
        return HandleResponse(result);
    }
}
