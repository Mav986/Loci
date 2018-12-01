using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LocalChatIntel
{
    /// <summary>
    /// Object responsible for retrieving and calculating all pilot statistics
    /// </summary>
    class StatsController
    {
        private const string BaseURL = "https://zkillboard.com/api/";
        private HttpClient client;

        private const int Carrier = 547;
        private const int Dreadnought = 485;
        private const int ForceAuxiliary = 1538;
        private const int Supercarrier = 659;
        private const int Titan = 30;

        public StatsController(HttpClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Get stats for a pilot
        /// </summary>
        /// <param name="pilotId">A long integer matching a pilot in CCP's database</param>
        /// <returns></returns>
        public async Task<PilotStats> GetPilotStats(long pilotId)
        {
            Task<bool> superPilotTask = HasKillsWith(pilotId, Supercarrier, Titan);
            Task<bool> capPilotTask = HasKillsWith(pilotId, Carrier, Dreadnought, ForceAuxiliary);
            JObject json = await GetStatsJson(pilotId);

            PilotStats stats = new PilotStats
            {
                PilotId = pilotId,
                DangerPercent = GetDangerPercent(json),
                SoloPercent = GetSoloPercent(json),
                CapitalPilot = await capPilotTask,
                SuperPilot = await superPilotTask
            };

            return stats;
        }

        /// <summary>
        /// Get json containing a pilot's stats
        /// </summary>
        /// <param name="pilotId">A long integer matching a pilot in CCP's database</param>
        /// <returns>A JObject containing the pilot's stats</returns>
        private async Task<JObject> GetStatsJson(long pilotId)
        {
            string url = BaseURL + $"stats/characterID/{pilotId}/";
            string response = await HttpGetRequest(url);
            JObject json = JObject.Parse(response);

            return json;
        }

        /// <summary>
        /// Get the dangerous percentage
        /// </summary>
        /// <param name="json">JObject containing a pilot's danger ratio</param>
        /// <returns>An integer representing a pilots Dangerous Percentage</returns>
        private int GetDangerPercent(JObject json)
        {
            return json["dangerRatio"] == null ? 0 : (int)json["dangerRatio"];
        }

        /// <summary>
        /// Get the solo kill percentage
        /// </summary>
        /// <param name="json">JObject containing a pilot's gang ratio</param>
        /// <returns>An integer representing a pilots Dangerous Percentage</returns>
        private int GetSoloPercent(JObject json)
        {
            return json["gangRatio"] == null ? 0 : 100 - (int)json["gangRatio"];
        }

        /// <summary>
        /// Check if a pilot has kills with certain ship types
        /// </summary>
        /// <param name="pilotId">A long integer matching a pilot in CCP's database</param>
        /// <param name="shipTypes">Optional integers representing ship types in CCP's database</param>
        /// <returns>A boolean indicating if the pilot can fly any of the ships</returns>
        private async Task<bool> HasKillsWith(long pilotId, params int[] shipTypes)
        {
            bool CanFly = false;

            foreach(int type in shipTypes)
            {
                string killsWithType = await GetKillsWithType(pilotId, type);
                CanFly = CanFly || killsWithType.Length > 2;
            }

            return CanFly;
        }

        /// <summary>
        /// Get all killmails of a pilot while using a specific ship
        /// </summary>
        /// <param name="pilotId">A long integer matching a pilot in CCP's database</param>
        /// <param name="shipType">A long integer matching a ship type in CCP's database</param>
        /// <returns></returns>
        private async Task<string> GetKillsWithType(long pilotId, int shipType)
        {
            string killmailURL = BaseURL + $"characterID/{pilotId}/groupID/{shipType}/";
            return await HttpGetRequest(killmailURL);
        }

        /// <summary>
        /// Perform an HTTP GET request to a url
        /// </summary>
        /// <param name="url">A string containing a valid url</param>
        /// <returns>A string containing the GET request response</returns>
        /// <exception cref="HttpRequestException">Thrown when the GET request is not successful</exception>
        private async Task<string> HttpGetRequest(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new HttpRequestException(response.ReasonPhrase);
            }
        }
    }
}
