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
    /// Defines common encoding methods for byte array encoders.
    /// Extends the base <see cref="IEncoder"/> interface.
    /// </summary>
    public interface IBinaryEncoder : IEncoder
    {
        /// <summary>
        /// Encodes a byte array and returns the encoded data as a byte array.
        /// </summary>
        /// <param name="source">Data to be encoded.</param>
        /// <returns>A byte array containing the encoded data.</returns>
        /// <exception cref="EncoderException">
        /// Thrown if the Encoder encounters a failure condition during the encoding process.
        /// </exception>
        byte[] Encode(byte[] source);
    }
} 