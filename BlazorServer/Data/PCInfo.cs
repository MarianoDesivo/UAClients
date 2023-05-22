using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorServer.Data
{
    static class PCInfo
    {
        static public string CpuUtilization
        {
            get; set;
        }
        static public string RAM
        {
            get; set;
        }
        static public string ProcessorCount
        {
            get; set;
        }
    }
}
