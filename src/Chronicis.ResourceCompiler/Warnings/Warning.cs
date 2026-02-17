using System.Diagnostics.CodeAnalysis;

namespace Chronicis.ResourceCompiler.Warnings;

[ExcludeFromCodeCoverage]
public sealed class Warning
{
    public Warning(WarningCode code, WarningSeverity severity, string message, string? entityName = null, string? jsonPath = null)
    {
        Code = code;
        Severity = severity;
        Message = message;
        EntityName = entityName;
        JsonPath = jsonPath;
    }

    public WarningCode Code { get; }
    public WarningSeverity Severity { get; }
    public string Message { get; }
    public string? EntityName { get; }
    public string? JsonPath { get; }
}
