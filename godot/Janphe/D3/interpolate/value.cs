using System;

namespace Janphe
{
    public partial class D3
    {
        public static Func<double, Color> interpolate(string a, string b)
        {
            return interpolateRgb()(a.ToColor(), b.ToColor());
        }
    }
}
