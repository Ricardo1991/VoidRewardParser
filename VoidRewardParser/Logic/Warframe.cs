using System.Linq;

namespace VoidRewardParser.Logic
{
    public class Warframe
    {
        public static bool WarframeIsRunning()
        {
            return System.Diagnostics.Process.GetProcesses().Any(p => string.Equals(p.ProcessName, "Warframe.x64") || string.Equals(p.ProcessName, "Warframe"));
        }
    }
}