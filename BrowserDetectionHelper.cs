using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Strombus.ServerShared
{
    public class BrowserDetectionHelper
    {

        public enum BrowserName
        {
            Unknown,
            Chrome,
            Edge,
            FireFox,
            InternetExplorer,
            Safari
        }
        public struct BrowserDetails
        {
            public BrowserName BrowserName;
            public Version Version;
        }
        public static BrowserDetails ConvertUserAgentStringToBrowserDetails(string userAgent)
        {
            /* NOTE: user-agent format is defined in RFC 2616 as:
             *       "The field can contain multiple product tokens (section 3.8) and comments identifying the agent and any 
             *        subproducts which form a significant part of the user agent. By convention, the product tokens are listed 
             *        in order of their significance for identifying the application."
             *        
             * Product token format:
             *   product/version
             * 
             * Other token format:
             *   (non-comment and non-product-token)
             *   
             * Comment format:
             *   '(' ... ')'
             *        
             * EXAMPLE (Edge on Windows 10):
             *   Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36 Edge/13.10586
             */

            // convert useragent to lower-case
            userAgent = userAgent.ToLowerInvariant();

            // convert useragent into its components.
            List<HttpToken> tokens = SplitFieldIntoTokensAndComments(userAgent);

            // NOTE: since some browsers list _other_ browsers as well, we process the user agent strings in a particular order (edge, msie, firefox, chrome, safari)

            // detect Edge browser
            foreach (HttpToken token in tokens)
            {
                if (token.GetType() == typeof(HttpProductToken))
                {
                    HttpProductToken productToken = (HttpProductToken)token;
                    if (productToken.Product.ToLowerInvariant() == "edge")
                    {
                        return new BrowserDetails() { BrowserName = BrowserName.Edge, Version = ConvertStringToVersion(productToken.Version) };
                    }
                }
            }

            // detect Internet Explorer browser
            foreach (HttpToken token in tokens)
            {
                if (token.GetType() == typeof(HttpComment))
                {
                    HttpComment comment = (HttpComment)token;
                    // if comment contains "MSIE 10.6" etc...
                    if (comment.Comment.ToLowerInvariant().IndexOf("msie") >= 0)
                    {
                        Version version = ConvertStringToVersion(ExtractNumberStringFromString(comment.Comment.Substring(comment.Comment.ToLowerInvariant().IndexOf("msie") + 4)));
                        return new BrowserDetails() { BrowserName = BrowserName.InternetExplorer, Version = version };
                    }
                    // if comment contains "Trident/" combined with "rv:{version}" (MSIE browser engine)
                    if (comment.Comment.ToLowerInvariant().IndexOf("trident/") >= 0 && comment.Comment.ToLowerInvariant().IndexOf("rv:") >= 0)
                    {
                        Version version = ConvertStringToVersion(ExtractNumberStringFromString(comment.Comment.Substring(comment.Comment.ToLowerInvariant().IndexOf("rv:") + 3)));
                        return new BrowserDetails() { BrowserName = BrowserName.InternetExplorer, Version = version };
                    }
                }
            }

            // detect FireFox browser
            foreach (HttpToken token in tokens)
            {
                if (token.GetType() == typeof(HttpProductToken))
                {
                    HttpProductToken productToken = (HttpProductToken)token;
                    if (productToken.Product.ToLowerInvariant() == "firefox")
                    {
                        return new BrowserDetails() { BrowserName = BrowserName.FireFox, Version = ConvertStringToVersion(productToken.Version) };
                    }
                }
            }

            // detect Chrome browser
            foreach (HttpToken token in tokens)
            {
                if (token.GetType() == typeof(HttpProductToken))
                {
                    HttpProductToken productToken = (HttpProductToken)token;
                    if (productToken.Product.ToLowerInvariant() == "chrome")
                    {
                        return new BrowserDetails() { BrowserName = BrowserName.Chrome, Version = ConvertStringToVersion(productToken.Version) };
                    }
                }
            }

            // detect Safari browser
            bool isSafariBrowser = false;
            foreach (HttpToken token in tokens)
            {
                if (token.GetType() == typeof(HttpProductToken))
                {
                    HttpProductToken productToken = (HttpProductToken)token;
                    if (productToken.Product.ToLowerInvariant() == "safari")
                    {
                        // we need to find the "version" product token (which contains the actual Safari version for Safari v3.0+)
                        isSafariBrowser = true;
                        break;
                    }
                }
            }
            if (isSafariBrowser)
            {
                foreach (HttpToken token in tokens)
                {
                    if (token.GetType() == typeof(HttpProductToken))
                    {
                        HttpProductToken productToken = (HttpProductToken)token;
                        if (productToken.Product.ToLowerInvariant() == "version")
                        {
                            // the "version" product token contains the actual version of safari.
                            return new BrowserDetails() { BrowserName = BrowserName.Safari, Version = ConvertStringToVersion(productToken.Version) };
                        }
                    }
                }
            }

            // in all other scenarios, return a default
            return new BrowserDetails() { BrowserName = BrowserName.Unknown, Version = null };
        }

        class HttpToken
        {
            public string RawValue;
        }
        class HttpProductToken : HttpToken
        {
            public string Product;
            public string Version;
        }
        class HttpComment : HttpToken
        {
            public string Comment;
        }

        static List<HttpToken> SplitFieldIntoTokensAndComments(string value)
        {
            List<HttpToken> result = new List<HttpToken>();

            if (value != null && value.Length > 0)
            {
                bool forwardSlashFound = false;
                bool leftParenFound = false;
                int startPos = 0;
                int currentPos = 0;
                while (true)
                {
                    char ch = value[currentPos];
                    bool isSeparator = false;
                    switch (ch)
                    {
                        case '<':
                        case '>':
                        case '@':
                        case ',':
                        case ';':
                        case ':':
                        case '\\':
                        case '"':
                        case '[':
                        case ']':
                        case '?':
                        case '=':
                        case '{':
                        case '}':
                        case ' ': // SP
                        case (char)9: // HT
                            // if we are not inside a comment, then this is a separator
                            isSeparator = !leftParenFound;
                            break;
                        case ')':
                            if (leftParenFound)
                            {
                                // if we were in a comment, then we have now exited the comment
                                leftParenFound = false;
                            }
                            else
                            {
                                // if we are not inside a comment, then this is a separator
                                isSeparator = true;
                            }
                            break;
                        case '/':
                            // the first slash is a product token product/version separator; two slashes are not supported so we have to treat a second one as a separator
                            isSeparator = forwardSlashFound;
                            forwardSlashFound = true;
                            break;
                        case '(':
                            // the first left paren is the start of a comment; two left parens are not supported so we have to treat a second one as a separator
                            isSeparator = leftParenFound;
                            leftParenFound = true;
                            break;
                        default:
                            // part of token/comment; proceed to next character
                            isSeparator = false;
                            break;
                    }

                    if (isSeparator || currentPos == value.Length - 1)
                    {
                        string tokenValue = value.Substring(startPos, currentPos - startPos);
                        //System.Diagnostics.Debug.WriteLine("tokenValue: " + tokenValue);
                        if (tokenValue.Length > 0)
                        {
                            if (tokenValue.Substring(0, 1) == "(" && tokenValue.Substring(tokenValue.Length - 1, 1) == ")")
                            {
                                // comment
                                result.Add(new HttpComment { RawValue = tokenValue, Comment = tokenValue.Substring(1, tokenValue.Length - 2) });
                            }
                            else if (tokenValue.IndexOf('/') > 0)
                            {
                                // product token
                                result.Add(new HttpProductToken { RawValue = tokenValue, Product = tokenValue.Substring(0, tokenValue.IndexOf('/')), Version = tokenValue.Substring(tokenValue.IndexOf('/') + 1) });
                            }
                            else
                            {
                                // token
                                result.Add(new HttpToken { RawValue = tokenValue });
                            }
                        }
                        startPos = currentPos + 1;
                        forwardSlashFound = false;
                        leftParenFound = false;
                    }
                    currentPos++;

                    if (currentPos >= value.Length) break;
                }
            }

            return result;
        }

        static string ExtractNumberStringFromString(string value)
        {
            value = value.Trim();
            int length = value.Length;
            for (int pos = 0; pos < value.Length; pos++)
            {
                char ch = value[pos];
                if (char.IsDigit(ch) || ch == '.')
                {
                    // digit or period; proceed
                }
                else
                {
                    length = pos;
                    break;
                }
            }

            return value.Substring(0, length);
        }

        static Version ConvertStringToVersion(string version)
        {
            int? major = ParseVersionComponent(ref version);
            int? minor = ParseVersionComponent(ref version);
            int? build = ParseVersionComponent(ref version);
            int? revision = ParseVersionComponent(ref version);
            return new System.Version(major.HasValue ? major.Value : 0, minor.HasValue ? minor.Value : 0, build.HasValue ? build.Value : 0, revision.HasValue ? revision.Value : 0);
        }

        static int? ParseVersionComponent(ref string value)
        {
            if (value == null)
                return null;

            int length = value.Length;
            if (value.IndexOf('.') >= 0)
            {
                length = value.IndexOf('.');
            }

            int intValue;
            bool success = int.TryParse(value.Substring(0, length), out intValue);
            if (success)
            {
                value = value.Substring(Math.Min(value.Length, length + 1));
                return intValue;
            }
            else
            {
                return null;
            }
        }
    }
}
