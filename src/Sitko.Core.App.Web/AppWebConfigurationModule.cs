using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Web;

public class AppWebConfigurationModule : BaseApplicationModule<AppWebConfigurationModuleOptions>,
    IHostBuilderModule,
    IWebApplicationModule<AppWebConfigurationModuleOptions>
{
    public override string OptionsKey => "Application:Web";

    public void PostConfigureWebHost(IApplicationContext applicationContext, ConfigureWebHostBuilder webHostBuilder,
        AppWebConfigurationModuleOptions options)
    {
        if (options.Ports.Count != 0)
        {
            webHostBuilder.ConfigureKestrel(serverOptions =>
            {
                foreach (var applicationPort in options.Ports.Values)
                {
                    applicationContext.Logger.LogInformation("Listen to {ApplicationPort}", applicationPort);
                    serverOptions.ListenAnyIP(applicationPort.Port, listenOptions =>
                    {
                        listenOptions.Protocols = applicationPort.Protocol;
                        if (applicationPort.UseTLS)
                        {
                            listenOptions.UseHttps(GetSelfSignedCertificate(applicationPort.TLSDomain));
                        }
                    });
                }
            });
        }
    }

    private static X509Certificate2 GetSelfSignedCertificate(string domain)
    {
        var password = Guid.NewGuid().ToString();
        const int rsaKeySize = 2048;
        const int years = 5;
        var hashAlgorithm = HashAlgorithmName.SHA256;

        using var rsa = RSA.Create(rsaKeySize);
        var request = new CertificateRequest($"cn={domain}", rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature, false)
        );
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.1")], false)
        );

        var certificate =
            request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(years));
        if (OperatingSystem.IsWindows())
        {
            certificate.FriendlyName = domain;
        }

        // Return the PFX exported version that contains the key
        return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password,
            X509KeyStorageFlags.MachineKeySet);
    }
}

public class AppWebConfigurationModuleOptions : BaseModuleOptions
{
    public Dictionary<string, ApplicationPort> Ports { get; set; } = new();
}

// ReSharper disable once ClassNeverInstantiated.Global
public class ApplicationPort
{
    public int Port { get; set; }
    public HttpProtocols Protocol { get; set; } = HttpProtocols.Http1AndHttp2;
    public bool UseTLS { get; set; }
    public string TLSDomain { get; set; } = "localhost";
}
