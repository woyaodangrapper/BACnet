using System.Buffers;

namespace BACnet.Serializer;

/// <summary>
/// Provides a buffer for encoding operations, supporting dynamic resizing and efficient memory management.
/// </summary>
/// <remarks>The <see cref="EncodeBuffer"/> class is designed to facilitate encoding operations by managing a byte
/// buffer with support for dynamic resizing and efficient memory pooling. It can either allocate its own buffer or wrap
/// an existing one. The buffer is expandable if created with the default constructor or the size-based constructor, but
/// not when wrapping an existing buffer.  This class uses the <see cref="ArrayPool{T}"/> to rent and return buffers,
/// reducing memory allocation overhead. Call <see cref="Dispose"/> to release resources when the buffer is no longer
/// needed.</remarks>
public class EncodeBuffer : IDisposable
{
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

    private byte[] _buffer;
    private int _bufferLength; // the rented length
    private bool _ownsBuffer;

    private int _offset;
    private int _maxOffset;
    private int _serializeCounter;
    private int _minLimit;
    private EncodeResult _result;
    private bool _expandable;

    public IEnumerable<byte> Buffer => _buffer;
    public int Offset => _offset;
    public int MaxOffset => _maxOffset;
    public int SerializeCounter => _serializeCounter;
    public int MinLimit => _minLimit;
    public EncodeResult Result => _result;
    public bool Expandable => _expandable;

    /// <summary>
    /// Creates a new EncodeBuffer with an initial size of 128 bytes.
    /// </summary>
    public EncodeBuffer() : this(128) { }

    /// <summary>
    /// Creates a new EncodeBuffer with a custom initial size.
    /// </summary>
    public EncodeBuffer(int size)
    {
        _bufferLength = size;
        _buffer = BytePool.Rent(_bufferLength);
        _ownsBuffer = true;

        _expandable = true;
        _maxOffset = _bufferLength - 1;
        _offset = 0;
        _serializeCounter = 0;
        _minLimit = 0;
        _result = EncodeResult.None;
    }

    /// <summary>
    /// Wraps an existing buffer (not rented) at a given offset. Will not be returned to pool.
    /// </summary>
    public EncodeBuffer([NotNull] byte[] buffer, int offset)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _bufferLength = buffer.Length;
        _ownsBuffer = false;

        _offset = offset;
        _maxOffset = _bufferLength;
        _expandable = false;
        _serializeCounter = 0;
        _minLimit = 0;
        _result = EncodeResult.None;
    }

    public void Increment()
    {
        if (_offset < _maxOffset)
        {
            if (_serializeCounter >= _minLimit)
                _offset++;
            _serializeCounter++;
        }
        else
        {
            if (_serializeCounter >= _minLimit)
                _offset++;
        }
    }

    public void Add(byte b)
    {
        if (_offset < _maxOffset)
        {
            if (_serializeCounter >= _minLimit)
                _buffer[_offset] = b;
        }
        else
        {
            if (_expandable)
            {
                var newSize = _bufferLength * 2;
                var newBuf = BytePool.Rent(newSize);
                Array.Copy(_buffer, 0, newBuf, 0, _bufferLength);

                if (_ownsBuffer)
                    BytePool.Return(_buffer);

                _buffer = newBuf;
                _bufferLength = newSize;
                _maxOffset = _bufferLength - 1;

                if (_serializeCounter >= _minLimit)
                    _buffer[_offset] = b;

                _ownsBuffer = true;
            }
            else
            {
                _result |= EncodeResult.NotBuffer;
            }
        }

        Increment();
    }

    public void Add([NotNull] byte[] data, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Add(data[i]);
        }
    }

    public void Add([NotNull] IEnumerable<byte> data, int count)
    {
        int added = 0;
        using var enumerator = data.GetEnumerator();
        while (added < count && enumerator.MoveNext())
        {
            Add(enumerator.Current);
            added++;
        }
    }

    public int GetDiff([NotNull] EncodeBuffer other)
    {
        int diff = Math.Abs(other._offset - _offset);
        diff = Math.Max(Math.Abs(other._serializeCounter - _serializeCounter), diff);
        return diff;
    }

    public EncodeBuffer Copy()
    {
        return new EncodeBuffer
        {
            _buffer = _buffer,
            _bufferLength = _bufferLength,
            _ownsBuffer = false,
            _maxOffset = _maxOffset,
            _minLimit = _minLimit,
            _offset = _offset,
            _serializeCounter = _serializeCounter,
            _result = _result,
            _expandable = _expandable
        };
    }

    public byte[] ToArray()
    {
        byte[] ret = new byte[_offset];
        Array.Copy(_buffer, 0, ret, 0, ret.Length);
        return ret;
    }

    public void Reset(int newOffset)
    {
        _offset = newOffset;
        _serializeCounter = 0;
        _result = EncodeResult.None;
    }

    public override string ToString() => _offset + " - " + _serializeCounter;

    public int GetLength() => Math.Min(_offset, _maxOffset);

    /// <summary>
    /// Releases the resources used by the EncodeBuffer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if called from Dispose, false if called from a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_ownsBuffer && _buffer != null)
            {
                BytePool.Return(_buffer);
                _buffer = null!;
            }
        }
    }
}