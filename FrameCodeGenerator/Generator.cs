using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;

public class Generator
{
    static string GenRgba(Color color)
    {
        double a = color.A / 255.0;
        return $"rgba({color.R}, {color.G}, {color.B}, {a})";
    }

    static string GenGridSquare(int rowNum, int colNum, Color pixel)
    {
        string rgba = GenRgba(pixel);
        string id = $"box{rowNum}-{colNum}";
        return $"\t<rect width=\"1\" height=\"1\" x=\"{rowNum}\" y=\"{colNum}\" fill=\"{rgba}\" id=\"{id}\"/>\n";
    }

    public static string GenFrameTagSvg(UInt16 code, float mmSize)
    {
        int gridSize = 9;
        var svg = new StringBuilder();
        svg.AppendLine("<?xml version=\"1.0\" standalone=\"yes\"?>");
        svg.AppendLine($"<svg width=\"{mmSize}mm\" height=\"{mmSize}mm\" viewBox=\"0,0,{gridSize},{gridSize}\" xmlns=\"http://www.w3.org/2000/svg\">");

        // draw borders
        for (int y = 0; y < gridSize; y++)
        {
            svg.Append(GenGridSquare(0, y, Color.Black));
        }
        for (int x = 0; x < gridSize; x++)
        {
            svg.Append(GenGridSquare(x, 0, Color.Black));
        }

        //draw barcodes
        for (int y = gridSize - 1; y > 0; y--)
        {
            svg.Append(GenGridSquare(1, y, (code & (1 << (gridSize - 1 - y))) != 0 ? Color.Black : Color.White));
        }
        for( int x = 2; x < gridSize - 1; x++)
        {
            svg.Append(GenGridSquare(x, 1, (code & (1 << (gridSize - 1 + x))) != 0 ? Color.Black : Color.White));
        }
        var setBits = System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount(code);   
        svg.Append(GenGridSquare(gridSize - 1, 1, (setBits % 2) == 0 ? Color.Black : Color.White));

        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    static int ComLine(string[] args)
    {
        int id = -1;
        int mmSize = 40; // Default size
        string tagFile = "tag41_12_00000"; // Default tag family
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Tag2Svg <tag_id> [size_mm]");
            Console.WriteLine("Default [size_mm] = 40");
            Console.WriteLine("Example: Tag2Svg 0 40");
            return 1;
        }
        if (args.Length > 0)
        {
            id = Convert.ToInt32(args[0]);
            tagFile = $"tag41_12_{id:00000}";
        }
        if (args.Length > 1)
        {
            mmSize = Convert.ToInt32(args[1]);
        }
        var client = new HttpClient();
        var res = client.GetAsync($"https://raw.githubusercontent.com/AprilRobotics/apriltag-imgs/master/tagStandard41h12/{tagFile}.png").GetAwaiter().GetResult();

        byte[] bytes = res.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

        using (Image image = Image.FromStream(new MemoryStream(bytes)))
        {


            using var bitmap = new Bitmap(image);
        //    string svg = GenAprilTagSvg(bitmap.Width, bitmap.Height, bitmap, mmSize, id);

        //    File.WriteAllText(@$"C:\temp\{tagFile}_{mmSize}mm.svg", svg);
        }
        return 0;
    }
}