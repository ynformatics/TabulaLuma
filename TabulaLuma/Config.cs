using OpenCvSharp;
using System.Text.Json;

namespace TabulaLuma
{
    public class Config 
    {
        static string ConfigFilePath { get; set; }
    
        public string? AppDataFolder => Path.GetDirectoryName(ConfigFilePath);
        public static Config Load(string filePath)
        {
            ConfigFilePath = filePath;
            if (!File.Exists(filePath))
            {
                var config = new Config();
                config.Save();
                return config;
            }
            else
            {              
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions() { IncludeFields = true });
                return config;
            }
        }
        public  void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }

        // Default values
        public int FrameWidth { get; set; } = 1920;
        public int FrameHeight { get; set; } = 1080;
        public bool Debugging { get; set; } = true;
        public CornerFrameConfig CornerFrame { get; set; } = new CornerFrameConfig();
        public class CornerFrameConfig
        {
            public int KeepTime { get; set; } = 300;
            public float LocationDelta { get; set; } = 3.0f;
            public double MinArea { get; set; } = 500.0;    
            public double MaxArea { get; set; } = 1050.0;
        }
        
        public CalibrationData Calibration { get; set; } = new CalibrationData();   
        public class CalibrationData
        {
            public bool IsCalibrated {get; set; } = false;  
            public List<Point2f> WorldPoints { get; set; } = [new Point2f(0, 0), new Point2f(880, 0), new Point2f(880, 520), new Point2f(0, 520)];
            public List<Point2f> CameraPoints { get; set; } = new List<Point2f>();
            public List<Point2f> ProjectorPoints { get; set; } = new List<Point2f>();
        }
        public CameraData Camera { get; set; } = new CameraData();
        public class CameraData
        {
            public string Name { get; set; } = "C920";
            public int Focus { get; set; } = 5;
            public int Exposure { get; set; } = -6;
        }
    }
}
