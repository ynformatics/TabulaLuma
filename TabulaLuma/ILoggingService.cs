using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabulaLuma
{
    public interface ILoggingService
    {
        public void LogError(string error);
        public void LogErrors(IEnumerable<string> errors);
        public string[] GetErrors();
        public void ClearErrors();
    }
}
