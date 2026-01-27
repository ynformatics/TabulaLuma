
namespace TabulaLuma.Program00005
{
    public class Program0005 : ProgramBase
    {
        public override int Id => 11;
        public override bool Resident => true;
        protected override void RunImpl()
        {
            Wish("(1) is labelled 'Page 1'");          
        }
    }
}
