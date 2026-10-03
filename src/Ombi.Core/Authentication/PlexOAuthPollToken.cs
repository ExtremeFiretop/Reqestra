namespace Ombi.Core.Authentication
{
    public static class PlexOAuthPollToken
    {
        public const int Length = 64;

        public static bool IsValid(string pollToken)
        {
            if (string.IsNullOrEmpty(pollToken) || pollToken.Length != Length)
            {
                return false;
            }

            foreach (var character in pollToken)
            {
                var isLowerHex = (character >= '0' && character <= '9') ||
                                 (character >= 'a' && character <= 'f');
                if (!isLowerHex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
