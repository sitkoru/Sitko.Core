using ImgProxy;
using JetBrains.Annotations;

namespace Sitko.Core.ImgProxy.Options;

[PublicAPI]
public class ResizeOption : ImgProxyOption
{
    public ResizeOption(string type, int width, int height, bool enlarge = false, bool extend = false)
    {
        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        Type = type;
        Width = width;
        Height = height;
        Enlarge = enlarge;
        Extend = extend;
    }

    private string Type { get; }
    private int Width { get; }
    private int Height { get; }
    private bool Enlarge { get; }
    private bool Extend { get; }

    public override string ToString()
    {
        var enlarge = Enlarge ? "1" : "0";
        var extend = Extend ? "1" : "0";

        return $"resize:{Type}:{Width}:{Height}:{enlarge}:{extend}";
    }
}

