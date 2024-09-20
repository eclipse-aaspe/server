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
                        count += collectSubmodelElements(children, diffTime, entryType, submodelId, idShortPath + sme.IdShort + ".", entries, diffEntry, noPayload);
                    }
                }
            }
            return count;
        }

        public static EventPayload CollectPayload(string changes, int depth, SubmodelElementCollection statusData, IReferable referable, string diff, List<String> diffEntry, bool noPayload)
        {
            var e = new EventPayload();
            e.status.transmitted = TimeStamp.TimeStamp.DateTimeToString(DateTime.UtcNow);
            e.status.lastUpdate = "";
            e.eventEntries = new List<EventPayloadEntry>();

            var isInitial = diff == "init";
            var isStatus = diff == "status";

            if (statusData != null && statusData.Value != null)
            {
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
            }

            lock (EventLock)
            {
                if (!isStatus && referable != null)
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
                        diffEntry.Add(entry.entryType + " " + entry.idShortPath);
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
                            if (!(referable is Submodel))
                            {
                                children = new List<ISubmodelElement>();
                                children.Add(referable as ISubmodelElement);
                                collectSubmodelElements(children, diffTime, "DELETE", submodel.Id, "", e.eventEntries, diffEntry, noPayload);
                            }
                            else
                            {
                                collectSubmodelElements(children, diffTime, "DELETE", submodel.Id, idShortPath, e.eventEntries, diffEntry, noPayload);
                            }
                        }
                        if (changes == null || changes.Contains("UPDATE"))
                        {
                            collectSubmodelElements(children, diffTime, "UPDATE", submodel.Id, idShortPath, e.eventEntries, diffEntry, noPayload);
                        }
                    }
                }
            }

            return e;
        }

        public static int changeData(string json, SubmodelElementCollection statusData, AdminShellPackageEnv[] env, IReferable referable, out string transmit, out string lastDiffValue, out string statusValue, List<String> diffEntry, int packageIndex = -1)
        {
            transmit = "";
            lastDiffValue = "";
            statusValue = "ERROR";
            int count = 0;

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

                /*
                var entryStatusData = new EventPayloadEntry();
                entryStatusData.entryType = "CREATE";
                entryStatusData.lastUpdate = eventPayload.status.lastUpdate;
                entryStatusData.payloadType = "sme";
                entryStatusData.payload = eventPayload.statusData;
                entryStatusData.submodelId = eventPayload.eventEntries[0].submodelId;
                entryStatusData.idShortPath = receiveSme.IdShort;
                entryStatusData.notDeletedIdShortList = new List<string>();
                eventPayload.eventEntries.Insert(0, entryStatusData);
                */
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

            /*
            if (submodel == null)
            {
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
                                data = submodel.SubmodelElements;
                                index = i;
                                packageIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
            else // parse EventElement in submodel
            {
                foreach (var sme in submodel.SubmodelElements)
                {
                    if (sme is Operation op && sme.IdShort.ToLower().StartsWith("eventelement"))
                    {
                        foreach (var input in op.InputVariables)
                        {
                            if (input.Value is ReferenceElement r && input.Value.IdShort == "data")
                            {
                                var refElement = env[packageIndex].AasEnv.FindReferableByReference(r.Value);
                                if (refElement is SubmodelElementCollection smc)
                                {
                                    dataCollection = smc;
                                    data = smc.Value;
                                }
                            }
                            if (input.Value is ReferenceElement r2 && input.Value.IdShort == "statusData")
                            {
                                var refElement = env[packageIndex].AasEnv.FindReferableByReference(r2.Value);
                                if (refElement is SubmodelElementCollection smc)
                                {
                                    if (statusDataCollection != null)
                                    {
                                        smc.Value = new List<ISubmodelElement>();
                                        smc.Value.Add(statusDataCollection);
                                    }
                                    smc.SetAllParentsAndTimestamps(smc.Parent as IReferable, dt, dt, dt);
                                    smc.SetTimeStamp(dt);
                                }
                            }
                        }

                        foreach (var output in op.OutputVariables)
                        {
                            var outputRef = output.Value;
                            if (outputRef is ReferenceElement r && output.Value.IdShort == "status")
                            {
                                var refElement = env[packageIndex].AasEnv.FindReferableByReference(r.Value);
                                if (refElement is SubmodelElementCollection smc)
                                {
                                    status = smc;
                                    status.SetAllParentsAndTimestamps(status.Parent as IReferable, dt, dt, dt);
                                    status.SetTimeStamp(dt);
                                }
                            }
                        }
                    }
                }
            }
            */

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
                        count += changeSubmodelElement(entry, referable as ISubmodelElement, children, "", diffEntry);
                        if (count > 0)
                        {
                            env[packageIndex].setWrite(true);
                        }
                    }
                }
            }

            statusValue = "Updated: " + count;


            return count;
        }

        public static int changeSubmodelElement(EventPayloadEntry entry, IReferable parent, List<ISubmodelElement> submodelElements, string idShortPath, List<String> diffEntry)
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
                                    count += changeSubmodelElement(entry, smc, smc.Value, path, diffEntry);
                                    break;
                                case ISubmodelElementList sml:
                                    count += changeSubmodelElement(entry, sml, sml.Value, path, diffEntry);
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
                                count += changeSubmodelElement(entry, smc, smc.Value, path, diffEntry);
                                break;
                            case ISubmodelElementList sml:
                                count += changeSubmodelElement(entry, sml, sml.Value, path, diffEntry);
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
                        diffEntry.Add(entry.entryType + " " + entry.idShortPath + ".*");
                        count++;
                        return count;
                    }
                    var path = idShortPath + sme.IdShort + ".";
                    if (entry.idShortPath.StartsWith(path))
                    {
                        count += changeSubmodelElement(entry, sme, children, path, diffEntry);
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
        public SubmodelElementCollection dataCollection = null;
        public SubmodelElementCollection statusData = null;
        public AasCore.Aas3_0.Property noPayload = null;

        public SubmodelElementCollection status = null;
        public AasCore.Aas3_0.Property message = null;
        public AasCore.Aas3_0.Property transmitted = null;
        public AasCore.Aas3_0.Property lastUpdate = null;
        public SubmodelElementCollection diff = null;

        public EventData()
        {

        }

        public static Operation FindEvent(ISubmodel submodel, string eventName)
        {
            foreach (var sme in submodel.SubmodelElements)
            {
                if (sme is Operation op && sme.IdShort == eventName)
                {
                    return op;
                }
            }

            return null;
        }

        public void ParseData(Operation op, AdminShellPackageEnv env)
        {
            SubmodelElementCollection smec = null;
            Submodel sm = null;
            AasCore.Aas3_0.Property p = null;

            foreach (var input in op.InputVariables)
            {
                smec = null;
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

                if (inputRef is ReferenceElement)
                {
                    var refElement = env.AasEnv.FindReferableByReference((inputRef as ReferenceElement).Value);
                    if (refElement is SubmodelElementCollection)
                    {
                        smec = refElement as SubmodelElementCollection;
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
                            endPoint = p;
                        break;
                    case "nopayload":
                        if (p != null)
                            noPayload = p;
                        break;
                    case "data":
                    case "observed":
                        if (sm != null)
                            dataSubmodel = sm;
                        if (smec != null)
                            dataCollection = smec;
                        break;
                    case "statusdata":
                        if (smec != null)
                            statusData = smec;
                        break;
                }
            }

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
