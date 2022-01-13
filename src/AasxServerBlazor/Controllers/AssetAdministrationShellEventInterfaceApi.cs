using AasxDemonstration;
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
using System.Net;

namespace IO.Swagger.Controllers
{
    /// <summary>
    /// Returns Event Messages for the specified Asset Admin Shell
    /// </summary>
    //[Authorize]
    [ApiController]
    public class AssetAdministrationShellEventInterfaceApi : ControllerBase
    {
        [HttpGet]
        [Route("/aas/{aasEnvIndex}/geteventmessages")]
        [ValidateModelState]
        [SwaggerOperation("GetEventMessages")]
        [SwaggerResponse(statusCode: 200, type: typeof(AasEventMsgEnvelope[]), description: "Requested event messages")]
        public virtual IActionResult GetEventMessages([FromRoute][Required] int aasEnvIndex)
        {
            return GenerateEventMessages(aasEnvIndex, DateTime.MinValue);
        }

        [HttpGet]
        [Route("/aas/{aasEnvIndex}/geteventmessages/time/{minimumDate}")]
        [ValidateModelState]
        [SwaggerOperation("GetEventMessagesTime")]
        [SwaggerResponse(statusCode: 200, type: typeof(AasEventMsgEnvelope[]), description: "Requested timestamps of event messages")]
        public virtual IActionResult GetEventMessagesTime([FromRoute][Required] int aasEnvIndex, [FromRoute][Required] DateTime minimumDate)
        {
            return GenerateEventMessages(aasEnvIndex, minimumDate);
        }

        private IActionResult GenerateEventMessages(int aasEnvIndex, DateTime minimumDate)
        {
            var envelopes = new List<AasEventMsgEnvelope>();

            bool doCreateDelete = true;

            if ((0 <= aasEnvIndex) && (aasEnvIndex < AasxServer.Program.env.Count))
            {
                var env = AasxServer.Program.env[aasEnvIndex];
                foreach (var aas in env?.AasEnv?.AdministrationShells)
                {
                    if (aas?.submodelRefs == null)
                    {
                        continue;
                    }

                    foreach (var smr in aas.submodelRefs)
                    {
                        var sm = env.AasEnv.FindSubmodel(smr);
                        if (sm == null)
                        {
                            continue;
                        }

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
                                doCreateDelete = false;
                            }
                            else if (true == bev.semanticId?.MatchesExactlyOneKey(
                                type: AdminShell.Key.ConceptDescription,
                                local: false,
                                idType: AdminShell.Identification.IRI,
                                id: "https://admin-shell.io/tmp/AAS/Events/StructureChangeOutwards",
                                matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                            {
                                doCreateDelete = true;
                            }
                            else continue;

                            // find obseverved as well
                            if (bev.observed == null && bev.observed.Count < 1)
                            {
                                continue;
                            }

                            var obs = env.AasEnv.FindReferableByReference(bev.observed);
                            if (obs == null)
                            {
                                continue;
                            }

                            AdminShell.SemanticId obsSemId = null;
                            if (obs is AdminShell.Submodel obssm)
                            {
                                obsSemId = obssm.semanticId;
                            }

                            if (obs is AdminShell.SubmodelElement obssme)
                            {
                                obsSemId = obssme.semanticId;
                            }

                            // Create event outer message
                            var eventsOuter = new AasEventMsgEnvelope(
                                    DateTime.UtcNow,
                                    source: bev.GetReference(),
                                    sourceSemanticId: bev.semanticId,
                                    observableReference: bev.observed,
                                    observableSemanticId: obsSemId);

                            // directly create lists of update value and structural change events
                            var plStruct = new AasPayloadStructuralChange();
                            var plUpdate = new AasPayloadUpdateValue();

                            if (doCreateDelete)
                            {
                                // Check for deletes
                                foreach (var d in EnergyModel.eventMessage.DeletedList)
                                {
                                    if (d.rf == null || d.sm != sm)
                                    {
                                        continue;
                                    }

                                    if (d.rf.TimeStamp > minimumDate)
                                    {
                                        // get the path
                                        AdminShell.KeyList p2 = null;
                                        if (d.rf is AdminShell.Submodel delsm)
                                        {
                                            p2 = delsm?.GetReference()?.Keys;
                                        }

                                        if (d.rf is AdminShell.SubmodelElement delsme)
                                        {
                                            p2 = delsme?.GetReference()?.Keys;
                                        }

                                        if (p2 == null)
                                        {
                                            continue;
                                        }

                                        // prepare p2 to be relative path to observable
                                        if (true == p2?.StartsWith(bev.observed?.Keys, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                                        {
                                            p2.RemoveRange(0, bev.observed.Keys.Count);
                                        }

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

                            DateTime diffTimeStamp = sm.TimeStamp;
                            if (diffTimeStamp > minimumDate)
                            {
                                foreach (var sme in sm.submodelElements)
                                    GetEventMsgRecurseDiff(
                                        plStruct,
                                        plUpdate,
                                        sme.submodelElement,
                                        minimumDate, doCreateDelete,
                                        bev.observed?.Keys);
                            }

                            // prepare message envelope and remember
                            if (plStruct.Changes.Count > 0)
                            {
                                eventsOuter.Payloads.Add(plStruct);
                            }

                            if (plUpdate.Values.Count > 0)
                            {
                                eventsOuter.Payloads.Add(plUpdate);
                            }

                            if (eventsOuter.Payloads.Count > 0)
                            {
                                envelopes.Add(eventsOuter);
                            }
                        }
                    }
                }
            }

            JsonSerializerSettings settings = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonSettings(new[] { typeof(AasEventMsgEnvelope) });
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.Formatting = Formatting.Indented;
            return new JsonResult(envelopes.ToArray(), settings) { StatusCode = (int)HttpStatusCode.OK };
        }

        static void GetEventMsgRecurseDiff(
            AasPayloadStructuralChange plStruct,
            AasPayloadUpdateValue plUpdate,
            AdminShell.SubmodelElement sme,
            DateTime minimumDate,
            bool doCreateDelete,
            AdminShell.KeyList observablePath = null)
        {
            DateTime diffTimeStamp;

            if (!(sme is AdminShell.SubmodelElementCollection))
            {
                if (doCreateDelete)
                {
                    diffTimeStamp = sme.TimeStampCreate;
                }
                else
                {
                    diffTimeStamp = sme.TimeStamp;
                }

                if (diffTimeStamp > minimumDate)
                {
                    // prepare p2 to be relative path to observable
                    var p2 = sme.GetReference()?.Keys;
                    if (true == p2?.StartsWith(observablePath, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                    {
                        p2.RemoveRange(0, observablePath.Count);
                    }

                    if (doCreateDelete)
                    {
                        if (plStruct != null)
                        {
                            plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                1,
                                sme.TimeStamp,
                                AasPayloadStructuralChangeItem.ChangeReason.Create,
                                p2,
                                // Assumption: models will be serialized correctly
                                JsonConvert.SerializeObject(sme)));
                        }
                    }
                    else
                    if (sme.TimeStamp != sme.TimeStampCreate)
                    {
                        if (plUpdate != null)
                        {
                            var val = sme.ValueAsText();
                            if (sme is AdminShell.Blob blob)
                            {
                                // take BLOB as "large" text
                                val = blob.value;
                            }

                            plUpdate.Values.Add(new AasPayloadUpdateValueItem(
                                p2,
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
                if (doCreateDelete || smec.TimeStamp != smec.TimeStampCreate)
                {
                    bool deeper = false;
                    if (!doCreateDelete)
                    {
                        deeper = true;
                    }
                    else
                    {
                        foreach (var sme2 in smec.value)
                        {
                            if (sme2.submodelElement.TimeStamp != smec.TimeStamp)
                            {
                                deeper = true;
                                break;
                            }
                        }
                    }

                    if (deeper)
                    {
                        foreach (var sme2 in smec.value)
                        {
                            GetEventMsgRecurseDiff(
                                plStruct,
                                plUpdate,
                                sme2.submodelElement,
                                minimumDate,
                                doCreateDelete,
                                observablePath);
                        }

                        return;
                    }

                    // prepare p2 to be relative path to observable
                    var p2 = sme.GetReference()?.Keys;
                    if (true == p2?.StartsWith(observablePath, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                    {
                        p2.RemoveRange(0, observablePath.Count);
                    }

                    if (doCreateDelete)
                    {
                        if (sme.TimeStampCreate > minimumDate)
                        {
                            if (plStruct != null)
                            {
                                plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                    count: 1,
                                    timeStamp: sme.TimeStamp,
                                    AasPayloadStructuralChangeItem.ChangeReason.Create,
                                    path: p2,
                                    // Assumption: models will be serialized correctly
                                    data: JsonConvert.SerializeObject(sme)));
                            }
                        }
                    }
                    else if (sme.TimeStamp != sme.TimeStampCreate)
                    {
                        if (plUpdate != null)
                        {
                            plUpdate.Values.Add(new AasPayloadUpdateValueItem(
                               path: p2,
                               sme.ValueAsText()));
                        }
                    }
                }
            }
        }
    }
}
