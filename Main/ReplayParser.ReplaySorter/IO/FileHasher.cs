using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.IO
{
    public static class FileHasher
    {
        public static string GetMd5Hash(byte[] bytes)
        {
            if (bytes == null)
                return null;

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(bytes);

                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("X2"));
                }

                return sBuilder.ToString();
            }
        }

        public static string GetMd5Hash(string filepath)
        {
            if (!System.IO.File.Exists(filepath))
                return null;

            return GetMd5Hash(System.IO.File.ReadAllBytes(filepath));
        }

        public static bool VerifyHash(string input, string hash)
        {
            if (input == null || hash == null)
                return false;

            string hashedInput = GetMd5Hash(Encoding.UTF8.GetBytes(input));

            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (comparer.Compare(hashedInput, hash) == 0)
                return true;

            return false;
        }
    }
}
