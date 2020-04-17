using System;
using System.Collections.Generic;

namespace Janphe
{
    public class TinyQueue<T>
    {
        private List<T> data;
        private uint length;
        private Comparison<T> compare;

        public uint Length => length;

        public TinyQueue(IList<T> d, Comparison<T> comparison)
        {
            data = new List<T>();
            length = 0;
            compare = comparison;

            if (d != null)
                d.forEach(t => push(t));
        }

        public void push(T item)
        {
            data.push(item);
            length++;
            _up((int)length - 1);
        }

        public T pop()
        {
            if (length == 0)
                return default(T);

            var top = data[0];
            var bottom = data.pop();
            length--;

            if (length > 0)
            {
                data[0] = bottom;
                _down(0);
            }

            return top;
        }

        public T peak() { return data[0]; }

        private void _up(int pos)
        {
            var item = data[pos];

            while (pos > 0)
            {
                var parent = (pos - 1) >> 1;
                var current = data[parent];
                if (compare(item, current) >= 0)
                    break;
                data[pos] = current;
                pos = parent;
            }

            data[pos] = item;
        }

        private void _down(int pos)
        {
            var halfLength = length >> 1;
            var item = data[pos];

            while (pos < halfLength)
            {
                var left = (pos << 1) + 1;
                var best = data[left];
                var right = left + 1;

                if (right < length && compare(data[right], best) < 0)
                {
                    left = right;
                    best = data[right];
                }
                if (compare(best, item) >= 0)
                    break;

                data[pos] = best;
                pos = left;
            }

            data[pos] = item;
        }
    }
}
