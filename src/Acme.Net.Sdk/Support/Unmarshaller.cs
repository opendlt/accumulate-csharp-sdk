using System;
using System.Collections.Generic;
using System.IO;

namespace Acme.Net.Sdk.Support
{
    /// <summary>
    /// Provides functionality to unmarshal binary data.
    /// </summary>
    public class Unmarshaller
    {
        private readonly byte[] _data;
        private int _position;

        /// <summary>
        /// Initializes a new instance of the Unmarshaller class.
        /// </summary>
        /// <param name="data">The binary data to unmarshal.</param>
        public Unmarshaller(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _position = 0;
        }

        /// <summary>
        /// Tries to read a field with the specified field number.
        /// </summary>
        /// <param name="fieldNumber">The field number to look for.</param>
        /// <param name="value">The value if found.</param>
        /// <returns>True if the field was found, false otherwise.</returns>
        public bool TryReadField(int fieldNumber, out object? value)
        {
            value = null;
            _position = 0;

            while (_position < _data.Length)
            {
                if (!TryReadVarint(out var key))
                    break;

                var field = (int)(key >> 3);
                var wireType = (int)(key & 0x7);

                if (field == fieldNumber)
                {
                    return TryReadValue(wireType, out value);
                }
                else
                {
                    // Skip this field
                    if (!SkipField(wireType))
                        break;
                }
            }

            return false;
        }

        private bool TryReadVarint(out ulong value)
        {
            value = 0;
            var shift = 0;

            while (_position < _data.Length)
            {
                var b = _data[_position++];
                value |= (ulong)(b & 0x7F) << shift;
                
                if ((b & 0x80) == 0)
                    return true;

                shift += 7;
                if (shift >= 64)
                    return false;
            }

            return false;
        }

        private bool TryReadValue(int wireType, out object? value)
        {
            value = null;

            switch (wireType)
            {
                case 0: // Varint
                    if (TryReadVarint(out var varintValue))
                    {
                        value = varintValue;
                        return true;
                    }
                    break;

                case 2: // Length-delimited
                    if (TryReadVarint(out var length))
                    {
                        if (_position + (int)length <= _data.Length)
                        {
                            var bytes = new byte[length];
                            Array.Copy(_data, _position, bytes, 0, (int)length);
                            _position += (int)length;
                            value = bytes;
                            return true;
                        }
                    }
                    break;
            }

            return false;
        }

        private bool SkipField(int wireType)
        {
            switch (wireType)
            {
                case 0: // Varint
                    return TryReadVarint(out _);

                case 2: // Length-delimited
                    if (TryReadVarint(out var length))
                    {
                        if (_position + (int)length <= _data.Length)
                        {
                            _position += (int)length;
                            return true;
                        }
                    }
                    break;

                case 1: // 64-bit
                    if (_position + 8 <= _data.Length)
                    {
                        _position += 8;
                        return true;
                    }
                    break;

                case 5: // 32-bit
                    if (_position + 4 <= _data.Length)
                    {
                        _position += 4;
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}