using System.Diagnostics;
using System.Linq;

namespace VoidRewardParser.Logic
{
    public class Warframe
    {
        public static bool WarframeIsRunning()
        {
            return Process.GetProcesses().Any(p => string.Equals(p.ProcessName, "Warframe.x64") || string.Equals(p.ProcessName, "Warframe"));
        }
    }
}