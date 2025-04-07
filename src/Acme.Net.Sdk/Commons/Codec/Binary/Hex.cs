/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;

namespace Acme.Net.Sdk.Commons.Codec.Binary
{
    /// <summary>
    /// Converts hexadecimal Strings. The Encoding used for certain operations can be set,
    /// the default is UTF-8.
    /// <para>
    /// This class is thread-safe.
    /// </para>
    /// </summary>
    public class Hex : IBinaryEncoder, IBinaryDecoder
    {
        /// <summary>
        /// Default encoding is <see cref="System.Text.Encoding.UTF8"/>.
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Default encoding name is <see cref="CharEncoding.Utf8"/>.
        /// </summary>
        public static readonly string DefaultEncodingName = CharEncoding.Utf8;

        /// <summary>
        /// Used to build output as hex.
        /// </summary>
        private static readonly char[] DigitsLower = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        /// <summary>
        /// Used to build output as hex.
        /// </summary>
        private static readonly char[] DigitsUpper = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        /// <summary>
        /// Converts an array of characters representing hexadecimal values into an array of bytes of those same values. The
        /// returned array will be half the length of the passed array, as it takes two characters to represent any given
        /// byte. An exception is thrown if the passed char array has an odd number of elements.
        /// </summary>
        /// <param name="data">An array of characters containing hexadecimal digits.</param>
        /// <returns>A byte array containing binary data decoded from the supplied char array.</returns>
        /// <exception cref="DecoderException">Thrown if an odd number of characters or illegal characters are supplied.</exception>
        public static byte[] DecodeHex(char[] data)
        {
            byte[] @out = new byte[data.Length >> 1];
            DecodeHex(data, @out, 0);
            return @out;
        }

        /// <summary>
        /// Converts an array of characters representing hexadecimal values into an array of bytes of those same values. The
        /// returned array will be half the length of the passed array, as it takes two characters to represent any given
        /// byte. An exception is thrown if the passed char array has an odd number of elements.
        /// </summary>
        /// <param name="data">An array of characters containing hexadecimal digits.</param>
        /// <param name="out">A byte array to contain the binary data decoded from the supplied char array.</param>
        /// <param name="outOffset">The position within <paramref name="out"/> to start writing the decoded bytes.</param>
        /// <returns>The number of bytes written to <paramref name="out"/>.</returns>
        /// <exception cref="DecoderException">Thrown if an odd number of characters or illegal characters are supplied.</exception>
        public static int DecodeHex(char[] data, byte[] @out, int outOffset)
        {
            int len = data.Length;

            if ((len & 0x01) != 0)
            {
                throw new DecoderException("Odd number of characters.");
            }

            int outLen = len >> 1;
            if (@out.Length - outOffset < outLen)
            {
                throw new DecoderException("Output array is not large enough to accommodate decoded data.");
            }

            // two characters form the hex value.
            for (int i = outOffset, j = 0; j < len; i++)
            {
                int f = ToDigit(data[j], j) << 4;
                j++;
                f |= ToDigit(data[j], j);
                j++;
                @out[i] = (byte)(f & 0xFF);
            }

            return outLen;
        }

        /// <summary>
        /// Converts a string representing hexadecimal values into an array of bytes of those same values. The returned array
        /// will be half the length of the passed string, as it takes two characters to represent any given byte. An
        /// exception is thrown if the passed string has an odd number of elements.
        /// </summary>
        /// <param name="data">A string containing hexadecimal digits.</param>
        /// <returns>A byte array containing binary data decoded from the supplied string.</returns>
        /// <exception cref="DecoderException">Thrown if an odd number of characters or illegal characters are supplied.</exception>
        public static byte[] DecodeHex(string data)
        {
            return DecodeHex(data.ToCharArray());
        }

        /// <summary>
        /// Converts an array of bytes into an array of characters representing the hexadecimal values of each byte in order.
        /// The returned array will be double the length of the passed array, as it takes two characters to represent any
        /// given byte.
        /// </summary>
        /// <param name="data">A byte[] to convert to hex characters.</param>
        /// <returns>A char[] containing lower-case hexadecimal characters.</returns>
        public static char[] EncodeHex(byte[] data)
        {
            return EncodeHex(data, true);
        }

        /// <summary>
        /// Converts an array of bytes into an array of characters representing the hexadecimal values of each byte in order.
        /// The returned array will be double the length of the passed array, as it takes two characters to represent any
        /// given byte.
        /// </summary>
        /// <param name="data">A byte[] to convert to Hex characters.</param>
        /// <param name="toLowerCase"><c>true</c> converts to lowercase, <c>false</c> to uppercase.</param>
        /// <returns>A char[] containing hexadecimal characters in the selected case.</returns>
        public static char[] EncodeHex(byte[] data, bool toLowerCase)
        {
            return EncodeHex(data, toLowerCase ? DigitsLower : DigitsUpper);
        }

        /// <summary>
        /// Converts an array of bytes into an array of characters representing the hexadecimal values of each byte in order.
        /// The returned array will be double the length of the passed array, as it takes two characters to represent any
        /// given byte.
        /// </summary>
        /// <param name="data">A byte[] to convert to hex characters.</param>
        /// <param name="toDigits">The output alphabet (must contain at least 16 chars).</param>
        /// <returns>A char[] containing the appropriate characters from the alphabet. For best results, this should be either upper- or lower-case hex.</returns>
        protected static char[] EncodeHex(byte[] data, char[] toDigits)
        {
            int dataLength = data.Length;
            char[] @out = new char[dataLength << 1];
            EncodeHex(data, 0, dataLength, toDigits, @out, 0);
            return @out;
        }


        /// <summary>
        /// Converts an array of bytes into an array of characters representing the hexadecimal values of each byte in order.
        /// </summary>
        /// <param name="data">A byte[] to convert to hex characters.</param>
        /// <param name="dataOffset">The position in <paramref name="data"/> to start encoding from.</param>
        /// <param name="dataLen">The number of bytes from <paramref name="dataOffset"/> to encode.</param>
        /// <param name="toLowerCase"><c>true</c> converts to lowercase, <c>false</c> to uppercase.</param>
        /// <returns>A char[] containing the appropriate characters. For best results, this should be either upper- or lower-case hex.</returns>
        public static char[] EncodeHex(byte[] data, int dataOffset, int dataLen, bool toLowerCase)
        {
            char[] @out = new char[dataLen << 1];
            EncodeHex(data, dataOffset, dataLen, toLowerCase ? DigitsLower : DigitsUpper, @out, 0);
            return @out;
        }


        /// <summary>
        /// Converts an array of bytes into an array of characters representing the hexadecimal values of each byte in order.
        /// </summary>
        /// <param name="data">A byte[] to convert to hex characters.</param>
        /// <param name="dataOffset">The position in <paramref name="data"/> to start encoding from.</param>
        /// <param name="dataLen">The number of bytes from <paramref name="dataOffset"/> to encode.</param>
        /// <param name="toLowerCase"><c>true</c> converts to lowercase, <c>false</c> to uppercase.</param>
        /// <param name="out">A char[] which will hold the resultant appropriate characters from the alphabet.</param>
        /// <param name="outOffset">The position within <paramref name="out"/> at which to start writing the encoded characters.</param>
        public static void EncodeHex(byte[] data, int dataOffset, int dataLen, bool toLowerCase, char[] @out, int outOffset)
        {
            EncodeHex(data, dataOffset, dataLen, toLowerCase ? DigitsLower : DigitsUpper, @out, outOffset);
        }

        /// <summary>
        /// Converts an array of bytes into an array of characters representing the hexadecimal values of each byte in order.
        /// </summary>
        /// <param name="data">A byte[] to convert to hex characters.</param>
        /// <param name="dataOffset">The position in <paramref name="data"/> to start encoding from.</param>
        /// <param name="dataLen">The number of bytes from <paramref name="dataOffset"/> to encode.</param>
        /// <param name="toDigits">The output alphabet (must contain at least 16 chars).</param>
        /// <param name="out">A char[] which will hold the resultant appropriate characters from the alphabet.</param>
        /// <param name="outOffset">The position within <paramref name="out"/> at which to start writing the encoded characters.</param>
        private static void EncodeHex(byte[] data, int dataOffset, int dataLen, char[] toDigits, char[] @out, int outOffset)
        {
            // two characters form the hex value.
            for (int i = dataOffset, j = outOffset; i < dataOffset + dataLen; i++)
            {
                @out[j++] = toDigits[(0xF0 & data[i]) >> 4]; // Corrected: Use >> (logical shift is implicit for non-negative int)
                @out[j++] = toDigits[0x0F & data[i]];
            }
        }

        /// <summary>
        /// Converts an array of bytes into a string representing the hexadecimal values of each byte in order. The returned
        /// string will be double the length of the passed array, as it takes two characters to represent any given byte.
        /// Uses lowercase letters for hex characters.
        /// </summary>
        /// <param name="data">A byte[] to convert to hex characters.</param>
        /// <returns>A string containing lower-case hexadecimal characters.</returns>
        public static string EncodeHexString(byte[] data)
        {
            return new string(EncodeHex(data));
        }

        /// <summary>
        /// Converts an array of bytes into a string representing the hexadecimal values of each byte in order. The returned
        /// string will be double the length of the passed array, as it takes two characters to represent any given byte.
        /// </summary>
        /// <param name="data">A byte[] to convert to hex characters.</param>
        /// <param name="toLowerCase"><c>true</c> converts to lowercase, <c>false</c> to uppercase.</param>
        /// <returns>A string containing hexadecimal characters in the selected case.</returns>
        public static string EncodeHexString(byte[] data, bool toLowerCase)
        {
            return new string(EncodeHex(data, toLowerCase));
        }

        /// <summary>
        /// Converts a hexadecimal character to an integer.
        /// </summary>
        /// <param name="ch">A character to convert to an integer digit.</param>
        /// <param name="index">The index of the character in the source.</param>
        /// <returns>An integer representing the hexadecimal value of the character.</returns>
        /// <exception cref="DecoderException">Thrown if the character is not a hexadecimal digit.</exception>
        protected static int ToDigit(char ch, int index)
        {
            int digit = -1;
            if (ch >= '0' && ch <= '9')
            {
                digit = ch - '0';
            }
            else if (ch >= 'a' && ch <= 'f')
            {
                digit = ch - 'a' + 10;
            }
            else if (ch >= 'A' && ch <= 'F')
            {
                digit = ch - 'A' + 10;
            }

            if (digit == -1)
            {
                throw new DecoderException($"Illegal hexadecimal character {ch} at index {index}");
            }
            return digit;
        }

        // --- Instance Members ---

        private readonly Encoding _encoding;

        /// <summary>
        /// Creates a new codec with the default encoding <see cref="DefaultEncoding"/> (UTF-8).
        /// </summary>
        public Hex()
        {
            this._encoding = DefaultEncoding;
        }

        /// <summary>
        /// Creates a new codec with the given <see cref="Encoding"/>.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        public Hex(Encoding encoding)
        {
            this._encoding = encoding;
        }

        /// <summary>
        /// Creates a new codec with the given encoding name.
        /// </summary>
        /// <param name="encodingName">The name of the encoding.</param>
        /// <exception cref="ArgumentException">If the named encoding is unavailable.</exception>
        public Hex(string encodingName)
        {
            try
            {
                this._encoding = Encoding.GetEncoding(encodingName);
            }
            catch (ArgumentException e)
            {
                // Wrap the ArgumentException (which indicates unsupported encoding) if needed,
                // or rethrow. Here we rethrow to match common .NET practices.
                throw new NotSupportedException($"Encoding not supported: {encodingName}", e);
            }
        }

        /// <summary>
        /// Gets the encoding used by this instance.
        /// </summary>
        /// <returns>The encoding.</returns>
        public Encoding GetEncoding()
        {
            return this._encoding;
        }

        /// <summary>
        /// Gets the name of the encoding used by this instance.
        /// </summary>
        /// <returns>The encoding name.</returns>
        public string GetEncodingName()
        {
            return this._encoding.WebName; // Use WebName for consistency with Java Charset.name()
        }

        /// <summary>
        /// Converts an array of character bytes representing hexadecimal values into an array of bytes of those same values.
        /// The returned array will be half the length of the passed array, as it takes two characters to represent any given
        /// byte. An exception is thrown if the passed char array has an odd number of elements.
        /// The characters are decoded using the encoding specified in the constructor.
        /// </summary>
        /// <param name="array">An array of character bytes containing hexadecimal digits.</param>
        /// <returns>A byte array containing binary data decoded from the supplied byte array (representing characters).</returns>
        /// <exception cref="DecoderException">Thrown if an odd number of characters or illegal characters are supplied, or an error occurs during character decoding.</exception>
        public byte[] Decode(byte[] array)
        {
            try
            {
                return DecodeHex(this._encoding.GetString(array).ToCharArray());
            }
            catch (DecoderFallbackException e)
            {
                 throw new DecoderException("Character decoding failed.", e);
            }
            catch (ArgumentNullException e) // Handle potential null from GetString or ToCharArray
            {
                 throw new DecoderException("Input array resulted in null character array.", e);
            }
        }

        /// <summary>
        /// Converts a string, byte array, or char array representing hexadecimal values into an array of bytes of those
        /// same values. The returned array will be half the length of the passed String or array, as it takes two characters
        /// to represent any given byte. An exception is thrown if the passed input has an odd number of elements.
        /// </summary>
        /// <param name="source">A string, byte[], or char[] containing hexadecimal digits.</param>
        /// <returns>A byte array containing binary data decoded from the supplied source.</returns>
        /// <exception cref="DecoderException">Thrown if an odd number of characters is supplied, illegal characters are found, the object type is unsupported, or character decoding fails.</exception>
        public object Decode(object source)
        {
            if (source == null)
            {
                throw new DecoderException("Parameter 'source' cannot be null.");
            }

            if (source is string str) // Preferred C# pattern matching
            {
                return DecodeHex(str.ToCharArray());
            }
            if (source is byte[] bytes) // byte[] represents hex chars encoded with instance encoding
            {
                return Decode(bytes);
            }
            if (source is char[] chars)
            {
                 return DecodeHex(chars);
            }
            // Omit ByteBuffer handling as it's not directly equivalent/used

            throw new DecoderException("Parameter supplied to Decode is not a string, byte[], or char[]");
        }

        /// <summary>
        /// Converts an array of bytes into an array of bytes for the characters representing the hexadecimal values of each
        /// byte in order. The returned array will be double the length of the passed array, as it takes two characters to
        /// represent any given byte.
        /// The conversion from hexadecimal characters to the returned bytes is performed with the encoding specified in the constructor.
        /// Uses lowercase hex characters.
        /// </summary>
        /// <param name="array">A byte[] to convert to hex characters.</param>
        /// <returns>A byte[] containing the bytes of the lower-case hexadecimal characters according to the instance's encoding.</returns>
        /// <exception cref="EncoderException">Thrown if character encoding fails.</exception>
        public byte[] Encode(byte[] array)
        {
            if (array == null)
            {
                // Decide how to handle null input. Throwing an exception is usually best.
                 throw new EncoderException("Input byte array cannot be null.");
                // Alternatively, could return Array.Empty<byte>();
            }
            try
            {
                // Instance encode defaults to lower case hex string, then gets bytes using instance encoding
                return this._encoding.GetBytes(EncodeHexString(array, true));
            }
            catch (EncoderFallbackException e)
            {
                 throw new EncoderException("Character encoding failed.", e);
            }
            catch (ArgumentNullException e)
            {
                 throw new EncoderException("Input array resulted in null hex string.", e);
            }
        }

        /// <summary>
        /// Converts a string or byte array into an array of characters representing the hexadecimal values of each
        /// byte in order. The returned value depends on the input type:
        /// - If input is byte[], returns char[] containing hex characters.
        /// - If input is string, converts the string to bytes using the instance's encoding, then returns char[] containing hex characters.
        /// The returned array will be double the length of the effective byte array.
        /// Uses lowercase hex characters.
        /// </summary>
        /// <param name="source">A string or byte[] to convert to hex characters.</param>
        /// <returns>A char[] containing lower-case hexadecimal characters.</returns>
        /// <exception cref="EncoderException">Thrown if the given object is not a string or byte[], or if character encoding fails.</exception>
        public object Encode(object source)
        {
            // Removed null check here, handle specific types first

            byte[]? byteArray = null; // Initialize to null
            if (source is string str) // Preferred C# pattern matching
            {
                 if (str == null) {
                     // Explicitly handle null string case if necessary.
                     // Currently, null string leads to null byteArray below.
                 }
                 else // Only call GetBytes if str is not null
                 {
                     try
                     {
                         byteArray = this._encoding.GetBytes(str);
                     }
                     catch(EncoderFallbackException e)
                     {
                         throw new EncoderException("String encoding failed.", e);
                     }
                     // ArgumentNullException for null string is avoided by the 'else' block
                 }
            }
            else if (source is byte[] bytes)
            {
                byteArray = bytes;
            }
             // Omit ByteBuffer handling
            else if (source != null) // Only throw if it's non-null and not string/byte[]
            {
                throw new EncoderException($"Parameter supplied to Encode is not a string or byte[]. Type was: {source.GetType().FullName}");
            }
            // else source is null, let byteArray remain null

            // Check if byteArray is null AFTER type checks and conversions
            if (byteArray == null)
            {
                // If byteArray is still null, it means the input was null or an unsupported type that wasn't explicitly caught.
                // Throw EncoderException as the input object could not be processed into bytes for encoding.
                throw new EncoderException("Input object cannot be encoded. Must be non-null string or byte[].");
            }

            return EncodeHex(byteArray, true); // Return char[] consistent with Java Object version
        }

        /// <summary>
        /// Returns a string representation of the object, which includes the encoding name.
        /// </summary>
        /// <returns>A string representation of the object.</returns>
        public override string ToString()
        {
            return base.ToString() + "[encoding=" + GetEncodingName() + "]";
        }

        // --- End of Hex class ---
    }
} 