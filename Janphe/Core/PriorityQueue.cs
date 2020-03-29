using System;
using System.Collections.Generic;

namespace Janphe
{
    public class PriorityQueue<T>
    {
        private List<T> data;
        private Comparison<T> __compare;

        public int Count => data.Count;


        public PriorityQueue(Comparison<T> comparison)
        {
            data = new List<T>();
            __compare = comparison;
        }

        private void _bubbleUp(int pos)
        {
            while (pos > 0)
            {
                var parent = (pos - 1) >> 1;
                if (__compare(data[pos], data[parent]) < 0)
                {
                    var x = data[parent]; data[parent] = data[pos]; data[pos] = x;
                    pos = parent;
                }
                else
                    break;
            }
        }

        public void push(T value)
        {
            data.Add(value);
            _bubbleUp(data.Count - 1);
        }

        private void _bubbleDown(int pos)
        {
            var last = data.Count - 1;
            while (true)
            {
                var left = (pos << 1) + 1;
                var right = left + 1;
                var minIndex = pos;
                if (left <= last && __compare(data[left], data[minIndex]) < 0)
                    minIndex = left;
                if (right <= last && __compare(data[right], data[minIndex]) < 0)
                    minIndex = right;

                if (minIndex != pos)
                {
                    var x = data[minIndex]; data[minIndex] = data[pos]; data[pos] = x;
                    pos = minIndex;
                }
                else
                    break;
            }
        }

        public T pop()
        {
            var ret = data[0];

            var length = data.Count - 1;
            var last = data[length]; data.RemoveAt(length);

            if (length > 0)
            {
                data[0] = last;
                _bubbleDown(0);
            }

            return ret;
        }

    }
}
