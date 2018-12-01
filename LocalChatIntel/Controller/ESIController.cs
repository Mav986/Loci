using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LocalChatIntel
{
    /// <summary>
    /// Object responsible for retreiving data from CCP's ESI endpoints
    /// </summary>
    class ESIController
    {
        private const string ESIBaseUrl = "https://esi.evetech.net/latest";
        private HttpClient client;

        public ESIController(HttpClient client)
        {
            this.client = client;
        }
        
        /// <summary>
        /// Get a list of Pilots
        /// </summary>
        /// <param name="names">A list of strings representing pilot names to be resolved</param>
        /// <returns>A list of Pilots</returns>
        public async Task<List<PilotId>> GetPilotList(List<string> names)
        {
            string url = ESIBaseUrl + "/universe/ids/?datasource=tranquility";
            JArray contentToSend = JArray.FromObject(names);
            JObject json = await JObjectPostRequest(url, contentToSend);

            return CreatePilotList(json, "characters");
        }

        /// <summary>
        /// Get Id's for each pilot's affiliated groups
        /// </summary>
        /// <param name="pilots">A list of Pilot objects</param>
        /// <returns>A list of Group Id objects</returns>
        public async Task<List<GroupId>> GetGroupIds(List<PilotId> pilots)
        {
            string url = ESIBaseUrl + "/characters/affiliation/?datasource=tranquility";
            JArray contentToSend = CreateJArray(pilots);
            JArray json = new JArray();
            if (contentToSend.Count > 0)
            {
                json = await JArrayPostRequest(url, contentToSend);
            }

            return CreateGroupIdList(json);

        }

        /// <summary>
        /// Get group details for each group Id
        /// </summary>
        /// <param name="groupIds">A list of GroupId objects</param>
        /// <returns>A list of Group objects</returns>
        public async Task<List<Group>> GetGroups(List<GroupId> groupIds)
        {
            string url = ESIBaseUrl + "/universe/names/?datasource=tranquility";
            JArray contentToSend = CreateJArray(groupIds);
            JArray json = new JArray();
            if (contentToSend.Count > 0)
            {
                json = await JArrayPostRequest(url, contentToSend);
            }

            return CreateGroupList(json);
        }

        /// <summary>
        /// Create a JArray from a List
        /// </summary>
        /// <param name="characters">A list of Pilot objects</param>
        /// <returns>A JArray containing a list of Pilot objects</returns>
        private JArray CreateJArray(List<PilotId> characters)
        {
            HashSet<long> noDupes = new HashSet<long>();

            foreach (PilotId character in characters)
            {
                noDupes.Add(character.Id);
            }

            return JArray.FromObject(noDupes);
        }

        /// <summary>
        /// Create a JArray from a List
        /// </summary>
        /// <param name="groups">A list of GroupId objects</param>
        /// <returns>A JArray containing a list of GroupId objects</returns>
        private JArray CreateJArray(List<GroupId> groups)
        {
            HashSet<long> noDupes = new HashSet<long>();

            foreach (GroupId group in groups)
            {
                noDupes.Add(group.Corporation_Id);
                if (group.Alliance_Id > 0)
                {
                    noDupes.Add(group.Alliance_Id);
                }
            }

            return JArray.FromObject(noDupes);
        }

        /// <summary>
        /// Make an HTTP POST request to a url
        /// </summary>
        /// <param name="url">A string containing a valid url</param>
        /// <param name="data">A JArray containing valid json data</param>
        /// <returns>A JObject representing the request response</returns>
        private async Task<JObject> JObjectPostRequest(string url, JArray data)
        {
            StringContent content = new StringContent(data.ToString());
            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();

            return JObject.Parse(result);
        }

        /// <summary>
        /// Create a List of Pilots from json
        /// </summary>
        /// <param name="json">A JObject containing valid pilot names</param>
        /// <param name="index">A string to be used as the json index</param>
        /// <returns>A list of Pilot objects</returns>
        private List<PilotId> CreatePilotList(JObject json, string index)
        {
            List<PilotId> result = new List<PilotId>();
            if (json.Count > 0)
            {
                foreach (JObject entry in json[index])
                {
                    result.Add(entry.ToObject<PilotId>());
                }
            }

            return result;
        }

        /// <summary>
        /// Make an HTTP POST request to a url
        /// </summary>
        /// <param name="url">A string containing a valid url</param>
        /// <param name="data">A JArray containing valid json data</param>
        /// <returns>A JArray representing the request response</returns>
        private async Task<JArray> JArrayPostRequest(string url, JArray data)
        {
            StringContent content = new StringContent(data.ToString());

            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();

            return JArray.Parse(result);
        }

        /// <summary>
        /// Create a list of GroupIds from json
        /// </summary>
        /// <param name="jArray">A JArray containing valid Pilot objects</param>
        /// <returns>A list of GroupId objects</returns>
        private List<GroupId> CreateGroupIdList(JArray jArray)
        {
            List<GroupId> result = new List<GroupId>();

            if (jArray.Count > 0)
            {
                foreach (JObject obj in jArray)
                {
                    result.Add(obj.ToObject<GroupId>());
                }
            }

            return result;
        }

        /// <summary>
        /// Create a list of Groups from json
        /// </summary>
        /// <param name="jArray">A JArray containing valid GroupId objects</param>
        /// <returns>A list of Group objects</returns>
        private List<Group> CreateGroupList(JArray jArray)
        {
            List<Group> result = new List<Group>();

            if (jArray.Count > 0)
            {
                foreach (JObject obj in jArray)
                {
                    result.Add(obj.ToObject<Group>());
                }
            }

            return result;
        }
    }
}
