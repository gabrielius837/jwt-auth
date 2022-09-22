using System.Diagnostics.CodeAnalysis;

namespace App.Api;

// taken from
// https://github.com/stella3d/BlobHandles/blob/master/Runtime/Structs/BlobHandle.cs
public class ByteComparison : IEqualityComparer<byte[]>
{
    private ByteComparison() { }

    public static ByteComparison Instance { get; } = new ByteComparison();

    public bool Equals(byte[]? x, byte[]? y)
    {
        if (x == y)
            return true;
        
        if (x is null || y is null)
            return false;

        if (x.Length != y.Length)
            return false;

        for (int i = 0; i < x.Length; i++)
            if (x[i] != y[i])
                return false;

        return true;
    }

    public int GetHashCode([DisallowNull] byte[] obj)
    {
        unchecked
        {
            return obj.Length * 397 ^ obj[0] ^ obj[obj.Length - 1];
        }
    }
}
