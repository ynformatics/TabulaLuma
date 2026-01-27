using OpenCvSharp;
using System.Diagnostics;

namespace TabulaLuma;

class Utils
{
    // Helper function to compare two float arrays 
  
    public static bool IsLocationChanged(Point2f[] prev, Point2f[] current, float delta)
    {
        if (prev == null || current == null || prev.Length != current.Length)
            return true;
        for (int i = 0; i < prev.Length; i++)
        {
            if (Math.Abs(prev[i].X - current[i].X) > delta || Math.Abs(prev[i].Y - current[i].Y) > delta)
                return true;
        }
        return false;
    }
    public static ulong GetElapsedMicroseconds()
    {
        return (ulong)Stopwatch.GetTimestamp() / TimeSpan.TicksPerMicrosecond;
    }
}
