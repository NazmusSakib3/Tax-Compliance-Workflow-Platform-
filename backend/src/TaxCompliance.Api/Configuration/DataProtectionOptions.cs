namespace TaxCompliance.Api.Configuration;

public class DataProtectionOptions
{
    public const string SectionName = "DataProtection";

    /// <summary>
    /// Directory where Data Protection keys are persisted. When set, MFA secrets survive API restarts.
    /// </summary>
    public string? KeysPath { get; set; }
}
