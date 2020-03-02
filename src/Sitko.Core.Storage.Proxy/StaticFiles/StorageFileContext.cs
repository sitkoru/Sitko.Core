using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Sitko.Core.Storage.Proxy.StaticFiles
{
    public struct StorageFileContext
    {
        private const int StreamCopyBufferSize = 64 * 1024;

        private readonly HttpContext _context;
        private readonly StorageFileOptions _options;
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;
        private readonly ILogger _logger;
        private readonly StorageRecord _record;
        private readonly string _contentType;

        private EntityTagHeaderValue _etag;
        private RequestHeaders? _requestHeaders;
        private ResponseHeaders? _responseHeaders;
        private RangeItemHeaderValue _range;

        private long _length;
        private readonly PathString _subPath;
        private DateTimeOffset _lastModified;

        private PreconditionState _ifMatchState;
        private PreconditionState _ifNoneMatchState;
        private PreconditionState _ifModifiedSinceState;
        private PreconditionState _ifUnmodifiedSinceState;

        private RequestType _requestType;

        public StorageFileContext(HttpContext context, StorageFileOptions options, ILogger logger,
            StorageRecord record,
            string contentType, PathString subPath)
        {
            _context = context;
            _options = options;
            _request = context.Request;
            _response = context.Response;
            _logger = logger;
            _record = record;
            string method = _request.Method;
            _contentType = contentType;
            _etag = null;
            _requestHeaders = null;
            _responseHeaders = null;
            _range = null;

            _length = record.FileSize;
            _subPath = subPath;
            _lastModified = new DateTimeOffset();
            _ifMatchState = PreconditionState.Unspecified;
            _ifNoneMatchState = PreconditionState.Unspecified;
            _ifModifiedSinceState = PreconditionState.Unspecified;
            _ifUnmodifiedSinceState = PreconditionState.Unspecified;

            if (HttpMethods.IsGet(method))
            {
                _requestType = RequestType.IsGet;
            }
            else if (HttpMethods.IsHead(method))
            {
                _requestType = RequestType.IsHead;
            }
            else
            {
                _requestType = RequestType.Unspecified;
            }
        }

        private RequestHeaders RequestHeaders => _requestHeaders ??= _request.GetTypedHeaders();

        private ResponseHeaders ResponseHeaders => _responseHeaders ??= _response.GetTypedHeaders();

        public bool IsHeadMethod => _requestType.HasFlag(RequestType.IsHead);

        public bool IsGetMethod => _requestType.HasFlag(RequestType.IsGet);

        public bool IsRangeRequest
        {
            get => _requestType.HasFlag(RequestType.IsRange);
            private set
            {
                if (value)
                {
                    _requestType |= RequestType.IsRange;
                }
                else
                {
                    _requestType &= ~RequestType.IsRange;
                }
            }
        }

        public string SubPath => _subPath.Value;

        public bool LookupFileInfo()
        {
            _length = _record.FileSize;

            DateTimeOffset last = _record.LastModified;
            // Truncate to the second.
            _lastModified =
                new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second,
                    last.Offset).ToUniversalTime();

            long etagHash = _lastModified.ToFileTime() ^ _length;
            _etag = new EntityTagHeaderValue('\"' + Convert.ToString(etagHash, 16) + '\"');
            return true;
        }

        public void ComprehendRequestHeaders()
        {
            ComputeIfMatch();

            ComputeIfModifiedSince();

            ComputeRange();

            ComputeIfRange();
        }

        private void ComputeIfMatch()
        {
            var requestHeaders = RequestHeaders;

            // 14.24 If-Match
            var ifMatch = requestHeaders.IfMatch;
            if (ifMatch?.Count > 0)
            {
                _ifMatchState = PreconditionState.PreconditionFailed;
                foreach (var etag in ifMatch)
                {
                    if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(_etag, true))
                    {
                        _ifMatchState = PreconditionState.ShouldProcess;
                        break;
                    }
                }
            }

            // 14.26 If-None-Match
            var ifNoneMatch = requestHeaders.IfNoneMatch;
            if (ifNoneMatch?.Count > 0)
            {
                _ifNoneMatchState = PreconditionState.ShouldProcess;
                foreach (var etag in ifNoneMatch)
                {
                    if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(_etag, useStrongComparison: true))
                    {
                        _ifNoneMatchState = PreconditionState.NotModified;
                        break;
                    }
                }
            }
        }

        private void ComputeIfModifiedSince()
        {
            var requestHeaders = RequestHeaders;
            var now = DateTimeOffset.UtcNow;

            // 14.25 If-Modified-Since
            var ifModifiedSince = requestHeaders.IfModifiedSince;
            if (ifModifiedSince.HasValue && ifModifiedSince <= now)
            {
                bool modified = ifModifiedSince < _lastModified;
                _ifModifiedSinceState = modified ? PreconditionState.ShouldProcess : PreconditionState.NotModified;
            }

            // 14.28 If-Unmodified-Since
            var ifUnmodifiedSince = requestHeaders.IfUnmodifiedSince;
            if (ifUnmodifiedSince.HasValue && ifUnmodifiedSince <= now)
            {
                bool unmodified = ifUnmodifiedSince >= _lastModified;
                _ifUnmodifiedSinceState =
                    unmodified ? PreconditionState.ShouldProcess : PreconditionState.PreconditionFailed;
            }
        }

        private void ComputeIfRange()
        {
            // 14.27 If-Range
            var ifRangeHeader = RequestHeaders.IfRange;
            if (ifRangeHeader != null)
            {
                // If the validator given in the If-Range header field matches the
                // current validator for the selected representation of the target
                // resource, then the server SHOULD process the Range header field as
                // requested.  If the validator does not match, the server MUST ignore
                // the Range header field.
                if (ifRangeHeader.LastModified.HasValue)
                {
                    if (_lastModified > ifRangeHeader.LastModified)
                    {
                        IsRangeRequest = false;
                    }
                }
                else if (_etag != null && ifRangeHeader.EntityTag != null &&
                         !ifRangeHeader.EntityTag.Compare(_etag, useStrongComparison: true))
                {
                    IsRangeRequest = false;
                }
            }
        }

        private void ComputeRange()
        {
            // 14.35 Range
            // http://tools.ietf.org/html/draft-ietf-httpbis-p5-range-24

            // A server MUST ignore a Range header field received with a request method other
            // than GET.
            if (!IsGetMethod)
            {
                return;
            }

            (var isRangeRequest, var range) = RangeHelper.ParseRange(_context, RequestHeaders, _length, _logger);

            _range = range;
            IsRangeRequest = isRangeRequest;
        }

        public void ApplyResponseHeaders(int statusCode)
        {
            _response.StatusCode = statusCode;
            if (statusCode < 400)
            {
                // these headers are returned for 200, 206, and 304
                // they are not returned for 412 and 416
                if (!string.IsNullOrEmpty(_contentType))
                {
                    _response.ContentType = _contentType;
                }

                var responseHeaders = ResponseHeaders;
                responseHeaders.LastModified = _lastModified;
                responseHeaders.ETag = _etag;
                responseHeaders.Headers[HeaderNames.AcceptRanges] = "bytes";
            }

            if (statusCode == StatusCodes.Status200OK)
            {
                // this header is only returned here for 200
                // it already set to the returned range for 206
                // it is not returned for 304, 412, and 416
                _response.ContentLength = _length;
            }

            _options?.OnPrepareResponse(new StorageFileResponseContext(_context, _record));
        }

        public PreconditionState GetPreconditionState()
            => GetMaxPreconditionState(_ifMatchState, _ifNoneMatchState, _ifModifiedSinceState,
                _ifUnmodifiedSinceState);

        private static PreconditionState GetMaxPreconditionState(params PreconditionState[] states)
        {
            PreconditionState max = PreconditionState.Unspecified;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] > max)
                {
                    max = states[i];
                }
            }

            return max;
        }

        public Task SendStatusAsync(int statusCode)
        {
            ApplyResponseHeaders(statusCode);

            return Task.CompletedTask;
        }

        public async Task ServeStaticFile(HttpContext context, RequestDelegate next)
        {
            ComprehendRequestHeaders();
            switch (GetPreconditionState())
            {
                case PreconditionState.Unspecified:
                case PreconditionState.ShouldProcess:
                    if (IsHeadMethod)
                    {
                        await SendStatusAsync(StatusCodes.Status200OK);
                        return;
                    }

                    try
                    {
                        if (IsRangeRequest)
                        {
                            await SendRangeAsync();
                            return;
                        }

                        await SendAsync();
                        _logger.LogDebug("File {Path} served", SubPath);
                        return;
                    }
                    catch (FileNotFoundException)
                    {
                        context.Response.Clear();
                    }

                    await next(context);
                    return;
                case PreconditionState.NotModified:
                    _logger.LogDebug("File {Path} not modified", SubPath);
                    await SendStatusAsync(StatusCodes.Status304NotModified);
                    return;
                case PreconditionState.PreconditionFailed:
                    _logger.LogDebug("File {Path} precondition failed", SubPath);
                    await SendStatusAsync(StatusCodes.Status412PreconditionFailed);
                    return;
                default:
                    var exception = new NotImplementedException(GetPreconditionState().ToString());
                    Debug.Fail(exception.ToString());
                    throw exception;
            }
        }

        public async Task SendAsync()
        {
            SetCompressionMode();
            ApplyResponseHeaders(StatusCodes.Status200OK);

            var sendFile = _context.Features.Get<IHttpResponseBodyFeature>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (sendFile != null && !string.IsNullOrEmpty(_record.PhysicalPath))
            {
                await sendFile.SendFileAsync(_record.PhysicalPath, 0, _length, CancellationToken.None);
                return;
            }

            try
            {
                await using var readStream = _record.OpenRead();
                // Larger StreamCopyBufferSize is required because in case of FileStream readStream isn't going to be buffering
                await StreamCopyOperation.CopyToAsync(readStream, _response.Body, _length, StreamCopyBufferSize,
                    _context.RequestAborted);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "File {Path} write cancelled", SubPath);
                // Don't throw this exception, it's most likely caused by the client disconnecting.
                // However, if it was cancelled for any other reason we need to prevent empty responses.
                _context.Abort();
            }
        }

        // When there is only a single range the bytes are sent directly in the body.
        internal async Task SendRangeAsync()
        {
            if (_range == null)
            {
                // 14.16 Content-Range - A server sending a response with status code 416 (Requested range not satisfiable)
                // SHOULD include a Content-Range field with a byte-range-resp-spec of "*". The instance-length specifies
                // the current length of the selected resource.  e.g. */length
                ResponseHeaders.ContentRange = new ContentRangeHeaderValue(_length);
                ApplyResponseHeaders(StatusCodes.Status416RangeNotSatisfiable);

                _logger.LogError("File {Path} range not satisfiable", SubPath);
                return;
            }

            ResponseHeaders.ContentRange = ComputeContentRange(_range, out var start, out var length);
            _response.ContentLength = length;
            SetCompressionMode();
            ApplyResponseHeaders(StatusCodes.Status206PartialContent);

            var sendFile = _context.Features.Get<IHttpResponseBodyFeature>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (sendFile != null && !string.IsNullOrEmpty(_record.PhysicalPath))
            {
                await sendFile.SendFileAsync(_record.PhysicalPath, start, length, CancellationToken.None);
                return;
            }

            try
            {
                await using var readStream = _record.OpenRead();
                if (readStream == null)
                {
                    throw new Exception("Empty strem");
                }

                if (!readStream.CanSeek)
                {
                    await using var memoryStream = new MemoryStream();
                    await StreamCopyOperation.CopyToAsync(readStream, memoryStream, _length, _context.RequestAborted);
                    await SendRangeAsync(memoryStream, start, length);
                }
                else
                {
                    await SendRangeAsync(readStream, start, length);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "File {Path} write cancelled", SubPath);
                // Don't throw this exception, it's most likely caused by the client disconnecting.
                // However, if it was cancelled for any other reason we need to prevent empty responses.
                _context.Abort();
            }
        }

        private Task SendRangeAsync(Stream stream, long start, long length)
        {
            stream.Seek(start, SeekOrigin.Begin);
            _logger.LogDebug("Copying file range {Range} for file {File}",
                _response.Headers[HeaderNames.ContentRange], SubPath);
            return StreamCopyOperation.CopyToAsync(stream, _response.Body, length, _context.RequestAborted);
        }

        // Note: This assumes ranges have been normalized to absolute byte offsets.
        private ContentRangeHeaderValue ComputeContentRange(RangeItemHeaderValue range, out long start, out long length)
        {
            // ReSharper disable once PossibleInvalidOperationException
            start = range.From.Value;
            // ReSharper disable once PossibleInvalidOperationException
            long end = range.To.Value;
            length = end - start + 1;
            return new ContentRangeHeaderValue(start, end, _length);
        }

        // Only called when we expect to serve the body.
        private void SetCompressionMode()
        {
            var responseCompressionFeature = _context.Features.Get<IHttpsCompressionFeature>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (responseCompressionFeature != null)
            {
                responseCompressionFeature.Mode = _options.HttpsCompression;
            }
        }

        public enum PreconditionState : byte
        {
            Unspecified,
            NotModified,
            ShouldProcess,
            PreconditionFailed
        }

        [Flags]
        private enum RequestType : byte
        {
            Unspecified = 0b_000,
            IsHead = 0b_001,
            IsGet = 0b_010,
            IsRange = 0b_100,
        }
    }

    public class StorageFileOptions
    {
        /// <summary>
        /// Used to map files to content-types.
        /// </summary>
        public IContentTypeProvider? ContentTypeProvider { get; set; }

        /// <summary>
        /// The default content type for a request if the ContentTypeProvider cannot determine one.
        /// None is provided by default, so the client must determine the format themselves.
        /// http://www.w3.org/Protocols/rfc2616/rfc2616-sec7.html#sec7
        /// </summary>
        public string? DefaultContentType { get; set; }

        /// <summary>
        /// If the file is not a recognized content-type should it be served?
        /// Default: false.
        /// </summary>
        public bool ServeUnknownFileTypes { get; set; }

        /// <summary>
        /// Indicates if files should be compressed for HTTPS requests when the Response Compression middleware is available.
        /// The default value is <see cref="HttpsCompressionMode.Compress"/>.
        /// </summary>
        /// <remarks>
        /// Enabling compression on HTTPS requests for remotely manipulable content may expose security problems.
        /// </remarks>
        public HttpsCompressionMode HttpsCompression { get; set; } = HttpsCompressionMode.Compress;

        /// <summary>
        /// Called after the status code and headers have been set, but before the body has been written.
        /// This can be used to add or change the response headers.
        /// </summary>
        public Action<StorageFileResponseContext>? OnPrepareResponse { get; set; }
    }
}
