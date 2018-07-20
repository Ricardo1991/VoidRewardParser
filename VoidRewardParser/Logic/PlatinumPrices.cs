﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using VoidRewardParser.Entities;

namespace VoidRewardParser.Logic
{
    public static class PlatinumPrices
    {
        private static TimeSpan _expirationTimespan = TimeSpan.Parse(ConfigurationManager.AppSettings["PlatinumCacheExpiration"]);
        private static Dictionary<string, CacheEntry<long?>> _marketCache = new Dictionary<string, CacheEntry<long?>>();
        private const string _baseUrl = "https://api.warframe.market/v1/items/{0}/orders";

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

        public static async Task<long?> GetPrimePlatSellOrders(string primeName)
        {
            if (string.IsNullOrEmpty(primeName)) return null;

            if (_marketCache.TryGetValue(primeName, out CacheEntry<long?> cacheItem))
            {
                if (!cacheItem.IsExpired(_expirationTimespan))
                {
                    return cacheItem.Value;
                }
            }

            string partName = primeName;
            if (_fixedQueryStrings.ContainsKey(primeName))
            {
                partName = partName.Replace(partName, _fixedQueryStrings[partName]);
            }
            partName = partName.ToLower().Replace(' ', '_');

            if (partName.Equals("forma_blueprint"))
            {
                _marketCache[primeName] = new CacheEntry<long?>(0);
                return 0;
            }

            using (var client = new WebClient())
            {
                Console.WriteLine("Hitting API for " + primeName + " platinum value");
                var uri = new Uri(string.Format(_baseUrl, Uri.EscapeDataString(partName)));

                try
                {
                    string jsonData = await client.DownloadStringTaskAsync(uri);

                    dynamic result = JsonConvert.DeserializeObject(jsonData);

                    IEnumerable<dynamic> orders = result.payload.orders;
                    long? smallestPrice = orders
                        .Where(order => order.user.status == "online" || order.user.status == "ingame")
                        .Where(order => order.order_type == "sell")
                        .Min(order => order.platinum);

                    _marketCache[primeName] = new CacheEntry<long?>(smallestPrice);
                    return smallestPrice;
                }
                catch
                {
                    Console.Error.WriteLine("Error getting platinum price for " + primeName);
                    return null;
                }
            }
        }
    }
}