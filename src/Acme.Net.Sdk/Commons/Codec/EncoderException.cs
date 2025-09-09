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
// using Acme.Net.Sdk.Commons.Codec.Binary; // Placeholder if BinaryEncoder is needed later

namespace Acme.Net.Sdk.Commons.Codec
{
    /// <summary>
    /// Thrown when there is a failure condition during the encoding process.
    /// This exception is thrown when an encoder encounters an encoding specific issue
    /// such as invalid data or inability to calculate a checksum.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.commons.codec.EncoderException.
    /// </summary>
    public class EncoderException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncoderException"/> class.
        /// </summary>
        public EncoderException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncoderException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EncoderException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncoderException"/> class
        /// with a specified error message and a reference to the inner exception that is
        /// the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception,
        /// or a null reference if no inner exception is specified.</param>
        public EncoderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

         /// <summary>
        /// Initializes a new instance of the <see cref="EncoderException"/> class
        /// with a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception,
        /// or a null reference if no inner exception is specified.</param>
        public EncoderException(Exception innerException)
             : base(innerException?.Message, innerException) // Pass message from inner exception if available
        {
        }
    }
} 