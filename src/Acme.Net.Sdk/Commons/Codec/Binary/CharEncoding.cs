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

namespace Acme.Net.Sdk.Commons.Codec.Binary
{
    /// <summary>
    /// Character encoding names required of every implementation of the .NET platform.
    /// Provides constants for standard encoding names.
    /// <para>
    /// This class is immutable and thread-safe.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Based on the Java documentation for Standard charsets:
    /// <a href="http://download.oracle.com/javase/7/docs/api/java/nio/charset/Charset.html">Standard charsets</a>.
    /// .NET typically uses encoding names recognized by <see cref="System.Text.Encoding"/>.
    /// </remarks>
    public static class CharEncoding
    {
        /// <summary>
        /// ISO Latin Alphabet No. 1, a.k.a. ISO-LATIN-1.
        /// </summary>
        public const string Iso8859_1 = "ISO-8859-1"; // Note: Often represented by Encoding.GetEncoding("iso-8859-1") in .NET

        /// <summary>
        /// Seven-bit ASCII, also known as ISO646-US, also known as the Basic Latin block of the Unicode character set.
        /// </summary>
        public const string UsAscii = "US-ASCII"; // Note: Represented by System.Text.Encoding.ASCII in .NET

        /// <summary>
        /// Sixteen-bit Unicode Transformation Format, The byte order specified by a mandatory initial byte-order mark
        /// (either order accepted on input, big-endian used on output).
        /// </summary>
        public const string Utf16 = "UTF-16"; // Note: Represented by System.Text.Encoding.Unicode in .NET (usually UTF-16LE on Windows)

        /// <summary>
        /// Sixteen-bit Unicode Transformation Format, big-endian byte order.
        /// </summary>
        public const string Utf16Be = "UTF-16BE"; // Note: Represented by System.Text.Encoding.BigEndianUnicode in .NET

        /// <summary>
        /// Sixteen-bit Unicode Transformation Format, little-endian byte order.
        /// </summary>
        public const string Utf16Le = "UTF-16LE"; // Note: Represented by System.Text.Encoding.Unicode in .NET

        /// <summary>
        /// Eight-bit Unicode Transformation Format.
        /// </summary>
        public const string Utf8 = "UTF-8"; // Note: Represented by System.Text.Encoding.UTF8 in .NET
    }
} 