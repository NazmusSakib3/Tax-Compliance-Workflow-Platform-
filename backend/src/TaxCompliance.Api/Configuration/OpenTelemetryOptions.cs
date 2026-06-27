namespace TaxCompliance.Api.Configuration;

public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; set; } = "TaxCompliance.Api";

    /// <summary>
    /// OTLP gRPC endpoint, for example http://otel-collector:4317.
    /// When unset, Development uses the console exporter; Production emits no trace export.
    /// </summary>
    public string? OtlpEndpoint { get; set; }
}
