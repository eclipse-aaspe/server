using AasCore.Aas3_0;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events
{
    public class EventPayloadEntry
    {
        public string entryType { get; set; } // CREATE, UPDATE, DELETED below
        public string payloadType { get; set; } // Submodel, SME, AAS
        public string payload { get; set; } // JSON Serialization
        public string idShortPath { get; set; } // for SMEs only

        public EventPayloadEntry()
        {
            entryType = "";
            payloadType = "";
            payload = "";
            idShortPath = "";
        }
    }
    public class EventPayload
    {
        public string sourceUrl { get; set; }
        public string lastUpdate { get; set; }

        public List<EventPayloadEntry> eventEntries { get; set; }

        public static int collectSubmodelElements(List<ISubmodelElement> submodelElements, DateTime diffTime, List<string> entryTypes, List<EventPayloadEntry> entries)
        {
            int count = 0;
            foreach (var entryType in entryTypes)
            {
                string idShortPath = "";
                foreach (var sme in submodelElements)
                {
                    DateTime timeStamp = new DateTime();

                    switch (entryType)
                    {
                        case "CREATE":
                            timeStamp = sme.TimeStampCreate;
                            break;
                        case "UPDATE":
                            timeStamp = sme.TimeStampTree;
                            break;
                    }
                    if ((diffTime - timeStamp).TotalMilliseconds > 1)
                    {
                        List <ISubmodelElement> children = new List<ISubmodelElement>();
                        switch (sme)
                        {
                            case ISubmodelElementCollection smc:
                                children = smc.Value;
                                break;
                            case ISubmodelElementList sml:
                                children = sml.Value;
                                break;
                        }
                        if (true || children.Count == 0)
                        {
                            var j = Jsonization.Serialize.ToJsonObject(sme);
                            var e = new EventPayloadEntry();
                            e.entryType = entryType;
                            e.payloadType = "sme";
                            e.payload = j.ToJsonString();
                            e.idShortPath = idShortPath + sme.IdShort;
                            entries.Add(e);
                            count++;
                        }
                        else
                        {

                        }
                    }
                }
            }
            return count;
        }

        public static EventPayload CollectPayload(string sourceUrl, Submodel submodel, string diff)
        {
            var e = new EventPayload();
            e.sourceUrl = sourceUrl;
            e.lastUpdate = "";
            e.eventEntries = new List<EventPayloadEntry>();

            var isInitial = diff == "init";

            if (submodel != null)
            {
                e.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(submodel.TimeStampTree);

                if (isInitial)
                {
                    string json = string.Empty;
                    if (submodel != null)
                    {
                        var j = Jsonization.Serialize.ToJsonObject(submodel);
                        json = j.ToJsonString();
                    }

                    var entry = new EventPayloadEntry();
                    entry.entryType = "UPDATE";
                    entry.payloadType = "Submodel";
                    entry.payload = json;
                    e.eventEntries.Add(entry);
                }
                else
                {
                    List<string> entryTypes = new List<string>();
                    entryTypes.Add("UPDATE");
                    var diffTime = DateTime.Parse(diff);
                    collectSubmodelElements(submodel.SubmodelElements, diffTime, entryTypes, e.eventEntries);
                }
            }

            return e;
        }
    }
}
