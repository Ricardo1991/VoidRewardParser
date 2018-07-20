using System;

namespace VoidRewardParser.Entities
{
    [Serializable]
    public class PrimeItem
    {
        public string Name { get; set; }
        public Rarity Rarity { get; set; }
        public int Ducats { get; set; }

        public string Url => $"https://warframe.market/items/{ Uri.EscapeDataString(Name.ToLower().Replace(' ', '_'))}";
    }
}