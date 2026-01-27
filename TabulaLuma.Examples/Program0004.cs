using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabulaLuma.Examples
{
    public class Program0004 : ProgramBase
    {
        public override int Id => 4;
        protected override void RunImpl()
        {
            When("(you) has region /region/ on /supporter/")
                .And("/other/ has region /otherregion/ on /supporter/"
             , (b) =>
             {
                 var supporter = b.Int("supporter");
                 var region = b.Point2fArray("region");
                 var otherregion = b.Point2fArray("otherregion");
                 var other = b.Int("other");

                 if (other != 4)
                 {
                     var centre = (region[0] + region[2]).Multiply(.5f);
                     var othercentre = (otherregion[0] + otherregion[2]).Multiply(.5f);

                     var ill = new Illumination();
                     ill.Line(centre, othercentre, "yellow");
                     Wish($"({supporter}) has illumination '{ill}'");
                 }
             });
        }
    }
}
