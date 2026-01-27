using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabulaLuma
{
    public interface ITransformService
    {
        public Mat? WorldToProjectorTransform { get; set; }
        public Point2f[] WorldPoints { get; }
        public void SetCamera(IEnumerable<Point2f> cameraPoints);
        public void SetProjector(IEnumerable<Point2f> projectorPoints);
        public IEnumerable<Point2f> WorldToProjector(IEnumerable<Point2f> world);
        public Point2f WorldToProjector(Point2f world);
        public IEnumerable<Point2f> CameraToWorld(IEnumerable<Point2f> camera);
        public bool Masking { get; set; }
    }
}
