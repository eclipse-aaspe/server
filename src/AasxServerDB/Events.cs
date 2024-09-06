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
using System.IO;

namespace Events
{
    public class EventPayloadEntry
    {
        public string entryType { get; set; } // CREATE, UPDATE, DELETE
        public string lastUpdate { get; set; } // timeStamp for this entry
        public string payloadType { get; set; } // Submodel, SME, AAS
        public string payload { get; set; } // JSON Serialization
        public string submodelId { get; set; } // ID of related Submodel
        public string idShortPath { get; set; } // for SMEs only
        public List<string> notDeletedIdShortList { get; set; } // for DELETE only, remaining idShort

        public EventPayloadEntry()
        {
            entryType = "";
            lastUpdate = "";
            payloadType = "";
            payload = "";
            submodelId = "";
            idShortPath = "";
            notDeletedIdShortList = new List<string>();
        }
    }

    public class EventStatus
    {
        public string mode { get; set; } // PULL or PUSH must be the same in publisher and consumer
        public string transmitted { get; set; } // timestamp of GET or PUT
        public string lastUpdate { get; set; } // latest timeStamp for all entries

        public EventStatus()
        {
            mode = "";
            transmitted = "";
            lastUpdate = "";
        }
    }
    public class EventPayload
    {
        public EventStatus status { get; set; }
        public string statusData { get; set; } // application status data, continuously sent, can be used for specific reconnect
        public List<EventPayloadEntry> eventEntries { get; set; }

        public EventPayload()
        {
            status = new EventStatus();
            statusData = "";
            eventEntries = new List<EventPayloadEntry>();
        }
        public static int collectSubmodelElements(List<ISubmodelElement> submodelElements, DateTime diffTime, string entryType,
            string submodelId, string idShortPath, List<EventPayloadEntry> entries, List<String> diffEntry, bool noPayload)
        {
            int count = 0;
            foreach (var sme in submodelElements)
            {
                bool tree = false;
                bool copy = false;
                bool delete = false;
                DateTime timeStamp = new DateTime();

                List<ISubmodelElement> children = new List<ISubmodelElement>();
                switch (sme)
                {
                    case ISubmodelElementCollection smc:
                        children = smc.Value;
                        break;
                    case ISubmodelElementList sml:
                        children = sml.Value;
                        break;
                }

                switch (entryType)
                {
                    case "CREATE":
                        timeStamp = sme.TimeStampCreate;
                        if (sme.TimeStampCreate >= sme.TimeStampTree && (sme.TimeStampCreate - diffTime).TotalMilliseconds > 1)
                        {
                            copy = true;
                        }
                        else
                        {
                            if (sme.TimeStampCreate < sme.TimeStampTree && (sme.TimeStampTree - diffTime).TotalMilliseconds > 1)
                            {
                                tree = true;
                            }
                        }
                        break;
                    case "UPDATE":
                        timeStamp = sme.TimeStampTree;
                        if (sme.TimeStampCreate < sme.TimeStampTree && (sme.TimeStampTree - diffTime).TotalMilliseconds > 1)
                        {
                            if (children != null || children.Count != 0)
                            {
                                foreach (ISubmodelElement child in children)
                                {
                                    if (child.TimeStampTree != sme.TimeStampTree)
                                    {
                                        tree = true;
                                        break;
                                    }
                                }
                            }
                            if (!tree)
                            {
                                copy = true;
                            }
                        }
                        break;
                    case "DELETE":
                        timeStamp = sme.TimeStampDelete;
                        if ((sme.TimeStampDelete - diffTime).TotalMilliseconds > 1)
                        {
                            delete = true;
                        }
                        if (children != null || children.Count != 0)
                        {
                            foreach (ISubmodelElement child in children)
                            {
                                if (child.TimeStampTree != sme.TimeStampTree)
                                {
                                    tree = true;
                                    break;
                                }
                            }
                        }
                        break;
                }
                if (copy)
                {
                    diffEntry.Add(entryType + " " + idShortPath + sme.IdShort);
                    var j = Jsonization.Serialize.ToJsonObject(sme);
                    var e = new EventPayloadEntry();
                    e.entryType = entryType;
                    e.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(timeStamp);
                    e.payloadType = "sme";
                    if (!noPayload)
                    {
                        e.payload = j.ToJsonString();
                    }
                    e.submodelId = submodelId;
                    e.idShortPath = idShortPath + sme.IdShort;
                    entries.Add(e);
                    count++;
                }
                if (delete)
                {
                    Console.WriteLine("DELETE SME " + idShortPath + sme.IdShort);
                    diffEntry.Add(entryType + " " + idShortPath + sme.IdShort);
                    var e = new EventPayloadEntry();
                    e.entryType = entryType;
                    e.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(timeStamp);
                    e.payloadType = "sme";
                    e.submodelId = submodelId;
                    e.idShortPath = idShortPath + sme.IdShort;
                    if (children != null || children.Count != 0)
                    {
                        foreach (var child in children)
                        {
                            e.notDeletedIdShortList.Add(child.IdShort);
                        }
                    }
                    entries.Add(e);
                    count++;
                }
                if (tree)
                {
                    if (tree && children.Count != 0)
                    {
                        count += collectSubmodelElements(children, diffTime, entryType, submodelId, idShortPath + sme.IdShort + ".", entries, diffEntry, noPayload);
                    }
                }
            }
            return count;
        }

        public static EventPayload CollectPayload(string changes, IReferable referable, string diff, List<String> diffEntry, bool noPayload)
        {
            var e = new EventPayload();
            e.status.transmitted = TimeStamp.TimeStamp.DateTimeToString(DateTime.UtcNow);
            e.status.lastUpdate = "";
            e.eventEntries = new List<EventPayloadEntry>();

            var isInitial = diff == "init";

            if (referable != null)
            {
                var entry = new EventPayloadEntry();
                var idShortPath = "";
                Submodel submodel = null;
                List<ISubmodelElement> children = null;
                if (referable is Submodel)
                {
                    submodel = referable as Submodel;
                    children = submodel.SubmodelElements;
                    entry.submodelId = submodel.Id;
                    entry.payloadType = "submodel";
                }
                else
                {
                    if (referable is SubmodelElementCollection smc)
                    {
                        children = smc.Value;
                    }
                    if (referable is SubmodelElementList sml)
                    {
                        children = sml.Value;
                    }
                    var r = referable;
                    while (r.Parent != null)
                    {
                        idShortPath = r.IdShort + "." + idShortPath;
                        r = r.Parent as IReferable;
                    }
                    if (r is Submodel)
                    {
                        submodel = r as Submodel;
                        entry.submodelId = submodel.Id;
                    }
                    entry.payloadType = "sme";
                    idShortPath = referable.IdShort + ".";
                }

                e.status.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(referable.TimeStampTree);

                if (isInitial)
                {
                    string json = string.Empty;
                    if (referable != null)
                    {
                        var j = Jsonization.Serialize.ToJsonObject(referable);
                        json = j.ToJsonString();
                    }

                    entry.entryType = "CREATE";
                    e.status.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(referable.TimeStampTree);
                    if (!noPayload)
                    {
                        entry.payload = json;
                    }
                    entry.lastUpdate = e.status.lastUpdate;
                    entry.idShortPath = idShortPath.TrimEnd('.');
                    e.eventEntries.Add(entry);
                }
                else
                {
                    var diffTime = DateTime.Parse(diff);
                    if (changes == null || changes.Contains("CREATE"))
                    {
                        collectSubmodelElements(children, diffTime, "CREATE", submodel.Id, idShortPath, e.eventEntries, diffEntry, noPayload);
                    }
                    if (changes == null || changes.Contains("DELETE"))
                    {
                        collectSubmodelElements(children, diffTime, "DELETE", submodel.Id, idShortPath, e.eventEntries, diffEntry, noPayload);
                        if (!(referable is Submodel))
                        {
                            children = new List<ISubmodelElement>();
                            children.Add(referable as ISubmodelElement);
                            collectSubmodelElements(children, diffTime, "DELETE", submodel.Id, "", e.eventEntries, diffEntry, noPayload);
                        }
                    }
                    if (changes == null || changes.Contains("UPDATE"))
                    {
                        collectSubmodelElements(children, diffTime, "UPDATE", submodel.Id, idShortPath, e.eventEntries, diffEntry, noPayload);
                    }
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
            var dt = TimeStamp.TimeStamp.StringToDateTime(eventPayload.status.lastUpdate);
            dt = DateTime.Parse(eventPayload.status.lastUpdate);
            lastDiffValue = TimeStamp.TimeStamp.DateTimeToString(dt);

            if (eventPayload.statusData != "" && eventPayload.eventEntries.Count != 0)
            {
                ISubmodelElement receiveSme = null;
                MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(eventPayload.statusData));
                JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);

                var entryStatusData = new EventPayloadEntry();
                entryStatusData.entryType = "CREATE";
                entryStatusData.lastUpdate = eventPayload.status.lastUpdate;
                entryStatusData.payloadType = "sme";
                entryStatusData.payload = eventPayload.statusData;
                entryStatusData.submodelId = eventPayload.eventEntries[0].submodelId;
                entryStatusData.idShortPath = receiveSme.IdShort;
                entryStatusData.notDeletedIdShortList = new List<string>();
                eventPayload.eventEntries.Insert(0, entryStatusData);
            }

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

                if (entry.payloadType == "submodel" && entry.payload != "")
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

                if (entry.payloadType == "sme" && submodel != null && (entry.payload != "" || entry.notDeletedIdShortList.Count != 0))
                {
                    count += changeSubmodelElement(entry, submodel, submodel.SubmodelElements, "", diffValue);
                    if (count > 0)
                    {
                        env[index].setWrite(true);
                    }
                }
            }

            statusValue = "Updated: " + count;


            return count;
        }

        public static int changeSubmodelElement(EventPayloadEntry entry, IReferable parent, List<ISubmodelElement> submodelElements, string idShortPath, List<ISubmodelElement> diffValue)
        {
            int count = 0;
            var dt = DateTime.Parse(entry.lastUpdate);

            ISubmodelElement receiveSme = null;
            if (entry.payload != "")
            {
                MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(entry.payload));
                JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);
                receiveSme.Parent = parent;
            }

            if (entry.entryType == "CREATE")
            {
                if (entry.idShortPath.StartsWith(idShortPath))
                {
                    var path = entry.idShortPath;
                    if (idShortPath != "")
                    {
                        path = path.Replace(idShortPath, "");
                    }
                    if (!path.Contains("."))
                    {
                        Console.WriteLine("Event CREATE SME: " + entry.idShortPath);
                        receiveSme.TimeStampCreate = dt;
                        receiveSme.TimeStampDelete = new DateTime();
                        int i = 0;
                        bool found = false;
                        for (; i < submodelElements.Count; i++)
                        {
                            var sme = submodelElements[i];
                            if (sme.IdShort == path)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            submodelElements[i] = receiveSme;
                        }
                        else
                        {
                            submodelElements.Add(receiveSme);
                        }
                        receiveSme.SetAllParentsAndTimestamps(parent, dt, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                        receiveSme.SetTimeStamp(dt);
                        diffValue.Add(receiveSme);
                        count++;
                        return count;
                    }
                    else
                    {
                        for (int i = 0; i < submodelElements.Count; i++)
                        {
                            var sme = submodelElements[i];
                            path = idShortPath + sme.IdShort + ".";
                            switch (sme)
                            {
                                case ISubmodelElementCollection smc:
                                    count += changeSubmodelElement(entry, smc, smc.Value, path, diffValue);
                                    break;
                                case ISubmodelElementList sml:
                                    count += changeSubmodelElement(entry, sml, sml.Value, path, diffValue);
                                    break;
                            }
                        }
                    }
                }
            }
            if (entry.entryType == "UPDATE")
            {
                for (int i = 0; i < submodelElements.Count; i++)
                {
                    var sme = submodelElements[i];
                    if (entry.idShortPath == idShortPath + sme.IdShort)
                    {
                        Console.WriteLine("Event UPDATE SME: " + entry.idShortPath);
                        receiveSme.TimeStampCreate = submodelElements[i].TimeStampCreate;
                        receiveSme.TimeStampDelete = submodelElements[i].TimeStampDelete;
                        submodelElements[i] = receiveSme;
                        receiveSme.SetAllParentsAndTimestamps(parent, dt, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                        receiveSme.SetTimeStamp(dt);
                        diffValue.Add(receiveSme);
                        count++;
                        return count;
                    }
                    var path = idShortPath + sme.IdShort + ".";
                    if (entry.idShortPath.StartsWith(path))
                    {
                        switch (sme)
                        {
                            case ISubmodelElementCollection smc:
                                count += changeSubmodelElement(entry, smc, smc.Value, path, diffValue);
                                break;
                            case ISubmodelElementList sml:
                                count += changeSubmodelElement(entry, sml, sml.Value, path, diffValue);
                                break;
                        }
                    }
                }
            }
            if (entry.entryType == "DELETE")
            {
                for (int i = 0; i < submodelElements.Count; i++)
                {
                    var sme = submodelElements[i];
                    List<ISubmodelElement> children = new List<ISubmodelElement>();
                    switch (sme)
                    {
                        case ISubmodelElementCollection smc:
                            children = smc.Value;
                            break;
                        case ISubmodelElementList sml:
                            children = sml.Value;
                            break;
                    }
                    if (entry.idShortPath == idShortPath + sme.IdShort)
                    {
                        Console.WriteLine("Event DELETE SME: " + entry.idShortPath);
                        if (children.Count != 0)
                        {
                            int c = 0;
                            while (c < children.Count)
                            {
                                if (!entry.notDeletedIdShortList.Contains(children[c].IdShort))
                                {
                                    children.RemoveAt(c);
                                    sme.SetTimeStampDelete(dt);
                                    count++;
                                }
                                else
                                {
                                    c++;
                                }
                            }
                        }
                        count++;
                        return count;
                    }
                    var path = idShortPath + sme.IdShort + ".";
                    if (entry.idShortPath.StartsWith(path))
                    {
                        count += changeSubmodelElement(entry, sme, children, path, diffValue);
                    }
                }
            }
            return count;
        }
    }
}
