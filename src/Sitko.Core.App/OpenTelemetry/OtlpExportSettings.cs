using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;

namespace Sitko.Core.App.OpenTelemetry
{
    internal sealed record OtlpExportSettings(Uri Endpoint, OtlpExportProtocol Protocol,
        Dictionary<string, string> Headers)
    {
        public static OtlpExportSettings Resolve(OpenTelemetryModuleOptions options)
        {
            var endpointText = GetEnvironmentValue("ENDPOINT");
            var endpoint = options.Endpoint;
            if (endpointText is not null && !Uri.TryCreate(endpointText, UriKind.Absolute, out endpoint))
            {
                throw Invalid("Endpoint must be an absolute HTTP or HTTPS URL.");
            }

            if (endpoint is not { IsAbsoluteUri: true } ||
                (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                throw Invalid("Endpoint must be an absolute HTTP or HTTPS URL.");
            }

            var protocol = GetEnvironmentValue("PROTOCOL")?.Trim().ToLowerInvariant() switch
            {
                null => options.OtlpExportProtocol,
                "grpc" => OtlpExportProtocol.Grpc,
                "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                _ => throw Invalid("Protocol must be grpc or http/protobuf.")
            };
            if (protocol is not (OtlpExportProtocol.Grpc or OtlpExportProtocol.HttpProtobuf))
            {
                throw Invalid("Protocol must be Grpc or HttpProtobuf.");
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var environmentHeaders = GetEnvironmentValue("HEADERS");
            if (environmentHeaders is not null)
            {
                // SDK 1.20.1 decodes the complete value before splitting pairs.
                var decodedHeaders = Uri.UnescapeDataString(environmentHeaders);
                var pairs = (decodedHeaders.EndsWith(',') ? decodedHeaders[..^1] : decodedHeaders).Split(',');
                foreach (var pair in pairs)
                {
                    var separator = pair.IndexOf('=');
                    if (separator < 1)
                    {
                        throw Invalid("Headers must contain comma-separated name=value pairs.");
                    }

                    headers[pair[..separator].Trim()] = pair[(separator + 1)..].Trim();
                }
            }
            else
            {
                if (options.Headers is null)
                {
                    throw Invalid("Headers must be a dictionary.");
                }

                foreach (var header in options.Headers)
                {
                    headers[header.Key] = header.Value;
                }
            }

            foreach (var header in headers)
            {
                if (string.IsNullOrEmpty(header.Key) ||
                    header.Key.Any(static character => !char.IsAsciiLetterOrDigit(character) &&
                        !"!#$%&'*+-.^_`|~".Contains(character, StringComparison.Ordinal)) ||
                    header.Value is null || header.Value.Any(static character => character is < ' ' or > '~' or ','))
                {
                    throw Invalid("Headers contain an invalid name or unsupported value.");
                }
            }

            return new OtlpExportSettings(endpoint, protocol, headers);
        }

        public Uri GetEndpoint(string signal) => Protocol == OtlpExportProtocol.HttpProtobuf
            ? new UriBuilder(Endpoint) { Path = Endpoint.AbsolutePath.TrimEnd('/') + "/v1/" + signal }.Uri
            : Endpoint;

        public void Apply(OtlpExporterOptions exporter, string signal)
        {
            exporter.Endpoint = GetEndpoint(signal);
            exporter.Protocol = Protocol;
            exporter.Headers = string.Join(",", Headers.Select(static header =>
                $"{Uri.EscapeDataString(header.Key)}={Uri.EscapeDataString(header.Value)}"));
        }

        private static string? GetEnvironmentValue(string suffix)
        {
            var value = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_" + suffix);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static OptionsValidationException Invalid(string message) =>
            new(nameof(OpenTelemetryModuleOptions), typeof(OpenTelemetryModuleOptions), [message]);
    }
}
