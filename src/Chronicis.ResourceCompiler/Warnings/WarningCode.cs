namespace Chronicis.ResourceCompiler.Warnings;

public enum WarningCode
{
    InvalidKey,
    MissingKey,
    DuplicateKey,
    MissingForeignKey,
    InvalidManifest,
    OrderByFieldMissing,
    MaxDepthExceeded,
    CycleDetected
}
