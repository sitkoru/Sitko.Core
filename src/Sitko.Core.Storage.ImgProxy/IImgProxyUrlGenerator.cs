namespace Sitko.Core.Storage.ImgProxy
{
    public interface IImgProxyUrlGenerator<TStorageOptions> where TStorageOptions : StorageOptions
    {
        /// <summary>
        /// Generate url for resized image
        /// </summary>
        /// <param name="item">StorageItem image to resize</param>
        /// <param name="width">Resize width to</param>
        /// <param name="height">Resize height to</param>
        /// <param name="type">Resize type - auto, fit, fill. Default is auto</param>
        /// <param name="enlarge">Allow to enlarge image. Default is false</param>
        /// <returns></returns>
        string Resize(StorageItem item, int width, int height, string type = "auto", bool enlarge = false);
    }
}
