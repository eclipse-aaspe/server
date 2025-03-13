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
using AasxServerDB;
using Contracts;
using System.Linq.Dynamic.Core;
using AasxServerDB.Entities;
using Microsoft.IdentityModel.Tokens;
using Namotion.Reflection;
using System.Reflection.Metadata;
using Contracts.Pagination;
// using static AasxCompatibilityModels.AdminShellV20;

namespace Events
{
    public class EventPayloadEntry : IComparable<EventPayloadEntry>
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

        public int CompareTo(EventPayloadEntry other)
        {
            var result = string.Compare(this.submodelId, other.submodelId);

            if (result == 0)
            {
                if (this.payloadType == other.payloadType)
                {
                    result = 0;
                }
                else
                {
                    if (this.payloadType == "sm")
                    {
                        result = -1;
                    }
                    else
                    {
                        result = 1;
                    }
                }
            }

            if (result == 0)
            {
                result = string.Compare(this.idShortPath, other.idShortPath);
            }

            return result;
        }
    }

    public class EventStatus
    {
        // public string mode { get; set; } // PULL or PUSH must be the same in publisher and consumer
        public string transmitted { get; set; } // timestamp of GET or PUT
        public string lastUpdate { get; set; } // latest timeStamp for all entries
        public int countSM { get; set; }
        public int countSME { get; set; }
        public string cursor { get; set; }
        public EventStatus()
        {
            // mode = "";
            transmitted = "";
            lastUpdate = "";
            countSM = 0;
            countSME = 0;
            cursor = "";
        }
    }
    public class EventPayload
    {
        public static object EventLock = new object();
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
            string submodelId, string idShortPath, List<EventPayloadEntry> entries, List<String> diffEntry, bool withPayload)
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
                        if ((sme.TimeStampCreate - sme.TimeStampTree).TotalMilliseconds >= 0
                            && (sme.TimeStampCreate - diffTime).TotalMilliseconds > 1)
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
                        var a = (diffTime - sme.TimeStampCreate).TotalMilliseconds;
                        var b = (sme.TimeStampTree - sme.TimeStampCreate).TotalMilliseconds;
                        var c = (sme.TimeStampTree - diffTime).TotalMilliseconds;
                        if ((diffTime - sme.TimeStampCreate).TotalMilliseconds >= -1
                            && (sme.TimeStampTree - sme.TimeStampCreate).TotalMilliseconds > 1
                            && (sme.TimeStampTree - diffTime).TotalMilliseconds > 1)
                        {
                            if (children != null && children.Count != 0)
                            {
                                foreach (ISubmodelElement child in children)
                                {
                                    if (child.TimeStampTree != sme.TimeStampTree || child.TimeStampCreate == sme.TimeStampTree)
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
                        if ((diffTime - sme.TimeStampCreate).TotalMilliseconds >= 0
                            && (sme.TimeStampDelete - diffTime).TotalMilliseconds > 1)
                        {
                            delete = true;
                        }
                        if (children != null && children.Count != 0)
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
                    if (withPayload)
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
                    diffEntry.Add(entryType + " " + idShortPath + sme.IdShort + ".*");
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
                        count += collectSubmodelElements(children, diffTime, entryType, submodelId, idShortPath + sme.IdShort + ".", entries, diffEntry, withPayload);
                    }
                }
            }
            return count;
        }

        public static EventPayload CollectPayload(string changes, int depth, SubmodelElementCollection statusData,
            ReferenceElement reference, IReferable referable, AasCore.Aas3_0.Property conditionSM, AasCore.Aas3_0.Property conditionSME,
            string diff, List<String> diffEntry, bool withPayload, int limitSm, int limitSme, int offsetSm, int offsetSme)
        {
            var e = new EventPayload();
            e.status.transmitted = TimeStamp.TimeStamp.DateTimeToString(DateTime.UtcNow);
            e.status.lastUpdate = "";
            e.eventEntries = new List<EventPayloadEntry>();

            var isInitial = diff == "init";
            var isStatus = diff == "status";

            if (statusData != null && statusData.Value != null)
            {
                var j = Jsonization.Serialize.ToJsonObject(statusData);
                e.statusData = j.ToJsonString();
                /*
                if (depth == 0)
                {
                    var j = Jsonization.Serialize.ToJsonObject(statusData);
                    e.statusData = j.ToJsonString();
                }
                if (depth == 1 && statusData.Value.Count == 1 && statusData.Value[0] is SubmodelElementCollection smc)
                {
                    var j = Jsonization.Serialize.ToJsonObject(smc);
                    e.statusData = j.ToJsonString();
                }
                */
            }

            lock (EventLock)
            {
                if (referable != null)
                {
                    if (!isStatus)
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
                                // idShortPath = r.IdShort + "." + idShortPath;
                                r = r.Parent as IReferable;
                            }
                            if (r is Submodel)
                            {
                                submodel = r as Submodel;
                                entry.submodelId = submodel.Id;
                            }
                            entry.payloadType = "sme";
                            if (depth == 0)
                            {
                                idShortPath = referable.IdShort + ".";
                            }
                        }

                        e.status.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(referable.TimeStampTree);

                        // foreach (var c in children)
                        {
                            if (isInitial)
                            {
                                var json = string.Empty;
                                var j = Jsonization.Serialize.ToJsonObject(referable);
                                json = j.ToJsonString();

                                entry.entryType = "CREATE";
                                e.status.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(referable.TimeStampTree);
                                if (withPayload)
                                {
                                    entry.payload = json;
                                }
                                entry.lastUpdate = e.status.lastUpdate;
                                entry.idShortPath = idShortPath.TrimEnd('.');
                                e.eventEntries.Add(entry);
                                diffEntry.Add(entry.entryType + " " + entry.idShortPath);
                                /*
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
                                diffEntry.Add(entry.entryType + " " + entry.idShortPath);
                                */
                            }
                            else
                            {
                                // idShortPath = c.IdShort + ".";
                                var diffTime = DateTime.Parse(diff);
                                if (changes == null || changes.Contains("CREATE"))
                                {
                                    collectSubmodelElements(children, diffTime, "CREATE", submodel.Id, idShortPath, e.eventEntries, diffEntry, withPayload);
                                }
                                if (changes == null || changes.Contains("DELETE"))
                                {
                                    if (!(referable is Submodel))
                                    {
                                        var c = new List<ISubmodelElement>();
                                        c.Add(referable as ISubmodelElement);
                                        collectSubmodelElements(c, diffTime, "DELETE", submodel.Id, "", e.eventEntries, diffEntry, withPayload);
                                    }
                                    else
                                    {
                                        collectSubmodelElements(children, diffTime, "DELETE", submodel.Id, idShortPath, e.eventEntries, diffEntry, withPayload);
                                    }
                                }
                                if (changes == null || changes.Contains("UPDATE"))
                                {
                                    collectSubmodelElements(children, diffTime, "UPDATE", submodel.Id, idShortPath, e.eventEntries, diffEntry, withPayload);
                                }
                            }
                        }
                    }
                }
                else // DB
                {
                    int countSM = 0;
                    int countSME = 0;
                    string searchSM = string.Empty;
                    string searchSME = string.Empty;
                    if (conditionSM != null && conditionSM.Value != null)
                    {
                        searchSM = conditionSM.Value;
                    }
                    if (conditionSME != null && conditionSME.Value != null)
                    {
                        searchSME = conditionSME.Value;
                    }

                    using AasContext db = new();
                    var timeStampMax = new DateTime();

                    if (diff == "status")
                    {
                        if (searchSM == "*" || searchSM == "")
                        {
                            var s = db.SMSets.Select(sm => sm.TimeStampTree);
                            if (s.Any())
                            {
                                timeStampMax = s.Max();
                            }
                        }
                        else
                        {
                            var s = db.SMSets.Where(searchSM).Select(sm => sm.TimeStampTree);
                            if (s.Any())
                            {
                                timeStampMax = s.Max();
                            }
                        }
                        e.status.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(timeStampMax);
                        return e;
                    }

                    var diffTime = new DateTime();
                    if (diff != "init")
                    {
                        diffTime = DateTime.Parse(diff);
                    }
                    diff = TimeStamp.TimeStamp.DateTimeToString(diffTime);
                    var diffTime1 = diffTime.AddMilliseconds(1);
                    var diff1 = TimeStamp.TimeStamp.DateTimeToString(diffTime1);

                    IQueryable<SMSet> smSearchSet = db.SMSets;
                    if (!searchSM.IsNullOrEmpty() && searchSM != "*")
                    {
                        smSearchSet = smSearchSet.Where(searchSM);
                    }
                    // smSearchSet = smSearchSet.Where(timeStampExpression[i]);
                    smSearchSet = smSearchSet.Where(sm => (sm.TimeStampCreate > diffTime1) ||
                        ((sm.TimeStampTree != sm.TimeStampCreate) && (sm.TimeStampTree > diffTime1) && (sm.TimeStampCreate <= diffTime1)));
                    smSearchSet = smSearchSet.OrderBy(sm => sm.TimeStampTree).Skip(offsetSm).Take(limitSm);
                    var smSearchList = smSearchSet.ToList();

                    foreach (var sm in smSearchList)
                    {
                        var entryType = "CREATE";

                        bool completeSM = true;
                        if (sm.TimeStampCreate <= diffTime1)
                        {
                            var smeSearchSM = db.SMESets.Where(sme => sme.SMId == sm.Id);
                            var smeSearchTimeStamp = smeSearchSM
                                .Where(sme => (sme.TimeStampTree != sme.TimeStampCreate) && (sme.TimeStampTree > diffTime1) && (sme.TimeStampCreate <= diffTime1))
                                .Where(sme => (sme.TimeStamp > diffTime1) || (sme.TimeStampDelete > diffTime1))
                                .OrderBy(sme => sme.TimeStampTree).Skip(offsetSme).Take(limitSme).ToList();
                            if (smeSearchTimeStamp.Count != 0)
                            {
                                // smeSearchTimeStamp = smeSearchSM.Where(sme => sme.ParentSMEId == null).ToList();
                                var tree = Converter.GetTree(db, sm, smeSearchTimeStamp);
                                var treeMerged = Converter.GetSmeMerged(db, tree);
                                // var lookupChildren = treeMerged?.ToLookup(m => m.smeSet.ParentSMEId);

                                completeSM = false;
                                foreach (var sme in smeSearchTimeStamp)
                                // foreach (var tm in treeMerged)
                                {
                                    // var sme = tm.smeSet;
                                    var notDeletedIdShortList = new List<string>();
                                    if (sme.TimeStampDelete > diffTime1)
                                    {
                                        entryType = "DELETE";
                                        var children = smeSearchSM.Where(c => sme.Id == c.ParentSMEId).ToList();
                                        foreach (var c in children)
                                        {
                                            notDeletedIdShortList.Add(c.IdShort);
                                        }
                                    }
                                    else
                                    {
                                        entryType = "UPDATE";
                                    }
                                    var parentId = sme.ParentSMEId;
                                    var idShortPath = sme.IdShort;
                                    while (parentId != null && parentId != 0)
                                    {
                                        var parentSME = smeSearchSM.Where(sme => sme.Id == parentId).FirstOrDefault();
                                        idShortPath = parentSME.IdShort + "." + idShortPath;
                                        parentId = parentSME.ParentSMEId;
                                    }

                                    if (sme.TimeStampTree > timeStampMax)
                                    {
                                        timeStampMax = sme.TimeStampTree;
                                    }

                                    var entry = new EventPayloadEntry();
                                    entry.entryType = entryType;
                                    entry.payloadType = "sme";
                                    entry.idShortPath = idShortPath;
                                    entry.submodelId = sm.Identifier;
                                    entry.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(sme.TimeStampTree);
                                    entry.notDeletedIdShortList = notDeletedIdShortList;

                                    if (entryType != "DELETE" && withPayload)
                                    {
                                        // var s = Converter.GetSubmodelElement(sme);
                                        var s = Converter.GetSubmodelElement(sme, treeMerged);
                                        if (s != null)
                                        {
                                            var j = Jsonization.Serialize.ToJsonObject(s);
                                            if (j != null)
                                            {
                                                var json = j.ToJsonString();
                                                entry.payload = json;
                                            }
                                        }
                                    }

                                    e.eventEntries.Add(entry);
                                    countSME++;
                                }
                            }
                        }

                        if (completeSM)
                        {
                            if (sm.TimeStampTree > timeStampMax)
                            {
                                timeStampMax = sm.TimeStampTree;
                            }

                            var entry = new EventPayloadEntry();
                            entry.entryType = entryType;
                            entry.payloadType = "sm";
                            entry.idShortPath = sm.IdShort;
                            entry.submodelId = sm.Identifier;
                            entry.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree);

                            if (withPayload)
                            {
                                var s = Converter.GetSubmodel(sm);
                                if (s != null)
                                {
                                    var j = Jsonization.Serialize.ToJsonObject(s);
                                    if (j != null)
                                    {
                                        var json = j.ToJsonString();
                                        entry.payload = json;
                                    }
                                }
                            }

                            e.eventEntries.Add(entry);
                            countSM++;
                        }
                    }
                    if (countSM == 0 && countSME == 0)
                    {
                        timeStampMax = db.SMSets.Where(searchSM).Select(sm => sm.TimeStampTree).Max();
                    }
                    e.status.lastUpdate = TimeStamp.TimeStamp.DateTimeToString(timeStampMax);
                    e.status.countSM = countSM;
                    e.status.countSME = countSME;
                    if (countSM == limitSm)
                    {
                        e.status.cursor = $"offsetSM={offsetSm + limitSm}";
                    }
                    else if (countSM == limitSm)
                    {
                        e.status.cursor = $"offsetSME={offsetSme + limitSme}";
                    }
                }
            }

            return e;
        }

        public static int changeData(string json, EventData eventData, AdminShellPackageEnv[] env, IReferable referable, out string transmit, out string lastDiffValue, out string statusValue, List<String> diffEntry, int packageIndex = -1)
        {
            transmit = "";
            lastDiffValue = "";
            statusValue = "ERROR";
            int count = 0;

            var statusData = eventData.statusData;

            EventPayload eventPayload = JsonSerializer.Deserialize<Events.EventPayload>(json);
            transmit = eventPayload.status.transmitted;
            var dt = TimeStamp.TimeStamp.StringToDateTime(eventPayload.status.lastUpdate);
            dt = DateTime.Parse(eventPayload.status.lastUpdate);
            lastDiffValue = TimeStamp.TimeStamp.DateTimeToString(dt);

            ISubmodelElementCollection statusDataCollection = null;
            if (eventPayload.statusData != "" && statusData != null)
            {
                ISubmodelElement receiveSme = null;
                MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(eventPayload.statusData));
                JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);
                if (receiveSme is SubmodelElementCollection smc)
                {
                    statusData.Value = new List<ISubmodelElement>();
                    statusData.Add(smc);
                    receiveSme.TimeStampCreate = dt;
                    receiveSme.TimeStampDelete = new DateTime();
                    receiveSme.SetAllParentsAndTimestamps(statusData, dt, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                    receiveSme.SetTimeStamp(dt);
                }
            }

            AasCore.Aas3_0.Environment aasEnv = null;
            int index = -1;
            ISubmodelElementCollection dataCollection = null;
            List<ISubmodelElement> data = new List<ISubmodelElement>();
            SubmodelElementCollection status = null;
            AasCore.Aas3_0.Property message = null;
            AasCore.Aas3_0.Property transmitted = null;
            AasCore.Aas3_0.Property lastUpdate = null;
            SubmodelElementCollection diff = null;

            if (referable != null) // memory
            {
                List<ISubmodelElement> children = null;
                Submodel submodel = null;
                if (referable is Submodel)
                {
                    submodel = referable as Submodel;
                    children = submodel.SubmodelElements;
                }
                else
                {
                    if (referable is SubmodelElementCollection smc)
                    {
                        dataCollection = smc;
                        if (smc.Value == null)
                        {
                            smc.Value = new List<ISubmodelElement>();
                        }
                        data = smc.Value;
                        children = smc.Value;
                    }
                    if (referable is SubmodelElementList sml)
                    {
                        if (sml.Value == null)
                        {
                            sml.Value = new List<ISubmodelElement>();
                        }
                        data = sml.Value;
                        children = sml.Value;
                    }
                    var r = referable;
                    while (r.Parent != null)
                    {
                        r = r.Parent as IReferable;
                    }
                    if (r is Submodel)
                    {
                        submodel = r as Submodel;
                    }
                }

                lock (EventLock)
                {
                    foreach (var entry in eventPayload.eventEntries)
                    {
                        var submodelId = entry.submodelId;
                        ISubmodel receiveSubmodel = null;
                        IAssetAdministrationShell aas = null;

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
                                dataCollection.Value = receiveSubmodel.SubmodelElements;
                                dataCollection.SetTimeStamp(dt);
                                env[packageIndex].setWrite(true);
                                return 1;

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

                        if (entry.payloadType == "sme" && referable != null && (entry.payload != "" || entry.notDeletedIdShortList.Count != 0))
                        {
                            count += changeSubmodelElement(eventData, entry, referable as ISubmodelElement, children, "", diffEntry);
                            if (count > 0)
                            {
                                env[packageIndex].setWrite(true);
                            }
                        }
                    }
                }
            }
            else // DB
            {
                // sort by submodelID + entryType + idShortPath by CompareTo(EventPayloadEntry)
                eventPayload.eventEntries.Sort();
                var entriesSubmodel = new List<EventPayloadEntry>();
                foreach (var entry in eventPayload.eventEntries)
                {
                    Console.WriteLine($"Event {entry.entryType} Type: {entry.payloadType} idShortPath: {entry.idShortPath}");
                    if (entry.payloadType == "sm")
                    {
                        Submodel receiveSM = null;
                        if (entry.payload != "")
                        {
                            MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(entry.payload));
                            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                            receiveSM = Jsonization.Deserialize.SubmodelFrom(node);
                        }
                        if (receiveSM != null)
                        {
                            using (var db = new AasContext())
                            {
                                int? aasDB = null;
                                int? envDB = null;
                                var smDBQuery = db.SMSets.Where(sm => sm.Identifier == receiveSM.Id);
                                var smDB = smDBQuery.ToList();
                                if (smDB != null && smDB.Count > 0)
                                {
                                    aasDB = smDB[0].AASId;
                                    envDB = smDB[0].EnvId;
                                    smDBQuery.ExecuteDeleteAsync().Wait();
                                    db.SaveChanges();
                                }
                                var visitor = new VisitorAASX();
                                visitor.VisitSubmodel(receiveSM);
                                visitor._smDB.AASId = aasDB;
                                visitor._smDB.EnvId = envDB;
                                db.Add(visitor._smDB);
                                db.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        bool changeSubmodel = false;
                        bool addEntry = false;
                        if (entriesSubmodel.Count == 0)
                        {
                            addEntry = true;
                        }
                        else
                        {
                            if (entry.submodelId != entriesSubmodel.Last().submodelId)
                            {
                                changeSubmodel = true;
                            }
                        }
                        if (entry == eventPayload.eventEntries.Last())
                        {
                            addEntry = true;
                            changeSubmodel = true;
                        }
                        if (addEntry)
                        {
                            entriesSubmodel.Add(entry);
                        }
                        if (changeSubmodel)
                        {
                            var submodelIdentifier = entriesSubmodel.Last().submodelId;
                            using (var db = new AasContext())
                            {
                                var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);
                                var smDB = smDBQuery.ToList();
                                if (smDB != null && smDB.Count == 1)
                                {
                                    var visitor = new VisitorAASX(smDB[0]);
                                    var smDBId = smDB[0].Id;
                                    var smeSmList = db.SMESets.Where(sme => sme.SMId == smDBId).ToList();
                                    Converter.CreateIdShortPath(db, smeSmList);
                                    var smeSmMerged = Converter.GetSmeMerged(db, smeSmList);

                                    foreach (var e in entriesSubmodel)
                                    {
                                        bool change = false;
                                        ISubmodelElement receiveSme = null;
                                        if (e.payload != "")
                                        {
                                            MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(entry.payload));
                                            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                            receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);
                                        }
                                        if (receiveSme != null)
                                        {
                                            var receiveSmeDB = visitor.VisitSMESet(receiveSme);
                                            receiveSmeDB.SMId = smDBId;
                                            var parentPath = "";
                                            if (e.idShortPath.Contains("."))
                                            {
                                                int lastDotIndex = e.idShortPath.LastIndexOf('.');
                                                if (lastDotIndex != -1)
                                                {
                                                    parentPath = e.idShortPath.Substring(0, lastDotIndex);
                                                }
                                            }
                                            else
                                            {
                                                change = true;
                                            }
                                            switch (e.entryType)
                                            {
                                                case "CREATE":
                                                case "UPDATE":
                                                    if (parentPath != "")
                                                    {
                                                        var parentDB = smeSmMerged.Where(sme => sme.smeSet.IdShortPath == parentPath).ToList();
                                                        if (parentDB.Count == 1)
                                                        {
                                                            receiveSmeDB.ParentSMEId = parentDB[0].smeSet.Id;
                                                            receiveSmeDB.IdShortPath = e.idShortPath;
                                                            change = true;
                                                        }
                                                    }
                                                    if (change)
                                                    {
                                                        db.SaveChanges();
                                                        var smeDB = smeSmMerged.Where(
                                                            sme => sme.smeSet.IdShortPath == e.idShortPath ||
                                                            sme.smeSet.IdShortPath.StartsWith(e.idShortPath + "."))
                                                            .ToList();
                                                        var smeDBList = smeDB.Select(sme => sme.smeSet.Id).ToList();
                                                        if (smeDBList.Count > 0)
                                                        {
                                                            if (receiveSme != null)
                                                            {
                                                                db.SMESets.Where(sme => smeDBList.Contains(sme.Id)).ExecuteDeleteAsync().Wait();
                                                                db.SaveChanges();
                                                            }
                                                        }
                                                        count++;
                                                    }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            if (e.entryType == "DELETE")
                                            {
                                                var notDeleted = e.notDeletedIdShortList;
                                                var parentPath = e.idShortPath;
                                                db.SaveChanges();
                                                var deletePathList = smeSmMerged.Where(
                                                    sme => sme.smeSet.IdShort != null
                                                    && sme.smeSet.IdShortPath == parentPath + "." + sme.smeSet.IdShort
                                                    && !notDeleted.Contains(sme.smeSet.IdShort))
                                                    .Select(sme => sme.smeSet.IdShortPath).ToList();
                                                var smeDBList = new List<int>();
                                                foreach (var deletePath in deletePathList)
                                                {
                                                    smeDBList.AddRange(
                                                        smeSmMerged.Where(
                                                        sme => sme.smeSet.IdShortPath == deletePath ||
                                                        sme.smeSet.IdShortPath.StartsWith(deletePath + "."))
                                                        .Select(sme => sme.smeSet.Id).ToList());
                                                }
                                                if (smeDBList.Count > 0)
                                                {
                                                    db.SMESets.Where(sme => smeDBList.Contains(sme.Id)).ExecuteDeleteAsync().Wait();
                                                    db.SaveChanges();
                                                }
                                                count++;
                                            }
                                        }
                                    }
                                }
                            }
                            entriesSubmodel.Clear();
                        }
                    }
                }
            }

            statusValue = "Updated: " + count;


            return count;
        }

        public static int changeSubmodelElement(EventData eventData, EventPayloadEntry entry, IReferable parent, List<ISubmodelElement> submodelElements, string idShortPath, List<String> diffEntry)
        {
            int count = 0;
            var dt = DateTime.Parse(entry.lastUpdate);

            int maxCount = 0;
            if (eventData.dataMaxSize != null && eventData.dataMaxSize.Value != null)
            {
                maxCount = Convert.ToInt32(eventData.dataMaxSize.Value);
            }

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
                        diffEntry.Add(entry.entryType + " " + entry.idShortPath);
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
                                    count += changeSubmodelElement(eventData, entry, smc, smc.Value, path, diffEntry);
                                    break;
                                case ISubmodelElementList sml:
                                    count += changeSubmodelElement(eventData, entry, sml, sml.Value, path, diffEntry);
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
                        diffEntry.Add(entry.entryType + " " + entry.idShortPath);
                        count++;
                        return count;
                    }
                    var path = idShortPath + sme.IdShort + ".";
                    if (entry.idShortPath.StartsWith(path))
                    {
                        switch (sme)
                        {
                            case ISubmodelElementCollection smc:
                                count += changeSubmodelElement(eventData, entry, smc, smc.Value, path, diffEntry);
                                break;
                            case ISubmodelElementList sml:
                                count += changeSubmodelElement(eventData, entry, sml, sml.Value, path, diffEntry);
                                break;
                        }
                    }
                }
            }
            if (entry.entryType == "DELETE")
            {
                if (maxCount == 0 || eventData.dataCollection != parent)
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
                            diffEntry.Add(entry.entryType + " " + entry.idShortPath + ".*");
                            count++;
                            break;
                        }
                        var path = idShortPath + sme.IdShort + ".";
                        if (entry.idShortPath.StartsWith(path))
                        {
                            count += changeSubmodelElement(eventData, entry, sme, children, path, diffEntry);
                        }
                    }
                }
            }

            if (maxCount != 0 && eventData.dataCollection == parent)
            {
                SubmodelElementCollection data = null;
                if (eventData.dataCollection is SubmodelElementCollection dataSmc)
                {
                    data = dataSmc;
                }
                // if (eventData.direction != null && eventData.direction.Value == "IN" && eventData.mode != null && (eventData.mode.Value == "PUSH" || eventData.mode.Value == "PUT"))
                if (data != null && eventData.direction != null && eventData.direction.Value == "IN" && eventData.mode != null)
                {
                    if (data.Value != null && data.Value.Count == 1 && data.Value[0] is SubmodelElementCollection smc)
                    {
                        data = smc;

                        while (data.Value != null && data.Value.Count > maxCount)
                        {
                            data.Value.RemoveAt(0);
                            data.SetTimeStampDelete(dt);
                        }
                    }
                }

            }

            return count;
        }
    }

    public class EventData
    {
        public SubmodelElementCollection authentication = null;
        public AasCore.Aas3_0.Property authType = null;
        public AasCore.Aas3_0.Property authServerEndPoint = null;
        public AasCore.Aas3_0.Property accessToken = null;
        public AasCore.Aas3_0.Property userName = null;
        public AasCore.Aas3_0.Property passWord = null;
        public string basicAuth = null;
        public AasCore.Aas3_0.File authServerCertificate = null;
        public AasCore.Aas3_0.File clientCertificate = null;
        public AasCore.Aas3_0.Property clientCertificatePassWord = null;
        public AasCore.Aas3_0.Property clientToken = null;

        public AasCore.Aas3_0.Property direction = null;
        public AasCore.Aas3_0.Property mode = null;
        public AasCore.Aas3_0.Property changes = null;
        public AasCore.Aas3_0.Property endPoint = null;
        public Submodel dataSubmodel = null;
        public ISubmodelElement dataCollection = null;
        public AasCore.Aas3_0.Property dataMaxSize = null;
        public SubmodelElementCollection statusData = null;
        public AasCore.Aas3_0.Property noPayload = null;

        // memory || database
        public AasCore.Aas3_0.Property persistence = null;
        // * = all SM, else query condition for SM
        public AasCore.Aas3_0.Property conditionSM = null;
        // "" only conditionSM, * = all SME in SM, else query condition for SME
        public AasCore.Aas3_0.Property conditionSME = null;
        public AasCore.Aas3_0.ReferenceElement dataReference = null;

        public SubmodelElementCollection status = null;
        public AasCore.Aas3_0.Property message = null;
        public AasCore.Aas3_0.Property transmitted = null;
        public AasCore.Aas3_0.Property lastUpdate = null;
        public SubmodelElementCollection diff = null;

        public EventData()
        {

        }

        public static Operation FindEvent(ISubmodel submodel, string eventPath)
        {
            return FindEvent(submodel, null, eventPath);
        }
        public static Operation FindEvent(ISubmodel submodel, ISubmodelElement sme, string eventPath)
        {
            var children = new List<ISubmodelElement>();

            if (submodel != null)
            {
                children = submodel.SubmodelElements;
            }
            if (sme != null && sme is SubmodelElementCollection smc)
            {
                children = smc.Value;
            }
            if (sme != null && sme is SubmodelElementList sml)
            {
                children = sml.Value;
            }

            foreach (var c in children)
            {
                if (c is Operation op && c.IdShort == eventPath)
                {
                    return op;
                }
                if (c is SubmodelElementCollection || c is SubmodelElementCollection)
                {
                    if (eventPath.StartsWith(c.IdShort + "."))
                    {
                        var result = FindEvent(null, c, eventPath.Substring(c.IdShort.Length + 1));
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }

        public void ParseData(Operation op, AdminShellPackageEnv env)
        {
            SubmodelElementCollection smec = null;
            SubmodelElementList smel = null;
            Submodel sm = null;
            AasCore.Aas3_0.Property p = null;

            foreach (var input in op.InputVariables)
            {
                smec = null;
                smel = null;
                sm = null;
                p = null;
                var inputRef = input.Value;
                if (inputRef is AasCore.Aas3_0.Property)
                {
                    p = (inputRef as AasCore.Aas3_0.Property);
                    if (p.Value == null)
                    {
                        p = null;
                    }
                }

                if (inputRef is SubmodelElementCollection)
                {
                    smec = (inputRef as SubmodelElementCollection);
                }
                if (inputRef is SubmodelElementList)
                {
                    smel = (inputRef as SubmodelElementList);
                }

                if (inputRef is ReferenceElement)
                {
                    var refElement = env.AasEnv.FindReferableByReference((inputRef as ReferenceElement).Value);
                    if (refElement is SubmodelElementCollection)
                    {
                        smec = refElement as SubmodelElementCollection;
                    }
                    if (refElement is SubmodelElementList)
                    {
                        smel = refElement as SubmodelElementList;
                    }
                    if (refElement is Submodel)
                    {
                        sm = refElement as Submodel;
                    }
                }

                switch (inputRef.IdShort.ToLower())
                {
                    case "direction":
                        if (p != null)
                            direction = p;
                        break;
                    case "mode":
                        if (p != null)
                            mode = p;
                        break;
                    case "changes":
                        if (p != null)
                            changes = p;
                        break;
                    case "authentication":
                        if (smec != null)
                            authentication = smec;
                        break;
                    case "endpoint":
                        if (p != null)
                        {
                            if (p.Value != null && p.Value.StartsWith("$"))
                            {
                                var envVarName = p.Value.Substring(1);
                                var url = System.Environment.GetEnvironmentVariable(envVarName);
                                if (url != null)
                                {
                                    Console.WriteLine($"{p.Value} = {url}");
                                    p.Value = url;
                                }
                                else
                                {
                                    Console.WriteLine($"Environment variable {envVarName} not found.");
                                }
                            }
                            endPoint = p;
                        }
                        break;
                    case "nopayload":
                        if (p != null)
                            noPayload = p;
                        break;
                    case "data":
                    case "observed":
                        if (inputRef is ReferenceElement r)
                        {
                            dataReference = r;
                        }
                        if (sm != null)
                            dataSubmodel = sm;
                        if (smec != null)
                            dataCollection = smec;
                        if (smel != null)
                            dataCollection = smel;
                        break;
                    case "datamaxsize":
                        if (p != null)
                            dataMaxSize = p;
                        break;
                    case "statusdata":
                        if (smec != null)
                            statusData = smec;
                        break;
                    case "persistence":
                        if (p != null)
                            persistence = p;
                        break;
                    case "conditionsm":
                        if (p != null)
                            conditionSM = p;
                        break;
                    case "conditionsme":
                        if (p != null)
                            conditionSME = p;
                        break;
                }
            }
            /*
            if (dataMaxSize == null)
            {
                var timeStamp = DateTime.UtcNow;
                dataMaxSize = new AasCore.Aas3_0.Property(DataTypeDefXsd.String, idShort: "dataMaxSize", value: "");
                var dataMaxSizeOp = new OperationVariable(dataMaxSize);
                dataMaxSize.SetAllParentsAndTimestamps(null, timeStamp, timeStamp, DateTime.MinValue);
                op.InputVariables.Add(dataMaxSizeOp);
            }
            */

            foreach (var output in op.OutputVariables)
            {
                smec = null;
                sm = null;
                p = null;
                var outputRef = output.Value;
                if (outputRef is AasCore.Aas3_0.Property)
                {
                    p = (outputRef as AasCore.Aas3_0.Property);
                }

                if (outputRef is SubmodelElementCollection)
                {
                    smec = outputRef as SubmodelElementCollection;
                }

                if (outputRef is ReferenceElement)
                {
                    var refElement = env.AasEnv.FindReferableByReference((outputRef as ReferenceElement).Value);
                    if (refElement is SubmodelElementCollection)
                        smec = refElement as SubmodelElementCollection;
                    if (refElement is Submodel)
                        sm = refElement as Submodel;
                }

                switch (outputRef.IdShort.ToLower())
                {
                    case "lastupdate":
                        if (p != null)
                            lastUpdate = p;
                        break;
                    case "status":
                        if (smec != null)
                            status = smec;
                        break;
                    case "statusdata":
                        if (smec != null)
                            statusData = smec;
                        break;
                    case "diff":
                        if (smec != null)
                            diff = smec;
                        break;
                }
            }

            if (status != null)
            {
                foreach (var sme in status.Value)
                {
                    switch (sme.IdShort.ToLower())
                    {
                        case "message":
                            if (sme is AasCore.Aas3_0.Property)
                            {
                                message = sme as AasCore.Aas3_0.Property;
                            }
                            break;
                        case "transmitted":
                            if (sme is AasCore.Aas3_0.Property)
                            {
                                transmitted = sme as AasCore.Aas3_0.Property;
                            }
                            break;
                        case "lastupdate":
                            if (sme is AasCore.Aas3_0.Property)
                            {
                                lastUpdate = sme as AasCore.Aas3_0.Property;
                            }
                            break;
                        case "diff":
                            if (sme is SubmodelElementCollection)
                            {
                                diff = sme as SubmodelElementCollection;
                            }
                            break;
                    }
                }
            }

            if (authentication != null)
            {
                smec = authentication;
                int countSmec = smec.Value.Count;
                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                {
                    var sme2 = smec.Value[iSmec];
                    var idShort = sme2.IdShort.ToLower();

                    switch (idShort)
                    {
                        case "authtype":
                            if (sme2 is AasCore.Aas3_0.Property)
                            {
                                authType = sme2 as AasCore.Aas3_0.Property;
                            }

                            break;

                        case "accesstoken":
                            if (sme2 is AasCore.Aas3_0.Property)
                            {
                                accessToken = sme2 as AasCore.Aas3_0.Property;
                            }

                            break;

                        case "clienttoken":
                            if (sme2 is AasCore.Aas3_0.Property)
                            {
                                clientToken = sme2 as AasCore.Aas3_0.Property;
                            }

                            break;

                        case "username":
                            if (sme2 is AasCore.Aas3_0.Property)
                            {
                                userName = sme2 as AasCore.Aas3_0.Property;
                            }

                            break;

                        case "password":
                            if (sme2 is AasCore.Aas3_0.Property)
                            {
                                passWord = sme2 as AasCore.Aas3_0.Property;
                            }

                            break;

                        case "authservercertificate":
                            if (sme2 is AasCore.Aas3_0.File)
                            {
                                authServerCertificate = sme2 as AasCore.Aas3_0.File;
                            }

                            break;

                        case "authserverendpoint":
                            if (sme2 is AasCore.Aas3_0.Property)
                            {
                                authServerEndPoint = sme2 as AasCore.Aas3_0.Property;
                            }

                            break;

                        case "clientcertificate":
                            if (sme2 is AasCore.Aas3_0.File)
                            {
                                clientCertificate = sme2 as AasCore.Aas3_0.File;
                            }

                            break;

                        case "clientcertificatepassword":
                            if (sme2 is AasCore.Aas3_0.Property)
                            {
                                clientCertificatePassWord = sme2 as AasCore.Aas3_0.Property;
                            }

                            break;
                    }
                }
            }
        }
    }
}
