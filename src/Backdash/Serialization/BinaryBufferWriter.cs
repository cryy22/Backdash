using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Network;

namespace Backdash.Serialization;

/// <summary>
/// Binary span writer.
/// </summary>
/// <remarks>
/// Initialize a new <see cref="BinaryRawBufferWriter"/> for <paramref name="buffer"/>
/// </remarks>
/// <param name="buffer">Byte buffer to be written</param>
/// <param name="endianness">Serialization endianness</param>
[DebuggerDisplay("Written: {WrittenCount}")]
public readonly struct BinaryBufferWriter(ArrayBufferWriter<byte> buffer, Endianness endianness = Endianness.BigEndian)
{
    /// <summary>
    /// Gets or init the value to define which endianness should be used for serialization.
    /// </summary>
    public readonly Endianness Endianness = endianness;

    /// <summary>
    /// Backing IBufferWriter <see cref="IBufferWriter{T}"/>
    /// </summary>
    public IBufferWriter<byte> Buffer => buffer;

    /// <summary>Total written byte count.</summary>
    public int WrittenCount => buffer.WrittenCount;

    /// <summary>Written span.</summary>
    public ReadOnlySpan<byte> WrittenSpan => buffer.WrittenSpan;

    /// <summary>Advance write pointer by <paramref name="count"/>.</summary>
    public void Advance(int count) => buffer.Advance(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteSpan<T>(in ReadOnlySpan<T> data) where T : unmanaged => Write(MemoryMarshal.AsBytes(data));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> AllocSpan<T>(in ReadOnlySpan<T> value) where T : unmanaged
    {
        var sizeBytes = Unsafe.SizeOf<T>() * value.Length;
        var result = MemoryMarshal.Cast<byte, T>(buffer.GetSpan(sizeBytes));
        Advance(sizeBytes);
        return result;
    }

    /// <summary>Writes a span bytes of <see cref="byte"/> <paramref name="value"/> into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(in ReadOnlySpan<byte> value) => buffer.Write(value);

    /// <summary>Writes single <see cref="byte"/> <paramref name="value"/> into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(in byte value) => buffer.Write(Mem.AsSpan(in value));

    /// <summary>Writes single <see cref="sbyte"/> <paramref name="value"/> into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(in sbyte value) => Write(unchecked((byte)value));

    /// <summary>Writes single <see cref="bool"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in bool value)
    {
        const int size = sizeof(bool);
        if (!BitConverter.TryWriteBytes(buffer.GetSpan(size), value))
            throw new NetcodeException("Destination is too short");

        Advance(size);
    }

    /// <summary>Writes single <see cref="short"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in short value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="ushort"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ushort value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="int"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in int value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="uint"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in uint value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="char"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in char value) => Write((ushort)value);

    /// <summary>Writes single <see cref="long"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in long value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="ulong"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ulong value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="Int128"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in Int128 value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="UInt128"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in UInt128 value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="Half"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in Half value) => Write(BitConverter.HalfToInt16Bits(value));

    /// <summary>Writes single <see cref="float"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in float value) => Write(BitConverter.SingleToInt32Bits(value));

    /// <summary>Writes single <see cref="double"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in double value) => Write(BitConverter.DoubleToInt64Bits(value));

    /// <summary>Writes a span of <see cref="sbyte"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<sbyte> value) => WriteSpan(in value);

    /// <summary>Writes a span of <see cref="bool"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<bool> value) => WriteSpan(in value);

    /// <summary>Writes a span of <see cref="short"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<short> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="ushort"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<ushort> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="char"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<char> value) => Write(MemoryMarshal.Cast<char, ushort>(value));

    /// <summary>Writes a span of <see cref="int"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<int> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="uint"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<uint> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="long"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<long> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="ulong"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<ulong> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="Int128"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<Int128> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="UInt128"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<UInt128> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a <see cref="IBinarySerializable"/> <paramref name="value"/> into buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable"/>.</typeparam>
    public void Write<T>(in T value) where T : IBinarySerializable => value.Serialize(in this);

    /// <summary>Writes span of <see cref="IBinarySerializable"/> <paramref name="values"/> into buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable"/>.</typeparam>
    public void Write<T>(in ReadOnlySpan<T> values) where T : IBinarySerializable
    {
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current.Serialize(in this);
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Writes array of <see cref="IBinarySerializable"/> <paramref name="values"/> into buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable"/>.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<T>(in T[] values) where T : IBinarySerializable =>
        Write<T>(values.AsSpan());

    /// <summary>Writes list of <see cref="IBinarySerializable"/> <paramref name="values"/> into buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable"/>.</typeparam>
    public void Write<T>(in List<T> values) where T : IBinarySerializable => Write<T>(GetListSpan(values));


    /// <summary>Writes an unmanaged struct into buffer.</summary>
    public void WriteStruct<T>(in T value) where T : unmanaged => Write(Mem.AsBytes(in value));

    /// <summary>Writes an unmanaged struct span into buffer.</summary>
    public void WriteStruct<T>(ReadOnlySpan<T> values) where T : unmanaged => Write(MemoryMarshal.AsBytes(values));

    /// <summary>Writes an unmanaged struct span into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStruct<T>(in T[] values) where T : unmanaged => WriteStruct<T>(values.AsSpan());

    /// <summary>Writes a struct into buffer.</summary>
    public void WriteStructUnsafe<T>(in T value) where T : struct => Write(Mem.AsBytesUnsafe(in value));

    /// <summary>Writes a struct span into buffer.</summary>
    public void WriteStructUnsafe<T>(ReadOnlySpan<T> values) where T : struct
    {
        ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<T>();
        Write(MemoryMarshal.AsBytes(values));
    }

    /// <summary>Writes an <see cref="string"/> <paramref name="value"/> into buffer.</summary>
    public void WriteString(in string value) => Write(value.AsSpan());

    /// <summary>Writes an <see cref="string"/> <paramref name="value"/> into buffer as UTF8.</summary>
    public void WriteUtf8String(in ReadOnlySpan<char> value)
    {
        var span = buffer.GetSpan(System.Text.Encoding.UTF8.GetByteCount(value));
        var writtenCount = System.Text.Encoding.UTF8.GetBytes(value, span);
        Advance(writtenCount);
    }

    /// <summary>Writes an <see cref="char"/> <paramref name="value"/> into buffer as UTF8.</summary>
    public void WriteUtf8Char(in char value) => WriteUtf8String(Mem.AsSpan(in value));

    /// <summary>Writes a <see cref="IBinaryInteger{T}"/> <paramref name="value"/> into buffer.</summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IBinaryInteger{T}"/>.</typeparam>
    public void WriteNumber<T>(in T value) where T : unmanaged, IBinaryInteger<T>
    {
        ref var valueRef = ref Unsafe.AsRef(in value);
        var size = Unsafe.SizeOf<T>();
        switch (Endianness)
        {
            case Endianness.LittleEndian:
                valueRef.TryWriteLittleEndian(buffer.GetSpan(size), out size);
                break;
            case Endianness.BigEndian:
                valueRef.TryWriteBigEndian(buffer.GetSpan(size), out size);
                break;
            default:
                return;
        }

        Advance(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> GetListSpan<T>(List<T> values)
    {
        var span = CollectionsMarshal.AsSpan(values);
        Write(span.Length);
        return span;
    }
}
