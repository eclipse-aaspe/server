namespace AasxServerStandardBib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Text.Json.Nodes;
using AasxServerDB.Entities;
using AasxServerDB;
using AdminShellNS;
using Contracts;
using Contracts.Events;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

public class EventService : IEventService
{
    public ICollection<EventDto> EventDtos;

    public static object EventLock = new object();

    public int CollectSubmodelElements(List<ISubmodelElement> submodelElements, DateTime diffTime, string entryType,
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
                    count += CollectSubmodelElements(children, diffTime, entryType, submodelId, idShortPath + sme.IdShort + ".", entries, diffEntry, withPayload);
                }
            }
        }
        return count;
    }

    public EventPayload CollectPayload(Dictionary<string, string> securityCondition, string changes, int depth, SubmodelElementCollection statusData,
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
                                CollectSubmodelElements(children, diffTime, "CREATE", submodel.Id, idShortPath, e.eventEntries, diffEntry, withPayload);
                            }
                            if (changes == null || changes.Contains("DELETE"))
                            {
                                if (!(referable is Submodel))
                                {
                                    var c = new List<ISubmodelElement>();
                                    c.Add(referable as ISubmodelElement);
                                    CollectSubmodelElements(c, diffTime, "DELETE", submodel.Id, "", e.eventEntries, diffEntry, withPayload);
                                }
                                else
                                {
                                    CollectSubmodelElements(children, diffTime, "DELETE", submodel.Id, idShortPath, e.eventEntries, diffEntry, withPayload);
                                }
                            }
                            if (changes == null || changes.Contains("UPDATE"))
                            {
                                CollectSubmodelElements(children, diffTime, "UPDATE", submodel.Id, idShortPath, e.eventEntries, diffEntry, withPayload);
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
                if (securityCondition != null && securityCondition.TryGetValue("sm.", out _))
                {
                    if (searchSM == string.Empty)
                    {
                        searchSM = securityCondition["sm."];
                    }
                    else
                    {
                        searchSM = $"({securityCondition["sm."]})&&({searchSM})";
                    }
                }
                if (conditionSME != null && conditionSME.Value != null)
                {
                    searchSME = conditionSME.Value;
                }
                if (securityCondition != null && securityCondition.TryGetValue("sme.", out _))
                {
                    if (searchSME == string.Empty)
                    {
                        searchSME = securityCondition["sme."];
                    }
                    else
                    {
                        searchSME = $"({securityCondition["sme."]})&&({searchSME})";
                    }
                }

                using AasContext db = new();
                var timeStampMax = new DateTime();

                if (diff == "status")
                {
                    if (searchSM is "(*)" or "*" or "")
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
                        var smeSearchTimeStamp = smeSearchSM.Where(sme =>
                                sme.TimeStampCreate > diffTime1
                                || sme.TimeStampDelete > diffTime1
                                || (sme.TimeStampCreate <= diffTime1 && sme.TimeStampDelete <= diffTime1
                                    && sme.TimeStampTree != sme.TimeStampCreate && sme.TimeStampTree > diffTime1)
                            )
                            .OrderBy(sme => sme.TimeStampTree).Skip(offsetSme).Take(limitSme).ToList();
                        if (smeSearchTimeStamp.Count != 0)
                        {
                            // smeSearchTimeStamp = smeSearchSM.Where(sme => sme.ParentSMEId == null).ToList();
                            var tree = CrudOperator.GetTree(db, sm, smeSearchTimeStamp);
                            var treeMerged = CrudOperator.GetSmeMerged(db, tree, sm);
                            // var lookupChildren = treeMerged?.ToLookup(m => m.smeSet.ParentSMEId);

                            completeSM = false;
                            List<int> skip = [];
                            foreach (var sme in smeSearchTimeStamp)
                            {
                                if (skip.Contains(sme.Id))
                                {
                                    var children = smeSearchSM.Where(c => sme.Id == c.ParentSMEId).Select(s => s.Id).ToList();
                                    skip.AddRange(children);
                                    continue;
                                }
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
                                    var allChildren = smeSearchTimeStamp.Where(s => s.ParentSMEId == sme.Id).ToList();
                                    var createChildren = allChildren.Where(s => s.ParentSMEId == sme.Id && s.TimeStampCreate > diffTime1).ToList();
                                    var updateChildren = allChildren.Where(s => s.ParentSMEId == sme.Id && s.TimeStampTree > diffTime1).ToList();
                                    var deleteChildren = allChildren.Where(s => s.ParentSMEId == sme.Id && s.TimeStampDelete > diffTime1).ToList();
                                    if (sme.TimeStampCreate > diffTime1)
                                    {
                                        if (allChildren.Count == 0 || allChildren.Count == createChildren.Count)
                                        {
                                            entryType = "CREATE";
                                            skip.AddRange(createChildren.Select(s => s.Id).ToList());
                                        }
                                        else // SKIP and use children instead
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (allChildren.Count == 0 ||
                                            (createChildren.Count == 0 && deleteChildren.Count == 0
                                                && allChildren.Count != 1 && allChildren.Count == updateChildren.Count))
                                        {
                                            entryType = "UPDATE";
                                            skip.AddRange(updateChildren.Select(s => s.Id).ToList());
                                        }
                                        else // SKIP and use children instead
                                        {
                                            continue;
                                        }
                                    }
                                }
                                var parentId = sme.ParentSMEId;
                                var idShortPath = sme.IdShortPath;

                                if (sme.TimeStampTree > timeStampMax)
                                {
                                    timeStampMax = sme.TimeStampTree;
                                }
                                /*
                                var idShortPath = sme.IdShort;
                                while (parentId != null && parentId != 0)
                                {
                                    var parentSME = smeSearchSM.Where(sme => sme.Id == parentId).FirstOrDefault();
                                    idShortPath = parentSME.IdShort + "." + idShortPath;
                                    parentId = parentSME.ParentSMEId;
                                }
                                */

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
                                    var s = CrudOperator.ReadSubmodelElement(sme, treeMerged);
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
                                diffEntry.Add(entry.entryType + " " + entry.idShortPath);
                                Console.WriteLine($"Event {entry.entryType} Type: {entry.payloadType} idShortPath: {entry.idShortPath}");
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
                            var s = CrudOperator.ReadSubmodel(db, sm);
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

                        diffEntry.Add(entry.entryType + " " + entry.idShortPath);
                        Console.WriteLine($"Event {entry.entryType} Type: {entry.payloadType} idShortPath: {entry.idShortPath}");
                        e.eventEntries.Add(entry);
                        countSM++;
                    }
                }
                if (countSM == 0 && countSME == 0)
                {
                    if (searchSM is "(*)" or "*" or "")
                    {
                        timeStampMax = db.SMSets.Select(sm => sm.TimeStampTree).Max();
                    }
                    else
                    {
                        timeStampMax = db.SMSets.Where(searchSM).Select(sm => sm.TimeStampTree).Max();
                    }
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

    public int ChangeData(string json, EventDto eventData, AdminShellPackageEnv[] env, IReferable referable, out string transmit, out string lastDiffValue, out string statusValue, List<String> diffEntry, int packageIndex = -1)
    {
        transmit = "";
        lastDiffValue = "";
        statusValue = "ERROR";
        int count = 0;

        var statusData = eventData.StatusData;

        EventPayload eventPayload = null;
        try
        {
            eventPayload = JsonSerializer.Deserialize<EventPayload>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        transmit = eventPayload.status.transmitted;
        var dt = TimeStamp.TimeStamp.StringToDateTime(eventPayload.status.lastUpdate);
        dt = DateTime.Parse(eventPayload.status.lastUpdate);
        var dtTransmit = DateTime.Parse(eventPayload.status.transmitted);
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
                receiveSme.TimeStampCreate = dtTransmit;
                receiveSme.TimeStampDelete = new DateTime();
                receiveSme.SetAllParentsAndTimestamps(statusData, dtTransmit, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                receiveSme.SetTimeStamp(dtTransmit);
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
                        count += ChangeSubmodelElement(eventData, entry, referable as ISubmodelElement, children, "", diffEntry);
                        if (count > 0)
                        {
                            env[packageIndex].setWrite(true);
                        }
                    }
                }
            }

            if (count > 0)
            {
                env[packageIndex].setWrite(true);
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
                Submodel receiveSM = null;
                if (entry.payloadType == "sm")
                {
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
                            if (entry.entryType == "DELETE")
                            {
                                var smDBQuery = db.SMSets.Where(sm => sm.Identifier == receiveSM.Id);
                                var smDB = smDBQuery.ToList();
                                if (smDB != null && smDB.Count > 0)
                                {
                                    smDBQuery.ExecuteDeleteAsync().Wait();
                                    db.SaveChanges();
                                }
                            }
                            var visitor = new VisitorAASX(db);
                            visitor.update = entry.entryType == "UPDATE";
                            visitor.currentDataTime = dt;
                            visitor.VisitSubmodel(receiveSM);
                            db.Add(visitor._smDB);
                            db.SaveChanges();
                            count++;
                        }
                    }
                }
                if (entry.payloadType == "sme")
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
                        List<int> smeDelete = [];
                        using (var db = new AasContext())
                        {
                            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);
                            var smDB = smDBQuery.FirstOrDefault();
                            if (smDB == null)
                            {
                                smDB = new SMSet();
                                smDB.Identifier = submodelIdentifier;
                                var idShort = Regex.Replace(submodelIdentifier, @"[^\w\d]", "_");
                                smDB.IdShort = idShort;
                                smDB.Kind = ModellingKind.Instance.ToString();
                                CrudOperator.setTimeStamp(smDB, dt);
                                db.Add(smDB);
                                db.SaveChanges();
                            }
                            if (smDB != null)
                            {
                                var visitor = new VisitorAASX(db);
                                visitor._smDB = smDB;
                                visitor.currentDataTime = dt;
                                var smDBId = smDB.Id;
                                var smeSmList = db.SMESets.Where(sme => sme.SMId == smDBId).ToList();
                                CrudOperator.CreateIdShortPath(db, smeSmList);
                                var smeSmMerged = CrudOperator.GetSmeMerged(db, smeSmList, smDB);
                                visitor.smSmeMerged = smeSmMerged;

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
                                        visitor.idShortPath = e.idShortPath;
                                        visitor.update = e.entryType == "UPDATE";
                                        var receiveSmeDB = visitor.VisitSMESet(receiveSme);
                                        if (receiveSmeDB != null)
                                        {
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
                                                        var parentDB = smeSmMerged.Where(sme => sme.smeSet.IdShortPath == parentPath).FirstOrDefault();
                                                        if (parentDB != null)
                                                        {
                                                            receiveSmeDB.ParentSMEId = parentDB.smeSet.Id;
                                                            receiveSmeDB.IdShortPath = e.idShortPath;
                                                            change = true;
                                                        }
                                                    }
                                                    if (change)
                                                    {
                                                        CrudOperator.setTimeStampTree(db, smDB, receiveSmeDB, receiveSmeDB.TimeStamp);
                                                        try
                                                        {
                                                            db.SMESets.Add(receiveSmeDB);
                                                            db.SaveChanges();
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                        }
                                                        var smeDB = smeSmMerged.Where(sme =>
                                                                !visitor.keepSme.Contains(sme.smeSet.Id) &&
                                                                visitor.deleteSme.Contains(sme.smeSet.Id)
                                                            ).ToList();
                                                        var smeDBList = smeDB.Select(sme => sme.smeSet.Id).Distinct().ToList();
                                                        smeDelete.AddRange(smeDBList);
                                                        count++;
                                                    }
                                                    break;
                                            }
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
                                                    smeSmMerged.Where(sme =>
                                                        sme.smeSet.IdShortPath != null &&
                                                        (sme.smeSet.IdShortPath == deletePath ||
                                                        sme.smeSet.IdShortPath.StartsWith(deletePath + ".")))
                                                    .Select(sme => sme.smeSet.Id).ToList());
                                            }
                                            smeDelete.AddRange(smeDBList);
                                            count++;
                                        }
                                    }
                                }
                            }
                            if (smeDelete.Count > 0)
                            {
                                db.SMESets.Where(sme => smeDelete.Contains(sme.Id)).ExecuteDeleteAsync().Wait();
                                db.SaveChanges();
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

    public int ChangeSubmodelElement(EventDto eventData, EventPayloadEntry entry, IReferable parent, List<ISubmodelElement> submodelElements, string idShortPath, List<String> diffEntry)
    {
        int count = 0;
        var dt = DateTime.Parse(entry.lastUpdate);

        int maxCount = 0;
        if (eventData.DataMaxSize != null && eventData.DataMaxSize.Value != null)
        {
            maxCount = Convert.ToInt32(eventData.DataMaxSize.Value);
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
                                count += ChangeSubmodelElement(eventData, entry, smc, smc.Value, path, diffEntry);
                                break;
                            case ISubmodelElementList sml:
                                count += ChangeSubmodelElement(eventData, entry, sml, sml.Value, path, diffEntry);
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
                            count += ChangeSubmodelElement(eventData, entry, smc, smc.Value, path, diffEntry);
                            break;
                        case ISubmodelElementList sml:
                            count += ChangeSubmodelElement(eventData, entry, sml, sml.Value, path, diffEntry);
                            break;
                    }
                }
            }
        }
        if (entry.entryType == "DELETE")
        {
            if (maxCount == 0 || eventData.DataCollection != parent)
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
                        count += ChangeSubmodelElement(eventData, entry, sme, children, path, diffEntry);
                    }
                }
            }
        }

        if (maxCount != 0 && eventData.DataCollection == parent)
        {
            SubmodelElementCollection data = null;
            if (eventData.DataCollection is SubmodelElementCollection dataSmc)
            {
                data = dataSmc;
            }
            // if (eventData.direction != null && eventData.direction.Value == "IN" && eventData.mode != null && (eventData.mode.Value == "PUSH" || eventData.mode.Value == "PUT"))
            if (data != null && eventData.Direction != null && eventData.Direction.Value == "IN" && eventData.Mode != null)
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


    /***********************           Event data           ****************************************/

    public Operation FindEvent(ISubmodel submodel, string eventPath)
    {
        return FindEvent(submodel, null, eventPath);
    }

    public Operation FindEvent(ISubmodel submodel, ISubmodelElement sme, string eventPath)
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

    public EventDto ParseData(Operation op, AdminShellPackageEnv env)
    {
        SubmodelElementCollection smec = null;
        SubmodelElementList smel = null;
        Submodel sm = null;
        AasCore.Aas3_0.Property p = null;
        EventDto eventDto = new EventDto();

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
                        eventDto.Direction = p;
                    break;
                case "mode":
                    if (p != null)
                        eventDto.Mode = p;
                    break;
                case "changes":
                    if (p != null)
                        eventDto.Changes = p;
                    break;
                case "authentication":
                    if (smec != null)
                        eventDto.Authentication = smec;
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
                        eventDto.EndPoint = p;
                    }
                    break;
                case "nopayload":
                    if (p != null)
                        eventDto.NoPayload = p;
                    break;
                case "data":
                case "observed":
                    if (inputRef is ReferenceElement r)
                    {
                        eventDto.DataReference = r;
                    }
                    if (sm != null)
                        eventDto.DataSubmodel = sm;
                    if (smec != null)
                        eventDto.DataCollection = smec;
                    if (smel != null)
                        eventDto.DataCollection = smel;
                    break;
                case "datamaxsize":
                    if (p != null)
                        eventDto.DataMaxSize = p;
                    break;
                case "statusdata":
                    if (smec != null)
                        eventDto.StatusData = smec;
                    break;
                case "persistence":
                    if (p != null)
                        eventDto.Persistence = p;
                    break;
                case "conditionsm":
                    if (p != null)
                        eventDto.ConditionSM = p;
                    break;
                case "conditionsme":
                    if (p != null)
                        eventDto.ConditionSME = p;
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
                        eventDto.LastUpdate = p;
                    break;
                case "status":
                    if (smec != null)
                        eventDto.Status = smec;
                    break;
                case "statusdata":
                    if (smec != null)
                        eventDto.StatusData = smec;
                    break;
                case "diff":
                    if (smec != null)
                        eventDto.Diff = smec;
                    break;
            }
        }

        if (eventDto.Status != null)
        {
            foreach (var sme in eventDto.Status.Value)
            {
                switch (sme.IdShort.ToLower())
                {
                    case "message":
                        if (sme is AasCore.Aas3_0.Property)
                        {
                            eventDto.Message = sme as AasCore.Aas3_0.Property;
                        }
                        break;
                    case "transmitted":
                        if (sme is AasCore.Aas3_0.Property)
                        {
                            eventDto.Transmitted = sme as AasCore.Aas3_0.Property;
                        }
                        break;
                    case "lastupdate":
                        if (sme is AasCore.Aas3_0.Property)
                        {
                            eventDto.LastUpdate = sme as AasCore.Aas3_0.Property;
                        }
                        break;
                    case "diff":
                        if (sme is SubmodelElementCollection)
                        {
                            eventDto.Diff = sme as SubmodelElementCollection;
                        }
                        break;
                }
            }
        }

        if (eventDto.Authentication != null)
        {
            smec = eventDto.Authentication;
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
                            eventDto.AuthType = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "accesstoken":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.AccessToken = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "clienttoken":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.ClientToken = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "username":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.UserName = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "password":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.PassWord = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "authservercertificate":
                        if (sme2 is AasCore.Aas3_0.File)
                        {
                            eventDto.AuthServerCertificate = sme2 as AasCore.Aas3_0.File;
                        }

                        break;

                    case "authserverendpoint":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.AuthServerEndPoint = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "clientcertificate":
                        if (sme2 is AasCore.Aas3_0.File)
                        {
                            eventDto.ClientCertificate = sme2 as AasCore.Aas3_0.File;
                        }

                        break;

                    case "clientcertificatepassword":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.ClientCertificatePassWord = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;
                }
            }
        }
        return eventDto;
    }
}
