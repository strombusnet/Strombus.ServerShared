using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Strombus.ServerShared
{
    public class ParsingHelper
    {
        public struct ServerDetails
        {
            public string AccountId;
            public string ServerType;
            public int? ServerId;

            public string ToAccountIdServerTypeServiceIdString()
            {
                if (AccountId != null && ServerType != null && ServerId != null)
                {
                    return AccountId + "-" + ServerType + "-" + ServerId.Value.ToString();
                }
                else if (ServerType != null && ServerId != null)
                {
                    return ServerType + "-" + ServerId.Value.ToString();
                }
                else
                {
                    return null;
                }
            }

            public string ToAccountIdServerIdIdentifierString()
            {
                if (AccountId != null && ServerId != null)
                {
                    return AccountId + "-" + ServerId.Value.ToString();
                }
                else if (ServerId != null)
                {
                    return ServerId.Value.ToString();
                }
                else
                {
                    return null;
                }
            }

            public string ToServerTypeServerIdIdentifierString()
            {
                if (ServerType != null && ServerId != null)
                {
                    return ServerType + "-" + ServerId.Value.ToString();
                }
                else if (ServerId != null)
                {
                    return ServerId.Value.ToString();
                }
                else
                {
                    return null;
                }
            }
        }
        public static ServerDetails? ExtractServerDetailsFromHostname(string hostname)
        {
            // if the hostname is null or empty, return null
            if (string.IsNullOrWhiteSpace(hostname)) return null;
            // if the hostname does not contain a subdomain, return null
            if (hostname.IndexOf('.') < 0) return null;

            // extract the subdomain from the hostname
            string subdomain = hostname.Substring(0, hostname.IndexOf('.')).ToLowerInvariant();

            // if the subdomain does not have a hyphen, it is simply a root serverType
            if (subdomain.IndexOf('-') < 0)
            {
                return new ServerDetails() { ServerType = subdomain };
            }

            string[] subdomainParts = subdomain.Split('-');
            foreach (string subdomainPart in subdomainParts)
            {
                // if there are double-hyphens or empty sections of the subdomain, return null
                if (string.IsNullOrWhiteSpace(subdomainPart)) return null;
            }

            switch (subdomainParts.Length)
            {
                case 1:
                    // format: {serverType}
                    return new ServerDetails() { ServerType = subdomain };
                case 2:
                    if (StringIsWholeNumber(subdomainParts[1]))
                    {
                        // format: {serverType}-{serverId}
                        return new ServerDetails() { ServerType = subdomainParts[0], ServerId = int.Parse(subdomainParts[1]) };
                    }
                    else
                    {
                        // format: {accountId}-{serverType}
                        return new ServerDetails() { AccountId = subdomainParts[0], ServerType = subdomainParts[1] };
                    }
                case 3:
                    // if the third portion of the subdomain is not a number, fail now.
                    if (!StringIsWholeNumber(subdomainParts[2])) return null;
                    // format: {accountId}-{serverType}-{serverId}
                    return new ServerDetails() { AccountId = subdomainParts[0], ServerType = subdomainParts[1], ServerId = int.Parse(subdomainParts[2]) };
                default:
                    // error: subdomains should not contain more than {accountId}-{serverType}-{serverId} (i.e. 3 values)
                    return null;
            }
        }

        public static ServerDetails? ExtractServerDetailsFromAccountServerId(string accountServerId)
        {
            if (accountServerId == null) return null;
            // re-use the ExtractServerDetailsFromAccountServerIdIdentifier function by adding a trailing hyphen to our accountServerId
            return ExtractServerDetailsFromAccountServerIdIdentifier(accountServerId + "-");
        }

        // NOTE: use this function for creating "AccountServerId" identifiers (for tokens, clients, etc....either account-#-id or #-id format)
        public static ServerDetails? ExtractServerDetailsFromAccountServerIdIdentifier(string identifier)
        {
            // if the identifier is null or empty, return null
            if (string.IsNullOrWhiteSpace(identifier)) return null;

            // if the identifier does not have a hyphen, it is invalid; return null
            if (identifier.IndexOf('-') < 0) return null;

            string accountId = null;
            string serverId = null;

            string firstPrefix = identifier.Substring(0, identifier.IndexOf('-'));
            if (StringIsWholeNumber(firstPrefix))
            {
                // auth server is a root server; return only the first prefix
                serverId = firstPrefix;
            }
            else
            {
                // auth server is an account-specific server
                accountId = firstPrefix;

                // the auth server must also include a server number
                int secondPrefixStartIndex = firstPrefix.Length + 1;
                if (identifier.IndexOf('-', secondPrefixStartIndex) < 0) return null;

                string secondPrefix = identifier.Substring(secondPrefixStartIndex, identifier.IndexOf('-', secondPrefixStartIndex) - secondPrefixStartIndex);
                if (StringIsWholeNumber(secondPrefix))
                {
                    serverId = secondPrefix;
                }
            }

            if (accountId != null && serverId != null)
            {
                // format: {accountId}-{serverId}
                return new ServerDetails() { AccountId = accountId, ServerId = int.Parse(serverId) };
            }
            else if (serverId != null)
            {
                // format: {serverId}
                return new ServerDetails() { ServerId = int.Parse(serverId) };
            }
            else
            {
                // error: identifiers should not contain more than two values (i.e. {accountId}-{serverId}) or fewer than one value (i.e. {serverId})
                return null;
            }
        }

        // NOTE: use this function for creating "ServerTypeServerId" identifiers (for tokens, clients, etc....either type-#-id or #-id format)
        public static ServerDetails? ExtractServerDetailsFromServerTypeServerIdIdentifier(string identifier)
        {
            // if the identifier is null or empty, return null
            if (string.IsNullOrWhiteSpace(identifier)) return null;

            // if the identifier does not have a hyphen, it is invalid; return null
            if (identifier.IndexOf('-') < 0) return null;

            string serverType = null;
            string serverId = null;

            string firstPrefix = identifier.Substring(0, identifier.IndexOf('-'));
            if (StringIsWholeNumber(firstPrefix))
            {
                // auth server is a root server; return only the first prefix
                serverId = firstPrefix;
            }
            else
            {
                // auth server is an account-specific server
                serverType = firstPrefix;

                // the auth server must also include a server number
                int secondPrefixStartIndex = firstPrefix.Length + 1;
                if (identifier.IndexOf('-', secondPrefixStartIndex) < 0) return null;

                string secondPrefix = identifier.Substring(secondPrefixStartIndex, identifier.IndexOf('-', secondPrefixStartIndex) - secondPrefixStartIndex);
                if (StringIsWholeNumber(secondPrefix))
                {
                    serverId = secondPrefix;
                }
            }

            if (serverType != null && serverId != null)
            {
                // format: {serverType}-{serverId}
                return new ServerDetails() { ServerType = serverType, ServerId = int.Parse(serverId) };
            }
            else if (serverId != null)
            {
                // format: {serverId}
                return new ServerDetails() { ServerId = int.Parse(serverId) };
            }
            else
            {
                // error: identifiers should not contain more than two values (i.e. {accountId}-{serverId}) or fewer than one value (i.e. {serverId})
                return null;
            }
        }

        static bool StringIsWholeNumber(string value)
        {
            value = value.Trim();
            UInt32 valueAsUInt32;
            if (UInt32.TryParse(value, out valueAsUInt32))
            {
                // first prefix was able to be converted to a number; now convert it back and see if it matches the original string
                if (valueAsUInt32.ToString() == value)
                {
                    // we could convert the value to a number and back without losing data: it is a whole number in string representation.
                    return true;
                }
            }

            // if we could not parse the value or it could not be converted back to the exact input value, it is not a whole number
            return false;
        }

        public static string RewriteUrlWithServiceHostname(HttpContext context, string serviceName, string accountId)
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            if (accountId != null)
            {
                uriBuilder.Host = accountId + "-" + serviceName + ".example.com";
            }
            else
            {
                uriBuilder.Host = serviceName + ".example.com";
            }
            uriBuilder.Path = context.Request.Path.ToUriComponent();
            uriBuilder.Query = context.Request.QueryString.ToUriComponent();
            return uriBuilder.ToString();
        }
    }
}
