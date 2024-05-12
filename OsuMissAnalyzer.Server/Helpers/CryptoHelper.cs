using System.Globalization;
using System.Security.Cryptography;

namespace OsuMissAnalyzer.Server.Helpers;

// thanks peppy :3
public abstract class CryptoHelper
{
    private static readonly MD5 Md5Hasher = MD5.Create();
    private static readonly NumberFormatInfo Nfi = new CultureInfo("en-US", false).NumberFormat;
    
    public static string GetMd5String(byte[] data)
    {
        lock (Md5Hasher)
        {
            try
            {
                // Convert the input string to a byte array and compute the hash.
                data = Md5Hasher.ComputeHash(data);
            }
            catch (Exception)
            {
                return "fail";
            }
        }

        var str = new char[data.Length * 2];
        for (var i = 0; i < data.Length; i++)
            data[i].ToString("x2", Nfi).CopyTo(0, str, i * 2, 2);

        return new string(str);
    }
}