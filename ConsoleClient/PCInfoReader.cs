using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleClient
{

    internal class PCInfoReader
    {
        PerformanceCounter ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        public float ReadCPU()
        {
            return cpuCounter.NextValue();
        }
        public float ReadRam()
        {
            return ramCounter.NextValue();
        }
        public int GetProcessorCount()
        {
            return Environment.ProcessorCount;
        }
    }
}
