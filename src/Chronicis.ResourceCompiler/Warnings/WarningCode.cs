namespace Chronicis.ResourceCompiler.Warnings;

public enum WarningCode
{
    InvalidKey,
    MissingKey,
    DuplicateKey,
    DuplicatePk,
    MissingForeignKey,
    InvalidManifest,
    OrderByFieldMissing,
    MaxDepthExceeded,
    CycleDetected,
    RawFileNotFound,
    RawFileUnreadable,
    RawJsonParseError,
    RawRootNotArray,
    RawRowNotObject,
    MissingPk,
    InvalidPkType,
    MissingFk,
    InvalidFkType,
    OutputWriteFailed
}
