using FlashCap;
using FlashCap.Utilities;
using Hexa.NET.SDL3;
using OpenCvSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;

namespace TabulaLuma
{
    public class Engine : IEngineService
    {
         Config config = Config.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TabulaLuma", "config.txt"));
         Database db = new Database();
         ConcurrentDictionary<int, (Point2f[] points, ulong timestamp)> fiducialLocationCache = new();
         Dictionary<int, Point2f[]> localFiducialLocationCache = new();

         BlockingCollection<Mat> frames = new BlockingCollection<Mat>(1);
         readonly object matSupporterLock = new();

         Mat matSupporter = new Mat();
         string fileStorePath;
        CaptureDevice captureDevice;
        nint windowHandle = 0;

        // IEngineService implementation
        string IEngineService.FileStorePath => fileStorePath;     
        void IEngineService.ShowPropertiesPage()
        {
            captureDevice?.ShowPropertyPageAsync(windowHandle);
        }
        Config IEngineService.Config => config;
        Database IEngineService.Database => db;
     
        public  async Task<int> Start(IHardware hardware)
        {
            System.Console.WriteLine("Hello, World!");
            ProgramStore programs = new ProgramStore();
            fileStorePath = Path.Combine(config.AppDataFolder, "shared");
            Directory.CreateDirectory(fileStorePath);
            Keyboard keyboard = new Keyboard();
            Stats.database = db;
            Transformer transformer = new Transformer(config);
            bool debugging = config.Debugging;
            db.Debugging = debugging;
            ServiceProvider.Register<ITransformService>(transformer);
            ServiceProvider.Register<IKeyboardService>(keyboard);
            ServiceProvider.Register<ILoggingService>(db);
            ServiceProvider.Register<IEngineService>(this);
          
            foreach(var plugin in ProgramStore.LoadPlugins(Assembly.GetExecutingAssembly().Location))
            {
                programs.Add(plugin);
                Debug.WriteLine($"Loaded plugin: {plugin.GetType().Name} (ID: {plugin.Id})");
            }
            foreach (var plugin in ProgramStore.LoadPlugins(fileStorePath))
            {
                programs.Add(plugin);
                Debug.WriteLine($"Loaded plugin: {plugin.GetType().Name} (ID: {plugin.Id})");
            }

            foreach (var fileName in Directory.GetFiles(fileStorePath, "*.txt"))
            {
                var progId = int.Parse(Path.GetFileNameWithoutExtension(fileName));
                var codeRef = Reference.Create<string[]>(Reference.Lifetimes.Session, File.ReadAllLines(fileName));
                ProgramBase? prog = Compiler.CompileProgramBase($"Program{progId:00000}", progId, codeRef, out var errors) as ProgramBase;

                if (prog == null)
                {
                    // We load a placeholder with the erroneous code
                    prog = new ProgramError(progId);
                    prog.SourceCodeRef = codeRef;
                    prog.Errors = errors;
                    prog.PreCompiled = programs.HasProgram(progId);
                    programs.Add(prog);
                    Debug.WriteLine($"Compiled program {progId} with errors from {progId}.txt");
                }
                else
                {
                    prog.PreCompiled = programs.HasProgram(progId);
                    programs.Add(prog);
                    Debug.WriteLine($"Compiled program {progId} from {progId}.txt");
                }
            }

            windowHandle = hardware.Initialise(config);

            if (!transformer.IsCalibrated)
            {
                if (Claim.TryParse(-1, $"(100000) is calibrating", out var calClaim).Success)
                {
                    db.AddStatement(calClaim);
                }
            }

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                foreach (var program in programs)
                {
                    if (program is ProgramBase pb)
                    {
                        pb.SaveMemories();
                    }
                }
            };
            int frameWidth = config.FrameWidth;
            int frameHeight = config.FrameHeight;

            var devices = new CaptureDevices();
            var descriptor0 = devices.EnumerateDescriptors()
            .Where(d => d.DeviceType == DeviceTypes.DirectShow 
            && d.Name.Contains(config.Camera.Name)
            && d.Characteristics.Any(c => c.PixelFormat == PixelFormats.JPEG && c.Width >= frameWidth && c.Height >= frameHeight))
            .FirstOrDefault();
            if(descriptor0 == null)
            {
                System.Console.WriteLine($"Failed to find camera: \"{config.Camera.Name}\" with required format!");
                hardware.Shutdown();
                return 1;
            }
            var characteristics = new VideoCharacteristics(
            PixelFormats.JPEG, 1920, 1080, 30);

            var cameraTask = Task.Run(async () =>
            {
                captureDevice = await descriptor0.OpenAsync(
                    characteristics,
                    bufferScope =>
                    {
                        var image = bufferScope.Buffer.CopyImage();
                        var frame = Mat.FromImageData(image.ToArray(), ImreadModes.Color);

                        //Remove and dispose all old frames
                        while (frames.TryTake(out var oldFrame))
                        {
                            oldFrame.Dispose();
                        }
                      
                        lock (matSupporterLock)
                        {
                            matSupporter?.Dispose();
                            matSupporter = frame.Clone();
                        }
                        // Add the latest frame
                        frames.Add(frame.Clone());
                    },
                    default
                );

                await captureDevice.StartAsync(default);
                await Task.Delay(Timeout.Infinite);
            });

            var running = true;

            float fpsSmooth = 0;
            ulong prevTimestamp = 0;

            void RemoveExpiredFiducialCacheItems()
            {
                foreach (var tag in fiducialLocationCache)
                {
                    ulong age = Utils.GetElapsedMicroseconds() - tag.Value.timestamp;
                    if (age > ((uint)config.CornerFrame.KeepTime * 1000)) // ms to microseconds
                    {
                        fiducialLocationCache.TryRemove(tag.Key, out var _); 
                    }
                }
            }
       
            Task DetectCornerFrames(Mat gray)
            {
                var cornerFrames = CornerFrame.DetectCornerFrames(gray, config.CornerFrame.MinArea, config.CornerFrame.MaxArea);

                RemoveExpiredFiducialCacheItems();

                foreach(var cornerFrame in cornerFrames)
                {
                    fiducialLocationCache.AddOrUpdate(cornerFrame.Id,
                        (cornerFrame.Points, Utils.GetElapsedMicroseconds()),
                        (id, old) => (cornerFrame.Points, old.timestamp)
                    );
                }

                gray.Dispose();
                return Task.CompletedTask;
            }

            List<Task> tasks = new List<Task>();

            try
            {
                while (running)
                {
                     if (frames.TryTake(out var oldFrame))
                    {
                        //Cv2.ImWrite(@"Z:\transfer\raw_image.jpg", oldFrame);
                        using var gray = new OpenCvSharp.Mat();
                        OpenCvSharp.Cv2.CvtColor(oldFrame, gray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                        oldFrame.Dispose();

                        if (transformer.IsCalibrated)
                        {
                            await DetectCornerFrames(gray.Clone());                    
                        }                    

                        db.Clear();
                        tasks.Clear();

                        if (!hardware.PollKeyboard(keyboard))
                             break;
             
                        hardware.NewVideoFrame();

                        foreach (var program in programs)
                        {
                            program.Present = program.Id < 0 ? true : false; // supporter and calibration
                            if (program.Resident)
                                tasks.Add(program.Run());
                        }

                        List<int> presentFrameIds = new List<int>();
                        foreach (var kvp in fiducialLocationCache)
                        {
                            var id = kvp.Key;
                            var points = kvp.Value.points;
                            presentFrameIds.Add(id);
                           
                            if (localFiducialLocationCache.TryGetValue(id, out var prevPoints) 
                                && !Utils.IsLocationChanged(prevPoints, points, config.CornerFrame.LocationDelta))
                            {
                                points = prevPoints;
                            }
                            else
                            {
                                localFiducialLocationCache[id] = points;
                            }

                            if(programs.TryGetProgram(id, out var program))
                            {
                                program.Present = true;                            
                                tasks.Add(program.Run(new Dictionary<string, object> { { "rawpoints", points } }));                                                      
                            }
                            else
                            {
                                var prog = new ProgramBlank(id);
                                prog.Present = true;
                                prog.SourceCodeRef = Reference.Create<string[]>(Reference.Lifetimes.Session, ["// Blank"]);
                                programs.Add(prog);
                                tasks.Add(prog.Run(new Dictionary<string, object> { { "rawpoints", points } }));
                            }
                        }

                        ulong timestamp = Utils.GetElapsedMicroseconds();

                        if (Claim.TryParse(-1, $"the clock time is ({timestamp / 1000000.0f})", out var timeClaim).Success)
                        {
                            db.AddStatement(timeClaim);
                        }

                        if (Claim.TryParse(-1, $"the last clock time was ({prevTimestamp / 1000000.0f})", out var lastTimeClaim).Success)
                        {
                            db.AddStatement(lastTimeClaim);
                        }
                        if (debugging)
                        {
                            float elapsed = prevTimestamp == 0 ? 0 : (timestamp - prevTimestamp) / 1000000.0f; // ns to seconds
                            float fps = elapsed > 0 ? 1.0f / elapsed : 0;
                            fpsSmooth = fpsSmooth * 0.9f + fps * 0.1f;

                            hardware.ShowDebugInfo($"    FPS:{fpsSmooth:0}");
                        }

                        lock (matSupporterLock)
                        {
                            string matRef = Reference.Create<Mat>(Reference.Lifetimes.Frame, matSupporter);
                            if (Claim.TryParse(-1, $"(-1) has appearance '{matRef}'", out var appearanceClaim).Success)
                            {
                                db.AddStatement(appearanceClaim);
                            }
                        }

                        prevTimestamp = timestamp;

                        await Task.WhenAll(tasks);

                        Illumination.RenderAll(presentFrameIds.Select(programs.GetProgram), programs.GetProgram(-2));

                        hardware.RenderFrame();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Exception: " + ex.ToString());
            }
            finally
            {
                running = false;
                cameraTask.Wait();
                hardware.Shutdown();                        
            }
            return 0;
        }

         byte[]? GetLatestJpegFrame()
        {
            lock (matSupporterLock)
            {
                try
                {
                    using var matCopy = matSupporter.Clone();
                    return matCopy.ImEncode(".jpg");
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
