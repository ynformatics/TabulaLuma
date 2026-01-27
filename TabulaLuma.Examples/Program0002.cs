using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabulaLuma.Examples
{
    public class Program0002 : ProgramBase
    {
        public override int Id => 2;
        protected override void RunImpl()
        {
            When("(you) has width /width/")
             .And("(you) has height /height/")
             .And("the clock time is /t/", (b) =>
             {
                 var ill = new Illumination();
                 var width = b.Float("width");
                 var height = b.Float("height");
                 var centre = new Point2f(width / 2, height / 2);
                 ill.Circle(centre, 40, "red");

                 Wish($"(you) has illumination '{ill}'");

                 var t = (int)b.Float("t");
                 Wish($"(you) is labelled '{t}'");

             });
        }
    }
}
