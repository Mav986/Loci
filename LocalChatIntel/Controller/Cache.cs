using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalChatIntel
{
    /// <summary>
    /// Object responsible for caching data
    /// </summary>
    class Cache
    {
        private List<Row> cache;

        public Cache()
        {
            cache = new List<Row>();
        }
        
        /// <summary>
        /// Add a row to the cache
        /// </summary>
        /// <param name="pilot">Pilot object containing Id and Name</param>
        /// <param name="affiliation">Affiliation object containing Corporation and Alliance</param>
        /// <param name="stats">PilotStats object containing DangerPercent, SoloPercent, and Notes</param>
        public void Add(PilotId pilot, Affiliation affiliation, PilotStats stats)
        {
            Row row = new Row
            {
                Pilot_Name = pilot.Name,
                Pilot_Id = pilot.Id,
                Corp_Name = affiliation.Corporation,
                Alliance_Name = affiliation.Alliance,
                Danger_Percent = stats.DangerPercent,
                Solo_Percent = stats.SoloPercent,
                Notes = stats.Notes
            };

            if (!Contains(row.Pilot_Name))
            {
                cache.Add(row);
            }
        }

        /// <summary>
        /// Check if a pilot is in the cache
        /// </summary>
        /// <param name="name">Pilot name</param>
        /// <returns>True if pilot is cached, false otherwise</returns>
        public bool Contains(string name)
        {
            return cache.Exists(x => x.Pilot_Name == name);
        }

        /// <summary>
        /// Get a Row from the cache
        /// </summary>
        /// <param name="name">Pilot name to identify row</param>
        /// <returns>Row containing the pilot's information</returns>
        public Row Get(string name)
        {
            return cache.First(x => x.Pilot_Name == name);
        }
    }
}
