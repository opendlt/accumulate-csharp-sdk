using System;
using System.Collections.Generic;
using System.Linq;
using Acme.Net.Sdk.Protocol; // For Principal, Url

namespace Acme.Net.Sdk.Signing
{
    /// <summary>
    /// Helper class to prepare a list of Signer objects, potentially for multi-signature scenarios.
    /// Corresponds to the Java class io.accumulatenetwork.sdk.signing.SignersPreparer.
    /// </summary>
    public class SignersPreparer
    {
        private readonly Url _signerUrl;
        private readonly int _signerVersion;
        private readonly Acme.Net.Sdk.Signing.SignatureKeyPair _signatureKeyPair;
        private readonly List<Principal> _additionalSignerSigners = new List<Principal>();
        private readonly List<Url> _delegators = new List<Url>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SignersPreparer"/> class.
        /// </summary>
        /// <param name="signatureKeyPair">The key pair for the primary signer.</param>
        /// <param name="signerUrl">The URL of the primary signer.</param>
        /// <param name="signerVersion">The version of the primary signer.</param>
        /// <exception cref="ArgumentNullException">Thrown if signatureKeyPair or signerUrl is null.</exception>
        public SignersPreparer(Acme.Net.Sdk.Signing.SignatureKeyPair signatureKeyPair, Url signerUrl, int signerVersion)
        {
            _signatureKeyPair = signatureKeyPair ?? throw new ArgumentNullException(nameof(signatureKeyPair));
            _signerUrl = signerUrl ?? throw new ArgumentNullException(nameof(signerUrl));
            _signerVersion = signerVersion;
        }

        /// <summary>
        /// Adds delegator URLs to be included in all prepared signers.
        /// </summary>
        /// <param name="delegators">An enumerable collection of delegator URLs.</param>
        /// <returns>The current <see cref="SignersPreparer"/> instance for chaining.</returns>
        public SignersPreparer WithDelegators(IEnumerable<Url> delegators)
        {
            if (delegators != null)
            {
                 // Note: Java code used delegators.addAll(delegators), implying modifying the input list?
                 // Here we add to our internal list, which seems safer.
                _delegators.AddRange(delegators);
            }
            return this;
        }

        /// <summary>
        /// Adds multiple additional signers (represented by Principal objects).
        /// </summary>
        /// <param name="additionalSigners">An enumerable collection of additional Principals.</param>
        /// <returns>The current <see cref="SignersPreparer"/> instance for chaining.</returns>
        public SignersPreparer WithAdditionalSigners(IEnumerable<Principal> additionalSigners)
        {
            if (additionalSigners != null)
            {
                _additionalSignerSigners.AddRange(additionalSigners);
            }
            return this;
        }

        /// <summary>
        /// Adds a single additional signer (represented by a Principal object).
        /// </summary>
        /// <param name="additionalSigner">The additional Principal to add.</param>
        /// <returns>The current <see cref="SignersPreparer"/> instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if additionalSigner is null.</exception>
        public SignersPreparer WithAdditionalSigner(Principal additionalSigner)
        {
            if (additionalSigner == null) throw new ArgumentNullException(nameof(additionalSigner));
            _additionalSignerSigners.Add(additionalSigner);
            return this;
        }

        /// <summary>
        /// Prepares and returns a list of Signer objects based on the configured primary signer,
        /// additional signers, and delegators.
        /// </summary>
        /// <returns>A List of configured Signer objects.</returns>
        public List<Signer> PrepareSigners()
        {
            var signers = new List<Signer>();

            // Prepare the primary signer
            var firstSigner = new Signer()
                .WithNonceFromTimeNow() // Set timestamp only for the first signer
                .WithDelegators(_delegators)
                .WithType(_signatureKeyPair.Type)
                .WithUrl(_signerUrl)
                .WithVersion(_signerVersion)
                .WithKeyPair(_signatureKeyPair); // Use WithKeyPair instead of WithSignerPrivateKey
            
            signers.Add(firstSigner);

            // Prepare additional signers
            foreach (var principal in _additionalSignerSigners)
            {
                // Check if principal or its components are null before accessing
                if (principal?.SignatureKeyPair == null || principal.Account?.Url == null)
                {
                     // Skip or throw? Let's skip for now, maybe log a warning.
                     // Consider adding logging if this becomes an issue.
                     Console.Error.WriteLine($"Warning: Skipping additional signer due to null Principal, KeyPair, or Account URL.");
                     continue;
                }

                var additionalSigner = new Signer()
                    .WithDelegators(_delegators) // Add same delegators
                    .WithType(principal.SignatureKeyPair.Type)
                    .WithUrl(principal.Account.Url)
                    .WithVersion(principal.SignerVersion)
                    .WithKeyPair(principal.SignatureKeyPair);
                    // Do NOT set timestamp/nonce for additional signers here
                    
                signers.Add(additionalSigner);
            }

            return signers;
        }

        // --- Getters matching Java version --- 

        public Url SignerUrl => _signerUrl;
        public Acme.Net.Sdk.Signing.SignatureKeyPair SignatureKeyPair => _signatureKeyPair;
        public IReadOnlyList<Url> Delegators => _delegators.AsReadOnly();
        public IReadOnlyList<Principal> AdditionalSigners => _additionalSignerSigners.AsReadOnly();
    }
}


