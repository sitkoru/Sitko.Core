using System;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.Storage.Proxy.StaticFiles
{
    public class StorageFileResponseContext
    {
        /// <summary>
        /// Constructs the <see cref="StorageFileResponseContext"/>.
        /// </summary>
        /// <param name="context">The request and response information.</param>
        /// <param name="file">The file to be served.</param>
        public StorageFileResponseContext(HttpContext context, StorageItem file)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            File = file;
        }

        /// <summary>
        /// The request and response information.
        /// </summary>
        public HttpContext Context { get; }

        /// <summary>
        /// The file to be served.
        /// </summary>
        public StorageItem File { get; }
    }
}
