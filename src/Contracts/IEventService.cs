namespace Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.Events;

public interface IEventService
{
    public void PublishMqttMessage(EventDto eventDto);

    public Events.EventPayload CollectPayload(Dictionary<string, string> securityCondition, string changes, int depth, SubmodelElementCollection statusData,
        ReferenceElement reference, IReferable referable, AasCore.Aas3_0.Property conditionSM, AasCore.Aas3_0.Property conditionSME,
        string diff, List<String> diffEntry, bool withPayload, int limitSm, int limitSme, int offsetSm, int offsetSme);

    public int ChangeData(string json, EventDto eventData, AdminShellPackageEnv[] env, IReferable referable, out string transmit, out string lastDiffValue, out string statusValue, List<String> diffEntry, int packageIndex = -1);

    public Operation FindEvent(ISubmodel submodel, string eventPath);

    public EventDto ParseData(Operation op, AdminShellPackageEnv env);
}
