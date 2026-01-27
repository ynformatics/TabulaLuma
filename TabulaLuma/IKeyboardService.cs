using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabulaLuma
{
    public interface IKeyboardService
    {
        /// <summary>
        /// Captures the keyboard input for the specified program id.
        /// </summary>
        /// <remarks>Receives keyboard input to this Id. If the Id has changed since a previous call to CaptureKeyboard 
        /// then the keyboard buffer will be cleared.</remarks>
        /// <param name="Id">The unique identifier of the program for which keyboard input should be captured.</param>
        public void CaptureKeyboard(int Id);
        public bool TryGetKeyEvent(out KeyEvent keyEvent);
    }
}
