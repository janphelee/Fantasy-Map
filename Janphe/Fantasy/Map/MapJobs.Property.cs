using System;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        public Coordinates mapCoordinates { get; set; }

        public Grid grid { get; private set; }
        public Grid pack { get; private set; }


        public Biomes biomesData { get; set; }
        public NamesGenerator Names { get; set; }
        public NamesGenerator.Names[] nameBases { get; set; }
        public Map6Routes Routes { get; set; }

    }
}
