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

        public readonly Style[] Lakes = new Style[] {
            new Style{ name="freshwater", opacity=0.5f, fill="#a6c1fd", stroke="#5f799d", strokeWidth=0.7f, filter=null },
            new Style{ name="salt",       opacity=0.5f, fill="#409b8a", stroke="#388985", strokeWidth=0.7f, filter=null },
            new Style{ name="sinkhole",   opacity=1.0f, fill="#5bc9fd", stroke="#53a3b0", strokeWidth=0.7f, filter=null   },
            new Style{ name="frozen",     opacity=.95f, fill="#cdd4e7", stroke="#cfe0eb", strokeWidth=0f, filter=null  },
            new Style{ name="lava",       opacity=0.7f, fill="#90270d", stroke="#f93e0c", strokeWidth=2f, filter="crumpled" },
        };

        public readonly Style[] Islands = new Style[] {
            new Style{ name="sea_island", opacity=0.5f, stroke="#1f3846", strokeWidth=0.7f, filter="dropShadow", autoFilter=true },
            new Style{ name="lake_island", opacity=1f, stroke="#7c8eaf", strokeWidth=0.35f},
        };
    }
}
