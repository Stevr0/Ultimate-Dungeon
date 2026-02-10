using System;
using System.IO;
using System.Text;

namespace UltimateDungeon.Networking
{
    /// <summary>
    /// Canonical AccountId normalization used by session/login + persistence.
    /// Rules are sourced from SESSION_AND_PERSISTENCE_MODEL.md.
    /// </summary>
    public static class SessionAccountId
    {
        public static string Normalize(string rawUsername)
        {
            if (string.IsNullOrWhiteSpace(rawUsername))
                return null;

            string trimmedLower = rawUsername.Trim().ToLowerInvariant();
            if (trimmedLower.Length == 0)
                return null;

            // Rule: collapse multiple spaces.
            // We normalize all whitespace runs to a single space first,
            // then replace spaces with '_' for folder-safe paths.
            var collapsed = new StringBuilder(trimmedLower.Length);
            bool previousWasWhitespace = false;

            for (int i = 0; i < trimmedLower.Length; i++)
            {
                char c = trimmedLower[i];
                bool isWhitespace = char.IsWhiteSpace(c);

                if (isWhitespace)
                {
                    if (!previousWasWhitespace)
                        collapsed.Append(' ');

                    previousWasWhitespace = true;
                    continue;
                }

                previousWasWhitespace = false;
                collapsed.Append(c);
            }

            if (collapsed.Length == 0)
                return null;

            // Rule: strip invalid path chars.
            char[] invalidPathChars = Path.GetInvalidFileNameChars();
            var folderSafe = new StringBuilder(collapsed.Length);

            for (int i = 0; i < collapsed.Length; i++)
            {
                char c = collapsed[i];
                if (Array.IndexOf(invalidPathChars, c) >= 0)
                    continue;

                // Keep account IDs path-safe and shell-friendly.
                folderSafe.Append(c == ' ' ? '_' : c);
            }

            if (folderSafe.Length == 0)
                return null;

            return folderSafe.ToString();
        }
    }
}
