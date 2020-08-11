using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Janphe
{
    public static partial class Extension
    {
        public static Dictionary<string, T> dict<T>(this JToken d)
        {
            var dic = new Dictionary<string, T>();
            var obj = d.Value<JObject>();
            foreach (var kv in obj) dic.Add(kv.Key, kv.Value.Value<T>());
            return dic;
        }
        public static Dictionary<string, int> dict(this JToken d) { return dict<int>(d); }

        public static IList<string> ww(this Dictionary<string, int> d)
        {
            var ak = new List<string>();
            foreach (var kv in d) for (var i = 0; i < kv.Value; ++i) ak.Add(kv.Key);
            return ak;
        }
        public static string rw(this Dictionary<string, int> d)
        {
            var array = ww(d);
            return array[(int)Math.Floor(Random.NextDouble() * array.Count)];
        }
    }
}
