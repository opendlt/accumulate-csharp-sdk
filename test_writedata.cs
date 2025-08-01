using System;
using System.Text;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Commons.Codec.Binary;

class TestWriteData
{
    static void Main()
    {
        // Create a WriteData transaction body
        var writeData = new WriteData();
        writeData.WithData(new byte[][] {
            Encoding.UTF8.GetBytes("foo"),
            Encoding.UTF8.GetBytes("bar"),
            Encoding.UTF8.GetBytes("baz")
        }.SelectMany(x => x).ToArray());

        // Marshal it
        var bodyBytes = writeData.MarshalBinary();
        Console.WriteLine($"WriteData marshaled bytes ({bodyBytes.Length} bytes):");
        Console.WriteLine(new string(Hex.EncodeHex(bodyBytes)));
        
        // Analyze the structure
        Console.WriteLine("\nByte breakdown:");
        for (int i = 0; i < Math.Min(bodyBytes.Length, 30); i++)
        {
            Console.WriteLine($"  [{i:D2}] 0x{bodyBytes[i]:X2} = {bodyBytes[i]} {(char.IsControl((char)bodyBytes[i]) ? "" : $"'{(char)bodyBytes[i]}'")}");
        }
        
        // Check if it starts with type field
        if (bodyBytes.Length > 2 && bodyBytes[0] == 0x01 && bodyBytes[1] == 0x05)
        {
            Console.WriteLine("\nType field found: field 1, value 5 (WriteData)");
        }
        else
        {
            Console.WriteLine("\nNo type field found at start of marshaled data");
        }
    }
}