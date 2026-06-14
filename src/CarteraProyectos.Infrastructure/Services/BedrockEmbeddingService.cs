using System.Net.Http.Headers;
using System.Text.Json;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using CarteraProyectos.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarteraProyectos.Infrastructure.Services;

public sealed class BedrockEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly AmazonBedrockRuntimeClient? _sdkClient;
    private static readonly HttpClient _httpClient = new();
    private readonly string? _bearerToken;
    private readonly string _region;
    private readonly ILogger<BedrockEmbeddingService> _logger;
    private const string ModelId = "amazon.titan-embed-text-v2:0";

    public BedrockEmbeddingService(IConfiguration config, ILogger<BedrockEmbeddingService> logger)
    {
        _logger = logger;
        _region = config["Aws:Region"] ?? "us-east-1";
        _bearerToken = config["Aws:BearerToken"];

        if (string.IsNullOrEmpty(_bearerToken))
        {
            var accessKey    = config["Aws:AccessKeyId"];
            var secretKey    = config["Aws:SecretAccessKey"];
            var sessionToken = config["Aws:SessionToken"];

            AWSCredentials credentials = !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey)
                ? !string.IsNullOrEmpty(sessionToken)
                    ? new SessionAWSCredentials(accessKey, secretKey, sessionToken)
                    : new BasicAWSCredentials(accessKey, secretKey)
                : new InstanceProfileAWSCredentials();

            _sdkClient = new AmazonBedrockRuntimeClient(credentials, RegionEndpoint.GetBySystemName(_region));
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        return !string.IsNullOrEmpty(_bearerToken)
            ? await GetEmbeddingWithBearerTokenAsync(text, ct)
            : await GetEmbeddingWithSdkAsync(text, ct);
    }

    private async Task<float[]> GetEmbeddingWithBearerTokenAsync(string text, CancellationToken ct)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new { inputText = text.Length > 8192 ? text[..8192] : text });
        var url  = $"https://bedrock-runtime.{_region}.amazonaws.com/model/{ModelId}/invoke";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return doc.RootElement.GetProperty("embedding")
                      .EnumerateArray()
                      .Select(e => e.GetSingle())
                      .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding (bearer token) for text of length {Length}", text.Length);
            throw;
        }
    }

    private async Task<float[]> GetEmbeddingWithSdkAsync(string text, CancellationToken ct)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new { inputText = text.Length > 8192 ? text[..8192] : text });

        var request = new InvokeModelRequest
        {
            ModelId     = ModelId,
            ContentType = "application/json",
            Accept      = "application/json",
            Body        = new MemoryStream(body),
        };

        try
        {
            var response = await _sdkClient!.InvokeModelAsync(request, ct);
            using var doc = await JsonDocument.ParseAsync(response.Body, cancellationToken: ct);
            return doc.RootElement.GetProperty("embedding")
                      .EnumerateArray()
                      .Select(e => e.GetSingle())
                      .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding (SDK) for text of length {Length}", text.Length);
            throw;
        }
    }

    public void Dispose() => _sdkClient?.Dispose();
}
