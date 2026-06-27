namespace TaxCompliance.Application.Common;

public class AppValidationException : Exception
{
    public AppValidationException(string message, IDictionary<string, string[]>? errors = null) : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public IDictionary<string, string[]> Errors { get; }
}

