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
// using Acme.Net.Sdk.Commons.Codec.Binary; // Placeholder if BinaryDecoder is needed later

namespace Acme.Net.Sdk.Commons.Codec
{
    /// <summary>
    /// Thrown when there is a failure condition during the decoding process.
    /// This exception is thrown when a decoder encounters a decoding specific issue
    /// such as invalid data, or characters outside of the expected range.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.commons.codec.DecoderException.
    /// </summary>
    public class DecoderException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecoderException"/> class.
        /// </summary>
        public DecoderException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecoderException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DecoderException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecoderException"/> class
        /// with a specified error message and a reference to the inner exception that is
        /// the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception,
        /// or a null reference if no inner exception is specified.</param>
        public DecoderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecoderException"/> class
        /// with a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception,
        /// or a null reference if no inner exception is specified.</param>
        public DecoderException(Exception innerException)
             : base(innerException?.Message, innerException) // Pass message from inner exception if available
        {
        }
    }
} 