using System;
using ImgProxy;

namespace Sitko.Core.Storage.ImgProxy
{
    public interface IImgProxyUrlGenerator<TStorageOptions> where TStorageOptions : StorageOptions
    {
        /// <summary>
        /// Generate url for optimized image
        /// </summary>
        /// <param name="item">StorageItem to optimize</param>
        /// <returns>Url to optimized image</returns>
        string Url(StorageItem item);

        /// <summary>
        /// Generate url for image with specified format
        /// </summary>
        /// <param name="item">StorageItem to display</param>
        /// <param name="format">Format</param>
        /// <returns>Url to image with format</returns>
        string Format(StorageItem item, string format);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="preset"></param>
        /// <returns>Url to image with preset</returns>
        string Preset(StorageItem item, string preset);

        /// <summary>
        /// Build url to image with options
        /// </summary>
        /// <param name="item">StorageItem to display</param>
        /// <param name="build">Action to configure ImgProxy options</param>
        /// <returns>Url to image</returns>
        string Build(StorageItem item, Action<ImgProxyBuilder> build);

        /// <summary>
        /// Generate url for resized image
        /// </summary>
        /// <param name="item">StorageItem image to resize</param>
        /// <param name="width">Resize width to</param>
        /// <param name="height">Resize height to</param>
        /// <param name="type">Resize type - auto, fit, fill. Default is auto</param>
        /// <param name="enlarge">Allow to enlarge image. Default is false</param>
        /// <returns>Url to resized image</returns>
        string Resize(StorageItem item, int width, int height, string type = "auto", bool enlarge = false);
    }
}
