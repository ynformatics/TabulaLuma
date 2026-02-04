using OpenCvSharp;
using System.Diagnostics;
using Point = OpenCvSharp.Point;

namespace TabulaLuma
{
    public class CodeEditorProgram : ProgramBase
    {
        public override int Id => 0;
        int cursorX = 0;
        int cursorY = 0;
        List<string> sourceCode = new List<string>(){""};
        List<Tuple<int, string>> Errors = new List<Tuple<int, string>>();
        bool saving = false;
        Reference originalCodeRef = null;
        string[] clip;
        protected override void RunImpl()
        {
            Wish("(you) points 'up'");
            When("(you) points 'up' at /p/").And("/p/ has codeRef /codeRef/", (b) =>
            {
                var progId = b.Int("p");
                var codeRef = b.Ref<String[]>("codeRef");

                if (originalCodeRef == null || !codeRef.Equals(originalCodeRef))
                {
                    var code = codeRef.Value;
                    code = code.Select(line => ExpandLeadingTabs(line, 2)).ToArray();

                    originalCodeRef = codeRef;
                    this.sourceCode.Clear();
                    this.sourceCode.AddRange(code);
                    cursorX = 0;
                    cursorY = 0;
                    if (ProgramStore.TryGetProgram(progId, out var prog))
                        Errors.AddRange(prog.Errors); ;
                }

                if (sourceCode.Count == 0)
                    sourceCode.Add("");

                When($"(you) has cursorX /cursorX/ cursorY /cursorY/", b =>
                {
                    cursorX = b.Int("cursorX");
                    cursorY = b.Int("cursorY");
                });

                When("(99994) has new key /key/", (b) =>
                {
                    var key = b.Json<KeyEvent>("key");
                    var scancode = (KeyScanCodes)key.ScanCode;
                    var control = key.Control;

                    switch (scancode)
                    {
                        case KeyScanCodes.Backspace:
                            if (cursorX > 0)
                            {
                                sourceCode[cursorY] = sourceCode[cursorY].Remove(cursorX - 1, 1);
                                cursorX--;
                            }
                            else
                            {
                                if (cursorY > 0)
                                {
                                    cursorX = sourceCode[cursorY - 1].Length;
                                    sourceCode[cursorY - 1] = sourceCode[cursorY - 1] + sourceCode[cursorY];
                                    sourceCode.RemoveAt(cursorY);
                                    cursorY--;
                                }
                            }
                            break;
                        case KeyScanCodes.S:
                            if (control)
                            {
                                saving = true;
                                Task.Run(() =>
                                {
                                    var code = this.sourceCode.ToArray();
                                    var filePath = Path.Combine(EngineService.FileStorePath, $"{progId}.txt");
                                    File.WriteAllLines(filePath, code);
                                    var codeRef = Reference.Create<string[]>(Reference.Lifetimes.Session, this.sourceCode.ToArray());
                                    var prog = Compiler.CompileProgramBase("UserProgram", progId, codeRef, out Tuple<int, string>[] errors);
                                    saving = false;
                                    Errors.Clear();
                                    if (prog == null)
                                    {
                                        Errors.AddRange(errors);
                                    }
                                    else
                                    {
                                        ProgramStore.Add(prog as IProgram);
                                    }
                                });
                            }
                            break;
                        case KeyScanCodes.Return:
                            sourceCode.Insert(cursorY + 1, sourceCode[cursorY].Substring(cursorX));
                            if (cursorX > 0)
                                sourceCode[cursorY] = sourceCode[cursorY].Remove(cursorX);
                            cursorY++;
                            cursorX = 0;
                            break;
                        case KeyScanCodes.Down: // Down arrow
                            if (cursorY < sourceCode.Count - 1)
                                cursorY++;

                            break;
                        case KeyScanCodes.Up: // Up arrow
                            if (cursorY > 0)
                                cursorY--;
                            break;
                        case KeyScanCodes.Right: // Right arrow
                            if (cursorX < sourceCode[cursorY].Length)
                                cursorX++;
                            break;
                        case KeyScanCodes.Left: // Left arrow
                            if (cursorX > 0)
                                cursorX--;
                            break;
                        case KeyScanCodes.B:
                            if (control)
                            {
                                ProgramStore.Delete(progId);
                                cursorX = 0;
                                cursorY = 0;
                            }
                            break;
                        case KeyScanCodes.C: // copy all
                            if (control)
                            {
                                clip = sourceCode.ToArray();
                            }
                            break;
                        case KeyScanCodes.V: // paste
                            if (control && clip != null)
                            {
                                for (int i = 0; i < clip.Length; i++)
                                {
                                    if (i == 0)
                                    {
                                        sourceCode[cursorY] = sourceCode[cursorY].Insert(cursorX, clip[i]);
                                        cursorX += clip[i].Length;
                                    }
                                    else
                                    {
                                        sourceCode.Insert(cursorY + 1, clip[i]);
                                        cursorY++;
                                        cursorX = clip[i].Length;
                                    }
                                }
                            }
                            break;
                        default:
                            if (key.KeyPress)
                            {
                                sourceCode[cursorY] = sourceCode[cursorY].Insert(cursorX, key.KeyChar.ToString());
                                cursorX++;
                            }
                            break;
                    }

                    if (cursorX > sourceCode[cursorY].Length)
                        cursorX = sourceCode[cursorY].Length;

                    Remember($"[for this session] (you) has cursorX ({cursorX}) cursorY ({cursorY})");
                });

                var ill = new Illumination();

                ill.MultiLineText(sourceCode.ToArray(), new Point2f(0, 0), "white", new RenderOptions() { CursorX = cursorX, CursorY = cursorY });

                try
                {
                    var errorLines = new string[sourceCode.Count];
                    for (int i = 0; i < errorLines.Length; i++)
                    {
                        errorLines[i] = "";
                    }
                    foreach (var error in Errors)
                    {
                        if (error.Item1 - 1 >= 0 && error.Item1 - 1 < sourceCode.Count)
                        {
                            errorLines[error.Item1 - 1] = error.Item2;
                        }
                    }
                    ill.MultiLineText(errorLines.ToArray(), new Point2f(200, 0), "red");

                    int offset = 0;
                    foreach (var error in Errors.Where(e => e.Item1 < 0 || e.Item1 >= sourceCode.Count))
                    {
                        ill.Text(error.Item2, new Point2f(0, (sourceCode.Count + offset++) * 14), "red");
                    }
                }
                catch
                {
                }
                Wish($"(you) has illumination \"{ill}\"");

                Wish($"(you) is labelled {(saving ? "'Saving...'" : "''")}");
            });
        }
        public static string ExpandLeadingTabs(string line, int spacesPerTab = 2)
        {
            int i = 0;
            while (i < line.Length && line[i] == '\t')
                i++;
            if (i == 0)
                return line;
            return new string(' ', i * spacesPerTab) + line.Substring(i);
        }
    }
    public class CalibrationProgram : ProgramBase
    {
        public override bool Resident => true;
        public override int Id => 100000;
        static int count = 0;
        static int rectHeight = 200;
        static int rectWidth = 200;
        static int hueCountdown = 30; // drops to 0 when we have the hue
        Scalar lowerBlue = new Scalar();
        Scalar upperBlue = new Scalar();
        unsafe protected override void RunImpl()
        {
            When("(you) is calibrating", (b) =>
            {
                When($"(99994) has new key /key/", b =>
                {
                    var key = b.Json<KeyEvent>("key");
                    var scancode = (KeyScanCodes)key.ScanCode;
                    var control = key.Control;

                    switch (scancode)
                    {
                        case KeyScanCodes.Escape:
                            Forget("(you) is calibrating");
                            return;
                        case KeyScanCodes.S:
                            Remember($"[for (10) seconds] (you) is saving");
                            Remember($"(you) has calibration rectangle width ({rectWidth}) height ({rectHeight})");
                            return;
                        case KeyScanCodes.Down:
                            rectHeight -= 20;
                            break;
                        case KeyScanCodes.Up:
                            rectHeight += 20;
                            break;
                        case KeyScanCodes.Right:
                            rectWidth += 20;
                            break;
                        case KeyScanCodes.Left:
                            rectWidth -= 20;
                            break;
                    }
                });
                int frameWidth = 1920;
                int frameHeight = 1080;
                var frameCentre = new Point2f(frameWidth / 2, frameHeight / 2);
                var subRectWidth = 180;
                var subRectHeight = 180;

                Point2f[] calRectCorners = [
                        frameCentre + new Point2f(-rectWidth, -rectHeight),
                frameCentre + new Point2f(rectWidth, -rectHeight),
                frameCentre + new Point2f(rectWidth , rectHeight),
                frameCentre + new Point2f(-rectWidth , rectHeight)
                    ];

                var ill = new Illumination() { Priority = 0 };

                if (hueCountdown == 0)
                {                
                    int ord = -1;
                    var worldPts = TransformService.WorldPoints;

                    foreach (var corner in calRectCorners)
                    {
                        ord++;
                        Point2f offset = new Point2f(
                            corner.X < frameCentre.X ? subRectWidth : -subRectWidth,
                            corner.Y < frameCentre.Y ? subRectHeight : -subRectHeight);
                        Point2f[] subRectCorners = [
                            corner,
                            corner + new Point2f(offset.X, 0),
                            corner + offset,
                            corner + new Point2f(0, offset.Y)
                        ];
                        ill.FilledQuad(subRectCorners, "blue");
                        ill.Text($"({worldPts[ord].X},{worldPts[ord].Y})", corner + offset.Multiply(0.5f), "white");
                    }
                    Wish($"calibration has illumination '{ill}'");
                }
                else
                {
                    ill.FilledQuad([new Point2f(0,0), new Point2f(frameWidth,0), new Point2f(frameWidth,frameHeight), new Point2f(0,frameHeight)], "blue");
                    hueCountdown--;
                    Wish($"calibration has illumination '{ill}'");
                }

                When("(-1) has appearance /image/", (b) =>
                {
                    var img = Reference.Get<Mat>(b.String("image"));
                    if (img?.Data == 0)
                        return;
                    var mat = Mat.FromPixelData(img.Height, img.Width, MatType.CV_8UC3, img.Data, img.Step());
                   // Cv2.ImWrite(@"Z:\transfer\debug_image.bmp", mat);
                    var hsv = new Mat();
                    Cv2.CvtColor(mat, hsv, ColorConversionCodes.BGR2HSV);

                    if (hueCountdown == 0)
                    {
                        var blueMask = new Mat();
                        Cv2.InRange(hsv, lowerBlue, upperBlue, blueMask);
                       // Cv2.ImWrite(@"Z:\transfer\blue_mask.png", blueMask);
                        Cv2.FindContours(blueMask, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

                        contours = contours.Where(c =>
                        {
                            var area = Cv2.ContourArea(c);
                            return area > 5000;
                        }).ToArray();
                        contours = contours.Where(c =>
                        {
                            var r = Cv2.MinAreaRect(c);
                            return Math.Abs(r.Size.Height - r.Size.Width) < 15;
                        }).ToArray();

                        if (contours.Length == 4)
                        {
                            var illLock = new Illumination();
                            illLock.FillCircle(frameCentre, 5, "blue");
                            Wish($"calibration has illumination '{illLock}'");
                            var boxPoints = new List<Point2f>();
                            foreach (var contour in contours)
                            {
                                var rrect = Cv2.MinAreaRect(contour);
                                boxPoints.AddRange(Cv2.BoxPoints(rrect));
                            }
                            var topLeft = boxPoints.OrderBy(p => p.X + p.Y).First();
                            var topRight = boxPoints.OrderBy(p => -p.X + p.Y).First();
                            var bottomRight = boxPoints.OrderBy(p => -p.X - p.Y).First();
                            var bottomLeft = boxPoints.OrderBy(p => p.X - p.Y).First();

                            When("(you) is saving", b =>
                            {
                                Forget("(you) is saving");

                                TransformService.SetCamera([topLeft, topRight, bottomRight, bottomLeft]);
                                TransformService.SetProjector(calRectCorners);
                                SaveMemories();
                                var illConf = new Illumination();
                                illConf.Text("Saved!", frameCentre + new Point2f(0, 40), "white");
                                Debug.WriteLine("Saved!");
                                Remember($"[for (2) seconds] calibration has illumination '{illConf}'");
                            });
                        }
                    }
                    else
                    {
                        Vec3b hsvPixel = hsv.At<Vec3b>(hsv.Height/2, hsv.Width/2);
                        //Debug.WriteLine($"Hue: {hsvPixel.Item0} Sat: {hsvPixel.Item1} Val: {hsvPixel.Item2}");
                        lowerBlue = new Scalar(Math.Max(0, hsvPixel.Item0 - 5), 150, 150);
                        upperBlue = new Scalar(Math.Min(179, hsvPixel.Item0 + 5), 255, 255);

                    }
                });
            }).Otherwise(() =>
            {
                When($"(99994) has new key /key/", b =>
                {
                    var key = b.Json<KeyEvent>("key");
                    var scancode = (KeyScanCodes)key.ScanCode;
                    var control = key.Control;

                    if (scancode == KeyScanCodes.L && control)
                    {
                        hueCountdown = 30; // give it 30 frames to ensure a good sample
                        When("(you) has calibration rectangle width /width/ height /height/", (b) =>
                        {
                            rectHeight = b.Int("height");
                            rectWidth = b.Int("width");
                        });
                        Remember($"[for this session] (you) is calibrating");
                    }
                });
            });
        }
    }

    public class DeepCalibrationProgram : ProgramBase
    {
        public override bool Resident => true;
        public override int Id => 100001;
        static int count = 0;
        double minArea = 200;
        double maxArea = 200;
        int focus;
        int exposure;
        static int nonSquarishnessThreshold = 10;
        unsafe protected override void RunImpl()
        {
            When("(you) is deep calibrating", (b) =>
            {
                ServiceProvider.GetService<ITransformService>()!.Masking = false;

                When($"(99994) has new key /key/", b =>
                {
                var key = b.Json<KeyEvent>("key");
                var scancode = (KeyScanCodes)key.ScanCode;
                var control = key.Control;

                switch (scancode)
                {
                    case KeyScanCodes.Escape:
                        ServiceProvider.GetService<ITransformService>()!.Masking = true;

                        Forget("(you) is deep calibrating");
                        return;
                    case KeyScanCodes.S:
                        if (key.Control)
                        {
                            Remember("[for (2) seconds] (-2) is labelled 'Saving...' with priority (1000)");
                            var config = EngineService.Config;
                            config.CornerFrame.MinArea = minArea;
                            config.CornerFrame.MaxArea = maxArea;
                            config.Camera.Focus = focus;
                            config.Camera.Exposure = exposure;
                            config.Save();
                        }
                        return;
                    case KeyScanCodes.P:
                        EngineService.ShowPropertiesPage();
                            break;                      
                        case KeyScanCodes.N:
                            minArea += key.Shift ? 20 : -20;
                            break;
                        case KeyScanCodes.X:
                            maxArea += key.Shift ? 20 : -20;
                            break;
                    }
                });

                When("(-1) has appearance /image/", (b) =>
                {
                    var img = Reference.Get<Mat>(b.String("image"));
                    if (img == null || img.Data == 0)
                        return;
                    //     var bitmap = new Bitmap(img.Width, img.Height, img.Step, System.Drawing.Imaging.PixelFormat.Format24bppRgb, img.Data);
                    //        bitmap.Save(@"Z:\transfer\debug_image.bmp");
                    var grey = new Mat();
                    Cv2.CvtColor(img, grey, ColorConversionCodes.BGR2GRAY);
                    var thresh = new Mat();
                    Cv2.Threshold(grey, thresh, 0, 255, ThresholdTypes.Otsu);
                    var final = new Mat();
                    Cv2.CvtColor(thresh, final, ColorConversionCodes.GRAY2BGR);

                    Cv2.FindContours(thresh, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

                    contours = contours.Where(c =>
                    {
                        var area = Cv2.ContourArea(c);
                        return area >= minArea && area <= maxArea;
                    }).ToArray();


                    foreach (var contour in contours)
                    {
                        var area = Cv2.ContourArea(contour);
                        //     Debug.WriteLine(area);
                        var rrect = Cv2.MinAreaRect(contour);
                        if (Math.Abs(rrect.Size.Width - rrect.Size.Height) < nonSquarishnessThreshold)
                        {
                            Cv2.Polylines(final, new Point[][] { Cv2.BoxPoints(rrect).Select(p => (Point)p).ToArray() }, true, Scalar.Lime, 2);
                        }
                    }
                    Cv2.DrawContours(final, contours, -1, Scalar.Red, 2);

                    Cv2.PutText(final, $"miN area: {minArea} maX area: {maxArea} Press P for Camera Properties", new Point(10, 30), HersheyFonts.HersheySimplex, 1, Scalar.Yellow, 2);
                    var ill = new Illumination();
                    ill.Image(Reference.Create<Mat>(Reference.Lifetimes.Frame, final), new Point2f(0, 0), (float)img.Width, (float)img.Height);
                    Wish($"(-2) has illumination '{ill}'");
                });
            }).Otherwise(() =>
            {
                When($"(99994) has new key /key/", b =>
                {
                    var key = b.Json<KeyEvent>("key");
                    var scancode = (KeyScanCodes)key.ScanCode;
                    var control = key.Control;

                    if (scancode == KeyScanCodes.K && control)
                    {
                        minArea = EngineService.Config.CornerFrame.MinArea;
                        maxArea = EngineService.Config.CornerFrame.MaxArea;
                        Remember($"[for this session] (you) is deep calibrating");
                    }
                });
            });
        }
    }


    public class IsLabelledProgram : ProgramBase
    {
        public override bool Resident => true;
        public override int Id => 99998;

        unsafe protected override void RunImpl()
        {
            When("/p/ is labelled /label/ with priority /priority/")
                .And("/p/ has width /width/")
                .And("/p/ has height /height/", (b) =>
                {
                    var label = b.String("label");
                    int p = b.Int("p");
                    var width = b.Float("width");
                    var height = b.Float("height");

                    if (string.IsNullOrEmpty(label))
                    {
                        return; // empty label
                    }

                    var ill = new Illumination();
                    ill.Text(label, new Point2f(width / 2, height / 2), "yellow", new RenderOptions() { CentreText = true });
                    if (b.ContainsKey("priority"))
                        Wish($"({p}) has illumination \"{ill}\" with priority ({b.Int("priority")})");
                    else
                        Wish($"({p}) has illumination \"{ill}\"");
                });
        }
    }
    public class PointsProgram : ProgramBase
    {
        public override bool Resident => true;
        public override int Id => 99997;

        unsafe protected override void RunImpl()
        {
            When("/p/ points /direction/")
                .And("/p/ has width /width/")
                .And("/p/ has height /height/", (b) =>
                {
                    var direction = b.String("direction");
                    var p = b.Int("p");
                    var width = b.Float("width");
                    var height = b.Float("height");

                    Point2f pstart = new Point2f(0, 0);
                    Point2f pend = new Point2f(0, 0);

                    switch (direction)
                    {
                        case "down":
                            pstart = new Point2f(width / 2, height);
                            pend = pstart + new Point2f(0, 100);
                            break;
                        case "left":
                            pstart = new Point2f(0, height / 2);
                            pend = pstart + new Point2f(-100, 0);
                            break;
                        case "right":
                            pstart = new Point2f(width, height / 2);
                            pend = pstart + new Point2f(100, 0);
                            break;
                        case "up":
                            pstart = new Point2f(width / 2, 0);
                            pend = pstart + new Point2f(0, -100);
                            break;
                    }
                    var ill = new Illumination();
                    ill.DashedLine(pstart, pend, "gray");
                    Wish($"({p}) has illumination \"{ill}\"");

                    foreach (var q in ProgramStore.ProgramsContainingPoint(p, pend))
                    {
                        if (q != -1)
                            Claim($"({p}) points '{direction}' at ({q})");
                    }
                });
        }
    }
    public class IsHighlightedProgram : ProgramBase
    {
        public override bool Resident => true;
        public override int Id => 99996;

        unsafe protected override void RunImpl()
        {
            When("/p/ is highlighted /colour/").And("/p/ has width /width/").And("/p/ has height /height/", (b) =>
            {
                var strColour = b.String("colour");
                var p = b.Int("p");
                var width = b.Float("width");
                var height = b.Float("height");

                var ill = new Illumination();
                ill.FilledQuad([new Point2f(0, 0), new Point2f(width, 0), new Point2f(width, height), new Point2f(0, height)], strColour, 0.5F);
                Wish($"({p}) has illumination \"{ill}\"");
            });
        }
    }
    public class IsOutlinedProgram : ProgramBase
    {
        public override bool Resident => true;
        public override int Id => 99995;

        unsafe protected override void RunImpl()
        {
            When("/p/ is outlined /colour/").And("/p/ has width /width/").And("/p/ has height /height/", (b) =>
            {
                var strColour = b.String("colour");
                var p = b.Int("p");
                var width = b.Float("width");
                var height = b.Float("height");

                var ill = new Illumination();

                ill.Line(new Point2f(0, 0), new Point2f(width, 0), strColour);
                ill.Line(new Point2f(width, 0), new Point2f(width, height), strColour);
                ill.Line(new Point2f(width, height), new Point2f(0, height), strColour);
                ill.Line(new Point2f(0, height), new Point2f(0, 0), strColour);

                Wish($"({p}) has illumination \"{ill}\"");
            });
        }
    }
    public class KeyboardProgram : ProgramBase
    {
        public override bool Resident => true;
        public override int Id => 99994;

        unsafe protected override void RunImpl()
        {
            var keyboardService = ServiceProvider.GetService<IKeyboardService>();

            keyboardService.CaptureKeyboard(Id);

            while (keyboardService.TryGetKeyEvent(out KeyEvent keyEvent))
            {
                Claim($"(99994) has new key '{keyEvent}'"); // hardcode destination for now        
            }
        }
    }
    public class HasIlluminationProgram : ProgramBase
    {
        public override bool Resident => true;
        public override int Id => 99993;

        unsafe protected override void RunImpl()
        {
            void AddIllumination(string illRef, int progId, int priority = 100)
            {
                if (ProgramStore.TryGetProgram(progId, out var prog))
                {
                    Illumination.Add(illRef, prog, priority); ;
                }
            }

            When($"calibration has illumination /ill/", (b) =>
            {
                AddIllumination(b.String("ill"), -2);
            });

            When($"/p/ has illumination /ill/ with priority /priority/", (b) =>
            {
                AddIllumination(b.String("ill"), b.Int("p"), b.Int("priority"));
            });

            When($"/p/ draws 'circle' with x /x/ y /y/", b =>
            {
                var p = b.Int("p");
                var x = b.Float("x");
                var y = b.Float("y");
                var centre = new Point2f(x, y);
                var ill = new Illumination();
                ill.Circle(centre, 10, "red");
                Wish($"({p}) has illumination \"{ill}\"");
            });
            Wish($"(4) draws 'circle' with x (80) y (80)");
        }
    }
}
