using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using VoidRewardParser.Entities;

namespace VoidRewardParser.Logic
{
    public static class DucatPrices
    {
        private static TimeSpan _expirationTimespan = TimeSpan.Parse(ConfigurationManager.AppSettings["PlatinumCacheExpiration"]);
        private static Dictionary<string, CacheEntry<int?>> _marketCache = new Dictionary<string, CacheEntry<int?>>();
        private const string _baseUrl = "https://api.warframe.market/v1/items/{0}";

        private static readonly string[] _removeBPSuffixPhrases = new[]{
            "Neuroptics", "Chassis", "Systems", "Harness", "Wings"
        };

        private static readonly Dictionary<string, string> _fixedQueryStrings = new Dictionary<string, string>()
        {
            { "Paris Prime Lower Limb", "Paris Prime  Lower Limb" },
            { "Paris Prime Grip", "Paris Prime  Grip" },
            { "Kavasa Prime Band", "Kavasa Prime Collar Band" },
            { "Kavasa Prime Kubrow Collar Blueprint", "Kavasa Prime Collar Blueprint" },
            { "Kavasa Prime Buckle", "Kavasa Prime Collar Buckle" },
            { "Lex Prime Receiver", "Lex Prime Reciever" },
        };

        public static async Task<int?> GetPrimePlatDucats(string primeName)
        {
            if (string.IsNullOrEmpty(primeName)) return null;

            if (_marketCache.TryGetValue(primeName, out CacheEntry<int?> cacheItem))
            {
                return cacheItem.Value;
            }

            var partName = primeName.ToLower().Replace(' ', '_');

            if (partName.Equals("forma_blueprint"))
            {
                _marketCache[primeName] = new CacheEntry<int?>(0);
                return 0;
            }

            using (var client = new WebClient())
            {
                var uri = new Uri(string.Format(_baseUrl, Uri.EscapeDataString(partName)));

                try
                {
                    string jsonData = await client.DownloadStringTaskAsync(uri);

                    dynamic result = JsonConvert.DeserializeObject(jsonData);

                    IEnumerable<dynamic> results = result.payload.item.items_in_set;
                    int? ducatValue = results
                        .Where(order => order.url_name == partName)
                        .Min(order => order.ducats);

                    _marketCache[primeName] = new CacheEntry<int?>(ducatValue);
                    return ducatValue;
                }
                catch
                {
                    Console.Error.WriteLine("Error getting ducats for " + primeName);
                    return null;
                }
            }
        }
    }
}