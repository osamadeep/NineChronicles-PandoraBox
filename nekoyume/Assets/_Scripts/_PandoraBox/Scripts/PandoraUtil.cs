using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;

namespace Nekoyume
{
    public class PandoraUtil
    {
        // Define the encryption key
        private const int ENCRYPTION_KEY = 38737652;

        // Encrypt a string using a simple XOR operation
        public static string SimpleEncrypt(string input)
        {
            char[] inputChars = input.ToCharArray();
            for (int i = 0; i < inputChars.Length; i++)
            {
                inputChars[i] = (char)(inputChars[i] ^ ENCRYPTION_KEY);
            }

            return new string(inputChars);
        }

        // Decrypt a string that was encrypted using the above method
        public static string SimpleDecrypt(string input)
        {
            char[] inputChars = input.ToCharArray();
            for (int i = 0; i < inputChars.Length; i++)
            {
                inputChars[i] = (char)(inputChars[i] ^ ENCRYPTION_KEY);
            }

            return new string(inputChars);
        }

        public static void ShowSystemNotification(string message, NotificationCell.NotificationType type)
        {
            NotificationSystem.Push(MailType.System, $"<color=green><b>PandoraBox</b></color>: {message}", type);
        }
    }
}
