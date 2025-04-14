# Acme.Net SDK

.NET SDK for interacting with the Accumulate blockchain protocol.

## Prerequisites

*   [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or higher
*   [Git](https://git-scm.com/)

## Getting Started

### Cloning the Repository

To get the SDK source code and the necessary test vector data (from the `test-data` submodule), clone the repository recursively. This is the recommended method:

```bash
git clone --recursive https://gitlab.com/accumulatenetwork/sdk/acme.net.git
cd acme.net
```

#### Adding Submodules Manually (If Not Cloned Recursively)

If you have already cloned the repository *without* the `--recursive` flag, or if the `test/vectors` directory is missing, you first need to **add** the submodule definition to your repository:

1.  **Add the Submodule**: 
    ```bash
    # Run from the repository root
    git submodule add https://gitlab.com/accumulatenetwork/sdk/test-data.git test/vectors
    ```
    *(This step is only needed once to register the submodule. If the command fails saying the path already exists, the submodule is likely already registered, and you can proceed to the next step.)*

2.  **Initialize and Update**: Download the submodule content:
    ```bash
    # Run from the repository root
    git submodule update --init --recursive
    ```

3.  **Commit Changes**: If you ran `git submodule add`, commit the changes:
    ```bash
    git commit -m "Add/update test-data submodule"
    ```

### Building the SDK

Navigate to the source directory and use the .NET CLI to build the solution:

```bash
cd src
dotnet build
```

This will compile the SDK library (`Acme.Net.Sdk`).

## Running Tests

### Standard Tests

To run the standard unit and integration tests for the SDK:

```bash
# Navigate back to the root if you are in src/
# cd .. 
dotnet test
```

This command will automatically discover and run tests in the `test/Acme.Net.Sdk.Tests` project.

### Test Vector Verification

The SDK includes tests that verify transaction hashing against a set of predefined test vectors located in the `test/vectors` submodule (sourced from [https://gitlab.com/accumulatenetwork/sdk/test-data.git](https://gitlab.com/accumulatenetwork/sdk/test-data.git)). These vectors ensure compatibility with the reference implementation of the Accumulate protocol.

To run these specific tests:

1.  **Ensure Submodules are Initialized**: Make sure you have cloned recursively or followed the steps in "Adding Submodules Manually" above to ensure the `test/vectors` directory is populated.

2.  **Run Vector Tests**: You can run *only* the tests that use these vectors using a filter:
    ```bash
    # From the repository root
    dotnet test --filter FullyQualifiedName~TransactionHasherVectorTests
    ```
    This command targets the tests within the `TransactionHasherVectorTests` class, which load `vectors.json` and compare generated hashes against the expected values.

## Using the SDK

### Basic Usage: Sending Tokens

Here's a basic example demonstrating how to build and sign a "Send Tokens" transaction using a Lite Identity and submit it to the network.

```csharp
using System;
using System.Numerics;
using System.Threading.Tasks;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing; // For SignatureKeyPair, AccKeyPairGenerator

public class Example
{
    public static async Task Main(string[] args)
    {
        // 1. Create an AcmeClient (connect to Testnet)
        var client = new AcmeClient("https://testnet.accumulatenetwork.io/v2");

        // 2. Load your Principal (e.g., from a saved key)
        //    For this example, we generate a new one.
        //    In a real app, load your saved key using LiteIdentityPrincipal.ImportFromBase64(savedKeyData);
        var senderPrincipal = LiteIdentityPrincipal.Generate(SignatureType.ED25519);
        Console.WriteLine($"Using sender: {senderPrincipal.LiteIdentity.Url}");

        // Define recipient and amount
        string recipientUrl = "acc://recipient-lite-identity-url/acme"; // Replace with actual recipient
        BigInteger amountToSend = BigInteger.Parse("1000000"); // e.g., 0.01 ACME

        // 3. Build the Send Tokens transaction
        var sendTokensBuilder = client.CreateSendTokensBuilder()
            .WithOrigin(senderPrincipal.LiteIdentity.Url) // Set the origin URL
            .WithSigner(senderPrincipal)                // Set the signer (Principal)
            .WithSourceUrl(senderPrincipal.LiteIdentity.Url + "/acme") // Specify the source token account
            .WithRecipientUrl(recipientUrl)             // Specify the recipient token account
            .WithAmount(amountToSend);                  // Specify the amount

        Console.WriteLine("Transaction built.");

        // 4. Execute the signed transaction
        try
        {
            Console.WriteLine("Submitting transaction...");
            var response = await sendTokensBuilder.ExecuteSignedAsync();

            Console.WriteLine($"Transaction ID: {response.TxId}");
            if (!string.IsNullOrEmpty(response.Error))
            {
                Console.WriteLine($"Error: {response.Error}");
                // Consider inspecting response.Code and response.Message for more details
            }
            else
            {
                Console.WriteLine("Transaction submitted successfully!");
                // You would typically query the transaction status using the TxId
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
```

*Note: This example uses a newly generated key. In a real application, you would securely load your existing private key.* 

### Building an Envelope Offline

This example demonstrates building a transaction, signing it, creating the envelope, and printing it as JSON without sending it to the network. This is useful for inspection or offline signing workflows.

1.  **Navigate to the Example Directory**: 
    ```bash
    cd examples/BuildEnvelopeExample
    ```

2.  **Run the Example**: 
    ```bash
    dotnet run
    ```

    This will execute the code in `examples/BuildEnvelopeExample/Program.cs`, which:
    *   Generates a temporary key.
    *   Builds a `SendTokens` transaction.
    *   Computes the transaction hash.
    *   Signs the hash.
    *   Constructs an `Envelope` object.
    *   Serializes the envelope to JSON and prints it to the console.

## Contributing

(TODO: Add contribution guidelines)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details (if added).
