using System;
using System.Collections.Generic;

namespace VoidRewardParser.Logic
{
    public static class PrimePartQueryFix
    {
        private static readonly string[] _removeBPSuffixPhrases = new[]{
            "Neuroptics", "Chassis", "Systems", "Harness", "Wings"
        };

        private static readonly Dictionary<string, string> _fixedQueryStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Kavasa Prime Band", "Kavasa Prime Collar Band" },
            { "Kavasa Prime Kubrow Collar Blueprint", "Kavasa Prime Collar Blueprint" },
            { "Kavasa Prime Buckle", "Kavasa Prime Collar Buckle" },
            { "Odonata Prime Harness Blueprint", "Odonata Prime Harness" },
            { "Odonata Prime Systems Blueprint", "Odonata Prime Systems" },
            { "Odonata Prime Wings Blueprint", "Odonata Prime Wings" },
        };

        public static string FixQueryString(string partName)
        {
            if (_fixedQueryStrings.ContainsKey(partName))
            {
                partName = partName.Replace(partName, _fixedQueryStrings[partName]);
            }
            partName = partName.ToLower().Replace(' ', '_');

            return partName;
        }
    }
}