using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

namespace TabulaLuma
{
    public enum CornerOrientation { Unknown, TopLeft, TopRight, BottomLeft, BottomRight }
    public class Corner
    {
        public Point2f[] Points { get; private set; }
        public ushort Code { get; private set; }
        public int CornerPointIndex { get; private set; }
        public Point2f CornerPoint => Points[CornerPointIndex];
 
        public Corner( ushort code, Point2f[] points, int cornerPointIndex)
        {
            Code = code;
            Points = points;
            CornerPointIndex = cornerPointIndex;
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

            var groups = corners.DistinctBy(c => c.Code).GroupBy(c => c.Code / 4).Where(g => g.Count() >= 3).ToArray();

            foreach(var group in groups)
            {
                var cornerFrame = new CornerFrame(group.Key, group.OrderBy(c => c.Code).Take(4).ToArray());
                cornerFrames.Add(cornerFrame);
            }
            return cornerFrames;
        }           

        public static Corner[] GetCornersFromImage( Mat grey, double minArea, double maxArea)
        {
            var tags = new List<Corner>();
            var thresh = new Mat();
            Cv2.Threshold(grey, thresh, 0, 255, ThresholdTypes.Otsu);

            Cv2.FindContours(thresh, out contours, out HierarchyIndex[] hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            contours = contours.Where(c =>
            {
                var area = Cv2.ContourArea(c);
                return area > minArea && area < maxArea;
            }).ToArray();

            foreach (var contour in contours)
            {          
                var rrect = Cv2.MinAreaRect(contour);
                if (Math.Abs(rrect.Size.Width - rrect.Size.Height) < 10 // squarish
                    && rrect.Size.Width > 45)
                {
                    Point2f[] box = Cv2.BoxPoints(rrect);
                    Mat src = thresh;

                    // Get the destination rectangle (unrotated)
                    Size rectSize = new Size((int)rrect.Size.Width, (int)rrect.Size.Height);

                    // Destination points for the upright rectangle
                    Point2f[] dstPoints = new Point2f[]
                    {
                        new Point2f(0, 0),
                        new Point2f(rectSize.Width - 1, 0),
                        new Point2f(rectSize.Width - 1, rectSize.Height - 1),
                        new Point2f(0, rectSize.Height - 1)
                    };

                    // Get the perspective transform
                    Mat M = Cv2.GetPerspectiveTransform(box, dstPoints);

                    // Warp the source image to get the upright rectangle
                    Mat rectMat = new Mat();
                    Cv2.WarpPerspective(src, rectMat, M, rectSize);

                    if( TryDecodeCornerImage(rectMat, box, out Corner corner))
                    {
                        // Successfully decoded corner image
                        tags.Add(corner);
                    }
                    else 
                    {
                 //       BitmapConverter.ToBitmap(rectMat).Save(@$"Z:\transfer\fails\debug_corner_{count++}.bmp");
                    }
                }
            }
            return tags.ToArray();
        }
        
        public static bool TryDecodeCornerImage( Mat image, Point2f[] box, out Corner corner, bool logFails = false)
        {
            count++;
            if(logFails)  BitmapConverter.ToBitmap(image).Save(@$"Z:\transfer\debug_corner_{count}.bmp");

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

            // Find the two darkest edges
            var edges = new[] {
                new { Name = "Top", Mean = topMean },
                new { Name = "Bottom", Mean = bottomMean },
                new { Name = "Left", Mean = leftMean },
                new { Name = "Right", Mean = rightMean }
            }.OrderBy(e => e.Mean).ToArray();

            string border1 = edges[0].Name;
            string border2 = edges[1].Name;

            // Set barcode strip ROIs based on detected borders
            Rect horizontalRoi, verticalRoi;
            CornerOrientation orientation = (border1 == "Top" && border2 == "Right") || (border1 == "Right" && border2 == "Top") ? CornerOrientation.TopRight :
                          (border1 == "Bottom" && border2 == "Left") || (border1 == "Left" && border2 == "Bottom") ? CornerOrientation.BottomLeft :
                          (border1 == "Top" && border2 == "Left") || (border1 == "Left" && border2 == "Top") ? CornerOrientation.TopLeft :
                          (border1 == "Bottom" && border2 == "Right") || (border1 == "Right" && border2 == "Bottom") ? CornerOrientation.BottomRight :
                          CornerOrientation.Unknown;

            // Starting from the orientation corner, step in diagonally to find the first white pixel
            for(int i = 3; i < 10; i++)
            {
                int x = orientation switch
                {
                    CornerOrientation.TopLeft => i,
                    CornerOrientation.TopRight => image.Cols - 1 - i,
                    CornerOrientation.BottomLeft => i,
                    CornerOrientation.BottomRight => image.Cols - 1 - i,
                    _ => 0
                };
                int y = orientation switch
                {
                    CornerOrientation.TopLeft => i,
                    CornerOrientation.TopRight => i,
                    CornerOrientation.BottomLeft => image.Rows - 1 - i,
                    CornerOrientation.BottomRight => image.Rows - 1 - i,
                    _ => 0
                };
                byte pixelValue = image.At<byte>(y, x);
                if(pixelValue > 200)
                {
                    // found white pixel
                    borderCellHeight = i;
                    borderCellWidth = i;
                    break;
                }
            }
          
            switch (orientation)
            {
                case CornerOrientation.TopRight:
                    horizontalRoi = new Rect(0, borderCellHeight, image.Cols - borderCellWidth, cellHeight);      // Top strip
                    verticalRoi = new Rect(image.Cols - borderCellWidth - cellWidth, borderCellHeight, cellWidth, image.Rows - borderCellHeight);     // Right strip
                    break;
                case CornerOrientation.BottomLeft:
                    horizontalRoi = new Rect(borderCellWidth, image.Rows - borderCellHeight - cellHeight, image.Cols - borderCellWidth, cellHeight);     // Bottom strip
                    verticalRoi = new Rect(borderCellWidth, 0, cellWidth, image.Rows - borderCellHeight);      // Left strip
                    break;
                case CornerOrientation.TopLeft:
                    horizontalRoi = new Rect(borderCellWidth, borderCellHeight, image.Cols - borderCellWidth, cellHeight);      // Top strip
                    verticalRoi = new Rect(borderCellWidth, borderCellHeight, cellWidth, image.Rows  - borderCellHeight);      // Left strip
                    break;
                case CornerOrientation.BottomRight:
                    horizontalRoi = new Rect(0, image.Rows - borderCellHeight - cellHeight, image.Cols - borderCellWidth, cellHeight);     // Bottom strip
                    verticalRoi = new Rect(image.Cols - borderCellWidth - cellWidth, 0, cellWidth, image.Rows - borderCellHeight);     // Right strip
                    break;
                default:
                    return false;
            }
  
           
            if( !CheckMatRoiValid(image, horizontalRoi) || !CheckMatRoiValid(image, verticalRoi))
            {
                return false;
            }

            try
            {
                Mat horizontalStrip = new Mat(image, horizontalRoi);
                Mat verticalStrip = new Mat(image, verticalRoi);

              if(logFails)      BitmapConverter.ToBitmap(horizontalStrip).Save(@$"Z:\transfer\debug_horizontalStrip_{count}.bmp");
              if(logFails)      BitmapConverter.ToBitmap(verticalStrip).Save(@$"Z:\transfer\debug_verticalStrip_{count}.bmp");

                byte bits1 = ExtractBarcodeBits(horizontalStrip, true, logFails);
                byte bits2 = ExtractBarcodeBits(verticalStrip, false, logFails);
                // horizontal strips are read left to right, MSB to LSB
                // vertical strips are read top to bottom, MSB to LSB
                // Convert to clockwise reading of the corner bits

                switch(orientation)
                {
                    case CornerOrientation.TopLeft:
                        bits1 = ReverseBits(bits1);
                        code = (UInt16)((bits1 << 7) | bits2);
                        break;
                    case CornerOrientation.TopRight:
                        bits1 = ReverseBits(bits1);
                        bits2 = ReverseBits(bits2);
                        code = (UInt16)((bits2 << 7) | bits1);
                        break;
                    case CornerOrientation.BottomRight:
                        bits2 = ReverseBits(bits2);                      
                        code = (UInt16)((bits1 << 7) | bits2);
                        break;
                    case CornerOrientation.BottomLeft:
                        code = (UInt16)((bits2 << 7) | bits1);
                        break;
                    default:
                        return false;
                }
                var setBits = System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount(code);
                if (setBits % 2 == 0)
                    return false; // parity error

                code = (ushort)(code & 0x3FFF);
                corner = new Corner(code, box, orientation switch
                {
                    CornerOrientation.TopLeft => 0,
                    CornerOrientation.TopRight => 1,
                    CornerOrientation.BottomRight => 2,
                    CornerOrientation.BottomLeft => 3,
                    _ => -1
                });             
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting barcode bits: {ex.Message}");
                return false;
            }

            return true;
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
                    ? new Rect((int)(i * cellWidth + 0.5), 0, (int)(cellWidth + 0.5), (int)(cellHeight + 0.5))   // For horizontal strip 
                    : new Rect(0, (int)(i * cellHeight + 0.5), (int)(cellWidth + 0.5), (int)(cellHeight + 0.5));  // For vertical strip 

                if (bitRoi.X + bitRoi.Width > strip.Cols)
                    bitRoi.Width = strip.Cols - bitRoi.X;
                if (bitRoi.Y + bitRoi.Height > strip.Rows)
                    bitRoi.Height = strip.Rows - bitRoi.Y;

                Mat bitMat = new Mat(strip, bitRoi);
                Scalar mean = Cv2.Mean(bitMat);
                means[i] = (byte)mean.Val0;
            }

            if(logFails) File.AppendAllLines(@$"Z:\transfer\debug_means_{(horizontal ? 'H' : 'V')}_{count}.txt", new[] { string.Join(",", means) });

            var sortedMeans = means.OrderBy(m => m).ToArray();

            if (sortedMeans[0] > 190)
                return 0;
            if (sortedMeans[7] < 125)
                return 0xFF;

            // find the sortedMean with the largest gap to the next value
            var gaps = sortedMeans.Zip(sortedMeans.Skip(1), (a, b) => b - a).ToArray();
            int maxGapIndex = Array.IndexOf(gaps, gaps.Max());
            byte threshold = (byte)((sortedMeans[maxGapIndex] + sortedMeans[maxGapIndex + 1]) / 2);

            if (threshold > 205)
                return 0;
            if (threshold < 125)
                return 0xFF;
         
            for (int i = 0; i < 8; i++)
            {
                int bit = means[i] < threshold ? 1 : 0;
                bits = (byte)((bits << 1) | bit);
            }

            return bits;
        }     
    }
}
