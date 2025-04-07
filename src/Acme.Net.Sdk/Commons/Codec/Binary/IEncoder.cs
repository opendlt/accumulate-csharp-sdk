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
    /// Provides the highest level of abstraction for Encoders.
    /// <para>
    /// This is the sister interface of <see cref="IDecoder"/>. Every implementation of Encoder provides this
    /// common generic interface which allows a user to pass a generic object to any Encoder implementation
    /// in the codec package.
    /// </para>
    /// </summary>
    public interface IEncoder
    {
        /// <summary>
        /// Encodes an <see cref="object"/> and returns the encoded content as an <see cref="object"/>.
        /// The objects here may just be <c>byte[]</c> or <c>string</c>s depending on the implementation used.
        /// </summary>
        /// <param name="source">An object to encode.</param>
        /// <returns>An "encoded" object.</returns>
        /// <exception cref="EncoderException">
        /// Thrown if the encoder experiences a failure condition during the encoding process.
        /// </exception>
        object Encode(object source);
    }
} 