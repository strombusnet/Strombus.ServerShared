using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Strombus.ServerShared
{
    public class RandomHelper
    {
        public static byte[] GenerateSalt(int length)
        {
            byte[] result = new byte[length];
            using (RandomNumberGenerator crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(result);
            }
            return result;
        }

        public static char[] CreateRandomCharacterSequence_Readable6bit_ForIdentifiers(int length)
        {
            byte[] cryptoBytes;

            // for entropy bias reasons, we MUST use 64 values, an even divisor of 256.
            char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_".ToCharArray();

            cryptoBytes = new byte[length];
            using (RandomNumberGenerator crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(cryptoBytes);
            }

            // now convert the bytes to a character array; we are losing 2 bits off each byte but the entropy should remain the same
            char[] cryptoChars = new char[cryptoBytes.Length];
            for (int i = 0; i < length; i++)
            {
                cryptoChars[i] = chars[cryptoBytes[i] % 64];
            }

            return cryptoChars;
        }
    }
}
