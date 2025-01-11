using Newtonsoft.Json;
using Picky;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class FileUtils
{

    public static FeederModel LoadFeederFromQRCode(string qr_code)
    {

        String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        String filename = ConvertToHashedFilename(qr_code, Constants.FEEDER_FILE_EXTENTION);
        FeederModel feeder = new FeederModel();
        try
        {
            using (StreamReader file = File.OpenText(path + "\\" + filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                feeder = ((FeederModel)serializer.Deserialize(file, typeof(FeederModel)));
            }
        }
        catch
        {
            Console.WriteLine("Can't find file: " + path + "\\" + filename);
        }
        return feeder;
    }

    /// <summary>
    /// Converts a given input string into a valid hashed filename.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <param name="extension">The desired file extension (optional).</param>
    /// <returns>A valid hashed filename with the optional extension.</returns>
    public static string ConvertToHashedFilename(string input, string extension = "")
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));

        // Generate a hash from the input string
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // Append the extension if provided
            return string.IsNullOrEmpty(extension) ? hash : $"{hash}{extension}";
        }
    }
}

