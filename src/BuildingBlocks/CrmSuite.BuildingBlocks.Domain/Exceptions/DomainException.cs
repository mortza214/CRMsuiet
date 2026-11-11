namespace CrmSuite.BuildingBlocks.Domain.Exceptions;

/// <summary>A business rule was violated. Expected and safe to expose to API clients.</summary>
public class DomainException : Exception
{
    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message)
    {
        Code = code;
    }

    public DomainException(string message, string code, IDictionary<string, string[]> errors)
        : base(message)
    {
        Code = code;
        Errors = new Dictionary<string, string[]>(errors, StringComparer.OrdinalIgnoreCase);
    }

    public string Code { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
}
