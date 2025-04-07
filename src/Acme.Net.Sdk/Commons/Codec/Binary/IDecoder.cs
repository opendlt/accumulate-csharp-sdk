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
    /// Provides the highest level of abstraction for Decoders.
    /// <para>
    /// This is the sister interface of <see cref="IEncoder"/>. All Decoders implement this common generic interface.
    /// Allows a user to pass a generic object to any Decoder implementation in the codec package.
    /// </para>
    /// <para>
    /// One of the two interfaces at the center of the codec package.
    /// </para>
    /// </summary>
    public interface IDecoder
    {
        /// <summary>
        /// Decodes an "encoded" object and returns a "decoded" object. Note that the implementation of this interface will
        /// try to cast the object parameter to the specific type expected by a particular Decoder implementation.
        /// </summary>
        /// <param name="source">The object to decode.</param>
        /// <returns>A "decoded" object.</returns>
        /// <exception cref="DecoderException">
        /// Thrown if a failure condition is encountered during the decode process.
        /// This can be caused by various reasons, such as the input object being null
        /// or failing a type cast required by the specific decoder implementation.
        /// </exception>
        object Decode(object source);
    }
} 