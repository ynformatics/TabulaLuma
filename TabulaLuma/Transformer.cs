using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace TabulaLuma
{ 
    public class Transformer : ITransformService
    {
        Config config;
        public  Mat? CameraToWorldTransform { get; set; }
        public  Mat? WorldToCameraTransform { get; set; }
        public  Mat? ProjectorToWorldTransform { get; set; }
        public  Mat? WorldToProjectorTransform { get; set; }
        public  Mat? CameraToProjectorTransform { get; set; }

        public bool IsCalibrated { get => config.Calibration.IsCalibrated; }
        public bool Masking { get; set; } = true;
        public Transformer(Config config)
        {
            this.config = config;
            if (config.Calibration.IsCalibrated)
            {
                SetCamera(config.Calibration.CameraPoints);
                SetProjector(config.Calibration.ProjectorPoints);
            }        
        }

        public Point2f[] WorldPoints { get => config.Calibration.WorldPoints.ToArray(); }
        void Save()
        {
            config.Calibration.IsCalibrated = true;
            config.Save();
        }
         void Update()
        { 
            if(CameraToWorldTransform is not null){
                WorldToCameraTransform = CameraToWorldTransform.Inv();
            }
            if(ProjectorToWorldTransform is not null){
                WorldToProjectorTransform = ProjectorToWorldTransform.Inv();
            }
            if(CameraToWorldTransform is not null && WorldToProjectorTransform is not null)
            {
                CameraToProjectorTransform = WorldToProjectorTransform * CameraToWorldTransform;
            }
            Save();
        }
     
        public  void SetWorld(IEnumerable<Point2f> world)
        {
            config.Calibration.WorldPoints = world.ToList();
            Update();
        }

        public  void SetCamera(IEnumerable<Point2f> camera)
        {
            config.Calibration.CameraPoints = camera.ToList();
            CameraToWorldTransform = Cv2.FindHomography(InputArray.Create(camera), InputArray.Create(config.Calibration.WorldPoints));
            Update();
        }
       
        public  IEnumerable<Point2f> CameraToWorld(IEnumerable<Point2f> camera)
        {
            if (CameraToWorldTransform is null) throw new Exception("CameraToWorldTransform is null");

            return Cv2.PerspectiveTransform(camera, CameraToWorldTransform);
        }
        public  IEnumerable<Point2f> WorldToCamera(IEnumerable<Point2f> world)
        {
            if (WorldToCameraTransform is null) throw new Exception("WorldToCameraTransform is null");

            return Cv2.PerspectiveTransform(world, WorldToCameraTransform);
        }

        public  void SetProjector(IEnumerable<Point2f> projector)
        {
            config.Calibration.ProjectorPoints = projector.ToList();
            ProjectorToWorldTransform = Cv2.FindHomography(InputArray.Create(projector), InputArray.Create(config.Calibration.WorldPoints));
            Update();
        }

        public  IEnumerable<Point2f> ProjectorToWorld(IEnumerable<Point2f> projector)
        {
            if (ProjectorToWorldTransform is null) throw new Exception("ProjectorToWorldTransform is null");

            return Cv2.PerspectiveTransform(projector, ProjectorToWorldTransform);
        }
        public  IEnumerable<Point2f> WorldToProjector(IEnumerable<Point2f> world)
        {
            if (WorldToProjectorTransform is null) throw new Exception("WorldToProjectorTransform is null");

            return Cv2.PerspectiveTransform(world, WorldToProjectorTransform);
        }

        public  Point2f WorldToProjector(Point2f world)
        {
            return WorldToProjector([world]).FirstOrDefault();
        }
        public  IEnumerable<Point2f> CameraToProjector(IEnumerable<Point2f> camera)
        {
            if (CameraToProjectorTransform is null) throw new Exception("CameraToProjectorTransform is null");

            return Cv2.PerspectiveTransform(camera, CameraToProjectorTransform);
        }
        public  Point2f CameraToProjector(Point2f camera)
        {
            if (CameraToProjectorTransform is null) throw new Exception("CameraToProjectorTransform is null");

            return Cv2.PerspectiveTransform([camera], CameraToProjectorTransform).FirstOrDefault();
        }
        public  IEnumerable<Point2f> CameraToProjector(IEnumerable<Point> camera)
        {
            if (CameraToProjectorTransform is null) throw new Exception("CameraToProjectorTransform is null");

            return Cv2.PerspectiveTransform(camera.Select(p => new Point2f(p.X,p.Y)), CameraToProjectorTransform);
        }
    
        public  Rect GetCameraROI(bool addMargin = true)
        {
            return new Rect((int)config.Calibration.CameraPoints[0].X, 
                (int)config.Calibration.CameraPoints[0].X,
                (int)Math.Abs(config.Calibration.CameraPoints[1].X - config.Calibration.CameraPoints[0].X),
                (int)Math.Abs(config.Calibration.CameraPoints[3].Y - config.Calibration.CameraPoints[0].Y));
            if (config.Calibration.CameraPoints.Count < 4) 
                return new Rect(0, 0, 1920, 1080);
            var xs = config.Calibration.CameraPoints.Select(p => p.X);
            var ys = config.Calibration.CameraPoints.Select(p => p.Y);
            var minX = (int)xs.Min();
            var maxX = (int)xs.Max();
            var minY = (int)ys.Min();
            var maxY = (int)ys.Max();
            if(addMargin)
            {
                var margin = (maxX - minX) * 0.1;
                return new Rect((int)(minX - margin), (int)(minY - margin), (int)(maxX - minX + 2 * margin), (int)(maxY - minY + 2 * margin));
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
