using System.Security.Cryptography;
using System.Text;

public static class HashHelper
{
    public static string GetHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}