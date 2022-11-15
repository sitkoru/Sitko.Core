using ImgProxy;
using JetBrains.Annotations;

namespace Sitko.Core.ImgProxy.Options;

[PublicAPI]
public class BlurOption : ImgProxyOption
{
    public BlurOption(float sigma)
    {
        if (sigma < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sigma));
        }

        Sigma = sigma;
    }

    private float Sigma { get; }

    public override string ToString() => $"blur:{Sigma}";
}

