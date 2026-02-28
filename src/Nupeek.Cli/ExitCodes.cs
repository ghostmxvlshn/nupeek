namespace Nupeek.Cli;

public static class ExitCodes
{
    public const int Success = 0;
    public const int GenericError = 1;
    public const int InvalidArguments = 2;
    public const int PackageResolutionFailure = 3;
    public const int TypeOrSymbolNotFound = 4;
    public const int DecompilationFailure = 5;
}
