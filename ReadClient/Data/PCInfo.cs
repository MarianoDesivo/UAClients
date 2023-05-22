using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadClient.Data
{
    static class PCInfo
    {
        static public string CpuUtilization
        {
            get; set;
        }
        static public float RAM
        {
            get; set;
        }
        static public int ProcessorCount
        {
            get; set;
        }
    }
}
