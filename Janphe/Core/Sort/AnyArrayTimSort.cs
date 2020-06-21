using System;

namespace Janphe.Core.Sort
{
    using Debug = System.Diagnostics.Debug;

    internal class AnyArrayTimSort<T>
    {
        protected const int MIN_MERGE = 32;
        protected const int MIN_GALLOP = 7;
        protected const int INITIAL_TMP_STORAGE_LENGTH = 256;


        protected readonly T[] _array;
        protected readonly int _arrayLength;
        protected int _minGallop = MIN_GALLOP;
        protected T[] _mergeBuffer;
        protected int _stackSize; // = 0; // Number of pending runs on stack
        protected int[] _runBase;
        protected int[] _runLength;


        protected AnyArrayTimSort(T[] array, int arrayLength)
        {
            _array = array;
            _arrayLength = arrayLength;

            // Allocate temp storage (which may be increased later if necessary)
            int mergeBufferLength =
                arrayLength < 2 * INITIAL_TMP_STORAGE_LENGTH
                ? arrayLength >> 1
                : INITIAL_TMP_STORAGE_LENGTH;
            _mergeBuffer = new T[mergeBufferLength];

            // Allocate runs-to-be-merged stack (which cannot be expanded).  The
            // stack length requirements are described in listsort.txt.  The C
            // version always uses the same stack length (85), but this was
            // measured to be too expensive when sorting "mid-sized" arrays (e.g.,
            // 100 elements) in Java.  Therefore, we use smaller (but sufficiently
            // large) stack lengths for smaller arrays.  The "magic numbers" in the
            // computation below must be changed if MIN_MERGE is decreased.  See
            // the MIN_MERGE declaration above for more information.
            int stackLength =
                arrayLength < 120 ? 5 :
                arrayLength < 1542 ? 10 :
                arrayLength < 119151 ? 19 :
                40;
            _runBase = new int[stackLength];
            _runLength = new int[stackLength];
        }

        protected static int GetMinimumRunLength(int n)
        {
            Debug.Assert(n >= 0);
            int r = 0; // Becomes 1 if any 1 bits are shifted off
            while (n >= MIN_MERGE)
            {
                r |= (n & 1);
                n >>= 1;
            }
            return n + r;
        }

        protected void MergeCollapse()
        {
            while (_stackSize > 1)
            {
                var n = _stackSize - 2;

                if (n > 0 && _runLength[n - 1] <= _runLength[n] + _runLength[n + 1])
                {
                    if (_runLength[n - 1] < _runLength[n + 1])
                        n--;
                    MergeAt(n);
                }
                else if (_runLength[n] <= _runLength[n + 1])
                {
                    MergeAt(n);
                }
                else
                {
                    break; // Invariant is established
                }
            }
        }

        protected void MergeForceCollapse()
        {
            while (_stackSize > 1)
            {
                var n = _stackSize - 2;
                if (n > 0 && _runLength[n - 1] < _runLength[n + 1])
                    n--;
                MergeAt(n);
            }
        }

        protected void PushRun(int runBase, int runLength)
        {
            _runBase[_stackSize] = runBase;
            _runLength[_stackSize] = runLength;
            _stackSize++;
        }

        protected T[] EnsureCapacity(int minCapacity)
        {
            if (_mergeBuffer.Length < minCapacity)
            {
                // Compute smallest power of 2 > minCapacity
                int newSize = minCapacity;
                newSize |= newSize >> 1;
                newSize |= newSize >> 2;
                newSize |= newSize >> 4;
                newSize |= newSize >> 8;
                newSize |= newSize >> 16;
                newSize++;

                newSize = newSize < 0 ? minCapacity : Math.Min(newSize, _arrayLength >> 1);

                _mergeBuffer = new T[newSize];
            }
            return _mergeBuffer;
        }

        protected static void CheckRange(int arrayLen, int fromIndex, int toIndex)
        {
            if (fromIndex > toIndex)
                throw new ArgumentException(string.Format("fromIndex({0}) > toIndex({1})", fromIndex, toIndex));
            if (fromIndex < 0)
                throw new IndexOutOfRangeException(string.Format("fromIndex ({0}) is out of bounds", fromIndex));
            if (toIndex > arrayLen)
                throw new IndexOutOfRangeException(string.Format("toIndex ({0}) is out of bounds", toIndex));
        }

        protected static void ArrayReverseRange(T[] array, int lo, int hi)
        {
            Array.Reverse(array, lo, hi - lo);
        }

        protected static void ArrayCopyRange(T[] buffer, int sourceIndex, int targetIndex, int length)
        {
            Array.Copy(buffer, sourceIndex, buffer, targetIndex, length);
        }

        protected static void ArrayCopyRange(T[] source, int sourceIndex, T[] target, int targetIndex, int length)
        {
            Array.Copy(source, sourceIndex, target, targetIndex, length);
        }

        private readonly Comparison<T> _comparer;

        private AnyArrayTimSort(T[] array, Comparison<T> comparer)
            : this(array, array.Length)
        {
            _comparer = comparer;
        }

        public static void Sort(T[] array, Comparison<T> comparer)
        {
            Sort(array, 0, array.Length, comparer);
        }

        public static void Sort(T[] array, int lo, int hi, Comparison<T> comparer)
        {
            CheckRange(array.Length, lo, hi);

            var width = hi - lo;
            if (width < 2)
                return; // Arrays of size 0 and 1 are always sorted

            // If array is small, do a "mini-TimSort" with no merges
            if (width < MIN_MERGE)
            {
                var initRunLength = CountRunAndMakeAscending(array, lo, hi, comparer);
                BinarySort(array, lo, hi, lo + initRunLength, comparer);
                return;
            }

            // March over the array once, left to right, finding natural runs,
            // extending short natural runs to minRun elements, and merging runs
            // to maintain stack invariant.
            var sorter = new AnyArrayTimSort<T>(array, comparer);
            int minRun = GetMinimumRunLength(width);
            do
            {
                // Identify next run
                var runLen = CountRunAndMakeAscending(array, lo, hi, comparer);

                // If run is short, extend to min(minRun, nRemaining)
                if (runLen < minRun)
                {
                    var force = width <= minRun ? width : minRun;
                    BinarySort(array, lo, lo + force, lo + runLen, comparer);
                    runLen = force;
                }

                // Push run onto pending-run stack, and maybe merge
                sorter.PushRun(lo, runLen);
                sorter.MergeCollapse();

                // Advance to find next run
                lo += runLen;
                width -= runLen;
            } while (width != 0);

            // Merge all remaining runs to complete sort
            Debug.Assert(lo == hi);
            sorter.MergeForceCollapse();
            Debug.Assert(sorter._stackSize == 1);
        }

        private static void BinarySort(T[] array, int lo, int hi, int start, Comparison<T> comparer)
        {
            var a = array;
            { // fixed (...)
                Debug.Assert(lo <= start && start <= hi);

                if (start == lo)
                    start++;

                for (/* nothing */; start < hi; start++)
                {
                    var pivot = a[start];

                    // Set left (and right) to the index where a[start] (pivot) belongs
                    var left = lo;
                    var right = start;
                    Debug.Assert(left <= right);

                    // Invariants:
                    // * pivot >= all in [lo, left).
                    // * pivot < all in [right, start).
                    while (left < right)
                    {
                        var mid = (left + right) >> 1;
                        if (comparer(pivot, a[mid]) < 0) // c(pivot, a[mid]) < 0
                        {
                            right = mid;
                        }
                        else
                        {
                            left = mid + 1;
                        }
                    }
                    Debug.Assert(left == right);

                    // The invariants still hold: pivot >= all in [lo, left) and
                    // pivot < all in [left, start), so pivot belongs at left.  Note
                    // that if there are elements equal to pivot, left points to the
                    // first slot after them -- that's why this sort is stable.
                    // Slide elements over to make room to make room for pivot.

                    var n = start - left; // The number of elements to move

                    // switch is just an optimization for copyRange in default case
                    switch (n)
                    {
                        case 2:
                            a[left + 2] = a[left + 1];
                            a[left + 1] = a[left];
                            break;
                        case 1:
                            a[left + 1] = a[left];
                            break;
                        default:
                            ArrayCopyRange(a, left, left + 1, n);
                            break;
                    }
                    a[left] = pivot;
                }
            } // fixed (...)
        }

        private static int CountRunAndMakeAscending(T[] array, int lo, int hi, Comparison<T> comparer)
        {
            var a = array;
            { // fixed (...)
                Debug.Assert(lo < hi);
                var runHi = lo + 1;
                if (runHi == hi)
                    return 1;

                // Find end of run, and reverse range if descending
                if (comparer(a[runHi++], a[lo]) < 0) // c(a[runHi++], a[lo]) < 0
                {
                    // Descending
                    while (runHi < hi && comparer(a[runHi], a[runHi - 1]) < 0)
                        runHi++;
                    ArrayReverseRange(a, lo, runHi);
                }
                else
                {
                    // Ascending
                    while (runHi < hi && comparer(a[runHi], a[runHi - 1]) >= 0)
                        runHi++; // c(a[runHi], a[runHi - 1]) >= 0
                }

                return runHi - lo;
            } // fixed (...)
        }

        protected void MergeAt(int runIndex)
        {
            Debug.Assert(_stackSize >= 2);
            Debug.Assert(runIndex >= 0);
            Debug.Assert(runIndex == _stackSize - 2 || runIndex == _stackSize - 3);

            var comparer = _comparer;
            var base1 = _runBase[runIndex];
            var len1 = _runLength[runIndex];
            var base2 = _runBase[runIndex + 1];
            var len2 = _runLength[runIndex + 1];
            Debug.Assert(len1 > 0 && len2 > 0);
            Debug.Assert(base1 + len1 == base2);

            // Record the length of the combined runs; if i is the 3rd-last
            // run now, also slide over the last run (which isn't involved
            // in this merge). The current run (i+1) goes away in any case.
            _runLength[runIndex] = len1 + len2;
            if (runIndex == _stackSize - 3)
            {
                _runBase[runIndex + 1] = _runBase[runIndex + 2];
                _runLength[runIndex + 1] = _runLength[runIndex + 2];
            }
            _stackSize--;

            // Find where the first element of run2 goes in run1. Prior elements
            // in run1 can be ignored (because they're already in place).
            var k = GallopRight(_array[base2], _array, base1, len1, 0, comparer);
            Debug.Assert(k >= 0);
            base1 += k;
            len1 -= k;
            if (len1 == 0)
                return;

            // Find where the last element of run1 goes in run2. Subsequent elements
            // in run2 can be ignored (because they're already in place).
            len2 = GallopLeft(_array[base1 + len1 - 1], _array, base2, len2, len2 - 1, comparer);
            Debug.Assert(len2 >= 0);
            if (len2 == 0)
                return;

            // Merge remaining runs, using tmp array with min(len1, len2) elements
            if (len1 <= len2)
                MergeLo(base1, len1, base2, len2);
            else
                MergeHi(base1, len1, base2, len2);
        }

        internal static int GallopLeft(T key, T[] array, int lo, int length, int hint, Comparison<T> comparer)
        {
            var a = array;
            { // fixed (...)
                Debug.Assert(length > 0 && hint >= 0 && hint < length);
                var lastOfs = 0;
                var ofs = 1;

                if (comparer(key, a[lo + hint]) > 0) // comparer(key, a[lo + hint]) > 0
                {
                    // Gallop right until a[base+hint+lastOfs] < key <= a[base+hint+ofs]
                    var maxOfs = length - hint;
                    while (ofs < maxOfs && comparer(key, a[lo + hint + ofs]) > 0) // comparer(key, a[lo + hint + ofs]) > 0
                    {
                        lastOfs = ofs;
                        ofs = (ofs << 1) + 1;
                        if (ofs <= 0)   // int overflow
                            ofs = maxOfs;
                    }
                    if (ofs > maxOfs)
                        ofs = maxOfs;

                    // Make offsets relative to base
                    lastOfs += hint;
                    ofs += hint;
                }
                else // if (key <= a[base + hint])
                {
                    // Gallop left until a[base+hint-ofs] < key <= a[base+hint-lastOfs]
                    var maxOfs = hint + 1;
                    while (ofs < maxOfs && comparer(key, a[lo + hint - ofs]) <= 0) // comparer(key, a[lo + hint - ofs]) <= 0
                    {
                        lastOfs = ofs;
                        ofs = (ofs << 1) + 1;
                        if (ofs <= 0) // int overflow
                            ofs = maxOfs;
                    }
                    if (ofs > maxOfs)
                        ofs = maxOfs;

                    // Make offsets relative to base
                    var tmp = lastOfs;
                    lastOfs = hint - ofs;
                    ofs = hint - tmp;
                }
                Debug.Assert(-1 <= lastOfs && lastOfs < ofs && ofs <= length);

                // Now a[base+lastOfs] < key <= a[base+ofs], so key belongs somewhere
                // to the right of lastOfs but no farther right than ofs.  Do a binary
                // search, with invariant a[base + lastOfs - 1] < key <= a[base + ofs].
                lastOfs++;
                while (lastOfs < ofs)
                {
                    var m = lastOfs + ((ofs - lastOfs) >> 1);

                    if (comparer(key, a[lo + m]) > 0) // comparer(key, a[lo + m]) > 0
                        lastOfs = m + 1; // a[base + m] < key
                    else
                        ofs = m; // key <= a[base + m]
                }
                Debug.Assert(lastOfs == ofs); // so a[base + ofs - 1] < key <= a[base + ofs]
                return ofs;
            } // fixed (...)
        }

        internal static int GallopRight(T key, T[] array, int lo, int length, int hint, Comparison<T> comparer)
        {
            var a = array;
            {
                Debug.Assert(length > 0 && hint >= 0 && hint < length);

                var ofs = 1;
                var lastOfs = 0;
                if (comparer(key, a[lo + hint]) < 0) // comparer(key, a[lo + hint]) < 0
                {
                    // Gallop left until a[b+hint - ofs] <= key < a[b+hint - lastOfs]
                    var maxOfs = hint + 1;
                    while (ofs < maxOfs && comparer(key, a[lo + hint - ofs]) < 0)
                    {
                        lastOfs = ofs;
                        ofs = (ofs << 1) + 1;
                        if (ofs <= 0)   // int overflow
                            ofs = maxOfs;
                    }
                    if (ofs > maxOfs)
                        ofs = maxOfs;

                    // Make offsets relative to b
                    var tmp = lastOfs;
                    lastOfs = hint - ofs;
                    ofs = hint - tmp;
                }
                else
                {
                    // a[b + hint] <= key
                    // Gallop right until a[b+hint + lastOfs] <= key < a[b+hint + ofs]
                    var maxOfs = length - hint;
                    while (ofs < maxOfs && comparer(key, a[lo + hint + ofs]) >= 0)
                    {
                        lastOfs = ofs;
                        ofs = (ofs << 1) + 1;
                        if (ofs <= 0)   // int overflow
                            ofs = maxOfs;
                    }
                    if (ofs > maxOfs)
                        ofs = maxOfs;

                    // Make offsets relative to b
                    lastOfs += hint;
                    ofs += hint;
                }
                Debug.Assert(-1 <= lastOfs && lastOfs < ofs && ofs <= length);

                // Now a[b + lastOfs] <= key < a[b + ofs], so key belongs somewhere to
                // the right of lastOfs but no farther right than ofs.  Do a binary
                // search, with invariant a[b + lastOfs - 1] <= key < a[b + ofs].
                lastOfs++;
                while (lastOfs < ofs)
                {
                    var m = lastOfs + ((ofs - lastOfs) >> 1);

                    if (comparer(key, a[lo + m]) < 0)
                        ofs = m; // key < a[b + m]
                    else
                        lastOfs = m + 1; // a[b + m] <= key
                }

                Debug.Assert(lastOfs == ofs); // so a[b + ofs - 1] <= key < a[b + ofs]
                return ofs;
            } // fixed (...)
        }

        private void MergeLo(int base1, int len1, int base2, int len2)
        {
            Debug.Assert(len1 > 0 && len2 > 0 && base1 + len1 == base2);

            // Copy first run into temp array
            var array = _array;
            var mergeBuffer = EnsureCapacity(len1);

            var m = mergeBuffer;
            var a = array;
            { // fixed (...)
                ArrayCopyRange(a, base1, m, 0, len1);

                var cursor1 = 0;       // Indexes into tmp array
                var cursor2 = base2;   // Indexes int a
                var dest = base1;      // Indexes int a

                // Move first element of second run and deal with degenerate cases
                a[dest++] = a[cursor2++];
                if (--len2 == 0)
                {
                    ArrayCopyRange(m, cursor1, a, dest, len1);
                    return;
                }
                if (len1 == 1)
                {
                    ArrayCopyRange(a, cursor2, dest, len2);
                    a[dest + len2] = m[cursor1]; // Last elt of run 1 to end of merge
                    return;
                }

                var comparer = _comparer;  // Use local variables for performance
                var minGallop = _minGallop;

                while (true)
                {
                    var count1 = 0; // Number of times in a row that first run won
                    var count2 = 0; // Number of times in a row that second run won

                    // Do the straightforward thing until (if ever) one run starts
                    // winning consistently.
                    do
                    {
                        Debug.Assert(len1 > 1 && len2 > 0);
                        if (comparer(a[cursor2], m[cursor1]) < 0) // c(a[cursor2], m[cursor1]) < 0
                        {
                            a[dest++] = a[cursor2++];
                            count2++;
                            count1 = 0;
                            if (--len2 == 0)
                                goto break_outer;
                        }
                        else
                        {
                            a[dest++] = m[cursor1++];
                            count1++;
                            count2 = 0;
                            if (--len1 == 1)
                                goto break_outer;
                        }
                    } while ((count1 | count2) < minGallop);

                    // One run is winning so consistently that galloping may be a
                    // huge win. So try that, and continue galloping until (if ever)
                    // neither run appears to be winning consistently anymore.
                    do
                    {
                        Debug.Assert(len1 > 1 && len2 > 0);
                        count1 = GallopRight(a[cursor2], mergeBuffer, cursor1, len1, 0, comparer);
                        if (count1 != 0)
                        {
                            ArrayCopyRange(m, cursor1, a, dest, count1);
                            dest += count1;
                            cursor1 += count1;
                            len1 -= count1;
                            if (len1 <= 1) // len1 == 1 || len1 == 0
                                goto break_outer;
                        }
                        a[dest++] = a[cursor2++];
                        if (--len2 == 0)
                            goto break_outer;

                        count2 = GallopLeft(m[cursor1], array, cursor2, len2, 0, comparer);
                        if (count2 != 0)
                        {
                            ArrayCopyRange(a, cursor2, dest, count2);
                            dest += count2;
                            cursor2 += count2;
                            len2 -= count2;
                            if (len2 == 0)
                                goto break_outer;
                        }
                        a[dest++] = m[cursor1++];
                        if (--len1 == 1)
                            goto break_outer;
                        minGallop--;
                    } while (count1 >= MIN_GALLOP | count2 >= MIN_GALLOP);

                    if (minGallop < 0)
                        minGallop = 0;
                    minGallop += 2;  // Penalize for leaving gallop mode
                }  // End of "outer" loop

            break_outer: // goto me! ;)

                _minGallop = minGallop < 1 ? 1 : minGallop;  // Write back to field

                if (len1 == 1)
                {
                    Debug.Assert(len2 > 0);
                    ArrayCopyRange(a, cursor2, dest, len2);
                    a[dest + len2] = m[cursor1]; //  Last elt of run 1 to end of merge
                }
                else if (len1 == 0)
                {
                    throw new ArgumentException("Comparison method violates its general contract!");
                }
                else
                {
                    Debug.Assert(len2 == 0);
                    Debug.Assert(len1 > 1);
                    ArrayCopyRange(m, cursor1, a, dest, len1);
                }
            } // fixed (...)
        }

        private void MergeHi(int base1, int len1, int base2, int len2)
        {
            Debug.Assert(len1 > 0 && len2 > 0 && base1 + len1 == base2);

            // Copy second run into temp array
            var array = _array; // For performance
            var mergeBuffer = EnsureCapacity(len2);

            var m = mergeBuffer;
            var a = array;
            { // fixed (...)
                ArrayCopyRange(a, base2, m, 0, len2);

                var cursor1 = base1 + len1 - 1;  // Indexes into a
                var cursor2 = len2 - 1;          // Indexes into mergeBuffer array
                var dest = base2 + len2 - 1;     // Indexes into a

                // Move last element of first run and deal with degenerate cases
                a[dest--] = a[cursor1--];
                if (--len1 == 0)
                {
                    ArrayCopyRange(m, 0, a, dest - (len2 - 1), len2);
                    return;
                }
                if (len2 == 1)
                {
                    dest -= len1;
                    cursor1 -= len1;
                    ArrayCopyRange(a, cursor1 + 1, dest + 1, len1);
                    a[dest] = m[cursor2];
                    return;
                }

                var comparer = _comparer;  // Use local variables for performance
                var minGallop = _minGallop;

                while (true)
                {
                    var count1 = 0; // Number of times in a row that first run won
                    var count2 = 0; // Number of times in a row that second run won

                    // Do the straightforward thing until (if ever) one run appears to win consistently.
                    do
                    {
                        Debug.Assert(len1 > 0 && len2 > 1);
                        if (comparer(m[cursor2], a[cursor1]) < 0) // c(m[cursor2], a[cursor1]) < 0
                        {
                            a[dest--] = a[cursor1--];
                            count1++;
                            count2 = 0;
                            if (--len1 == 0)
                                goto break_outer;
                        }
                        else
                        {
                            a[dest--] = m[cursor2--];
                            count2++;
                            count1 = 0;
                            if (--len2 == 1)
                                goto break_outer;
                        }
                    } while ((count1 | count2) < minGallop);

                    // One run is winning so consistently that galloping may be a
                    // huge win. So try that, and continue galloping until (if ever)
                    // neither run appears to be winning consistently anymore.
                    do
                    {
                        Debug.Assert(len1 > 0 && len2 > 1);
                        count1 = len1 - GallopRight(m[cursor2], array, base1, len1, len1 - 1, comparer);
                        if (count1 != 0)
                        {
                            dest -= count1;
                            cursor1 -= count1;
                            len1 -= count1;
                            ArrayCopyRange(a, cursor1 + 1, dest + 1, count1);
                            if (len1 == 0)
                                goto break_outer;
                        }
                        a[dest--] = m[cursor2--];
                        if (--len2 == 1)
                            goto break_outer;

                        count2 = len2 - GallopLeft(a[cursor1], mergeBuffer, 0, len2, len2 - 1, comparer);
                        if (count2 != 0)
                        {
                            dest -= count2;
                            cursor2 -= count2;
                            len2 -= count2;
                            ArrayCopyRange(m, cursor2 + 1, a, dest + 1, count2);
                            if (len2 <= 1)  // len2 == 1 || len2 == 0
                                goto break_outer;
                        }
                        a[dest--] = a[cursor1--];
                        if (--len1 == 0)
                            goto break_outer;
                        minGallop--;
                    } while (count1 >= MIN_GALLOP | count2 >= MIN_GALLOP);

                    if (minGallop < 0)
                        minGallop = 0;
                    minGallop += 2;  // Penalize for leaving gallop mode
                } // End of "outer" loop

            break_outer: // goto me! ;)

                _minGallop = minGallop < 1 ? 1 : minGallop;  // Write back to field

                if (len2 == 1)
                {
                    Debug.Assert(len1 > 0);
                    dest -= len1;
                    cursor1 -= len1;
                    ArrayCopyRange(a, cursor1 + 1, dest + 1, len1);
                    a[dest] = m[cursor2];  // Move first elt of run2 to front of merge
                }
                else if (len2 == 0)
                {
                    throw new ArgumentException("Comparison method violates its general contract!");
                }
                else
                {
                    Debug.Assert(len1 == 0);
                    Debug.Assert(len2 > 0);
                    ArrayCopyRange(m, 0, a, dest - (len2 - 1), len2);
                }
            } // fixed (...)
        }
    }
}
