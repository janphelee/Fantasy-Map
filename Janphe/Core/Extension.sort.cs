using System;
using System.Collections.Generic;
using System.Linq;

namespace Janphe
{
    public static partial class Extension
    {

        // 哇，最快稳定排序是这个
        // 最好直接用IOrderedEnumerable 不要转成Array或者List，会降低效率
        private class TComparer<T> : IComparer<T> { public Comparison<T> func { get; set; } public int Compare(T x, T y) => func(x, y); }
        public static IOrderedEnumerable<T> sort<T>(this IList<T> d, Comparison<T> comparison)
        { return d.OrderBy(x => x, new TComparer<T>() { func = comparison }); }
        public static IOrderedEnumerable<T> sort<T>(this IEnumerable<T> d, Comparison<T> comparison)
        { return d.OrderBy(x => x, new TComparer<T>() { func = comparison }); }

        public static T[] SortTim<T>(this T[] d, Comparison<T> comparison)
        {
            d.TimSort(comparison);
            return d;
        }

        public static T[] SortBubble<T>(this T[] arr, Comparison<T> comparison)
        {
            var len = arr.Length;
            for (var i = 0; i < len; i++)
            {
                for (var j = 0; j < len - 1 - i; j++)
                {
                    if (comparison(arr[j], arr[j + 1]) > 0)
                    {
                        var tmp = arr[j + 1];
                        arr[j + 1] = arr[j];
                        arr[j + 0] = tmp;
                    }
                }
            }
            return arr;
        }

        public static T[] SortInsertion<T>(this T[] arr, Comparison<T> comparison)
        {
            for (var i = 1; i < arr.Length; i++)
            {
                var element = arr[i];
                var j = i - 1;
                for (; j >= 0; j--)
                {
                    var tmp = arr[j];
                    if (comparison(element, tmp) < 0)
                    {
                        arr[j + 1] = tmp;
                    }
                    else
                    {
                        break;
                    }
                }
                arr[j + 1] = element;
            }

            return arr;
        }

        public static T[] SortSelection<T>(this T[] arr, Comparison<T> comparison)
        {
            var len = arr.Length;
            for (var i = 0; i < len; i++)
            {
                var min = i;
                for (var j = i + 1; j < len; j++)
                {
                    if (comparison(arr[j], arr[min]) < 0)
                    {
                        min = j;
                    }
                }
                if (min != i)
                {
                    var tmp = arr[min];
                    arr[min] = arr[i];
                    arr[i] = tmp;
                }
            }
            return arr;
        }

    }
}
