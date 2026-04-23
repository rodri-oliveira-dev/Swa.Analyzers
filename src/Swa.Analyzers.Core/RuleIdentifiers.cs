namespace Swa.Analyzers.Core;

internal static class RuleIdentifiers
{
    public const string AvoidAsyncVoid = "ARCH001";
    public const string AvoidTaskContinueWith = "ARCH002";
    public const string ProhibitNotBeNullInTests = "ARCH003";
    public const string EnforceSutNamingInUnitTests = "ARCH004";
    public const string RestrictArgAnyUsage = "ARCH005";
    public const string WarnOnExcludingInBeEquivalentTo = "ARCH006";
    public const string DetectStringConcatenationInsideLoops = "ARCH007";
    public const string ProhibitManualPathComposition = "ARCH008";
    public const string ProhibitSyncOverAsyncBlockingCalls = "ARCH009";
    public const string EnforceCancellationTokenPropagation = "ARCH010";
    public const string ProhibitAsyncOrBlockingInConstructors = "ARCH011";
    public const string PreferDateTimeOffsetOverDateTime = "ARCH012";
    public const string RestrictMockingFrameworksToNSubstitute = "ARCH013";
    public const string PreferIsEquivalentOverArgIs = "ARCH014";
}
