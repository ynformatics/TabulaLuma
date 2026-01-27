using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabulaLuma
{
    public interface IEngineService
    {
        public string FileStorePath { get; }
        public int Focus { get; set; }
        public int Exposure { get; set; }
        public Config Config { get; }
        public Database Database { get; }
    }
}
