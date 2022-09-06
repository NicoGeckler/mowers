using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Mowers;

internal readonly ref struct NullableRef<T>
    where T : struct
{
    //FIX-blocked: in net7, a ref field could be used instead of span
    private readonly Span<T> _ref;

    public readonly bool HasValue => this._ref.Length > 0;
    public readonly ref T Ref => ref this._ref[0];

    public readonly T? NullableValue => this.HasValue ? this.Ref : null;

    public NullableRef(ref T value)
    {
        this._ref = MemoryMarshal.CreateSpan(ref value, 1);
    }
}
