using System.Text;

namespace HasheoProyecto.Helpers
{
    public class CustomEncryptionStrings
    {
        private static readonly byte[] SBox = new byte[256];
        private static readonly byte[] InverseSBox = new byte[256];

        static CustomEncryptionStrings()
        {
            for (int i = 0; i < 256; i++)
            {
                SBox[i] = (byte)((i * 31 + 53) % 256);
                InverseSBox[SBox[i]] = (byte)i;
            }
        }

        public static string Encrypt(string input, string key)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);
            byte[] expandedKey = ExpandKey(keyBytes, inputBytes.Length);

            for (int round = 0; round < 10; round++)
            {
                for (int i = 0; i < inputBytes.Length; i++)
                {
                    inputBytes[i] = SBoxSubstitution(inputBytes[i]);
                    inputBytes[i] ^= expandedKey[i % expandedKey.Length];
                    inputBytes[i] = RotateLeft(inputBytes[i], 3);
                    inputBytes[i] = (byte)((inputBytes[i] + round * 7) % 256);
                }
            }

            // Generar una huella única de la llave y agregarla al texto encriptado
            string keyHash = Convert.ToBase64String(Encoding.ASCII.GetBytes(key));
            return Convert.ToBase64String(inputBytes) + "|" + keyHash;
        }

        public static string Decrypt(string encryptedInput, string key)
        {
            // Dividir el texto encriptado de la huella de la llave
            string[] parts = encryptedInput.Split('|');
            if (parts.Length != 2)
                throw new Exception("Formato de texto encriptado no válido.");

            string encodedData = parts[0];
            string storedKeyHash = parts[1];

            // Validar que la llave proporcionada coincida con la usada para encriptar
            string providedKeyHash = Convert.ToBase64String(Encoding.ASCII.GetBytes(key));
            if (storedKeyHash != providedKeyHash)
            {
                throw new Exception("Llave incorrecta. No se puede desencriptar el texto.");
            }

            byte[] inputBytes = Convert.FromBase64String(encodedData);
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);
            byte[] expandedKey = ExpandKey(keyBytes, inputBytes.Length);

            for (int round = 9; round >= 0; round--)
            {
                for (int i = 0; i < inputBytes.Length; i++)
                {
                    inputBytes[i] = (byte)((inputBytes[i] - round * 7 + 256) % 256);
                    inputBytes[i] = RotateRight(inputBytes[i], 3);
                    inputBytes[i] ^= expandedKey[i % expandedKey.Length];
                    inputBytes[i] = InverseSBoxSubstitution(inputBytes[i]);
                }
            }

            return Encoding.ASCII.GetString(inputBytes);
        }

        private static byte SBoxSubstitution(byte input)
        {
            return SBox[input];
        }

        private static byte InverseSBoxSubstitution(byte input)
        {
            return InverseSBox[input];
        }

        private static byte RotateLeft(byte value, int shift)
        {
            return (byte)((value << shift) | (value >> (8 - shift)));
        }

        private static byte RotateRight(byte value, int shift)
        {
            return (byte)((value >> shift) | (value << (8 - shift)));
        }

        private static byte[] ExpandKey(byte[] key, int length)
        {
            byte[] expandedKey = new byte[length];
            for (int i = 0; i < length; i++)
            {
                expandedKey[i] = key[i % key.Length];
            }
            return expandedKey;
        }
    }
}