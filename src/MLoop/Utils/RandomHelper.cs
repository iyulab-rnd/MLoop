namespace MLoop.Utils
{
    public static class RandomHelper
    {
        private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const string digit = "0123456789";
        private const string chars = alphabet + digit;

        private static readonly Random random = new();

        public static string RandomString(int length, string? chars = null)
        {
            chars ??= RandomHelper.chars;
            var stringChars = new char[length];

            stringChars[0] = alphabet[random.Next(alphabet.Length)];
            for (int i = 1; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new string(stringChars);
        }
    }
}
