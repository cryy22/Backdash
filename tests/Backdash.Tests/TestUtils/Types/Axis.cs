using System.Runtime.InteropServices;

namespace Backdash.Tests.TestUtils.Types;

[StructLayout(LayoutKind.Sequential, Size = 2), Serializable]
public record struct Axis(sbyte X, sbyte Y)
{
    public sbyte X = X;
    public sbyte Y = Y;
}
