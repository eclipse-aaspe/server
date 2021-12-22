using AasxRestServerLibrary;
using AdminShellEvents;
using AdminShellNS;
using IO.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static AasxRestServerLibrary.AasxRestServer;

namespace IO.Swagger.Controllers
{
    /// <summary>
    /// Returns Event Messages for the specified Asset Admin Shell
    /// </summary>
    //[Authorize]
    [ApiController]
    public class AssetAdministrationShellEventInterfaceApi : ControllerBase
    {
        private bool _setAllParentsExecuted = false;

        [HttpGet]
        [Route("/aas/{aasIndex}/geteventmessages")]
        [ValidateModelState]
        [SwaggerOperation("GetEventMessages")]
        [SwaggerResponse(statusCode: 200, type: typeof(AasEventMsgEnvelope[]), description: "Requested event messages")]
        public virtual void GetEventMessages([FromRoute][Required] int aasIndex)
        {
            GenerateMessagesInternal(aasIndex, DateTime.MinValue, true, true);
        }

        [HttpGet]
        [Route("/aas/{aasIndex}/geteventmessages/values")]
        [ValidateModelState]
        [SwaggerOperation("GetEventMessagesValues")]
        [SwaggerResponse(statusCode: 200, type: typeof(AasEventMsgEnvelope[]), description: "Requested values of event messages")]
        public virtual void GetEventMessagesValues([FromRoute][Required] int aasIndex)
        {
            GenerateMessagesInternal(aasIndex, DateTime.MinValue, true, false);
        }

        [HttpGet]
        [Route("/aas/{aasIndex}/geteventmessages/time/{minimumDate}")]
        [ValidateModelState]
        [SwaggerOperation("GetEventMessagesTime")]
        [SwaggerResponse(statusCode: 200, type: typeof(AasEventMsgEnvelope[]), description: "Requested timestamps of event messages")]
        public virtual void GetEventMessagesTime([FromRoute][Required] int aasIndex, [FromRoute][Required] DateTime minimumDate)
        {
            GenerateMessagesInternal(aasIndex, minimumDate, true, true);
        }

        [HttpGet]
        [Route("/aas/{aasIndex}/geteventmessages/deltasecs/{secs}")]
        [ValidateModelState]
        [SwaggerOperation("GetEventMessagesTimeSecs")]
        [SwaggerResponse(statusCode: 200, type: typeof(AasEventMsgEnvelope[]), description: "Requested event messages for the specified seconds")]
        public virtual void GetEventMessagesTimeSecs([FromRoute][Required] int aasIndex, [FromRoute][Required] int secs)
        {
            DateTime minimumDate = DateTime.UtcNow.AddSeconds(-1.0 * secs);
            GenerateMessagesInternal(aasIndex, minimumDate, true, true);
        }

        private void GenerateMessagesInternal(int aasIndex, DateTime minimumDate, bool doUpdate, bool doCreateDelete)
        {
            // Set parents for all childs.
            // Note: this has to be done only once for AASX Server, therefore a better place than
            // here could be figured out
            if (!_setAllParentsExecuted)
            {
                _setAllParentsExecuted = true;

                if (AasxServer.Program.env != null)
                    foreach (var e in AasxServer.Program.env)
                        if (e?.AasEnv?.Submodels != null)
                            foreach (var sm in e.AasEnv.Submodels)
                                if (sm != null)
                                    sm.SetAllParents();
            }

            var envelopes = new List<AasEventMsgEnvelope>();

            int aascount = AasxServer.Program.env.Length;
            for (int i = 0; i < aascount; i++)
            {
                if (aasIndex >= 0 && i != aasIndex)
                    continue;

                var env = AasxServer.Program.env[i];
                if (env?.AasEnv?.AdministrationShells == null)
                    continue;

                foreach (var aas in env.AasEnv.AdministrationShells)
                {
                    if (aas?.submodelRefs == null)
                        continue;

                    foreach (var smr in aas.submodelRefs)
                    {
                        // find Submodel
                        var sm = env.AasEnv.FindSubmodel(smr);
                        if (sm == null)
                            continue;

                        // find a matching event element
                        foreach (var bev in sm.FindDeep<AdminShell.BasicEvent>())
                        {
                            // find interesting event?
                            if (true == bev.semanticId?.MatchesExactlyOneKey(
                                type: AdminShell.Key.ConceptDescription,
                                local: false,
                                idType: AdminShell.Identification.IRI,
                                id: "https://admin-shell.io/tmp/AAS/Events/UpdateValueOutwards",
                                matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                            {
                                doUpdate = true;
                                doCreateDelete = false;
                            }
                            else
                            if (true == bev.semanticId?.MatchesExactlyOneKey(
                                type: AdminShell.Key.ConceptDescription,
                                local: false,
                                idType: AdminShell.Identification.IRI,
                                id: "https://admin-shell.io/tmp/AAS/Events/StructureChangeOutwards",
                                matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                            {
                                doUpdate = false;
                                doCreateDelete = true;
                            }
                            else
                                continue;

                            // find obseverved as well
                            if (bev.observed == null && bev.observed.Count < 1)
                                continue;
                            var obs = env.AasEnv.FindReferableByReference(bev.observed);
                            if (obs == null)
                                continue;

                            // obseverved semantic id is pain in the ..
                            AdminShell.SemanticId obsSemId = null;
                            if (obs is AdminShell.Submodel obssm)
                                obsSemId = obssm.semanticId;
                            if (obs is AdminShell.SubmodelElement obssme)
                                obsSemId = obssme.semanticId;

                            //
                            // Create event outer message
                            //

                            var eventsOuter = new AasEventMsgEnvelope(
                                    DateTime.UtcNow,
                                    source: bev.GetReference(),
                                    sourceSemanticId: bev.semanticId,
                                    observableReference: bev.observed,
                                    observableSemanticId: obsSemId);

                            // directly create lists of update value and structural change events

                            var plStruct = new AasPayloadStructuralChange();
                            var plUpdate = new AasPayloadUpdateValue();

                            string[] modes = { "CREATE", "UPDATE" };

                            //
                            // Check for deletes
                            //

                            if (doCreateDelete)
                            {
                                foreach (var d in TestResource.eventMessage.DeletedList)
                                {
                                    if (d.rf == null || d.sm != sm)
                                        continue;
                                    if (d.rf.TimeStamp > minimumDate)
                                    {
                                        // get the path
                                        AdminShell.KeyList p2 = null;
                                        if (d.rf is AdminShell.Submodel delsm)
                                            p2 = delsm?.GetReference()?.Keys;
                                        if (d.rf is AdminShell.SubmodelElement delsme)
                                            p2 = delsme?.GetReference()?.Keys;
                                        if (p2 == null)
                                            continue;

                                        // prepare p2 to be relative path to observable
                                        if (true == p2?.StartsWith(bev.observed?.Keys, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                                            p2.RemoveRange(0, bev.observed.Keys.Count);

                                        // make payload
                                        var pliDel = new AasPayloadStructuralChangeItem(
                                            count: 1,
                                            timeStamp: d.rf.TimeStamp,
                                            AasPayloadStructuralChangeItem.ChangeReason.Delete,
                                            path: p2);

                                        // add
                                        plStruct.Changes.Add(pliDel);
                                    }
                                }
                            }
                            else
                            {
                            }

                            //
                            // Create & update
                            //

                            //for (int imode = 0; imode < modes.Length; imode++)
                            //{
                            if ((doCreateDelete || doUpdate) == false)
                                throw new Exception("invalid flags");

                            DateTime diffTimeStamp = sm.TimeStamp;
                            var strMode = "";
                            if (doCreateDelete)
                                strMode = "CREATE";
                            if (doUpdate)
                                strMode = "UPDATE";
                            if (strMode != "")
                                if (diffTimeStamp > minimumDate)
                                {
                                    ;
                                    foreach (var sme in sm.submodelElements)
                                        GetEventMsgRecurseDiff(
                                            strMode,
                                            plStruct, plUpdate,
                                            sme.submodelElement,
                                            minimumDate, doUpdate, doCreateDelete,
                                            bev.observed?.Keys);
                                }
                            //}

                            // prepare message envelope and remember

                            if (plStruct.Changes.Count > 0)
                                eventsOuter.Payloads.Add(plStruct);

                            if (plUpdate.Values.Count > 0)
                                eventsOuter.Payloads.Add(plUpdate);

                            if (eventsOuter.Payloads.Count > 0)
                                envelopes.Add(eventsOuter);
                        }
                    }
                }
            }

            AasxHttpContextHelper.SendJsonResponse2(HttpContext, envelopes.ToArray());
            //var result = new ObjectResult(envelopes.ToArray()) { StatusCode = (int)HttpStatusCode.OK };
            //return result ;
        }

        static void GetEventMsgRecurseDiff(
            string mode,
            AasPayloadStructuralChange plStruct,
            AasPayloadUpdateValue plUpdate,
            AdminShell.SubmodelElement sme, DateTime minimumDate,
            bool doUpdate, bool doCreateDelete,
            AdminShell.KeyList observablePath = null)
        {
            DateTime diffTimeStamp;

            if (!(sme is AdminShell.SubmodelElementCollection))
            {
                if (mode == "CREATE")
                    diffTimeStamp = sme.TimeStampCreate;
                else // UPDATE
                    diffTimeStamp = sme.TimeStamp;
                if (diffTimeStamp > minimumDate)
                {
                    // prepare p2 to be relative path to observable
                    var p2 = sme.GetReference()?.Keys;
                    if (true == p2?.StartsWith(observablePath, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                        p2.RemoveRange(0, observablePath.Count);

                    if (mode == "CREATE")
                    {
                        if (/* doCreateDelete && */ plStruct != null)
                            plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                count: 1,
                                timeStamp: sme.TimeStamp,
                                AasPayloadStructuralChangeItem.ChangeReason.Create,
                                path: p2,
                                // Assumption: models will be serialized correctly
                                data: JsonConvert.SerializeObject(sme)));
                    }
                    else
                    if (sme.TimeStamp != sme.TimeStampCreate)
                    {
                        if (/* doUpdate && */ plUpdate != null)
                        {
                            var val = sme.ValueAsText();
                            if (sme is AdminShell.Blob blob)
                                // take BLOB as "large" text
                                val = blob.value;
                            plUpdate.Values.Add(new AasPayloadUpdateValueItem(
                                path: p2,
                                val));
                        }
                    }
                }

                return;
            }

            var smec = sme as AdminShell.SubmodelElementCollection;
            diffTimeStamp = smec.TimeStamp;
            if (smec.TimeStamp > minimumDate)
            {
                // TODO: check if to modify to send serializations of whole SMCs on CREATE
                if (mode == "CREATE" || smec.TimeStamp != smec.TimeStampCreate)
                {
                    bool deeper = false;
                    if (doUpdate /* && !doCreateDelete */)
                    {
                        deeper = true;
                    }
                    else
                    {
                        foreach (var sme2 in smec.value)
                            if (sme2.submodelElement.TimeStamp != smec.TimeStamp)
                            {
                                deeper = true;
                                break;
                            }
                    }

                    if (deeper)
                    {
                        foreach (var sme2 in smec.value)
                            GetEventMsgRecurseDiff(
                                mode,
                                plStruct, plUpdate,
                                sme2.submodelElement, minimumDate, doUpdate, doCreateDelete, observablePath);
                        return;
                    }

                    // prepare p2 to be relative path to observable
                    var p2 = sme.GetReference()?.Keys;
                    if (true == p2?.StartsWith(observablePath, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                        p2.RemoveRange(0, observablePath.Count);

                    if (mode == "CREATE")
                    {
                        if (sme.TimeStampCreate > minimumDate)
                        {
                            if (/* doCreateDelete && */ plStruct != null)
                                plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                    count: 1,
                                    timeStamp: sme.TimeStamp,
                                    AasPayloadStructuralChangeItem.ChangeReason.Create,
                                    path: p2,
                                    // Assumption: models will be serialized correctly
                                    data: JsonConvert.SerializeObject(sme)));
                        }
                    }
                    else
                    if (sme.TimeStamp != sme.TimeStampCreate)
                    {
                        if (/* doUpdate && */ plUpdate != null)
                            plUpdate.Values.Add(new AasPayloadUpdateValueItem(
                                path: p2,
                                sme.ValueAsText()));
                    }
                }
            }
        }
    }
}
