using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Strombus.ServerShared
{
    public class HttpHelper
    {
        public static readonly System.Text.Encoding DEFAULT_HTTP_ENCODING = System.Text.Encoding.GetEncoding("iso-8859-1");

        public const int RESPONSE_CODE_HTTP_200_OK = 200;
        public const int RESPONSE_CODE_HTTP_201_CREATED = 201;
        public const int RESPONSE_CODE_HTTP_204_NO_CONTENT = 204;
        public const int RESPONSE_CODE_HTTP_302_FOUND = 302;
        public const int RESPONSE_CODE_HTTP_307_TEMPORARY_REDIRECT = 307;
        public const int RESPONSE_CODE_HTTP_308_PERMANENT_REDIRECT = 308;
        public const int RESPONSE_CODE_HTTP_400_BAD_REQUEST = 400;
        public const int RESPONSE_CODE_HTTP_401_UNAUTHORIZED = 401;
        public const int RESPONSE_CODE_HTTP_403_FORBIDDEN = 403;
        public const int RESPONSE_CODE_HTTP_404_NOT_FOUND = 404;
        public const int RESPONSE_CODE_HTTP_405_METHOD_NOT_ALLOWED = 405;
        public const int RESPONSE_CODE_HTTP_406_NOT_ACCEPTABLE = 406;
        public const int RESPONSE_CODE_HTTP_410_GONE = 410;
        public const int RESPONSE_CODE_HTTP_413_PAYLOAD_TOO_LARGE = 413;
        public const int RESPONSE_CODE_HTTP_415_UNSUPPORTED_MEDIA_TYPE = 415;
        public const int RESPONSE_CODE_HTTP_500_INTERNAL_SERVER_ERROR = 500;

        private const string BEARER_PREFIX_LOWERCASE = "bearer ";

        public static void SetHttpResponseOk(HttpContext context)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_200_OK;
        }

        public static void SetHttpResponseCreated(HttpContext context, string location)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_201_CREATED;
            if (location != null) { 
                context.Response.Headers["Location"] = location;
            }
        }

        public static void SetHttpResponseFound(HttpContext context, string location)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_302_FOUND;
            context.Response.Headers["Location"] = location;
        }

        public static void SetHttpResponseTemporaryRedirect(HttpContext context, string location)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_307_TEMPORARY_REDIRECT;
            context.Response.Headers["Location"] = location;
        }

        public static void SetHttpResponsePermanentRedirect(HttpContext context, string location)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_308_PERMANENT_REDIRECT;
            context.Response.Headers["Location"] = location;
        }

        public static void SetHttpResponseBadRequest(HttpContext context)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_400_BAD_REQUEST;
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["Pragma"] = "no-store";
        }

        public static void SetHttpResponseUnauthorized(HttpContext context)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_401_UNAUTHORIZED;
            context.Response.Headers["WWW-Authenticate"] = "Bearer";
        }

        public static void SetHttpResponseForbidden(HttpContext context)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_403_FORBIDDEN;
        }

        public static void SetHttpResponseNotFound(HttpContext context)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_404_NOT_FOUND;
        }

        public static void SetHttpResponseMethodNotAllowed(HttpContext context, string[] supportedMethods)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_405_METHOD_NOT_ALLOWED;
            context.Response.Headers["Allow"] = string.Join(", ", supportedMethods);
        }

        public static void SetHttpResponsePayloadTooLarge(HttpContext context)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_413_PAYLOAD_TOO_LARGE;
        }

        public static void SetHttpResponseInternalServerError(HttpContext context)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_500_INTERNAL_SERVER_ERROR;
        }

        public static void SetHttpResponseNoContent(HttpContext context)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_204_NO_CONTENT;
        }

        public static void SetHttpResponseNoContent_ForOptionsMethod(HttpContext context, string[] supportedMethods)
        {
            context.Response.StatusCode = RESPONSE_CODE_HTTP_204_NO_CONTENT;
            context.Response.Headers["Allow"] = string.Join(", ", supportedMethods);
        }

        const string HTTP_CONTENT_TYPE_JSON_AS_STRING = "application/json";
        const string HTTP_CONTENT_TYPE_WWW_FORM_URLENCODED_AS_STRING = "application/x-www-form-urlencoded";

        public enum HttpContentType
        {
            Json,
            WwwFormUrlencoded
        }

        public static async Task<bool> VerifyContentTypeHeaderIsJson(HttpContext context)
        {
            // NOTE: we currently only supported UTF8 encoding for JSON; if other options are supported in the future, consider returning the charset instead of bool
            return await VerifyContentTypeHeader(context, HttpContentType.Json, new List<System.Text.Encoding>() { System.Text.Encoding.UTF8 });
        }

        public static async Task<bool> VerifyContentTypeHeaderIsWwwFormUrlEncodedAsync(HttpContext context)
        {
            return await VerifyContentTypeHeader(context, HttpContentType.WwwFormUrlencoded, new List<System.Text.Encoding>() { System.Text.Encoding.UTF8 });
        }

        public static async Task<bool> VerifyContentTypeHeader(HttpContext context, HttpContentType requiredContentType, List<System.Text.Encoding> requiredEncodingOptions)
        {
            // validate the requiredContentType argument
            string requiredContentTypeAsString = ConvertHttpContentTypeToString(requiredContentType);
            if (requiredContentTypeAsString == null)
            {
                throw new ArgumentException("Invalid content type.", nameof(requiredContentType));
            }
            //
            if (requiredEncodingOptions == null)
            {
                // if requiredEncodings is null, determine the default encoding based on the content type
                requiredEncodingOptions = new List<System.Text.Encoding>() { GetDefaultEncodingForHttpContentType(requiredContentType) };
            }
            else if (requiredEncodingOptions.Count == 0)
            {
                // if requiredEncoding is an empty (non-null) list, we cannot match the encoding: return false.
                return false;
            }

            // verify that the content-type header matches the required type
            var contentTypeResults = ParseContentTypeHeader(context.Request.ContentType);
            string contentType = contentTypeResults.ContentType;
            if (contentType != requiredContentTypeAsString)
            { 
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = RESPONSE_CODE_HTTP_415_UNSUPPORTED_MEDIA_TYPE;
                await context.Response.WriteAsync("ERROR: Content-Type must be '" + requiredContentTypeAsString + "'.");
                return false;
            }

            // verify the charset (encoding)
            if (!VerifyContentEncoding(requiredEncodingOptions, contentTypeResults.ExplicitEncoding, contentTypeResults.DefaultEncoding))
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = RESPONSE_CODE_HTTP_415_UNSUPPORTED_MEDIA_TYPE;
                await context.Response.WriteAsync("ERROR: Charset encoding is not supported for this Content-Type.");
                return false;
            }

            return true;
        }

        private static bool VerifyContentEncoding(System.Text.Encoding requiredEncoding, System.Text.Encoding explicitEncoding, System.Text.Encoding defaultEncoding)
        {
            return VerifyContentEncoding(new List<System.Text.Encoding>() { requiredEncoding }, explicitEncoding, defaultEncoding);
        }

        private static bool VerifyContentEncoding(List<System.Text.Encoding> requiredEncodingOptions, System.Text.Encoding explicitEncoding, System.Text.Encoding defaultEncoding)
        {
            foreach (System.Text.Encoding requiredEncoding in requiredEncodingOptions)
            {
                if ((requiredEncoding == explicitEncoding) || (explicitEncoding == null && requiredEncoding == defaultEncoding))
                {
                    return true;
                }
            }
            // no match found
            return false;
        }

        private static string ConvertHttpContentTypeToString(HttpContentType contentType)
        {
            switch (contentType)
            {
                case HttpContentType.Json:
                    return HTTP_CONTENT_TYPE_JSON_AS_STRING;
                case HttpContentType.WwwFormUrlencoded:
                    return HTTP_CONTENT_TYPE_WWW_FORM_URLENCODED_AS_STRING;
                default:
                    return null;
            }
        }

        private static System.Text.Encoding GetDefaultEncodingForHttpContentType(HttpContentType contentType)
        {
            switch (contentType)
            {
                case HttpContentType.Json:
                    return System.Text.Encoding.UTF8;
                case HttpContentType.WwwFormUrlencoded:
                    return DEFAULT_HTTP_ENCODING;
                default:
                    return DEFAULT_HTTP_ENCODING;
            }
        }

        public static async Task<bool> VerifyAcceptHeaderIsJson(HttpContext context)
        {
            return await VerifyAcceptHeader(context, "application/json");
        }

        public static async Task<bool> VerifyAcceptHeader(HttpContext context, string contentType)
        {
            // verify that the accept header is valid
            var acceptContentTypes = context.Request.Headers["Accept"];
            if (!acceptContentTypes.Contains(contentType))
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = RESPONSE_CODE_HTTP_406_NOT_ACCEPTABLE;
                await context.Response.WriteAsync("ERROR: Accept header must equal '" + contentType + "'.");
                return false;
            }

            return true;
        }

        public class ParseContentTypeHeaderResult
        {
            public string ContentType { get; }
            public System.Text.Encoding ExplicitEncoding { get; }
            public System.Text.Encoding DefaultEncoding { get; }

            public ParseContentTypeHeaderResult(string contentType, System.Text.Encoding explicitEncoding, System.Text.Encoding defaultEncoding)
            {
                this.ContentType = contentType;
                this.ExplicitEncoding = explicitEncoding;
                this.DefaultEncoding = defaultEncoding;
            }
        }
        // NOTE: this function returns the parsed content-type, the parsed encoding...and the default encoding for the parsed content-type
        //       if no content-type was specified, then the content-type is assumed to be application/octet-stream.
        public static ParseContentTypeHeaderResult ParseContentTypeHeader(string headerValue)
        {
            string CHARSET_QUERY_NAME = "charset";
            string contentType;
            System.Text.Encoding explicitContentEncoding = null;
            System.Text.Encoding defaultContentEncoding = null;
            bool mustIgnoreExplicitEncoding = false;

            if (headerValue == null)
            {
                // NOTE: all HTTP/1.1 requests with bodies SHOULD include the Content-Type header; if it is missing we should assume application/octet-stream.
                contentType = "application/octet-stream";
                defaultContentEncoding = DEFAULT_HTTP_ENCODING;
            }
            else
            {
                string[] contentTypeElements = headerValue.Split(';');
                // NOTE: the first element _must_ be the content type
                contentType = contentTypeElements[0];

                // determine the default encoding
                switch (contentType.ToLowerInvariant())
                {
                    case HTTP_CONTENT_TYPE_JSON_AS_STRING:
                        // application/json uses UTF8 by default
                        defaultContentEncoding = System.Text.Encoding.UTF8;
                        /* NOTE: we are assuming that this type cannot have an explictly-provided charset and that the charset must be inferred. */
                        mustIgnoreExplicitEncoding = true;
                        break;
                    case HTTP_CONTENT_TYPE_WWW_FORM_URLENCODED_AS_STRING:
                        // application/x-www-form-urlencoded uses UTF8 exclusively and does not support the optional charset argument
                        defaultContentEncoding = System.Text.Encoding.UTF8;
                        mustIgnoreExplicitEncoding = true;
                        break;
                    default:
                        // for all other encodings, assume the default HTTP encoding
                        defaultContentEncoding = DEFAULT_HTTP_ENCODING; // default HTTP encoding
                        break;
                }

                if (!mustIgnoreExplicitEncoding)
                {
                    // now parse the remaining elements to find the explicitly-defined encoding, if one is supplied.
                    for (int iElement = 1; iElement < contentTypeElements.Length; iElement++)
                    {
                        var element = contentTypeElements[iElement].ToLowerInvariant().Trim();
                        if (element.Substring(0, CHARSET_QUERY_NAME.Length + 1) == CHARSET_QUERY_NAME + "=")
                        {
                            switch (element.Substring(CHARSET_QUERY_NAME.Length + 1))
                            {
                                case "utf-8":
                                    explicitContentEncoding = System.Text.Encoding.UTF8;
                                    break;
                                case "iso-8859-1":
                                    explicitContentEncoding = System.Text.Encoding.GetEncoding("iso-8859-1");
                                    break;
                                default:
                                    // unknown encoding
                                    break;
                            }
                        }
                    }
                }
            }

            return new ParseContentTypeHeaderResult(contentType, explicitContentEncoding, defaultContentEncoding);
        }

        public static string ExtractBearerTokenFromAuthorizationHeaderValue(Microsoft.Extensions.Primitives.StringValues headerValues)
        {
            if (headerValues.Count > 0 && headerValues[0].Substring(0, BEARER_PREFIX_LOWERCASE.Length).ToLowerInvariant() == BEARER_PREFIX_LOWERCASE)
            {
                return headerValues[0].Substring(BEARER_PREFIX_LOWERCASE.Length);
            }
            else
            {
                return null;
            }
        }
    }
}
