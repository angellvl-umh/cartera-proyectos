using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using CarteraProyectos.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarteraProyectos.Infrastructure.Identity;

public sealed class KeycloakOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string Realm { get; init; } = string.Empty;
    public string AdminClientId { get; init; } = string.Empty;
    public string AdminClientSecret { get; init; } = string.Empty;
}

public sealed class KeycloakAdminService : IIdentityProviderService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakAdminService> _logger;

    // Alfabeto sin ambiguos: sin l, 1, O, 0, i, I
    private const string Alphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789!@#$%";

    public KeycloakAdminService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<KeycloakAdminService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = new KeycloakOptions
        {
            BaseUrl = config["Keycloak:BaseUrl"] ?? "http://keycloak:8080",
            Realm = config["Keycloak:Realm"] ?? "cartera",
            AdminClientId = config["Keycloak:AdminClientId"] ?? "cartera-admin",
            AdminClientSecret = config["Keycloak:AdminClientSecret"] ?? "cartera-admin-secret",
        };
    }

    public async Task<IdentityCredentialsResult> CreateUserWithTemporaryPasswordAsync(
        string name, string email, CancellationToken ct)
    {
        try
        {
            var token = await GetAccessTokenAsync(ct);
            if (token is null)
                return new IdentityCredentialsResult(IdentityCredentialsStatus.Unavailable, null);

            var tempPassword = GenerateTemporaryPassword();

            var firstName = name.Split(' ', 2)[0];
            var lastName = name.Split(' ', 2).Length > 1 ? name.Split(' ', 2)[1] : string.Empty;

            var userPayload = new
            {
                username = email,
                email = email,
                firstName = firstName,
                lastName = lastName,
                enabled = true,
                emailVerified = true,
                requiredActions = new[] { "UPDATE_PASSWORD" },
                credentials = new[]
                {
                    new { type = "password", value = tempPassword, temporary = true }
                }
            };

            var json = JsonSerializer.Serialize(userPayload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
                return new IdentityCredentialsResult(IdentityCredentialsStatus.Created, tempPassword);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return new IdentityCredentialsResult(IdentityCredentialsStatus.AlreadyExists, null);

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Keycloak devolvió {StatusCode} al crear usuario {Email}. Body: {Body}",
                (int)response.StatusCode, email, body);
            return new IdentityCredentialsResult(IdentityCredentialsStatus.Unavailable, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario {Email} en Keycloak.", email);
            return new IdentityCredentialsResult(IdentityCredentialsStatus.Unavailable, null);
        }
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var tokenUrl = $"{_options.BaseUrl}/realms/{_options.Realm}/protocol/openid-connect/token";

        var formContent = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _options.AdminClientId),
            new KeyValuePair<string, string>("client_secret", _options.AdminClientSecret),
        ]);

        try
        {
            using var response = await _httpClient.PostAsync(tokenUrl, formContent, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Keycloak token endpoint devolvió {StatusCode}. Body: {Body}",
                    (int)response.StatusCode, body);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener token de Keycloak.");
            return null;
        }
    }

    private static string GenerateTemporaryPassword()
    {
        var chars = new char[12];
        var alphabetLength = Alphabet.Length;

        for (int i = 0; i < chars.Length; i++)
        {
            // RandomNumberGenerator.GetInt32 es criptográficamente seguro
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(alphabetLength)];
        }

        return new string(chars);
    }
}
