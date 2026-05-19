using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Servers;
using Xunit;

namespace VoiceCraft.Network.Tests.Servers;

public class WebRtcVoiceCraftServerTests
{
    [Fact]
    public void Start_WithWssAndAutoCertificate_CreatesCertificateFile()
    {
        var certificatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pfx");
        using var world = new VoiceCraftWorld();
        using var server = new WebRtcVoiceCraftServer(world)
        {
            Config = new WebRtcVoiceCraftServer.WebRtcVoiceCraftConfig
            {
                Enabled = true,
                SignalingUrl = $"wss://127.0.0.1:{GetFreeTcpPort()}/",
                Tls = new WebRtcVoiceCraftServer.WebRtcTlsConfig
                {
                    CertificateMode = WebRtcVoiceCraftServer.WebRtcCertificateModes.SelfSigned,
                    CertificatePath = certificatePath,
                    SubjectAlternativeNames = ["127.0.0.1"]
                }
            }
        };

        try
        {
            server.Start();

            Assert.True(File.Exists(certificatePath));
            using var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                string.Empty,
                X509KeyStorageFlags.EphemeralKeySet,
                Pkcs12LoaderLimits.Defaults);
            Assert.True(certificate.HasPrivateKey);
        }
        finally
        {
            server.Stop();
            TryDelete(certificatePath);
        }
    }

    [Fact]
    public void Start_WithWssAndAutoCertificateDisabled_RequiresCertificateFile()
    {
        var certificatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pfx");
        using var world = new VoiceCraftWorld();
        using var server = new WebRtcVoiceCraftServer(world)
        {
            Config = new WebRtcVoiceCraftServer.WebRtcVoiceCraftConfig
            {
                Enabled = true,
                SignalingUrl = $"wss://127.0.0.1:{GetFreeTcpPort()}/",
                Tls = new WebRtcVoiceCraftServer.WebRtcTlsConfig
                {
                    CertificateMode = WebRtcVoiceCraftServer.WebRtcCertificateModes.SelfSigned,
                    AutoGenerateCertificate = false,
                    CertificatePath = certificatePath
                }
            }
        };

        try
        {
            Assert.Throws<InvalidOperationException>(() => server.Start());
        }
        finally
        {
            TryDelete(certificatePath);
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup for temp test certificates.
        }
    }
}
