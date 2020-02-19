// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Sitko.Core.Storage.Proxy.StaticFiles
{
    /// <summary>
    /// Provides a parser for the Range Header in an <see cref="HttpContext.Request"/>.
    /// </summary>
    internal static class RangeHelper
    {
        /// <summary>
        /// Returns the normalized form of the requested range if the Range Header in the <see cref="HttpContext.Request"/> is valid.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the request.</param>
        /// <param name="requestHeaders">The <see cref="RequestHeaders"/> associated with the given <paramref name="context"/>.</param>
        /// <param name="length">The total length of the file representation requested.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <returns>A boolean value which represents if the <paramref name="requestHeaders"/> contain a single valid 
        /// range request. A <see cref="RangeItemHeaderValue"/> which represents the normalized form of the 
        /// range parsed from the <paramref name="requestHeaders"/> or <c>null</c> if it cannot be normalized.</returns>
        /// <remark>If the Range header exists but cannot be parsed correctly, or if the provided length is 0, then the range request cannot be satisfied (status 416). 
        /// This results in (<c>true</c>,<c>null</c>) return values.</remark>
        public static (bool isRangeRequest, RangeItemHeaderValue range) ParseRange(
            HttpContext context,
            RequestHeaders requestHeaders,
            long length,
            ILogger logger)
        {
            var rawRangeHeader = context.Request.Headers[HeaderNames.Range];
            if (StringValues.IsNullOrEmpty(rawRangeHeader))
            {
                logger.LogTrace("Range header's value is empty.");
                return (false, null);
            }

            // Perf: Check for a single entry before parsing it
            if (rawRangeHeader.Count > 1 || rawRangeHeader[0].IndexOf(',') >= 0)
            {
                logger.LogDebug("Multiple ranges are not supported.");

                // The spec allows for multiple ranges but we choose not to support them because the client may request
                // very strange ranges (e.g. each byte separately, overlapping ranges, etc.) that could negatively
                // impact the server. Ignore the header and serve the response normally.               
                return (false, null);
            }

            var rangeHeader = requestHeaders.Range;
            if (rangeHeader == null)
            {
                logger.LogDebug("Range header's value is invalid.");
                // Invalid
                return (false, null);
            }

            // Already verified above
            Debug.Assert(rangeHeader.Ranges.Count == 1);

            var ranges = rangeHeader.Ranges;
            if (ranges == null)
            {
                logger.LogDebug("Range header's value is invalid.");
                return (false, null);
            }

            if (ranges.Count == 0)
            {
                return (true, null);
            }

            if (length == 0)
            {
                return (true, null);
            }

            // Normalize the ranges
            var range = NormalizeRange(ranges.SingleOrDefault(), length);

            // Return the single range
            return (true, range);
        }

        // Internal for testing
        internal static RangeItemHeaderValue NormalizeRange(RangeItemHeaderValue range, long length)
        {
            var start = range.From;
            var end = range.To;

            // X-[Y]
            if (start.HasValue)
            {
                if (start.Value >= length)
                {
                    // Not satisfiable, skip/discard.
                    return null;
                }

                if (!end.HasValue || end.Value >= length)
                {
                    end = length - 1;
                }
            }
            else
            {
                // suffix range "-X" e.g. the last X bytes, resolve
                if (end.Value == 0)
                {
                    // Not satisfiable, skip/discard.
                    return null;
                }

                var bytes = Math.Min(end.Value, length);
                start = length - bytes;
                end = start + bytes - 1;
            }

            return new RangeItemHeaderValue(start, end);
        }
    }
}
