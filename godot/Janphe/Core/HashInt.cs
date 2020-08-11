/**
 * Thomas Wang's Original Homepage (now down):  http://www.cris.com/~Ttwang/tech/inthash.htm
 * Bob Jenkins' Write Up: http://burtleburtle.net/bob/hash/integer.html
 */

namespace Janphe
{
    public static class HashInt
    {
        public static int move(this int x, int right)
        {
            int mask = 0x7fffffff; //Integer.MAX_VALUE
            for (int i = 0; i < right; i++)
            {
                x >>= 1;
                x &= mask;
            }
            return x;
        }

        public static int hash(this int x)
        {
            var AA = x | 0;
            AA -= (AA << 6);
            AA ^= AA.move(17);
            AA -= (AA << 9);
            AA ^= (AA << 4);
            AA -= (AA << 3);
            AA ^= (AA << 10);
            AA ^= AA.move(15);
            //return AA & int.MaxValue;
            return AA;
        }
    }
}
