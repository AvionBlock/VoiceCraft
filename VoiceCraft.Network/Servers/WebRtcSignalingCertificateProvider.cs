using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace VoiceCraft.Network.Servers;

internal static class WebRtcSignalingCertificateProvider
{
    private const string DefaultSubjectName = "VoiceCraft WebRTC Signaling";
    private const string DefaultCertificatePath = "config/webrtc-signaling.pfx";

    public static X509Certificate2 LoadOrCreate(
        string signalingUrl,
        WebRtcVoiceCraftServer.WebRtcTlsConfig config)
    {
        var certificatePath = ResolvePath(string.IsNullOrWhiteSpace(config.CertificatePath)
            ? DefaultCertificatePath
            : config.CertificatePath);
        if (File.Exists(certificatePath))
            return LoadCertificate(certificatePath, config.CertificatePassword);

        if (!IsSelfSignedMode(config))
            throw new InvalidOperationException(
                $"WebRTC WSS signaling requires a certificate at '{certificatePath}'. Configure {nameof(config.CertificatePath)}, use the '{WebRtcVoiceCraftServer.WebRtcCertificateModes.SelfSigned}' certificate mode, or let the VoiceCraft server create a Let's Encrypt certificate before starting the WebRTC transport.");

        if (!config.AutoGenerateCertificate)
            throw new InvalidOperationException(
                $"WebRTC WSS signaling requires a certificate. Configure {nameof(config.CertificatePath)} or enable {nameof(config.AutoGenerateCertificate)} for self-signed certificates.");

        using var certificate = CreateSelfSignedCertificate(signalingUrl, config);
        Directory.CreateDirectory(Path.GetDirectoryName(certificatePath) ?? AppDomain.CurrentDomain.BaseDirectory);
        File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pkcs12, GetPassword(config)));
        return LoadCertificate(certificatePath, config.CertificatePassword);
    }

    private static X509Certificate2 LoadCertificate(string certificatePath, string? password)
    {
        return X509CertificateLoader.LoadPkcs12FromFile(
            certificatePath,
            password ?? string.Empty,
            X509KeyStorageFlags.Exportable,
            Pkcs12LoaderLimits.Defaults);
    }

    private static X509Certificate2 CreateSelfSignedCertificate(
        string signalingUrl,
        WebRtcVoiceCraftServer.WebRtcTlsConfig config)
    {
        using var rsa = RSA.Create(2048);
        var subjectName = string.IsNullOrWhiteSpace(config.SubjectName)
            ? DefaultSubjectName
            : config.SubjectName.Trim();
        var request = new CertificateRequest(
            $"CN={EscapeDistinguishedName(subjectName)}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication")],
            false));

        var alternativeNames = GetAlternativeNames(signalingUrl, config.SubjectAlternativeNames ?? []);
        if (alternativeNames.Count > 0)
            request.CertificateExtensions.Add(BuildSubjectAlternativeNames(alternativeNames));

        var lifetimeDays = Math.Max(config.CertificateLifetimeDays, 1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(lifetimeDays));

        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pkcs12, GetPassword(config)),
            GetPassword(config),
            X509KeyStorageFlags.Exportable,
            Pkcs12LoaderLimits.Defaults);
    }

    private static X509Extension BuildSubjectAlternativeNames(IReadOnlyCollection<string> alternativeNames)
    {
        var builder = new SubjectAlternativeNameBuilder();
        foreach (var alternativeName in alternativeNames)
        {
            if (IPAddress.TryParse(alternativeName, out var ipAddress))
                builder.AddIpAddress(ipAddress);
            else
                builder.AddDnsName(alternativeName);
        }

        return builder.Build();
    }

    private static List<string> GetAlternativeNames(string signalingUrl, IEnumerable<string> configuredNames)
    {
        var names = configuredNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (Uri.TryCreate(signalingUrl, UriKind.Absolute, out var uri) &&
            !IsAnyAddress(uri.Host))
            names.Add(uri.Host);

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsAnyAddress(string host) =>
        string.Equals(host, "0.0.0.0", StringComparison.Ordinal) ||
        string.Equals(host, "::", StringComparison.Ordinal) ||
        string.Equals(host, "[::]", StringComparison.Ordinal) ||
        string.Equals(host, "*", StringComparison.Ordinal);

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
    }

    private static string GetPassword(WebRtcVoiceCraftServer.WebRtcTlsConfig config) =>
        config.CertificatePassword ?? string.Empty;

    private static bool IsSelfSignedMode(WebRtcVoiceCraftServer.WebRtcTlsConfig config) =>
        string.Equals(
            config.CertificateMode,
            WebRtcVoiceCraftServer.WebRtcCertificateModes.SelfSigned,
            StringComparison.OrdinalIgnoreCase);

    private static string EscapeDistinguishedName(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("+", "\\+", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("<", "\\<", StringComparison.Ordinal)
            .Replace(">", "\\>", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal);
}
