using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LocalChatIntel
{
    /// <summary>
    /// Object responsible for retrieving all pilot row information
    /// </summary>
    class LookupController
    {
        private Cache cache;
        private readonly HttpClient client;

        private List<PilotId> pilots;
        private List<Affiliation> affiliations;

        private ESIController esiController;
        private StatsController statController;

        public LookupController()
        {
            cache = new Cache();
            client = GetHttpClient();
            esiController = new ESIController(client);
            statController = new StatsController(client);
        }

        /// <summary>
        /// Get an HttpClient object with default headers
        /// </summary>
        /// <returns>A ready to use HttpClient object</returns>
        private HttpClient GetHttpClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Discord Mareau#5576");

            return client;
        }

        /// <summary>
        /// Get a list of rows from the cache
        /// </summary>
        /// <param name="names">A list of names to match to rows in the cache</param>
        /// <returns>A list of Row objects</returns>
        public List<Row> FromCache(ref List<string> names)
        {
            List<Row> rows = new List<Row>();

            foreach (string name in names.ToList())
            {
                if (cache.Contains(name))
                {
                    rows.Add(cache.Get(name));
                    names.Remove(name);
                }
            }

            return rows;
        }

        /// <summary>
        /// Get a list of Ids from a list of pilot names
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public async Task<List<PilotId>> GetIds(List<string> names)
        {
            pilots = await esiController.GetPilotList(names);

            return pilots;
        }

        /// <summary>
        /// Find groups for a list of pilots
        /// </summary>
        /// <param name="pilots"></param>
        /// <returns></returns>
        public async Task<List<Affiliation>> GetAffiliations(List<PilotId> pilots)
        {
            affiliations = new List<Affiliation>();
            List<GroupId> groupIds = await esiController.GetGroupIds(pilots);
            List<Group> groups = await esiController.GetGroups(groupIds);

            foreach (GroupId group in groupIds)
            {
                Affiliation characterGroup = new Affiliation
                {
                    Character_Id = group.Character_Id,
                    Corporation = groups.First(x => x.Id == group.Corporation_Id).Name,
                    Alliance = groups.FirstOrDefault(x => x.Id == group.Alliance_Id)?.Name
                };

                affiliations.Add(characterGroup);
            }

            return affiliations;
        }

        /// <summary>
        /// Find stats for a list of pilots
        /// </summary>
        /// <param name="pilots"></param>
        /// <returns></returns>
        public async Task<PilotStats> FindStatsFor(Affiliation affiliation)
        {
            PilotStats stats = await statController.GetPilotStats(affiliation.Character_Id);
            stats.Notes = GetNotesFor(stats);
            PilotId pilot = pilots.Find(x => x.Id == affiliation.Character_Id);
            cache.Add(pilot, affiliation, stats);

            return stats;
        }

        /// <summary>
        /// Get notes on a pilots statistics
        /// </summary>
        /// <param name="pilotStats">A PilotStats object</param>
        /// <returns>A string with any relevant notes on the PilotStats object</returns>
        public string GetNotesFor(PilotStats pilotStats)
        {
            string notes = "";
            string CapitalPilotNote = "Capital Pilot";
            string SuperPilotNote = "Supercap Pilot";

            if (pilotStats.CapitalPilot)
            {
                notes = CapitalPilotNote;
                if (pilotStats.SuperPilot)
                {
                    notes = SuperPilotNote;
                }
            }

            return notes;
        }
    }
}
