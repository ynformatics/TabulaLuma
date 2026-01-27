using Hexa.NET.SDL3;
using OpenCvSharp;
using System.ComponentModel.Design;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TabulaLuma
{
   public class RenderOptions
    {
        public static RenderOptions Default => new RenderOptions();
        public RenderOptions() { }
        public bool CentreText { get; set; } = false;
        public int CursorX { get; set; } = -1;
        public int CursorY { get; set; } = -1;
    }
    unsafe public class Renderer
    {
        public static SDLRenderer* renderer;
        ProgramBase program;
        Mat transform;
        ITransformService TransformService => ServiceProvider.GetService<ITransformService>();
        public Renderer(ProgramBase program)
        {
            this.program = program;            
            this.transform = program.CalibrationSupporter 
                ? (Mat)program.LocalToWorldTransform 
                : TransformService.WorldToProjectorTransform * (Mat)program.LocalToWorldTransform;
        }
        public Color ColourFromText(string text)
        {
            return Color.FromName(text);
        }
        public void Render(string illuminationJson)
        {
            if (renderer == null)
                return;
            SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
            var commands = JsonNode.Parse(illuminationJson).AsArray();
            foreach (var cmd in commands)
            {
                string method = cmd["method"]?.GetValue<string>();
                switch (method)
                {
                    case "circle":
                        {
                            var centre = cmd["centre"];
                            float x = centre["X"].GetValue<float>();
                            float y = centre["Y"].GetValue<float>();
                            float radius = cmd["radius"].GetValue<float>();
                            string stroke = cmd["stroke"].GetValue<string>();
                            RenderDrawCircle(new Point2f(x, y), (int)radius, stroke);
                        }
                        break;
                    case "fillCircle":
                        {
                            var centre = cmd["centre"];
                            float x = centre["X"].GetValue<float>();
                            float y = centre["Y"].GetValue<float>();
                            float radius = cmd["radius"].GetValue<float>();
                            string fill = cmd["fill"].GetValue<string>();
                            RenderFillCircle(new Point2f(x, y), (int)radius, fill);
                        }
                        break;
                    case "line":
                        {
                            var p1 = cmd["p1"];
                            var p2 = cmd["p2"];
                            float x1 = p1["X"].GetValue<float>();
                            float y1 = p1["Y"].GetValue<float>();
                            float x2 = p2["X"].GetValue<float>();
                            float y2 = p2["Y"].GetValue<float>();
                            string lineColour = cmd["stroke"]?.GetValue<string>();
                            RenderLine(new Point2f(x1, y1), new Point2f(x2, y2), lineColour);
                        }
                        break;
                    case "filledQuad":
                        {
                            var pointsArray = cmd["points"].AsArray();
                            Point2f[] points = new Point2f[pointsArray.Count];
                            for (int i = 0; i < pointsArray.Count; i++)
                            {
                                float px = pointsArray[i]["X"].GetValue<float>();
                                float py = pointsArray[i]["Y"].GetValue<float>();
                                points[i] = new Point2f(px, py);
                            }
                            string fillColour = cmd["fill"].GetValue<string>();
                            float alpha = cmd["alpha"] != null ? cmd["alpha"].GetValue<float>() : 1.0F;
                            DrawFilledQuad(points, fillColour, alpha);
                        }
                        break;

                    case "dashedLine":
                        {
                            var dp1 = cmd["p1"];
                            var dp2 = cmd["p2"];
                            float dx1 = dp1["X"].GetValue<float>();
                            float dy1 = dp1["Y"].GetValue<float>();
                            float dx2 = dp2["X"].GetValue<float>();
                            float dy2 = dp2["Y"].GetValue<float>();
                            string dashedLineColour = cmd["stroke"]?.GetValue<string>();
                            RenderDashedLine(new Point2f(dx1, dy1), new Point2f(dx2, dy2), dashedLineColour);
                        }
                        break;
                    case "text":
                        {
                            var text = cmd["text"].GetValue<string>();
                            var locationNode = cmd["location"];
                            float x = locationNode["X"].GetValue<float>();
                            float y = locationNode["Y"].GetValue<float>();
                            Point2f location = new Point2f(x, y);
                            string textFill = cmd["fill"].GetValue<string>();
                            RenderOptions? renderOptions = cmd["renderOptions"] is JsonObject obj
                                    ? obj.Deserialize<RenderOptions>()
                                    : RenderOptions.Default;

                            RenderText(text, location, textFill, renderOptions);
                        }
                        break;
                    case "multilinetext":
                        {
                            var text = cmd["text"].AsArray().Select(node => node.GetValue<string>()).ToArray();
                            var locationNode = cmd["location"];
                            float x = locationNode["X"].GetValue<float>();
                            float y = locationNode["Y"].GetValue<float>();
                            Point2f location = new Point2f(x, y);
                            string textFill = cmd["fill"].GetValue<string>();
                            RenderOptions? renderOptions = cmd["renderOptions"] is JsonObject obj
                                    ? obj.Deserialize<RenderOptions>()
                                    : RenderOptions.Default;                         

                            RenderMultilineText(text, location, textFill, renderOptions);
                        }
                        break;
                    case "image":
                        {
                            var imageRef = cmd["imageRef"]?.GetValue<string>();
                            var locationNode = cmd["location"];
                            float x = locationNode["X"].GetValue<float>();
                            float y = locationNode["Y"].GetValue<float>();
                            Point2f location = new Point2f(x, y);
                            float width = cmd["width"].GetValue<float>();
                            float height = cmd["height"].GetValue<float>();

                            RenderImage(imageRef, location, width, height);
                        }
                        break;
                }
            }
        }
        public void DrawFilledQuad(Point2f[] points, string strColour, float alpha)
        {
            var colour = Color.FromName(strColour);
            SDLFColor colourValue = new SDLFColor(colour.R / 255.0F, colour.G / 255.0F, colour.B / 255.0F, colour.A / 255.0F);

            if(alpha < 1.0)
            {
                colourValue.A = colourValue.A * alpha;
            }
            points = Cv2.PerspectiveTransform(points, transform);
            int len = 3; //draw tringles
            SDLVertex* vertices = stackalloc SDLVertex[len];
            for (int i = 0; i < 3; i++) // 0,1,2 first triangle
            {
                vertices[i] = new SDLVertex
                {
                    Position = new SDLFPoint { X = points[i].X, Y = points[i].Y },
                    Color = colourValue,
                    TexCoord = new SDLFPoint { X = 0, Y = 0 }
                };
            }
            SDL.RenderGeometry(renderer, null, vertices, len, (int*)0, 0);

            for (int i = 0; i < 3; i++) // 0,2,3 second triangle
            {
                int j = i == 0 ? 0 : i + 1;
                vertices[i] = new SDLVertex
                {
                    Position = new SDLFPoint { X = points[j].X, Y = points[j].Y },
                    Color = colourValue,
                    TexCoord = new SDLFPoint { X = 0, Y = 0 }
                };
            }
            SDL.RenderGeometry(renderer, null, vertices, len, (int*)0, 0);
        }

        public void RenderLine(Point2f p1, Point2f p2, string penColour = "white")
        {
            var colour = ColourFromText(penColour);
            SDL.SetRenderDrawColor(renderer, colour.R, colour.G, colour.B, colour.A);
            var points = Cv2.PerspectiveTransform([p1, p2], transform);
            SDL.RenderLine(renderer, points[0].X, points[0].Y, points[1].X, points[1].Y);
        }
        public void RenderDashedLine(Point2f p1, Point2f p2, string penColour = "white")
        {
            var colour = ColourFromText(penColour);
            SDL.SetRenderDrawColor(renderer, colour.R, colour.G, colour.B, colour.A);
            var points = Cv2.PerspectiveTransform([p1, p2], transform);

            var length = p1.DistanceTo(p2);
            var dashLength = 10.0f;
            var gapLength = 5.0f;
            var direction = (p2 - p1) * (1.0 / length);
            float currentLength = 0.0f;
            while (currentLength < length)
            {
                var start = p1 + direction * currentLength;
                var endLength = Math.Min(dashLength, length - currentLength);
                var end = start + direction * endLength;
                var transformedStart = Cv2.PerspectiveTransform([start], transform).FirstOrDefault();
                var transformedEnd = Cv2.PerspectiveTransform([end], transform).FirstOrDefault();
                SDL.RenderLine(renderer, transformedStart.X, transformedStart.Y, transformedEnd.X, transformedEnd.Y);
                currentLength += dashLength + gapLength;
            }
        }
        public int RenderDrawCircle(Point2f centre, int radius, string stroke = "white")
        {
            var trans = Cv2.PerspectiveTransform([centre, centre + new Point2f(radius, 0)], transform);
            centre = trans[0];
            radius = (int)Math.Abs(trans[1].X - trans[0].X); // approx
          
            float x = centre.X;
            float y = centre.Y;
            int offsetx = 0;
            int offsety = radius;
            int d = radius - 1;
            int status = 0;
            var strokeColour = ColourFromText(stroke);
            SDL.SetRenderDrawColor(renderer, strokeColour.R, strokeColour.G, strokeColour.B, strokeColour.A);
            while (offsety >= offsetx)
            {
                SDL.RenderPoint(renderer, x + offsetx, y + offsety);
                SDL.RenderPoint(renderer, x + offsety, y + offsetx);
                SDL.RenderPoint(renderer, x - offsetx, y + offsety);
                SDL.RenderPoint(renderer, x - offsety, y + offsetx);
                SDL.RenderPoint(renderer, x + offsetx, y - offsety);
                SDL.RenderPoint(renderer, x + offsety, y - offsetx);
                SDL.RenderPoint(renderer, x - offsetx, y - offsety);
                SDL.RenderPoint(renderer, x - offsety, y - offsetx);

                if (status < 0)
                {
                    status = -1;
                    break;
                }

                if (d >= 2 * offsetx)
                {
                    d -= 2 * offsetx + 1;
                    offsetx += 1;
                }
                else if (d < 2 * (radius - offsety))
                {
                    d += 2 * offsety - 1;
                    offsety -= 1;
                }
                else
                {
                    d += 2 * (offsety - offsetx - 1);
                    offsety -= 1;
                    offsetx += 1;
                }
            }

            return status;
        }
        public bool RenderFillCircle(Point2f centre, int radius, string fill = "white")
        {
            var trans = Cv2.PerspectiveTransform([centre, centre + new Point2f(radius, 0)], transform);
            centre = trans[0];
            radius = (int)Math.Abs(trans[1].X - trans[0].X); // approx
            int offsetx = 0;
            int offsety = radius;
            int d = radius - 1;
            bool status = true;
            float x = centre.X;
            float y = centre.Y;

            var strokeColour = ColourFromText(fill);
            SDL.SetRenderDrawColor(renderer, strokeColour.R, strokeColour.G, strokeColour.B, strokeColour.A);

            while (offsety >= offsetx)
            {
                status = status && SDL.RenderLine(renderer, x - offsety, y + offsetx,
                                             x + offsety, y + offsetx);
                status = status && SDL.RenderLine(renderer, x - offsetx, y + offsety,
                                             x + offsetx, y + offsety);
                status = status && SDL.RenderLine(renderer, x - offsetx, y - offsety,
                                             x + offsetx, y - offsety);
                status = status && SDL.RenderLine(renderer, x - offsety, y - offsetx,
                                             x + offsety, y - offsetx);

                if (!status)
                {
                    break;
                }

                if (d >= 2 * offsetx)
                {
                    d -= 2 * offsetx + 1;
                    offsetx += 1;
                }
                else if (d < 2 * (radius - offsety))
                {
                    d += 2 * offsety - 1;
                    offsety -= 1;
                }
                else
                {
                    d += 2 * (offsety - offsetx - 1);
                    offsety -= 1;
                    offsetx += 1;
                }
            }

            return status;
        }
    
        unsafe static SDLTexture* CreateTextureFromText(SDLRenderer* renderer, string[] text, Color colour, string fontFamilyName, int emSize, out SizeF textSize, out SizeF charSize, out float cursorXPos, int cursorX = -1, int cursorY = -1 )
        {
            cursorXPos = 0;
            using (FontFamily fontFamily = new FontFamily(fontFamilyName))
            using (System.Drawing.Font font = new(fontFamily, emSize, FontStyle.Regular, GraphicsUnit.Pixel))
            using (System.Drawing.Bitmap bmp = new(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                var format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.MeasureTrailingSpaces };
                var lines = text.Length;
                charSize = g.MeasureString("M", font, new PointF(0, 0), format);
                if (cursorX >= 0 && cursorY >= 0 && cursorY < text.Length)
                {
                    cursorXPos = g.MeasureString(text[cursorY].Substring(0, Math.Min(cursorX, text[cursorY].Length)), font, new PointF(0, 0), format).Width;
                }

                var maxWidth = text.Max(t => g.MeasureString(t, font, new PointF(0, 0), format).Width);

                textSize = new SizeF(maxWidth, charSize.Height * lines);
                if(textSize.Width == 0 || textSize.Height == 0)
                {
                    return null;
                }
                using (System.Drawing.Bitmap textBmp = new Bitmap((int)textSize.Width, (int)textSize.Height))
                using (Graphics gText = Graphics.FromImage(textBmp))
                using (Brush brush = new SolidBrush(colour))
                {
                    gText.Clear(System.Drawing.Color.Transparent);
                    foreach(var textline in text.Select((t, i) => (t, i)))
                    {
                        gText.DrawString(textline.t, font, brush, new PointF(0, textline.i * charSize.Height), format);
                    }
                    var bmpdata = textBmp.LockBits(new Rectangle(0, 0, textBmp.Width, textBmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    var bmpSurface = SDL.CreateSurfaceFrom(textBmp.Width, textBmp.Height, SDLPixelFormat.Bgra32, (void*)bmpdata.Scan0, textBmp.Width * 4);
                    var bmpTexture = SDL.CreateTextureFromSurface(renderer, bmpSurface);
                    SDL.SetTextureBlendMode(bmpTexture, SDLBlendMode.Blend);
                    textBmp.UnlockBits(bmpdata);
                    SDL.DestroySurface(bmpSurface);
                    return bmpTexture;
                }
            }
        }     
       
        public void RenderText(string text, Point2f location, string stroke, RenderOptions? renderOptions = null)
        {
            RenderMultilineText([text], location, stroke, renderOptions);
        }
        public void RenderMultilineText(string[] text, Point2f location, string stroke, RenderOptions? renderOptions = null)
        {
            if(renderOptions == null)
                renderOptions = RenderOptions.Default;

            location = Cv2.PerspectiveTransform([location], transform).FirstOrDefault();

            if (text.Length > 0)
            {
                var labeling = CreateTextureFromText(renderer, text, ColourFromText(stroke),
                    "Consolas", 14, out SizeF textSize, out SizeF charSize, out float cursorXPos, 
                    renderOptions.CursorX, renderOptions.CursorY);
                if (labeling != null)
                {
                    if(renderOptions.CentreText)
                    {
                        location.X -= textSize.Width / 2;
                        location.Y -= textSize.Height / 2;
                    }
                    var dstRect = new SDLFRect()
                    {
                        X = location.X,
                        Y = location.Y,
                        W = textSize.Width,
                        H = textSize.Height
                    };
                    if (program == null || program.Settings == null)
                        return;
              
                    var centre = new SDLFPoint(0, 0);
                    var angleRad = GetProgramAngle();
                    var angleDeg = (float)(angleRad * 180.0 / Math.PI);
                    SDL.RenderTextureRotated(renderer, labeling, null, &dstRect, (float)angleDeg, &centre, SDLFlipMode.None);
                    SDL.DestroyTexture(labeling);

                    if ( renderOptions.CursorX >= 0 && renderOptions.CursorY >= 0)
                    {
                        var yDist = renderOptions.CursorY * charSize.Height;
                        var xDist = cursorXPos;
                        var pt1 = new Point2f((float)(Math.Cos(angleRad) * xDist - Math.Sin(angleRad) * yDist + location.X),
                                              (float)(Math.Sin(angleRad) * xDist + Math.Cos(angleRad) * yDist + location.Y));
                        var pt2 = pt1 + new Point2f((float)(-Math.Sin(angleRad) * charSize.Height), (float)(Math.Cos(angleRad) * charSize.Height));

                        SDL.RenderLine(renderer, pt1.X, pt1.Y, pt2.X, pt2.Y);
                    }
                }
            }           
        }

        float GetProgramAngle()
        {
            float angleRad = 0;
            if (!program.Supporter && !program.CalibrationSupporter)
            {
                var br = TransformService.WorldToProjector((Point2f)program.Settings["bottomRight"]);
                var bl = TransformService.WorldToProjector((Point2f)program.Settings["bottomLeft"]);

                float dx = br.X - bl.X;
                float dy = br.Y - bl.Y;
                angleRad = (float)Math.Atan2(dy, dx);
            }
            return angleRad;
        }

        public void RenderImage(string imageRef, Point2f location, float width, float height)
        {
            if(renderer == null || imageRef == null)
                return;
            var corners = new Point2f[]
            {
                location,
                new Point2f(location.X + width, location.Y),
                new Point2f(location.X + width, location.Y + height),
                new Point2f(location.X, location.Y + height)
            };
            corners = Cv2.PerspectiveTransform(corners, transform);

            var imgRef = Reference.Get<Mat>(imageRef);
            if (imgRef != null)
            {
                var mat = imgRef;
                SDLSurface* imgSurface;
                if (mat.Step() == mat.Width) // grayscale image, need to expand to BGR
                {
                    if (mat.Data == IntPtr.Zero || mat.Width <= 0 || mat.Height <= 0 || mat.Step() < mat.Width)
                        return;

                    int srcWidth = mat.Width;
                    int srcHeight = mat.Height;
                    long srcStep = mat.Step();
                    int dstStep = srcWidth * 3;
                    int dstSize = dstStep * srcHeight;
                    byte[] bgrBuffer = new byte[dstSize];

                    unsafe
                    {
                        byte* src = (byte*)mat.Data;
                        fixed (byte* dst = bgrBuffer)
                        {
                            for (int y = 0; y < srcHeight; y++)
                            {
                                byte* srcRow = src + y * srcStep;
                                byte* dstRow = dst + y * dstStep;
                                for (int x = 0; x < srcWidth; x++)
                                {
                                    byte gray = srcRow[x];
                                    dstRow[x * 3 + 0] = gray; // B
                                    dstRow[x * 3 + 1] = gray; // G
                                    dstRow[x * 3 + 2] = gray; // R
                                }
                            }
                        }
                    }

                    // Create SDL surface from the expanded BGR buffer
                    unsafe
                    {
                        fixed (byte* bgrPtr = bgrBuffer)
                        {
                            imgSurface = SDL.CreateSurfaceFrom(
                                srcWidth,
                                srcHeight,
                                SDLPixelFormat.Bgr24,
                                bgrPtr,
                                dstStep
                            );                      
                        }
                    }
                }
                else
                {
                    imgSurface = SDL.CreateSurfaceFrom(mat.Width, mat.Height, SDLPixelFormat.Bgr24, (void*)mat.Data, (int)mat.Step());
                }
                    //var bitmap = new Bitmap(img.Width, img.Height, img.Step, System.Drawing.Imaging.PixelFormat.Format24bppRgb, img.Data);
                    //bitmap.Save(@"C:\temp\debug_image.bmp");

                if (imgSurface != null)
                {
                    var imgTexture = SDL.CreateTextureFromSurface(renderer, imgSurface);
                    SDL.SetTextureBlendMode(imgTexture, SDLBlendMode.Blend);
                    SDL.DestroySurface(imgSurface);
                    var dstRect = new SDLFRect()
                    {
                        X = corners[0].X,
                        Y = corners[0].Y,
                        W = corners[1].X - corners[0].X,
                        H = corners[3].Y - corners[0].Y
                    };
                    var centre = new SDLFPoint(0, 0);
                    var angleRad = GetProgramAngle();
                    var angleDeg = (float)(angleRad * 180.0 / Math.PI);
                    SDL.RenderTextureRotated(renderer, imgTexture, null, &dstRect, angleDeg, ref centre, SDLFlipMode.None);
                    SDL.DestroyTexture(imgTexture);
                }
                else
                {
                    var error = SDL.GetErrorS();
                }                
            }
        }
    }
}
