using System;
using NSec.Cryptography;
using Acme.Net.Sdk.Protocol.Generated;

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Represents a key pair for ED25519 signatures.
    /// Simplifies creation of ED25519 key pairs.
    /// </summary>
    public class Ed25519SignatureKeyPair : SignatureKeyPair
    {
        /// <summary>
        /// Creates a new ED25519 signature key pair by generating a new key.
        /// </summary>
        public Ed25519SignatureKeyPair() 
            : base(Key.Create(SignatureAlgorithm.Ed25519), SignatureType.ED25519)
        {
        }

        /// <summary>
        /// Creates a new ED25519 signature key pair from an existing key.
        /// </summary>
        /// <param name="key">The ED25519 key to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        /// <exception cref="ArgumentException">Thrown if key is not an ED25519 key.</exception>
        public Ed25519SignatureKeyPair(Key key) 
            : base(key, SignatureType.ED25519)
        {
            if (key.Algorithm != SignatureAlgorithm.Ed25519)
            {
                throw new ArgumentException("Key must be an ED25519 key", nameof(key));
            }
        }
    }
} 