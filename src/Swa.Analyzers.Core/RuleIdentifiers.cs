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
}
