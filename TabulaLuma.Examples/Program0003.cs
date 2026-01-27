using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabulaLuma.Examples
{
    public class Program0003 : ProgramBase
    {
        public override int Id => 3;
        protected override void RunImpl()
        {
            When("(-1) has appearance /image/")
                .And("(you) has width /width/")
                .And("(you) has height /height/", (b) =>
                {
                    var width = b.Float("width");
                    var height = b.Float("height");
                    var imageRef = b.String("image");

                    var ill = new Illumination();
                    ill.Image(imageRef, new Point2f(0, 0), width, height);
                    Wish($"(you) has illumination '{ill}'");
                });
        }
    }
}
