using System;
using ImgProxy;

namespace Sitko.Core.ImgProxy
{
    // ReSharper disable once UnusedTypeParameter
    public interface IImgProxyUrlGenerator
    {
        /// <summary>
        /// Generate url for optimized image
        /// </summary>
        /// <param name="url">Image url to optimize</param>
        /// <returns>Url to optimized image</returns>
        string Url(string url);

        /// <summary>
        /// Generate url for image with specified format
        /// </summary>
        /// <param name="url">Image url to display</param>
        /// <param name="format">Format</param>
        /// <returns>Url to image with format</returns>
        string Format(string url, string format);

        /// <summary>
        ///
        /// </summary>
        /// <param name="url">Image url</param>
        /// <param name="preset">Preset name</param>
        /// <returns>Url to image with preset</returns>
        string Preset(string url, string preset);

        /// <summary>
        /// Build url to image with options
        /// </summary>
        /// <param name="url">Image url to display</param>
        /// <param name="build">Action to configure ImgProxy options</param>
        /// <returns>Url to image</returns>
        string Build(string url, Action<ImgProxyBuilder> build);

        /// <summary>
        /// Generate url for resized image
        /// </summary>
        /// <param name="url">Image url to resize</param>
        /// <param name="width">Resize width to</param>
        /// <param name="height">Resize height to</param>
        /// <param name="type">Resize type - auto, fit, fill. Default is auto</param>
        /// <param name="enlarge">Allow to enlarge image. Default is false</param>
        /// <param name="extend"></param>
        /// <returns>Url to resized image</returns>
        string Resize(string url, int width, int height, string type = "auto", bool enlarge = false,
            bool extend = false);
    }
}
