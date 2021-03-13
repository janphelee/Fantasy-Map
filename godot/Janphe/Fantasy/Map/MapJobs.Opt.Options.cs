using Newtonsoft.Json.Linq;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        public Options Options { get; set; }

        private void initOptions()
        {
            Options = new Options();
        }

        public string Get_Options() => Options.GetOptions();

        public JObject Get_On_Options() => Options.ToJson();

        public void On_Options_Toggled(JObject obj) => Options.FromJson(obj);
    }
}
