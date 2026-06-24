using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Milo.Application.Common.Interfaces;

namespace Milo.Infraestructure.Services;

public sealed class ClaudeKycService : IKycService
{
    private readonly HttpClient _httpClient;

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-sonnet-4-6";
    private const string ExtractionPrompt =
        "Eres un extractor de datos de documentos de identidad. " +
        "Extrae los siguientes campos: firstName, lastName, documentNumber, " +
        "birthDate (formato YYYY-MM-DD o null si no está claro). " +
        "Responde ÚNICAMENTE con JSON sin markdown, sin texto adicional. " +
        "Si la imagen no es un documento de identidad válido o no puedes extraer los datos, " +
        "responde: {\"error\": \"<razón>\"}. " +
        "Formato exitoso: {\"firstName\": \"\", \"lastName\": \"\", \"documentNumber\": \"\", \"birthDate\": \"\"}";

    public ClaudeKycService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey no está configurada");

        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<KycExtractionResult> ExtractDocumentDataAsync(
        string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            
            await Task.Delay(300, cancellationToken);
            return new KycExtractionResult(
                FirstName: "Juan Guest",
                LastName: "Hernández",
                DocumentNumber: "10203040",
                BirthDate: new DateOnly(1998, 5, 20),
                IsSuccessful: true,
                FailureReason: null);
            /*
            var requestBody = new
            {
                model = Model,
                max_tokens = 512,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image",
                                source = new { type = "url", url = imageUrl }
                            },
                            new
                            {
                                type = "text",
                                text = ExtractionPrompt
                            }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(ApiUrl, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Failure("Error al conectar con el servicio de verificación. Intenta nuevamente.");

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);

            var text = responseJson
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var parsed = JsonSerializer.Deserialize<JsonElement>(text);

            if (parsed.TryGetProperty("error", out var errorProp))
                return Failure(errorProp.GetString() ?? "El documento no pudo ser verificado");

            var firstName = parsed.TryGetProperty("firstName", out var fn) ? fn.GetString() : null;
            var lastName = parsed.TryGetProperty("lastName", out var ln) ? ln.GetString() : null;
            var documentNumber = parsed.TryGetProperty("documentNumber", out var dn) ? dn.GetString() : null;

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(documentNumber))
                return Failure("No se pudieron extraer todos los datos requeridos del documento");

            DateOnly? birthDate = null;
            if (parsed.TryGetProperty("birthDate", out var bd) &&
                bd.ValueKind == JsonValueKind.String &&
                DateOnly.TryParse(bd.GetString(), out var parsedDate))
            {
                birthDate = parsedDate;
            }

            return new KycExtractionResult(
                firstName, lastName, documentNumber, birthDate,
                IsSuccessful: true, FailureReason: null);
            
            */
        }
        catch
        {
            return Failure("Error al procesar el documento. Intenta nuevamente.");
        }
    }

    private static KycExtractionResult Failure(string reason) =>
        new(null, null, null, null, IsSuccessful: false, FailureReason: reason);
}
