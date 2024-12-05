using System;
using System.Text;

namespace HasheoProyecto.Helpers
{
    public class CustomEncryption
    {
        private const string SecretKey = "MiClaveSecreta"; // Llave secreta fija

        private static readonly byte[] SBox = new byte[256];
        private static readonly byte[] InverseSBox = new byte[256];

        static CustomEncryption()
        {
            for (int i = 0; i < 256; i++)
            {
                SBox[i] = (byte)((i * 31 + 53) % 256);
                InverseSBox[SBox[i]] = (byte)i;
            }
        }

        // Método para encriptar la contraseña
        public static string Encrypt(string input, string userKey)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] keyBytes = Encoding.ASCII.GetBytes(userKey); // Usar la clave proporcionada
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

            // Se devuelve la contraseña encriptada junto con un hash de la clave
            string keyHash = Convert.ToBase64String(Encoding.ASCII.GetBytes(userKey)); // Cambiar para usar userKey
            return Convert.ToBase64String(inputBytes) + "|" + keyHash;
        }


        // Método para desencriptar la contraseña
        public static string Decrypt(string encryptedInput, string userKey)
        {
            Console.WriteLine("Valor encriptado antes de desencriptar: " + encryptedInput); // Verifica lo que llega

            if (string.IsNullOrWhiteSpace(encryptedInput))
                throw new Exception("Entrada encriptada vacía.");


            string[] parts = encryptedInput.Split('|');
            if (parts.Length != 2)
                throw new Exception("Formato de texto encriptado no válido.");

            string encodedData = parts[0];
            string storedKeyHash = parts[1];

            // Comparar el hash de la clave almacenado con el proporcionado
            string providedKeyHash = Convert.ToBase64String(Encoding.ASCII.GetBytes(userKey));

            if (storedKeyHash != providedKeyHash)
                throw new Exception("Llave incorrecta. No se puede desencriptar el texto.");

            byte[] inputBytes = Convert.FromBase64String(encodedData);
            byte[] keyBytes = Encoding.ASCII.GetBytes(userKey); // Usar la clave proporcionada por el usuario
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

        // Método para validar la contraseña del usuario (para login)
        public static bool ValidateUserPassword(string enteredPassword, string storedEncryptedPassword, string userKey)
        {
            try
            {
                // Desencriptar la contraseña almacenada en la base de datos
                string decryptedPassword = Decrypt(storedEncryptedPassword, userKey);

                // Comparar la contraseña ingresada con la desencriptada
                return enteredPassword == decryptedPassword;
            }
            catch (Exception ex)
            {
                // Si ocurre un error durante el desencriptado, significa que la contraseña no es correcta
                Console.WriteLine("Error al intentar desencriptar la contraseña: " + ex.Message);
                return false;
            }
        }

        // Métodos de soporte para la sustitución SBox y el manejo de rotaciones
        private static byte SBoxSubstitution(byte input) => SBox[input];
        private static byte InverseSBoxSubstitution(byte input) => InverseSBox[input];

        private static byte RotateLeft(byte value, int shift) =>
            (byte)((value << shift) | (value >> (8 - shift)));

        private static byte RotateRight(byte value, int shift) =>
            (byte)((value >> shift) | (value << (8 - shift)));

        // Método para expandir la clave y ajustarla a la longitud de la entrada
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
