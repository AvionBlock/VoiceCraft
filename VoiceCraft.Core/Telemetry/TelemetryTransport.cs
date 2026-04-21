using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceCraft.Core.Telemetry;

public static class TelemetryTransport
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static async Task SendTelemetryAsync(TelemetryEventRequest payload,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("/v1/telemetry");
        Exception exception;
        try
        {
            using var content = JsonContent.Create(payload, TelemetryJsonContext.Default.TelemetryEventRequest);
            using var response = await HttpClient.PostAsync(uri, content, cancellationToken);
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            exception = new Exception(
                $"Telemetry POST {uri} failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }
        catch (Exception ex)
        {
            exception = new Exception($"Telemetry POST {uri} failed with exception {ex.GetType().Name}: {ex.Message}");
        }

        throw exception;
    }

    public static async Task<TelemetryDumpResponse?> SendDumpAsync(TelemetryDumpRequest payload,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("/v1/dumps");
        Exception exception;
        try
        {
            using var content = JsonContent.Create(payload, TelemetryJsonContext.Default.TelemetryDumpRequest);
            using var response = await HttpClient.PostAsync(uri, content, cancellationToken);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync(
                    TelemetryJsonContext.Default.TelemetryDumpResponse,
                    cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            exception = new Exception(
                $"Telemetry dump POST {uri} failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }
        catch (Exception ex)
        {
            exception = new Exception(
                $"Telemetry dump POST {uri} failed with exception {ex.GetType().Name}: {ex.Message}");
        }

        throw exception;
    }

    private static Uri BuildUri(string relativePath)
    {
        return new Uri(new Uri(Constants.TelemetryBaseUrl, UriKind.Absolute), relativePath);
    }
}

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TelemetryEventRequest))]
[JsonSerializable(typeof(TelemetryDumpRequest))]
[JsonSerializable(typeof(TelemetryDumpResponse))]
public partial class TelemetryJsonContext : JsonSerializerContext;