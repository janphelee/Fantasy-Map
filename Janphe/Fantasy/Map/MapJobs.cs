using System;

namespace Janphe.Fantasy.Map
{
    internal partial class MapJobs : JobThread, IDisposable
    {
        public class Coordinates
        {
            public double latT;
            public double latN;
            public double latS;
            public double lonT;
            public double lonW;
            public double lonE;
        }

        public MapJobs()
        {
            initOptions();
            initFonts();
            initLayers();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
