using System.Linq;

namespace VoidRewardParser.Logic
{
    public class Warframe
    {
        /// <summary>
        /// Returns true if Warframe is running
        /// </summary>
        /// <returns></returns>
        public static bool WarframeIsRunning()
        {
#if DEBUG
            return System.Diagnostics.Process.GetProcesses().Any(p => string.Equals(p.ProcessName, "Warframe.x64") || string.Equals(p.ProcessName, "Warframe") || string.Equals(p.ProcessName, "notepad"));
#else
            return System.Diagnostics.Process.GetProcesses().Any(p => string.Equals(p.ProcessName, "Warframe.x64") || string.Equals(p.ProcessName, "Warframe"));
#endif
        }

        /// <summary>
        /// Get the Warframe process
        /// </summary>
        /// <returns></returns>
        public static System.Diagnostics.Process GetProcess()
        {
            foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
            {
#if DEBUG
                if (string.Equals(p.ProcessName, "Warframe.x64") || string.Equals(p.ProcessName, "Warframe") || string.Equals(p.ProcessName, "notepad"))

#else
                if (string.Equals(p.ProcessName, "Warframe.x64") || string.Equals(p.ProcessName, "Warframe"))

#endif
                {
                    return p;
                }
            }

            return null;
        }
    }
}