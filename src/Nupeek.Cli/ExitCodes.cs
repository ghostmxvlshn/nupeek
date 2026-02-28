namespace Nupeek.Cli;

/// <summary>
/// Stable process exit codes for CLI and automation consumers.
/// </summary>
public static class ExitCodes
{
    /// <summary>Command completed successfully.</summary>
    public const int Success = 0;

    /// <summary>Unexpected or unclassified failure.</summary>
    public const int GenericError = 1;

    /// <summary>Invalid user input or command arguments.</summary>
    public const int InvalidArguments = 2;

    /// <summary>Package acquisition or package metadata resolution failure.</summary>
    public const int PackageResolutionFailure = 3;

    /// <summary>Requested type/symbol could not be resolved in package content.</summary>
    public const int TypeOrSymbolNotFound = 4;

    /// <summary>Failure while decompiling or writing generated output.</summary>
    public const int DecompilationFailure = 5;

    /// <summary>Operation canceled by user (Ctrl+C).</summary>
    public const int OperationCanceled = 130;
}
