using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabulaLuma
{
    public interface IHardware
    {
        public nint Initialise(Config config);
        public void Shutdown();
        public void NewVideoFrame();

        public void RenderFrame();

        public bool PollKeyboard(Keyboard keyboard);
        public void ShowDebugInfo(string msg);
    }
}
