using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

namespace TabulaLuma
{
    public class Corner
    {
        public Point2f[] Points { get; private set; }
        public ushort Code { get; private set; }
        public int CornerPointIndex { get; private set; } = 0;
        public Point2f CornerPoint => Points[0];
 
        public Corner( ushort code, Point2f[] points)
        {
            Code = code;
            Points = points;
        }
    }
  
    public class CornerFrame
    {
        static int count = 0;
        public int Id { get; private set; }
        public Point2f[] Points { get; set; }
        public static Point[][] contours;
        public CornerFrame(int id, Corner[] givenCorners)
        {
            var corners = new Corner[4];
            Points = new Point2f[4];

            for (int i = 0; i < givenCorners.Length; i++)
            {
                var givenCorner = givenCorners[i];
                corners[givenCorner.Code % 4] = givenCorner;
            }

            for(int i = 0; i < 4; i++)
            {
                if (corners[i] != null)
                {
                    Points[i] = corners[i].CornerPoint;
                }
                else
                {
                    //Debug.WriteLine($"3 corners:{id}");
                    var cw = corners[(i + 1) % 4];
                    var ccw = corners[(i + 3) % 4];
                 
                    FindExtendedLineIntersection(
                        cw.Points[(cw.CornerPointIndex + 3) % 4],
                        cw.Points[cw.CornerPointIndex],
                        ccw.Points[(ccw.CornerPointIndex + 1) % 4],
                        ccw.Points[ccw.CornerPointIndex],
                        out Point2f intersectionPoint);
                    Points[i] = intersectionPoint;
                }
            }      
         
            Id = id;
        }

       
        /// <summary>
        /// Finds the intersection point of two extended parametric lines.
        /// </summary>
        /// <param name="p1">Start point of Line 1</param>
        /// <param name="p2">End point of Line 1</param>
        /// <param name="p3">Start point of Line 2</param>
        /// <param name="p4">End point of Line 2</param>
        /// <param name="intersectionPoint">The resulting intersection point if lines intersect.</param>
        /// <returns>True if the lines intersect at a single point, false if parallel or collinear.</returns>
        public static bool FindExtendedLineIntersection(Point2f p1, Point2f p2, Point2f p3, Point2f p4, out Point2f intersectionPoint)
        {
            intersectionPoint = default(Point2f);

            double dx1 = p2.X - p1.X;
            double dy1 = p2.Y - p1.Y;
            double dx2 = p4.X - p3.X;
            double dy2 = p4.Y - p3.Y;

            double denominator = dx1 * dy2 - dx2 * dy1;

            if (Math.Abs(denominator) < 0.0001)
            {
                // Lines are parallel or collinear.            
                return false;
            }

            double tNumerator = dy2 * (p3.X - p1.X) - dx2 * (p3.Y - p1.Y);
            double t = tNumerator / denominator;

            // Calculate intersection point using parameter t
            intersectionPoint = new Point2f
            {
                X = (float)(p1.X + t * dx1),
                Y = (float)(p1.Y + t * dy1)
            };

            // For *extended* lines, the intersection always occurs if they are not parallel.
            return true;
        }
        
        public static List<CornerFrame> DetectCornerFrames(Mat grey, double minArea, double maxArea)
        {
            var corners = GetCornersFromImage(grey, minArea, maxArea);
            var cornerFrames = GroupCornersIntoFrames(corners);
            return cornerFrames;
        }
        public static List<CornerFrame> GroupCornersIntoFrames(IList<Corner> corners)
        {
            var cornerFrames = new List<CornerFrame>();
            var minCorners = 3;
            var groups = corners.DistinctBy(c => c.Code).GroupBy(c => c.Code / 4).Where(g => g.Count() >= minCorners).ToArray();

            foreach(var group in groups)
            {
                var cornerFrame = new CornerFrame(group.Key, group.OrderBy(c => c.Code).Take(4).ToArray());
                cornerFrames.Add(cornerFrame);
            }
            return cornerFrames;
        }

        static Point2f[] OrderTriangleClockwise(Point2f[] pts)
        {
            // Compute centroid
            var centroid = new Point2f(
                (pts[0].X + pts[1].X + pts[2].X) / 3f,
                (pts[0].Y + pts[1].Y + pts[2].Y) / 3f
            );

            // Compute angle from centroid to each point
            var ordered = pts
                .Select(p => new { Point = p, Angle = Math.Atan2(p.Y - centroid.Y, p.X - centroid.X) })
                .OrderBy(pa => pa.Angle) // clockwise
                .Select(pa => pa.Point)
                .ToArray();

            return ordered;
        }
        static int FindRightAngleIndex(Point2f[] triPts)
        {
            // find subtended angles at each triangle vertex
            var angles = new List<Tuple<int, double>>();
            for (int i = 0; i < 3; i++)
            {
                var a = triPts[i];
                var x = triPts[(i + 1) % 3];
                var y = triPts[(i + 2) % 3];
                var v1 = x - a;
                var v2 = y - a;
                double dot = v1.DotProduct(v2);
                double magV1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
                double magV2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);
                double angle = Math.Acos(dot / (magV1 * magV2));
                angles.Add(new Tuple<int, double>(i, angle));
            }
            angles = angles.OrderBy(a => a.Item2).ToList();
            return angles[2].Item1;
        }

        public static Corner[] GetCornersFromImage( Mat grey, double minArea, double maxArea)
        {
            var tags = new List<Corner>();
            var thresh = new Mat();
            Cv2.Threshold(grey, thresh, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.BinaryInv);

            Cv2.FindContours(thresh, out contours, out HierarchyIndex[] hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            contours = contours.Where(c =>
            {
                var area = Cv2.ContourArea(c);
                return area > minArea && area < maxArea;
            }).ToArray();

            foreach (var contour in contours)
            { 
                var tri = Cv2.MinEnclosingTriangle(contour, out Point2f[] triPts);
                if(triPts.Length != 3)
                    continue; 

                triPts = OrderTriangleClockwise(triPts);             
                var iRa = FindRightAngleIndex(triPts);

                //rotate triPts so right angle point is at index 0
                triPts = [triPts[iRa], triPts[(iRa + 1) % 3], triPts[(iRa + 2) % 3] ];
               
                Point2f[] boundingParallelogram = [triPts[0], triPts[1], triPts[1] + triPts[2] - triPts[0], triPts[2]];

                //  Set points for the destination rectangle
                Size rectSize = new Size((int)triPts[0].DistanceTo(triPts[1]), (int)triPts[0].DistanceTo(triPts[2]));
                Point2f[] dstPoints = new Point2f[]
                {
                        new Point2f(0, 0),
                        new Point2f(rectSize.Width - 1, 0),
                        new Point2f(rectSize.Width - 1, rectSize.Height - 1),
                        new Point2f(0, rectSize.Height - 1)
                };

                // Get the perspective transform
                Mat M = Cv2.GetPerspectiveTransform(boundingParallelogram, dstPoints);

                // Warp the source image to get the upright rectangle
                Mat rectMat = new Mat();
                Cv2.WarpPerspective(thresh, rectMat, M, rectSize);

                // crop the edges
                var boundingRect = Cv2.BoundingRect(rectMat);
                var rectMat2 = rectMat.SubMat(boundingRect);
                if (Math.Abs(boundingRect.Width - boundingRect.Height) < 10)
                {
                    if (TryDecodeCornerImage(rectMat2, boundingParallelogram, out Corner corner))
                    {
                        tags.Add(corner);
                    }
                }            
            }
            return tags.ToArray();
        }
        
        public static bool TryDecodeCornerImage( Mat image, Point2f[] box, out Corner corner, bool logFails = false)
        {
            count++;

            corner = null;

            ushort code = 0;
            int cellHeight = image.Rows / 9;
            int cellWidth = image.Cols / 9;
            int borderCellHeight = cellHeight;
            int borderCellWidth = cellWidth;
            // Probe edges
            double topMean = Cv2.Mean(new Mat(image, new Rect(0, 0, image.Cols, cellHeight))).Val0;
            double bottomMean = Cv2.Mean(new Mat(image, new Rect(0, image.Rows - cellHeight, image.Cols, cellHeight))).Val0;
            double leftMean = Cv2.Mean(new Mat(image, new Rect(0, 0, cellWidth, image.Rows))).Val0;
            double rightMean = Cv2.Mean(new Mat(image, new Rect(image.Cols - cellWidth, 0, cellWidth, image.Rows))).Val0;

            // Find the two lightest edges
            var edges = new[] {
                new { Name = "Top", Mean = topMean },
                new { Name = "Bottom", Mean = bottomMean },
                new { Name = "Left", Mean = leftMean },
                new { Name = "Right", Mean = rightMean }
            }.OrderByDescending(e => e.Mean).ToArray();

            string borderName = edges[0].Name + edges[1].Name;
        
            if(borderName != "TopLeft" && borderName != "LeftTop")
            {
                return DumpFailData(count, "wrong orientation", image);
            }
            // Starting from the orientation corner, step in diagonally to find the first black pixel
            for(int i = 3; i < 10; i++)
            {
                byte pixelValue = image.At<byte>(i, i);
                if(pixelValue < 55)
                {
                    // found black pixel
                    borderCellHeight = i;
                    borderCellWidth = i;
                    break;
                }
            }

            // Set barcode strip ROIs based on borders
            Rect horizontalRoi = new Rect(borderCellWidth, borderCellHeight, image.Cols - borderCellWidth, cellHeight);      // Top strip
            Rect verticalRoi = new Rect(borderCellWidth, borderCellHeight, cellWidth, image.Rows  - borderCellHeight);      // Left strip
                     
            if( !CheckMatRoiValid(image, horizontalRoi) || !CheckMatRoiValid(image, verticalRoi))
            {
                return DumpFailData(count, $"Invalid roi h:{horizontalRoi} v:{verticalRoi}", image );
            }

            try
            {
                Mat horizontalStrip = new Mat(image, horizontalRoi);
                Mat verticalStrip = new Mat(image, verticalRoi);

                byte hBits = ExtractBarcodeBits(horizontalStrip, horizontal: true, logFails);
                byte vBits = ExtractBarcodeBits(verticalStrip, horizontal: false, logFails);
                // horizontal strips are read left to right
                // vertical strips are read top to bottom
                // Convert to clockwise reading of the corner bits

                code = (UInt16)((ReverseBits(hBits) << 7) | vBits);
             
                var setBits = System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount(code);
                if (setBits % 2 == 0)
                    return DumpFailData(count, $"parity hbits:{Convert.ToString(hBits, 2).PadLeft(8, '0')} vbits:{Convert.ToString(vBits, 2).PadLeft(8, '0')}", image, horizontalStrip, verticalStrip); // parity error

                code = (ushort)(code & 0x3FFF); // mask off any parity bit

                corner = new Corner(code, box);                          
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting barcode bits: {ex.Message}");
                return false;
            }

            return true;
        }
        static bool DumpFailData(int count, string message, Mat image, Mat horizontalStrip = null, Mat verticalStrip = null, bool force = false)
        {
            if (!force)
                return false;
            if (!string.IsNullOrEmpty(message))
                File.WriteAllText(@$"Z:\transfer\tl\{count}_debug_fail.txt", message);
            if (image != null)
                Cv2.ImWrite(@$"Z:\transfer\tl\{count}_debug_corner.bmp", image);
            if(horizontalStrip != null)
                Cv2.ImWrite(@$"Z:\transfer\tl\{count}_debug_horizontalStrip.bmp", horizontalStrip);
            if(verticalStrip != null)
                Cv2.ImWrite(@$"Z:\transfer\tl\{count}_debug_verticalStrip.bmp", verticalStrip);
            return false;
        }
        public static byte ReverseBits(byte b)
        {
            b = (byte)(((b * 0x0802 & 0x22110) | (b * 0x8020 & 0x88440)) * 0x10101 >> 16);
            return b;
        }
        static bool CheckMatRoiValid(Mat mat, Rect roi)
        {
            return roi.X >= 0 && roi.Y >= 0 && roi.X + roi.Width <= mat.Cols && roi.Y + roi.Height <= mat.Rows;
        }
        private static byte ExtractBarcodeBits(Mat strip, bool horizontal, bool logFails = false)
        {
            double cellHeight = horizontal ? strip.Rows : strip.Rows / 8.0;
            double cellWidth = horizontal ? strip.Cols / 8.0 : strip.Cols;

            byte bits = 0;
            byte[] means = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                Rect bitRoi = horizontal
                    ? new Rect((int)(i * cellWidth + 0.5), (int)cellHeight/2, (int)(cellWidth + 0.5), 1)   // For horizontal strip 
                    : new Rect((int)cellWidth/2, (int)(i * cellHeight + 0.5), 1, (int)(cellHeight + 0.5));  // For vertical strip 

                if (bitRoi.X + bitRoi.Width > strip.Cols)
                    bitRoi.Width = strip.Cols - bitRoi.X;
                if (bitRoi.Y + bitRoi.Height > strip.Rows)
                    bitRoi.Height = strip.Rows - bitRoi.Y;

                Mat bitMat = new Mat(strip, bitRoi);
                Scalar mean = Cv2.Mean(bitMat);
                means[i] = (byte)mean.Val0;
            }

            var sortedMeans = means.OrderBy(m => m).ToArray();

            byte threshold = 128;      
         
            for (int i = 0; i < 8; i++)
            {
                int bit = means[i] > threshold ? 1 : 0;
                bits = (byte)((bits << 1) | bit);
            }

            return bits;
        }     
    }
}
