using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace NetworkRemote
{
    /// <summary>
    /// Challenge-Response implementation for NetworkRemote
    /// </summary>
    static class ChallengeResponse
    {
        /// <summary>
        /// Get a new Challenge string
        /// </summary>
        /// <returns>A new Challenge string</returns>
        public static string GetChallenge()
        {
            return RandomString(Settings.MinimumSecretLength);
        }

        /// <summary>
        /// Compute the set of (response => command) mappings corresponding to the chosen challenge and check if one of them matches with the provided response
        /// </summary>
        /// <param name="settings">Program settings containing the set of Commands and API keys</param>
        /// <param name="challenge">Randomly chosen challenge (see GetChallenge())</param>
        /// <param name="response">Response sent by the client</param>
        /// <param name="clientName">Name of client associated with the API key</param>
        /// <param name="commandName">Name of command associated with the response</param>
        /// <returns>Command matching the provided response, or null</returns>
        public static ProcessStartInfo CheckResponse(Settings settings, string challenge, string response, ref string clientName, ref string commandName)
        {
            if (challenge.Length < Settings.MinimumSecretLength || response.Length < Settings.MinimumSecretLength)
                return null;
            foreach (KeyValuePair<string, string> apikey in settings.AllowedKeys)
            {
                foreach (KeyValuePair<string, ProcessStartInfo> commandMapping in settings.Commands)
                {
                    if (ComputeSHA256(challenge + apikey.Key + commandMapping.Key) == response)
                    {
                        clientName = apikey.Value;
                        commandName = commandMapping.Key;
                        return commandMapping.Value;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Random string generator
        /// </summary>
        /// <param name="length">Desired length</param>
        /// <param name="allowedChars">Allowed characters</param>
        /// <remarks>https://stackoverflow.com/questions/730268/unique-random-string-generation</remarks>
        /// <returns>Randomly generated string</returns>
        private static string RandomString(int length, string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be less than zero.");
            if (string.IsNullOrEmpty(allowedChars)) throw new ArgumentException("allowedChars may not be empty.");

            const int byteSize = 0x100;
            var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
            if (byteSize < allowedCharSet.Length) throw new ArgumentException(String.Format("allowedChars may contain no more than {0} characters.", byteSize));

            // Guid.NewGuid and System.Random are not particularly random. By using a
            // cryptographically-secure random number generator, the caller is always
            // protected, regardless of use.
            using (var rng = RandomNumberGenerator.Create())
            {
                var result = new StringBuilder();
                var buf = new byte[128];
                while (result.Length < length)
                {
                    rng.GetBytes(buf);
                    for (var i = 0; i < buf.Length && result.Length < length; ++i)
                    {
                        // Divide the byte into allowedCharSet-sized groups. If the
                        // random value falls into the last group and the last group is
                        // too small to choose from the entire allowedCharSet, ignore
                        // the value in order to avoid biasing the result.
                        var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
                        if (outOfRangeStart <= buf[i]) continue;
                        result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                    }
                }
                return result.ToString();
            }
        }

        /// <summary>
        /// SHA256 hash generator
        /// </summary>
        /// <param name="value">Text to hash</param>
        /// <remarks>https://stackoverflow.com/questions/16999361/obtain-sha-256-string-of-a-string</remarks>
        /// <returns>Computed SHA256 hash</returns>
        private static String ComputeSHA256(string value)
        {
            StringBuilder Sb = new StringBuilder();

            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }
    }
}
