using OpenCvSharp;

namespace TabulaLuma
{
    public static class Extensions
    {
        public static float Length(this Point2f pt) =>
            (float)Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
    }
}
