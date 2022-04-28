/// <summary>
/// Needed to support C#9 init only properties.
/// Ref: https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
/// </summary>
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}