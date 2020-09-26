using Strombus.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Strombus.ServerShared
{
    public class RedisHelper
    {
        private const string REDIS_PREFIX_SERVICE = "service";
        private const string REDIS_PREFIX_SEPARATOR = ":";
        //
        private const string REDIS_ASTERISK = "*";
        private const string REDIS_SLASH = "/";
        private const char REDIS_SLASH_AS_CHAR = '/';
        //
        private const string REDIS_SUFFIX_SERVER_IDS = "server-ids";
        private const string REDIS_SUFFIX_SEPARATOR = "#";

        public struct VerifyAccountIdAndServerIdResult
        {
            public bool Success;     // if false, the hostname is invalid
            public string AccountId; // if null, root server; otherwise this is an account-specific server
            public int? ServerId;  // this will return the specified server-id (or if none was specified, this will return the default server-id for the account+service)
        }
        public static async Task<VerifyAccountIdAndServerIdResult> VerifyAccountIdAndServerIdAsync(RedisClient redisClient, string serviceName, string accountId, bool allowRootService, int? serverId)
        {
            // if an accountId is required, verify that one was provided
            if (allowRootService == false && accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            // make sure we support the account and server-id specified; if no server-id is specified, then return the default server-id
            string defaultServerIdAsString;
            int defaultServerId;
            string baseKey;
            if (accountId != null)
            {
                // account server key
                baseKey = REDIS_PREFIX_SERVICE + REDIS_PREFIX_SEPARATOR + accountId + REDIS_SLASH + serviceName;
            }
            else
            {
                // root server key
                baseKey = REDIS_PREFIX_SERVICE + REDIS_PREFIX_SEPARATOR + REDIS_ASTERISK + REDIS_SLASH + serviceName;
            }

            // determine if the service is enabled for this account (or root, if no account)--and also fetch the corresponding defaultServerId
            if (await redisClient.HashExistsAsync(baseKey, "default-server-id") == 1)
            {
                // if the default-server-id field exists, the service is enabled
                defaultServerIdAsString = await redisClient.HashGetAsync<string, string, string>(baseKey, "default-server-id");
                if (!int.TryParse(defaultServerIdAsString, out defaultServerId))
                {
                    // severe error: could not convert the default-server-id into an integer
                    return new VerifyAccountIdAndServerIdResult() { Success = false };
                }
            }
            else
            {
                // service is not supported for this accountId on this server (or for root, if no accountId was specified)
                return new VerifyAccountIdAndServerIdResult() { Success = false };
            }

            // finally, make sure that the serverId (if specified) belongs to this specific server
            if (serverId != null)
            {
                if (await VerifyServerIdAsync(redisClient, serviceName, accountId, allowRootService, serverId.Value.ToString()) == false)
                { 
                    // service is not enabled from this serverId
                    return new VerifyAccountIdAndServerIdResult() { Success = false };
                }
            }

            // at this point, we know that the service is supported

            // return our accountId and serverId (or the default serverId, if no serverId was specified)
            return new VerifyAccountIdAndServerIdResult()
            {
                Success = true,
                AccountId = accountId,
                ServerId = serverId ?? defaultServerId
            };
        }

        public static async Task<bool> VerifyServerIdAsync(RedisClient redisClient, string serviceName, string accountId, bool allowRootService, string serverId)
        {
            // if an accountId is required, verify that one was provided
            if (allowRootService == false && accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            // make sure we support the account and server-id specified
            string baseKey;
            if (accountId != null)
            {
                // account server key
                baseKey = REDIS_PREFIX_SERVICE + REDIS_PREFIX_SEPARATOR + accountId + REDIS_SLASH + serviceName;
            }
            else
            {
                // root server key
                baseKey = REDIS_PREFIX_SERVICE + REDIS_PREFIX_SEPARATOR + REDIS_ASTERISK + REDIS_SLASH + serviceName;
            }

            // finally, make sure that the serverId (if specified) belongs to this specific server
            if (serverId != null)
            {
                if (await redisClient.SetIsMemberAsync(baseKey + REDIS_SUFFIX_SEPARATOR + REDIS_SUFFIX_SERVER_IDS, serverId) == 0)
                {
                    // service is not enabled from this serverId
                    return false;
                }
            }

            // at this point, we know that the service is supported

            // return success
            return true;
        }
    }
}
