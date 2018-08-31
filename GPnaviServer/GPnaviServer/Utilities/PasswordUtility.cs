using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GPnaviServer.Utilities
{
    /// <summary>
    /// パスワードユーティリティ
    /// </summary>
    public class PasswordUtility
    {
        /// <summary>
        /// ハッシュを得る
        /// </summary>
        /// <param name="password">入力されたパスワード</param>
        /// <returns>ハッシュ化されたパスワード</returns>
        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            byte[] tmpByte;
            SHA256 sha256 = new SHA256Managed();
            tmpByte = sha256.ComputeHash(GetKeyByteArray(password));

            StringBuilder rst = new StringBuilder();
            for (int i = 0; i < tmpByte.Length; i++)
            {
                rst.Append(tmpByte[i].ToString("x2"));
            }
            sha256.Clear();
            return rst.ToString();
        }
        private static byte[] GetKeyByteArray(string strKey)
        {
            UTF8Encoding Asc = new UTF8Encoding();
            int tmpStrLen = strKey.Length;
            byte[] tmpByte = new byte[tmpStrLen - 1];
            tmpByte = Asc.GetBytes(strKey);
            return tmpByte;
        }

    }
}
