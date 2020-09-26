using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Strombus.ServerShared
{
    public class FormattingHelper
    {
        private const string alphaNumericCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        //static public readonly string AllowedIdentifierSpecialCharactersAsString = "-_.~!*'()"; // NOTE: '~' and ''' are historically "sometimes-urlencoded" but technically fine.
        //static public readonly string AllowedIdentifierSpecialCharactersAsString = "-_.!*()"; // NOTE: this is the expanded set we can allow if we don't need to reserve them.
        public const string AllowedIdentifierSpecialCharactersAsString = "-_";
        public static readonly char[] AllowedIdentifierSpecialCharacters = AllowedIdentifierSpecialCharactersAsString.ToArray();

        public static bool ContainsOnlyAlphaNumericCharacters(string value)
        {
            return ContainsOnlySpecifiedCharacters(value, alphaNumericCharacters);
        }

        public static bool ContainsOnlyAllowedIdentifierCharacters(string value)
        {
            return ContainsOnlySpecifiedCharacters(value, alphaNumericCharacters + AllowedIdentifierSpecialCharactersAsString);
        }

        private static bool ContainsOnlySpecifiedCharacters(string value, string allowedCharactersAsString)
        {
            char[] allowedChars = allowedCharactersAsString.ToCharArray();
            foreach (char c in value)
            {
                if (!allowedChars.Contains(c))
                    return false;
            }

            // if all characters passed, return true.
            return true;
        }

        public static bool ContainsGuid(string value)
        {
            char[] validChars = "0123456789ABCDEFabcdef-".ToCharArray();
            foreach (char c in value)
            {
                if (!validChars.Contains(c))
                    return false;
            }

            Guid parsedGuid;
            if (Guid.TryParse(value, out parsedGuid) == false)
                return false;

            // finally, convert the GUID back and make sure it's still valid; if so, return success.
            return (parsedGuid.ToString("D").ToUpperInvariant() == value.ToUpperInvariant());
        }

        public static string FormatGuidAsSafeIdentifierGuid(string value)
        {
            return Guid.Parse(value).ToString("D").ToUpperInvariant();
        }
    }
}
