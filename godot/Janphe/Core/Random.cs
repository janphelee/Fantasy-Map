using System;
using System.Security.Cryptography;
using System.Text;

namespace Janphe
{
    public class Random
    {
        private static Rander random { get; set; } = new Rander(0);

        private static int GetRandomSeed()
        {
            byte[] bytes = new byte[4];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static void Seed(int seed)
        {
            random = new Rander(seed);
        }

        public static void Seed(string seed)
        {
            if (int.TryParse(seed, out var ret))
            {
                Seed(ret);
                return;
            }

            var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
            Seed(BitConverter.ToInt32(bytes, 0));
        }

        public static int Next()
        {
            return random.Next();
        }
        public static int Next(int maxValue)
        {
            return random.Next(maxValue);
        }
        public static int Next(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }
        public static double NextDouble()
        {
            return random.NextDouble();
        }
    }
}
