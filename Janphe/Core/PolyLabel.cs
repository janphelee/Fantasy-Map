using System;

namespace Janphe
{
    public static class PolyLabel
    {
        public static double[] polylabel(this double[][] polygon, double precision = 1.0) => polylabel(new double[][][] { polygon }, precision);

        private class Cell
        {
            public double x, y, h, d, max;

            public Cell(double _x, double _y, double _h, double[][][] polygon)
            {
                x = _x;
                y = _y;
                h = _h;

                d = pointToPolygonDist(_x, _y, polygon);
                max = d + h * Math.Sqrt(2.0);
            }
        }
        public static double[] polylabel(double[][][] polygon, double precision = 1.0, bool debug = false)
        {
            // find the bounding box of the outer ring
            double minX, minY, maxX, maxY;
            minX = minY = maxX = maxY = 0;
            for (var i = 0; i < polygon[0].Length; i++)
            {
                var p = polygon[0][i];
                if (0 == i || p[0] < minX)
                    minX = p[0];
                if (0 == i || p[1] < minY)
                    minY = p[1];
                if (0 == i || p[0] > maxX)
                    maxX = p[0];
                if (0 == i || p[1] > maxY)
                    maxY = p[1];
            }

            var width = maxX - minX;
            var height = maxY - minY;
            var cellSize = Math.Min(width, height);
            var h = cellSize / 2;

            if (cellSize == 0)
                return new double[] { minX, minY };

            // a priority queue of cells in order of their "potential" (max distance to polygon)
            var cellQueue = new TinyQueue<Cell>(null, (a, b) => b.max.CompareTo(a.max));

            // cover polygon with initial cells
            for (var x = minX; x < maxX; x += cellSize)
            {
                for (var y = minY; y < maxY; y += cellSize)
                {
                    cellQueue.push(new Cell(x + h, y + h, h, polygon));
                }
            }

            // take centroid as the first best guess
            var bestCell = getCentroidCell(polygon);

            // special case for rectangular polygons
            var bboxCell = new Cell(minX + width / 2, minY + height / 2, 0, polygon);
            if (bboxCell.d > bestCell.d)
                bestCell = bboxCell;

            var numProbes = cellQueue.Length;

            while (cellQueue.Length > 0)
            {
                // pick the most promising cell from the queue
                var cell = cellQueue.pop();

                // update the best cell if we found a better one
                if (cell.d > bestCell.d)
                {
                    bestCell = cell;
                    if (debug)
                        Debug.Log($"found best {Math.Round(1e4 * cell.d) / 1e4} after {numProbes} probes");
                }

                // do not drill down further if there's no chance of a better solution
                if (cell.max - bestCell.d <= precision)
                    continue;

                // split the cell into four cells
                h = cell.h / 2;
                cellQueue.push(new Cell(cell.x - h, cell.y - h, h, polygon));
                cellQueue.push(new Cell(cell.x + h, cell.y - h, h, polygon));
                cellQueue.push(new Cell(cell.x - h, cell.y + h, h, polygon));
                cellQueue.push(new Cell(cell.x + h, cell.y + h, h, polygon));
                numProbes += 4;
            }

            if (debug)
            {
                Debug.Log("num probes: " + numProbes);
                Debug.Log("best distance: " + bestCell.d);
            }
            return new double[] { bestCell.x, bestCell.y };
        }

        private static double pointToPolygonDist(double x, double y, double[][][] polygon)
        {
            var inside = false;
            var minDistSq = double.PositiveInfinity;

            for (var k = 0; k < polygon.Length; k++)
            {
                var ring = polygon[k];

                for (int i = 0, len = ring.Length, j = len - 1; i < len; j = i++)
                {
                    var a = ring[i];
                    var b = ring[j];

                    if ((a[1] > y != b[1] > y) &&
                        (x < (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]) + a[0]))
                        inside = !inside;

                    minDistSq = Math.Min(minDistSq, getSegDistSq(x, y, a, b));
                }
            }

            return (inside ? 1 : -1) * Math.Sqrt(minDistSq);
        }

        private static double getSegDistSq(double px, double py, double[] a, double[] b)
        {
            var x = a[0];
            var y = a[1];
            var dx = b[0] - x;
            var dy = b[1] - y;

            if (dx != 0 || dy != 0)
            {

                var t = ((px - x) * dx + (py - y) * dy) / (dx * dx + dy * dy);

                if (t > 1)
                {
                    x = b[0];
                    y = b[1];

                }
                else if (t > 0)
                {
                    x += dx * t;
                    y += dy * t;
                }
            }

            dx = px - x;
            dy = py - y;

            return dx * dx + dy * dy;
        }

        // get polygon centroid
        private static Cell getCentroidCell(double[][][] polygon)
        {
            double area = 0;
            double x = 0;
            double y = 0;
            var points = polygon[0];

            for (int i = 0, len = points.Length, j = len - 1; i < len; j = i++)
            {
                var a = points[i];
                var b = points[j];
                var f = a[0] * b[1] - b[0] * a[1];
                x += (a[0] + b[0]) * f;
                y += (a[1] + b[1]) * f;
                area += f * 3;
            }
            if (area == 0)
                return new Cell(points[0][0], points[0][1], 0, polygon);
            return new Cell(x / area, y / area, 0, polygon);
        }
    }
}
