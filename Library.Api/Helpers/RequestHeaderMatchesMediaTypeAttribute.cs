using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System;

namespace Library.Api.Helpers
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
    {
        private readonly string _requestHeaderToMatch;
        private readonly string[] _mediaTypes;

        public RequestHeaderMatchesMediaTypeAttribute(string requestHeaderToMatch, string[] mediaTypes)
        {
            _requestHeaderToMatch = requestHeaderToMatch;
            _mediaTypes = mediaTypes;
        }

        public int Order => 0;

        public bool Accept(ActionConstraintContext context)
        {
            IHeaderDictionary requestHeaders = context.RouteContext.HttpContext.Request.Headers;

            if (!requestHeaders.ContainsKey(_requestHeaderToMatch))
            {
                return false;
            }

            foreach (string mediaType in _mediaTypes)
            {
                bool mediaTypeMatches = string.Equals(requestHeaders[_requestHeaderToMatch].ToString(), mediaType,
                    StringComparison.OrdinalIgnoreCase);

                if (mediaTypeMatches)
                {
                    return true;
                }
            }

            return false;
        }
    }
}