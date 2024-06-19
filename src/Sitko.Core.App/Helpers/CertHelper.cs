namespace Sitko.Core.App.Helpers;

public static class CertHelper
{
    public static string GetCertPath(string sslCertBase64)
    {
        var cert = Convert.FromBase64String(sslCertBase64);
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, cert);
        return path;
    }
}
