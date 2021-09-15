using System;
using ImgProxy;
using JetBrains.Annotations;

namespace Sitko.Core.ImgProxy.Options
{
    [PublicAPI]
    public class BlurOption : ImgProxyOption
    {
        private float Sigma { get; }

        public BlurOption(float sigma)
        {
            if (sigma < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sigma));
            }

            Sigma = sigma;
        }

        public override string ToString() => $"blur:{Sigma}";
    }
}
