using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceCraft.Core.Telemetry;

public static class TelemetryTransport
{
    public static Action<string>? FailureLogger { get; set; }

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static async Task<bool> SendTelemetryAsync(TelemetryEventRequest payload, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("/v1/telemetry");
        try
        {
            using var content = JsonContent.Create(payload, TelemetryJsonContext.Default.TelemetryEventRequest);
            using var response = await HttpClient.PostAsync(uri, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                LogFailure($"Telemetry POST {uri} failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogFailure($"Telemetry POST {uri} failed with exception {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    public static async Task<TelemetryDumpResponse?> SendDumpAsync(TelemetryDumpRequest payload, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("/v1/dumps");
        try
        {
            using var content = JsonContent.Create(payload, TelemetryJsonContext.Default.TelemetryDumpRequest);
            using var response = await HttpClient.PostAsync(uri, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                LogFailure($"Telemetry dump POST {uri} failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync(
                TelemetryJsonContext.Default.TelemetryDumpResponse,
                cancellationToken);
        }
        catch (Exception ex)
        {
            LogFailure($"Telemetry dump POST {uri} failed with exception {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static Uri BuildUri(string relativePath)
    {
        return new Uri(new Uri(Constants.TelemetryBaseUrl, UriKind.Absolute), relativePath);
    }

    private static void LogFailure(string message)
    {
        FailureLogger?.Invoke(message);
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
