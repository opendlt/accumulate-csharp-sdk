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
    /// Defines common decoding methods for byte array decoders.
    /// Extends the base <see cref="IDecoder"/> interface.
    /// </summary>
    public interface IBinaryDecoder : IDecoder
    {
        /// <summary>
        /// Decodes a byte array and returns the results as a byte array.
        /// </summary>
        /// <param name="source">A byte array which has been encoded with the appropriate encoder.</param>
        /// <returns>A byte array that contains decoded content.</returns>
        /// <exception cref="DecoderException">
        /// Thrown if a Decoder encounters a failure condition during the decode process.
        /// </exception>
        byte[] Decode(byte[] source);
    }
} 