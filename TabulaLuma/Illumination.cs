using OpenCvSharp;
using System.Text.Json;

namespace TabulaLuma
{
    public class Illumination
    {
        static List<(IProgram, string, int)> illuminations = new List<(IProgram, string, int)>();
        static int illCount = 0; 
        int id = 0;
        public int Priority { get; set; } = 100;
        List<string> items = new List<string>();
        string illRef;
        static ITransformService transformService = ServiceProvider.GetService<ITransformService>();
        public Illumination()
        {
            id = illCount++;
            illRef = Reference.Create<string>(Reference.Lifetimes.Frame, "[]");
        }
    
        public static void Clear()
        {
            illuminations.Clear();
        }
        public static void Add(string illRef, IProgram program, int priority = 100)
        {
            if (illRef.StartsWith(Reference.Prefix))
                illRef = illRef.Substring(Reference.Prefix.Length).Trim();
            illuminations.Add((program, illRef, priority));
        }
        public static void RenderAll(IEnumerable<IProgram?> presentPrograms, IProgram? supporter)
        {
            if (supporter == null)
                return;

            foreach (var pair in illuminations.OrderBy(ill => ill.Item3))
            {
                if(!((ProgramBase) pair.Item1).Present) // program not present, skip
                    continue;
                var renderer = new Renderer((ProgramBase)pair.Item1);
                var json = Reference.Get<string>(pair.Item2);
                if(json != null)
                   renderer.Render(json);
                else
                {

                }
            }

            var errorLogs = ServiceProvider.GetService<ILoggingService>()?.GetErrors();
            var errIll = new Illumination();
            errIll.MultiLineText(errorLogs ?? new string[] { "No errors logged." }, new Point2f(20, 20), "red");
            var rendererErr = new Renderer((ProgramBase)supporter);
            rendererErr.Render(errIll.GetJson());

            if(transformService.Masking)
                foreach (var program in presentPrograms)
                {
                    if (program == null || program.Settings == null || !program.Settings.ContainsKey("width") || !program.Settings.ContainsKey("height"))
                        continue;
                    var width = (float)program.Settings["width"];
                    var height = (float)program.Settings["height"];

                    var maskRenderer = new Renderer((ProgramBase)program);
                    float m = 15.0f;
                    float l = 50f;
                    string colour = "black";
                    var maskIllumination = new Illumination();
                    maskIllumination.FilledQuad([new Point2f(-m, -m), new Point2f(l - m, -m), new Point2f(l - m, -1), new Point2f(-m, -1)], colour);
                    maskIllumination.FilledQuad([new Point2f(-m, -m), new Point2f(-1, -m), new Point2f(-1, l - m), new Point2f(-m, l - m)], colour);

                    maskIllumination.FilledQuad([new Point2f(width - l + m, -m), new Point2f(width + m, -m), new Point2f(width + m, -1), new Point2f(width - l + m, -1)], colour);
                    maskIllumination.FilledQuad([new Point2f(width +1, -m), new Point2f(width + m, -m), new Point2f(width + m, l - m), new Point2f(width +1, l - m)], colour);

                    maskIllumination.FilledQuad([new Point2f(-m, height +1), new Point2f(l - m, height +1), new Point2f(l - m, height + m), new Point2f(-m, height + m)], colour);
                    maskIllumination.FilledQuad([new Point2f(-m, height - l + m), new Point2f(-1, height - l + m), new Point2f(-1, height + m), new Point2f(-m, height + m)], colour);

                    maskIllumination.FilledQuad([new Point2f(width - l + m, height +1), new Point2f(width + m, height +1), new Point2f(width + m, height + m), new Point2f(width - l + m, height + m)], colour);
                    maskIllumination.FilledQuad([new Point2f(width +1, height - l + m), new Point2f(width + m, height - l + m), new Point2f(width + m, height + m), new Point2f(width +1, height + m)], colour);

                    maskRenderer.Render(maskIllumination.GetJson());
                }
        }

        public void Circle(Point2f centre, double radius, string stroke)
        {
            var obj = new
            {
                method = "circle",
                centre = new { X = centre.X, Y = centre.Y },
                radius = radius,
                stroke = stroke,
            };

            string json = JsonSerializer.Serialize(obj);
            items.Add(json);
        }
        public void FillCircle(Point2f centre, double radius, string fill = "white")
        {
            var obj = new
            {
                method = "fillCircle",
                centre = new { X = centre.X, Y = centre.Y },
                radius = radius,
                fill = fill
            };

            string json = JsonSerializer.Serialize(obj);
            items.Add(json);
        }
      
        public void Line(Point2f p1, Point2f p2, string stroke = "white")
        {
            var obj = new
            {
                method = "line",
                p1 = new { X = p1.X, Y = p1.Y },
                p2 = new { X = p2.X, Y = p2.Y },
                stroke = stroke
            };
            string json = JsonSerializer.Serialize(obj);
            items.Add(json);
        }
        public void DashedLine(Point2f p1, Point2f p2, string stroke = "white")
        {
            var obj = new
            {
                method = "dashedLine",
                p1 = new { X = p1.X, Y = p1.Y },
                p2 = new { X = p2.X, Y = p2.Y },
                stroke = stroke
            };
            string json = JsonSerializer.Serialize(obj);
            items.Add(json);
        }
        public void FilledQuad(Point2f[] points, string strColour, float alpha = 1)
        {
            var obj = new
            {
                method = "filledQuad",
                points = points.Select(p => new { X = p.X, Y = p.Y }).ToArray(),
                fill = strColour,
                alpha = alpha
            };
            string json = JsonSerializer.Serialize(obj);
            items.Add(json);
        }
 
        public void Text(string text, Point2f location, string fill, RenderOptions renderOptions = null)
        {
            var obj = new
            {
                method = "text",
                text = text,
                location = new { X = location.X, Y = location.Y },
                fill = fill,
                renderOptions = renderOptions
            };
            string json = JsonSerializer.Serialize(obj);
            items.Add(json);
        }
        public void MultiLineText(string[] text, Point2f location, string fill, RenderOptions renderOptions = null)
        {
            var obj = new
            {
                method = "multilinetext",
                text = text ,
                location = new { X = location.X, Y = location.Y },
                fill = fill,
                renderOptions = renderOptions
            };
           
            string json = JsonSerializer.Serialize(obj);
            items.Add(json);
        }
        public void Image(string imageRef, Point2f location, double width, double height)
        {
            var obj = new
            {
                method = "image",
                imageRef = imageRef,
                location = new { X = location.X, Y = location.Y },
                width = width,
                height = height
            };
            string json = JsonSerializer.Serialize(obj);
            items.Add(json);
        }

        string GetJson()
        {
            return "[" + string.Join(",", items) + "]";
        }
    
        public override string ToString()
        {  
            Reference.Set<string>(illRef, GetJson());
            return $"{Reference.Prefix}{illRef}";
        }
    }
}
