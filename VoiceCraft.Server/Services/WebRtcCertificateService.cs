using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using OpenPort.Net;
using OpenPort.Net.Models;
using Spectre.Console;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Server.Services;

public sealed class WebRtcCertificateService
{
    public async Task EnsureCertificateAsync(
        WebRtcVoiceCraftServer.WebRtcVoiceCraftConfig config,
        CancellationToken cancellationToken = default)
    {
        if (!config.Enabled ||
            !IsSecureSignalingUrl(config.SignalingUrl))
            return;

        var tls = config.Tls ?? new WebRtcVoiceCraftServer.WebRtcTlsConfig();
        if (!IsLetsEncryptMode(tls))
            return;

        var certificatePath = ResolvePath(tls.CertificatePath);
        if (IsCertificateFresh(certificatePath, tls.CertificatePassword, tls.Acme?.RenewBeforeDays ?? 30))
            return;

        var acmeConfig = tls.Acme ?? new WebRtcVoiceCraftServer.WebRtcAcmeConfig();
        var domains = GetDomains(config.SignalingUrl, acmeConfig.Domains);
        if (domains.Count == 0)
        {
            throw new InvalidOperationException(
                "WebRTC Let's Encrypt certificate mode requires a public DNS name in Tls.Acme.Domains or SignalingUrl.");
        }

        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(certificatePath) ?? AppDomain.CurrentDomain.BaseDirectory);

        AnsiConsole.MarkupLine($"[yellow]Requesting WebRTC WSS certificate for {string.Join(", ", domains)}.[/]");
        await using var challengeServer = new WebRtcAcmeHttpChallengeServer(
            acmeConfig.HttpChallengeBindAddress,
            acmeConfig.HttpChallengePort);
        await challengeServer.StartAsync(cancellationToken).ConfigureAwait(false);
        await using var challengeLease = await TryMapChallengePortAsync(acmeConfig, cancellationToken)
            .ConfigureAwait(false);

        var accountKey = LoadOrCreateAccountKey(acmeConfig.AccountKeyPath);
        var acme = new AcmeContext(GetDirectoryUri(acmeConfig), accountKey);
        await acme.NewAccount(GetAccountContacts(acmeConfig.Email), true).ConfigureAwait(false);

        var order = await acme.NewOrder(domains.ToList()).ConfigureAwait(false);
        foreach (var authorization in await order.Authorizations().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var challenge = await authorization.Http().ConfigureAwait(false);
            challengeServer.AddChallenge(challenge.Token, challenge.KeyAuthz);
            await challenge.Validate().ConfigureAwait(false);
            await WaitForChallengeAsync(
                    challenge,
                    TimeSpan.FromSeconds(Math.Max(acmeConfig.ValidationTimeoutSeconds, 30)),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var certificateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
        var certificate = await order.Generate(
                new CsrInfo
                {
                    CommonName = domains[0],
                    Organization = "VoiceCraft"
                },
                certificateKey)
            .ConfigureAwait(false);

        var pfx = certificate
            .ToPfx(certificateKey)
            .Build("VoiceCraft WebRTC Signaling", tls.CertificatePassword ?? string.Empty);
        await File.WriteAllBytesAsync(certificatePath, pfx, cancellationToken).ConfigureAwait(false);
        AnsiConsole.MarkupLine($"[green]WebRTC WSS certificate saved to {certificatePath}.[/]");
    }

    private static async Task WaitForChallengeAsync(
        IChallengeContext challenge,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var stopAt = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resource = await challenge.Resource().ConfigureAwait(false);
            switch (resource.Status)
            {
                case ChallengeStatus.Valid:
                    return;
                case ChallengeStatus.Invalid:
                    throw new InvalidOperationException(
                        $"Let's Encrypt HTTP-01 challenge failed: {resource.Error?.Detail ?? resource.Error?.Type ?? "unknown error"}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Timed out while waiting for Let's Encrypt HTTP-01 challenge validation.");
    }

    private static async Task<OpenPortLease?> TryMapChallengePortAsync(
        WebRtcVoiceCraftServer.WebRtcAcmeConfig acmeConfig,
        CancellationToken cancellationToken)
    {
        if (!acmeConfig.AutoMapHttpChallengePort)
            return null;

        try
        {
            var client = new OpenPortClient(new OpenPortOptions
            {
                Timeout = TimeSpan.FromMilliseconds(5000)
            });
            return await client.OpenLeaseAsync(
                    new PortMapping
                    {
                        InternalPort = acmeConfig.HttpChallengePort,
                        ExternalPort = 80,
                        Protocol = PortProtocol.Tcp,
                        Description = "VoiceCraft ACME HTTP-01",
                        Lifetime = TimeSpan.FromMinutes(10)
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[yellow]Could not automatically map TCP/80 for Let's Encrypt HTTP-01. Continuing without the temporary mapping.[/]");
            LogService.Log(ex);
            return null;
        }
    }

    private static bool IsCertificateFresh(string certificatePath, string? password, int renewBeforeDays)
    {
        if (!File.Exists(certificatePath))
            return false;

        try
        {
            using var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                password ?? string.Empty,
                X509KeyStorageFlags.EphemeralKeySet,
                Pkcs12LoaderLimits.Defaults);
            return certificate.NotAfter.ToUniversalTime() >
                   DateTime.UtcNow.AddDays(Math.Max(renewBeforeDays, 1));
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            return false;
        }
    }

    private static IKey LoadOrCreateAccountKey(string accountKeyPath)
    {
        var resolvedPath = ResolvePath(accountKeyPath);
        if (File.Exists(resolvedPath))
            return KeyFactory.FromPem(File.ReadAllText(resolvedPath));

        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath) ?? AppDomain.CurrentDomain.BaseDirectory);
        var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
        if (key is not IEncodable encodable)
            throw new InvalidOperationException("Failed to export Let's Encrypt account key.");

        File.WriteAllText(resolvedPath, encodable.ToPem());
        return key;
    }

    private static IReadOnlyList<string> GetDomains(string signalingUrl, IEnumerable<string>? configuredDomains)
    {
        var domains = (configuredDomains ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (domains.Count == 0 &&
            Uri.TryCreate(signalingUrl, UriKind.Absolute, out var uri))
        {
            domains.Add(uri.Host);
        }

        return domains
            .Where(IsValidAcmeDnsName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsValidAcmeDnsName(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain) ||
            domain.Contains('*', StringComparison.Ordinal) ||
            string.Equals(domain, "localhost", StringComparison.OrdinalIgnoreCase) ||
            IPAddress.TryParse(domain, out _))
            return false;

        return domain.Contains('.', StringComparison.Ordinal);
    }

    private static Uri GetDirectoryUri(WebRtcVoiceCraftServer.WebRtcAcmeConfig config)
    {
        var directoryUrl = config.UseStaging
            ? config.StagingDirectoryUrl
            : config.DirectoryUrl;
        return Uri.TryCreate(directoryUrl, UriKind.Absolute, out var uri)
            ? uri
            : new Uri("https://acme-v02.api.letsencrypt.org/directory");
    }

    private static IList<string> GetAccountContacts(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return [];

        return [$"mailto:{email.Trim()}"];
    }

    private static bool IsLetsEncryptMode(WebRtcVoiceCraftServer.WebRtcTlsConfig config) =>
        string.Equals(
            config.CertificateMode,
            WebRtcVoiceCraftServer.WebRtcCertificateModes.LetsEncrypt,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsSecureSignalingUrl(string signalingUrl) =>
        Uri.TryCreate(signalingUrl, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase);

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
    }

    private sealed class WebRtcAcmeHttpChallengeServer(
        string bindAddress,
        int port) : IAsyncDisposable
    {
        private readonly Dictionary<string, string> _responses = new(StringComparer.Ordinal);
        private readonly CancellationTokenSource _stop = new();
        private TcpListener? _listener;
        private Task? _listenTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!IPAddress.TryParse(bindAddress, out var address))
                address = IPAddress.Any;

            _listener = new TcpListener(address, port);
            _listener.Start();
            _listenTask = AcceptLoopAsync(_stop.Token);
            return Task.CompletedTask;
        }

        public void AddChallenge(string token, string keyAuthorization)
        {
            lock (_responses)
            {
                _responses[token] = keyAuthorization;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            try
            {
                _listener?.Stop();
            }
            catch
            {
                // Best-effort shutdown.
            }

            if (_listenTask != null)
            {
                try
                {
                    await _listenTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            _stop.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            if (_listener == null)
                return;

            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    return;
                }

                _ = HandleClientAsync(client, cancellationToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var _ = client;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                var stream = client.GetStream();
                var request = await ReadRequestAsync(stream, timeout.Token).ConfigureAwait(false);
                var token = GetChallengeToken(request);
                string? response;
                lock (_responses)
                {
                    response = token != null && _responses.TryGetValue(token, out var value) ? value : null;
                }

                await WriteResponseAsync(stream, response, timeout.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException or OperationCanceledException)
            {
            }
        }

        private static async Task<string> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            var length = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            return System.Text.Encoding.ASCII.GetString(buffer, 0, length);
        }

        private static string? GetChallengeToken(string request)
        {
            var firstLineEnd = request.IndexOf("\r\n", StringComparison.Ordinal);
            var firstLine = firstLineEnd >= 0 ? request[..firstLineEnd] : request;
            var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 ||
                !string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase))
                return null;

            var path = parts[1].Split('?', 2)[0];
            const string prefix = "/.well-known/acme-challenge/";
            if (!path.StartsWith(prefix, StringComparison.Ordinal))
                return null;

            return Uri.UnescapeDataString(path[prefix.Length..]);
        }

        private static async Task WriteResponseAsync(
            NetworkStream stream,
            string? response,
            CancellationToken cancellationToken)
        {
            var statusLine = response == null
                ? "HTTP/1.1 404 Not Found\r\n"
                : "HTTP/1.1 200 OK\r\n";
            var body = response ?? "Not Found";
            var bytes = System.Text.Encoding.ASCII.GetBytes(
                statusLine +
                "Content-Type: text/plain\r\n" +
                $"Content-Length: {System.Text.Encoding.ASCII.GetByteCount(body)}\r\n" +
                "Connection: close\r\n" +
                "\r\n" +
                body);
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
    }
}
