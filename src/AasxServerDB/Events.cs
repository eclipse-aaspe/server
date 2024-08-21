using AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TimeStamp;
using System.Runtime.Intrinsics.X86;

namespace Events
{
    public class EventPayloadEntry
    {
        public string entryType { get; set; } // CREATE, UPDATE, DELETED below
        public string lastUpdate { get; set; } // timeStamp for this entry
        public string payloadType { get; set; } // Submodel, SME, AAS
        public string payload { get; set; } // JSON Serialization
        public string submodelId { get; set; } // ID of related Submodel
        public string idShortPath { get; set; } // for SMEs only

        public EventPayloadEntry()
        {
            entryType = "";
            lastUpdate = "";
            payloadType = "";
            payload = "";
            submodelId = "";
            idShortPath = "";
        }
    }
    public class EventPayload
    {
        public string sourceUrl { get; set; } // API endpoint, for new values: sourceUrl+lastUpdate
        public string lastUpdate { get; set; } // latest timeStamp for all entries

        public List<EventPayloadEntry> eventEntries { get; set; }

        public static int collectSubmodelElements(List<ISubmodelElement> submodelElements, DateTime diffTime, List<string> entryTypes, string submodelId, string idShortPath, List<EventPayloadEntry> entries, List<ISubmodelElement> diffValue)
        {
            int count = 0;
            foreach (var entryType in entryTypes)
            {
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
                    if ((timeStamp - diffTime).TotalMilliseconds > 1)
                    {
                        bool tree = false;
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
                        if (children.Count != 0)
                        {
                            if (entryType == "UPDATE")
                            {
                                if (sme.TimeStampTree > sme.TimeStamp)
                                {
                                    tree = true;
                                }
                            }
                        }
                        if (!tree)
                        {
                            diffValue.Add(sme);
                            var j = Jsonization.Serialize.ToJsonObject(sme);
                            var e = new EventPayloadEntry();
                            e.entryType = entryType;
                            e.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(timeStamp);
                            e.payloadType = "sme";
                            e.payload = j.ToJsonString();
                            e.submodelId = submodelId;
                            e.idShortPath = idShortPath + sme.IdShort;
                            entries.Add(e);
                            count++;
                        }
                        else
                        {
                           count += collectSubmodelElements(children, diffTime, new List<String> { entryType }, submodelId, idShortPath + sme.IdShort + ".", entries, diffValue);
                        }
                    }
                }
            }
            return count;
        }

        public static EventPayload CollectPayload(string sourceUrl, Submodel submodel, string diff, List<ISubmodelElement> diffValue)
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
                    e.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(submodel.TimeStampTree);
                    entry.payloadType = "submodel";
                    entry.submodelId = submodel.Id;
                    entry.payload = json;
                    entry.lastUpdate = e.lastUpdate;
                    e.eventEntries.Add(entry);
                }
                else
                {
                    List<string> entryTypes = new List<string>();
                    entryTypes.Add("UPDATE");
                    var diffTime = DateTime.Parse(diff);
                    collectSubmodelElements(submodel.SubmodelElements, diffTime, entryTypes, submodel.Id, "", e.eventEntries, diffValue);
                }
            }

            return e;
        }

        public static int changeData(string json, AdminShellPackageEnv[] env, out string lastDiffValue, out string statusValue, List<ISubmodelElement> diffValue)
        {
            lastDiffValue = "";
            statusValue = "ERROR";
            int count = 0;

            EventPayload eventPayload = JsonSerializer.Deserialize<Events.EventPayload>(json);
            var dt = TimeStamp.TimeStamp.StringToDateTime(eventPayload.lastUpdate);
            dt = DateTime.Parse(eventPayload.lastUpdate);
            lastDiffValue = TimeStamp.TimeStamp.DateTimeToString(dt);

            foreach (var entry in eventPayload.eventEntries)
            {
                var submodelId = entry.submodelId;
                ISubmodel submodel = null;
                ISubmodel receiveSubmodel = null;
                IAssetAdministrationShell aas = null;
                AasCore.Aas3_0.Environment aasEnv = null;
                int index = -1;

                for (int i = 0; i < env.Length; i++)
                {
                    var package = env[i];
                    if (package != null)
                    {
                        aasEnv = package.AasEnv;
                        if (aasEnv != null)
                        {
                            var submodels = aasEnv.Submodels.Where(a => a.Id.Equals(submodelId));
                            if (submodels.Any())
                            {
                                submodel = submodels.First();
                                // index = aasEnv.Submodels.IndexOf(submodel);
                                index = i;
                                break;
                            }
                        }
                    }
                }

                if (entry.payloadType == "submodel")
                {
                    MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(entry.payload));
                    JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                    receiveSubmodel = Jsonization.Deserialize.SubmodelFrom(node);
                    receiveSubmodel.TimeStampCreate = dt;
                    receiveSubmodel.SetTimeStamp(dt);
                    receiveSubmodel.SetAllParents(dt);

                    if (receiveSubmodel != null)
                    {
                        if (index != -1) // submodel exisiting
                        {
                            aasEnv.Submodels.Remove(submodel);
                            aas = aasEnv.FindAasWithSubmodelId(submodel.Id);

                            // aasEnv.Submodels.Insert(index, receiveSubmodel);
                            aasEnv.Submodels.Add(receiveSubmodel);
                            env[index].setWrite(true);
                            count++;
                            Console.WriteLine("Event Submodel: " + receiveSubmodel.Id);
                        }
                        else
                        {
                            return 0;

                            index = 0;
                            aasEnv = env[index].AasEnv;
                            aas = aasEnv.AssetAdministrationShells[0];
                            aas.Submodels.Add(receiveSubmodel.GetReference());
                        }
                    }
                }

                if (entry.payloadType == "sme" && submodel != null)
                {
                    count += changeSubmodelElement(entry, submodel.SubmodelElements, "", diffValue);
                    if (count > 0)
                    {
                        env[index].setWrite(true);
                    }
                }
            }

            statusValue = "Updated: " + count;


            return count;
        }

        public static int changeSubmodelElement(EventPayloadEntry entry, List<ISubmodelElement> submodelElements, string idShortPath, List<ISubmodelElement> diffValue)
        {
            int count = 0;
            var dt = DateTime.Parse(entry.lastUpdate);

            for (int i = 0; i < submodelElements.Count; i++)
            {
                var sme = submodelElements[i];
                if (entry.idShortPath == idShortPath + sme.IdShort)
                {
                    MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(entry.payload));
                    JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                    var receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);

                    if (entry.entryType == "UPDATE")
                    {
                        Console.WriteLine("Event SME: " + entry.idShortPath);
                        receiveSme.Parent = submodelElements[i].Parent;
                        receiveSme.TimeStampCreate = submodelElements[i].TimeStampCreate;
                        submodelElements[i] = receiveSme;
                        receiveSme.SetAllParentsAndTimestamps((IReferable)receiveSme.Parent, dt, receiveSme.TimeStampCreate);
                        receiveSme.SetTimeStamp(dt);
                        diffValue.Add(receiveSme);
                        count++;
                        return count;
                    }
                }
                var path = idShortPath + sme.IdShort + ".";
                if (entry.idShortPath.StartsWith(path))
                {
                    switch (sme)
                    {
                        case ISubmodelElementCollection smc:
                            count += changeSubmodelElement(entry, smc.Value, path, diffValue);
                            break;
                        case ISubmodelElementList sml:
                            count += changeSubmodelElement(entry, sml.Value, path, diffValue);
                            break;
                    }
                }
            }
            return count;
        }
    }
}
