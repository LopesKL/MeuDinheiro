using Application.Dto.Finance;
using System.Text.RegularExpressions;

namespace Application.Finance;

/// <summary>OCR simulado: extrai pistas do nome do arquivo e tamanho.</summary>
public class OcrService
{
    public Task<OcrResultDto> AnalyzeAsync(string? fileName, long fileLengthBytes)
    {
        var hint = $"arquivo={fileName ?? "sem_nome"}, bytes={fileLengthBytes}";
        decimal? amount = null;
        string? merchant = null;

        if (!string.IsNullOrEmpty(fileName))
        {
            var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
            var match = Regex.Match(nameNoExt, @"(\d+[.,]\d{2}|\d+)");
            if (match.Success && decimal.TryParse(match.Value.Replace(",", "."),
                    System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                amount = v;

            merchant = nameNoExt
                .Replace(match.Value, "")
                .Replace("_", " ")
                .Replace("-", " ")
                .Trim();
            if (string.IsNullOrWhiteSpace(merchant))
                merchant = "Estabelecimento (simulado)";
        }

        if (amount == null)
            amount = Math.Round((decimal)(fileLengthBytes % 500) / 10m + 10m, 2);

        merchant ??= "Comércio simulado";

        return Task.FromResult(new OcrResultDto
        {
            DetectedAmount = amount,
            MerchantName = merchant,
            RawHint = hint
        });
    }
}
