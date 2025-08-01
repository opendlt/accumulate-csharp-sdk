using System;
using System.Linq;
using System.Text;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Commons.Codec.Binary;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing WriteData marshaling...\n");
        
        // Create a WriteData transaction body
        var writeData = new WriteData();
        
        // Set data as combined bytes from foo, bar, baz
        var dataItems = new[] { "foo", "bar", "baz" };
        var combinedData = dataItems.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray();
        writeData.WithData(combinedData);

        // Marshal the body
        var bodyBytes = writeData.MarshalBinary();
        Console.WriteLine($"WriteData marshaled bytes ({bodyBytes.Length} bytes):");
        Console.WriteLine(new string(Hex.EncodeHex(bodyBytes)));
        
        // Analyze the structure
        Console.WriteLine("\nByte breakdown:");
        for (int i = 0; i < bodyBytes.Length; i++)
        {
            Console.WriteLine($"  [{i:D2}] 0x{bodyBytes[i]:X2} = {bodyBytes[i]:D3} {(char.IsControl((char)bodyBytes[i]) ? "" : $"'{(char)bodyBytes[i]}'")}");
        }
        
        // Check if it starts with type field
        if (bodyBytes.Length > 2 && bodyBytes[0] == 0x01 && bodyBytes[1] == 0x05)
        {
            Console.WriteLine("\nType field found: field 1, value 5 (WriteData)");
        }
        else
        {
            Console.WriteLine("\nNo type field found at start of marshaled data");
            Console.WriteLine("Expected: 01 05 (field 1, value 5 for WriteData type)");
        }
        
        // Now let's check what the test vector expects
        Console.WriteLine("\n\nAnalyzing test vector body portion:");
        var testVectorBodyHex = "150105021101020203666f6f02036261720203626179";
        Console.WriteLine($"Test vector body hex: {testVectorBodyHex}");
        
        // Parse it
        Console.WriteLine("Test vector body breakdown:");
        Console.WriteLine("  15 = length 21 bytes");
        Console.WriteLine("  01 05 = field 1, value 5 (WriteData type)");
        Console.WriteLine("  02 11 = field 2, length 17");
        Console.WriteLine("  ... (data content)");
    }
}