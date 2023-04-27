
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

/*
Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
Copyright (c) 2018-2020 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
*/

namespace AasxServer
{
    public class i40LanguageAutomaton
    {
        public SubmodelElementCollection automatonControl;

        public string name = "";
        public string getStatus = "stop";
        public string getActualStates = "";
        public string getTransitionsEnabled = "";
        public string setMode = "";
        public string getErrors = "";
        public List<string> getMessages = new List<string>();
        public string setForcedStates = "";
        public string setSingleStep = "";
        public string setSteppingTime = "3";

        public string[] frameNames;
        public Dictionary<string, string>[] frameContent;

        public Dictionary<string, string> constants;

        public Dictionary<string, string> variablesStrings;
        public Dictionary<string, SubmodelElementCollection> variablesCollections;

        public HashSet<string> states = new HashSet<string>();
        public HashSet<string> initialStates = new HashSet<string>();
        public HashSet<string> actualStates = new HashSet<string>();

        public List<SubmodelElementCollection> transitions;

        public int tick = 0;
        public int maxTick = 2;

        public i40LanguageAutomaton()
        {

        }
    }

    public static class i40LanguageRuntime
    {
        public static List<i40LanguageAutomaton> automatons = new List<i40LanguageAutomaton> { };
        public static Thread i40LanguageThread;
        public static ThreadStart threadDelegate;
        static public void initialize()
        {
            int aascount = Program.env.Length;

            for (int envi = 0; envi < aascount; envi++)
            {
                if (Program.env[envi] != null)
                {
                    foreach (var sm in Program.env[envi].AasEnv.Submodels)
                    {
                        if (sm != null && sm.IdShort != null)
                        {
                            bool withI40Language = false;
                            int count = sm.Qualifiers.Count;
                            if (count != 0)
                            {
                                int j = 0;

                                while (j < count) // Scan qualifiers
                                {
                                    var p = sm.Qualifiers[j] as Qualifier;

                                    if (p.Type == "i40language")
                                    {
                                        withI40Language = true;
                                    }
                                    j++;
                                }
                            }
                            if (withI40Language)
                            {
                                var auto = new i40LanguageAutomaton();
                                automatons.Add(auto);
                                auto.name = sm.IdShort;
                                if (auto.name == "automatonServiceRequester")
                                    isRequester = true;
                                if (auto.name == "automatonServiceProvider")
                                    isProvider = true;

                                foreach (var smw1 in sm.SubmodelElements)
                                {
                                    var sme1 = smw1;

                                    if (sme1 is SubmodelElementCollection && sme1.IdShort == "automatonControl")
                                    {
                                        var smc1 = sme1 as SubmodelElementCollection;
                                        auto.automatonControl = smc1;

                                        foreach (var smw2 in smc1.Value)
                                        {
                                            var sme2 = smw2;
                                            if (sme2 is Property && sme2.IdShort == "setSteppingTime")
                                            {
                                                string value = (sme2 as Property).Value;
                                                Regex regex = new Regex(@"^\d+$");
                                                if (regex.IsMatch(value))
                                                {
                                                    auto.setSteppingTime = value;
                                                }
                                                else
                                                {
                                                    (sme2 as Property).Value = auto.setSteppingTime;
                                                }
                                            }
                                        }
                                    }

                                    if (sme1 is SubmodelElementCollection && sme1.IdShort == "frames")
                                    {
                                        var smc1 = sme1 as SubmodelElementCollection;
                                        auto.frameNames = new string[smc1.Value.Count];
                                        auto.frameContent = new Dictionary<string, string>[smc1.Value.Count];

                                        int i = 0;
                                        foreach (var smw2 in smc1.Value)
                                        {
                                            var sme2 = smw2;
                                            if (sme2 is SubmodelElementCollection)
                                            {
                                                var smc2 = sme2 as SubmodelElementCollection;
                                                auto.frameNames[i] = smc2.IdShort;
                                                auto.frameContent[i] = new Dictionary<string, string>();

                                                foreach (var smw3 in smc2.Value)
                                                {
                                                    var sme3 = smw3;
                                                    if (sme3 is Property)
                                                    {
                                                        auto.frameContent[i].Add(sme3.IdShort, (sme3 as Property).Value);
                                                    }
                                                }
                                            }
                                            i++;
                                        }
                                    }

                                    if (sme1 is SubmodelElementCollection && sme1.IdShort == "constants")
                                    {
                                        var smc1 = sme1 as SubmodelElementCollection;
                                        auto.constants = new Dictionary<string, string>();

                                        foreach (var smw2 in smc1.Value)
                                        {
                                            var sme2 = smw2;
                                            if (sme2 is Property)
                                            {
                                                auto.constants.Add(sme2.IdShort, (sme2 as Property).Value);
                                            }
                                        }
                                    }

                                    if (sme1 is SubmodelElementCollection && sme1.IdShort == "variables")
                                    {
                                        var smc1 = sme1 as SubmodelElementCollection;
                                        auto.variablesStrings = new Dictionary<string, string>();
                                        auto.variablesCollections = new Dictionary<string, SubmodelElementCollection>();

                                        foreach (var smw2 in smc1.Value)
                                        {
                                            var sme2 = smw2;
                                            if (sme2 is Property)
                                            {
                                                auto.variablesStrings.Add(sme2.IdShort, (sme2 as Property).Value);
                                            }
                                            if (sme2 is SubmodelElementCollection)
                                            {
                                                auto.variablesCollections.Add(sme2.IdShort, (sme2 as SubmodelElementCollection));
                                            }
                                        }
                                    }

                                    if (sme1 is SubmodelElementCollection && sme1.IdShort == "states")
                                    {
                                        var smc1 = sme1 as SubmodelElementCollection;

                                        foreach (var smw2 in smc1.Value)
                                        {
                                            var sme2 = smw2;
                                            if (sme2 is Property || (sme2 is SubmodelElementCollection && sme2.IdShort != "initialStates"))
                                            {
                                                auto.states.Add(sme2.IdShort);
                                            }
                                            if (sme2 is SubmodelElementCollection && sme2.IdShort == "initialStates")
                                            {
                                                var smc2 = sme2 as SubmodelElementCollection;

                                                foreach (var smw3 in smc2.Value)
                                                {
                                                    var sme3 = smw3;
                                                    if (sme3 is Property || (sme3 is SubmodelElementCollection && sme3.IdShort != "initialStates"))
                                                    {
                                                        auto.initialStates.Add(sme3.IdShort);
                                                        auto.states.Add(sme3.IdShort);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (sme1 is SubmodelElementCollection && sme1.IdShort == "transitions")
                                    {
                                        var smc1 = sme1 as SubmodelElementCollection;
                                        auto.transitions = new List<SubmodelElementCollection>();

                                        foreach (var smw2 in smc1.Value)
                                        {
                                            var sme2 = smw2;
                                            if (sme2 is SubmodelElementCollection)
                                            {
                                                auto.transitions.Add(sme2 as SubmodelElementCollection);
                                            }
                                        }
                                    }
                                }

                                auto.actualStates = auto.initialStates;
                                auto.getStatus = "run";
                                auto.maxTick = Convert.ToInt32(auto.setSteppingTime);
                            }
                        }
                    }
                }
            }

            if (automatons.Count != 0)
            {
                threadDelegate = new ThreadStart(nextTick);
                i40LanguageThread = new Thread(threadDelegate);
                // MICHA
                // i40LanguageThread.Start();
            }
        }

        static bool treeChanged = false;

        //        public static string debugAutomaton = "";
        // public static string debugAutomaton = "automatonServiceRequester";
        public static string debugAutomaton = "automatonServiceProvider";
        public static void nextTick()
        {
            while (true)
            {
                foreach (var auto in automatons)
                {
                    // if (auto.name != "automatonServiceRequester")
                    // if (auto.name != "automatonServiceProvider")
                    //   continue;

                    if (auto.name == debugAutomaton)
                    {
                    }

                    // get actual automaton data from AAS
                    foreach (var smw1 in auto.automatonControl.Value)
                    {
                        var sme1 = smw1;
                        if (sme1 is Property && sme1.IdShort == "setMode")
                        {
                            auto.setMode = (sme1 as Property).Value;
                        }
                        if (sme1 is Property && sme1.IdShort == "stopAtStates")
                        {
                            string stopAtStates = (sme1 as Property).Value;
                            string[] states = stopAtStates.Split(' ');
                            foreach (var s in states)
                                if (auto.actualStates.Contains(s))
                                    auto.setMode = "stop";
                        }
                        if (sme1 is Property && sme1.IdShort == "setForcedStates")
                        {
                            auto.setForcedStates = (sme1 as Property).Value;
                            (sme1 as Property).Value = "";
                        }
                        if (sme1 is Property && sme1.IdShort == "setSingleStep")
                        {
                            auto.setSingleStep = (sme1 as Property).Value;
                            (sme1 as Property).Value = "";
                        }
                        if (sme1 is Property && sme1.IdShort == "setSteppingTime")
                        {
                            string value = (sme1 as Property).Value;
                            Regex regex = new Regex(@"^\d+$");
                            if (regex.IsMatch(value))
                            {
                                auto.setSteppingTime = value;
                                auto.maxTick = Convert.ToInt32(auto.setSteppingTime);
                            }
                        }
                    }

                    if (auto.setMode == "stop")
                        continue;

                    if (auto.setMode == "step" && auto.setSingleStep != "step")
                        continue;

                    if (auto.setMode == "force")
                    {
                        if (auto.setForcedStates == "")
                            continue;

                        auto.actualStates.Clear();
                        string[] states = auto.setForcedStates.Split(' ');
                        foreach (var s in states)
                            auto.actualStates.Add(s);
                    }

                    if (auto.tick >= auto.maxTick)
                    {
                        if (auto.name == debugAutomaton)
                        {
                        }

                        auto.tick = 0;

                        // Console.WriteLine(auto.name + ":");
                        string states = "";
                        foreach (var s in auto.actualStates)
                        {
                            states += s + " ";
                        }
                        // Console.WriteLine("states = " + states);

                        List<string> transitionsEnabled = new List<string>();
                        List<string> transitionsActive = new List<string>();
                        List<string> fromStates = new List<string>();
                        List<string> toStates = new List<string>();

                        // collect enabled transitons
                        foreach (var t in auto.transitions)
                        {
                            foreach (var smw1 in t.Value)
                            {
                                var sme1 = smw1;
                                if (sme1 is SubmodelElementCollection && (sme1.IdShort == "from" || sme1.IdShort == "From"))
                                {
                                    var smc1 = sme1 as SubmodelElementCollection;

                                    foreach (var smw2 in smc1.Value)
                                    {
                                        var sme2 = smw2;

                                        fromStates.Add(sme2.IdShort);
                                    }
                                }
                            }

                            bool allFromStatesActive = true;
                            foreach (var from in fromStates)
                            {
                                if (!auto.actualStates.Contains(from))
                                {
                                    allFromStatesActive = false;
                                    break;
                                }
                            }
                            if (allFromStatesActive)
                            {
                                transitionsEnabled.Add(t.IdShort);
                            }
                            fromStates.Clear();
                        }

                        string enabled = "";
                        foreach (var te in transitionsEnabled)
                        {
                            enabled += te + " ";
                        }
                        // Console.WriteLine("Transition enabled: " + enabled);

                        // Check which enabled transitions are active by their inputs
                        foreach (var t in auto.transitions)
                        {
                            if (transitionsEnabled.Contains(t.IdShort))
                            {
                                bool includesInput = false;
                                foreach (var smw1 in t.Value)
                                {
                                    var sme1 = smw1;
                                    if (sme1 is SubmodelElementCollection && sme1.IdShort == "input")
                                    {
                                        includesInput = true;
                                        var smc1 = sme1 as SubmodelElementCollection;

                                        foreach (var smw2 in smc1.Value)
                                        {
                                            var sme2 = smw2;

                                            if (sme2 is Operation)
                                            {
                                                if (auto.name == debugAutomaton)
                                                {
                                                }

                                                var op = sme2 as Operation;
                                                // Console.WriteLine("Operation: " + op.IdShort);
                                                bool opResult = false;
                                                switch (op.IdShort)
                                                {
                                                    case "wait":
                                                        opResult = operation_wait(op, auto);
                                                        if (opResult)
                                                            transitionsActive.Add(t.IdShort);
                                                        break;
                                                    case "message":
                                                        opResult = operation_message(op, auto);
                                                        break;
                                                    case "checkCollection":
                                                        opResult = operation_checkCollection(op, auto);
                                                        if (opResult)
                                                            transitionsActive.Add(t.IdShort);
                                                        break;
                                                    case "check":
                                                        opResult = operation_check(op, auto);
                                                        if (opResult)
                                                            transitionsActive.Add(t.IdShort);
                                                        break;
                                                    case "receiveProposals":
                                                        opResult = operation_receiveProposals(op, auto);
                                                        if (opResult)
                                                            transitionsActive.Add(t.IdShort);
                                                        break;
                                                    case "receiveSRAnswer":
                                                        opResult = operation_receiveSRAnswer(op, auto);
                                                        break;
                                                    case "receiveI40message":
                                                        opResult = operation_receiveI40message(op, auto);
                                                        break;
                                                    case "executeLogic":
                                                        opResult = operation_executeLogic(op, auto);
                                                        break;
                                                    case "sendI40frame":
                                                        opResult = operation_sendI40frame(op, auto);
                                                        break;
                                                    case "receiveI40frame":
                                                        opResult = operation_receiveI40frame(op, auto);
                                                        break;
                                                    case "calculate":
                                                        opResult = operation_calculate(op, auto);
                                                        break;
                                                    default:
                                                        transitionsActive.Add(t.IdShort);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (!includesInput)
                                    transitionsActive.Add(t.IdShort);
                            }
                        }

                        // collect fromStates and toStates from enabled transitions
                        foreach (var t in auto.transitions)
                        {
                            if (transitionsActive.Contains(t.IdShort))
                            {
                                // execute output operations
                                foreach (var smw1 in t.Value)
                                {
                                    var sme1 = smw1;
                                    if (sme1 is SubmodelElementCollection && sme1.IdShort == "output")
                                    {
                                        var smc1 = sme1 as SubmodelElementCollection;

                                        foreach (var smw2 in smc1.Value)
                                        {
                                            var sme2 = smw2;

                                            if (sme2 is Operation)
                                            {
                                                if (auto.name == debugAutomaton)
                                                {
                                                }

                                                var op = sme2 as Operation;
                                                // Console.WriteLine("Operation: " + op.IdShort);
                                                bool opResult = false;
                                                switch (op.IdShort)
                                                {
                                                    case "message":
                                                        opResult = operation_message(op, auto);
                                                        break;
                                                    case "clearMessages":
                                                        opResult = operation_clearMessages(op, auto);
                                                        break;
                                                    case "clear":
                                                        opResult = operation_clear(op, auto);
                                                        break;
                                                    case "sendFrame":
                                                        opResult = operation_sendFrame(op, auto);
                                                        break;
                                                    case "sendI40message":
                                                        opResult = operation_sendI40message(op, auto);
                                                        break;
                                                    case "processRequesterResponse":
                                                        opResult = operation_processRequesterResponse(op, auto);
                                                        break;
                                                    case "executeLogic":
                                                        opResult = operation_executeLogic(op, auto);
                                                        break;
                                                    case "sendI40frame":
                                                        opResult = operation_sendI40frame(op, auto);
                                                        break;
                                                    case "receiveI40frame":
                                                        opResult = operation_receiveI40frame(op, auto);
                                                        break;
                                                    case "calculate":
                                                        opResult = operation_calculate(op, auto);
                                                        break;
                                                }
                                            }
                                        }
                                    }

                                    // changes states
                                    foreach (var smw21 in t.Value)
                                    {
                                        sme1 = smw21;
                                        if (sme1 is SubmodelElementCollection && (sme1.IdShort == "from" || sme1.IdShort == "From"))
                                        {
                                            var smc1 = sme1 as SubmodelElementCollection;

                                            foreach (var smw2 in smc1.Value)
                                            {
                                                var sme2 = smw2;

                                                if (!fromStates.Contains(sme2.IdShort))
                                                    fromStates.Add(sme2.IdShort);
                                            }
                                        }
                                        if (sme1 is SubmodelElementCollection && (sme1.IdShort == "to" || sme1.IdShort == "To"))
                                        {
                                            var smc1 = sme1 as SubmodelElementCollection;

                                            foreach (var smw2 in smc1.Value)
                                            {
                                                var sme2 = smw2;

                                                if (!toStates.Contains(sme2.IdShort))
                                                    toStates.Add(sme2.IdShort);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // remove fromStates from activeStates and add toStates to activeStates
                        foreach (var from in fromStates)
                        {
                            auto.actualStates.Remove(from);
                        }
                        foreach (var to in toStates)
                        {
                            if (!fromStates.Contains(to))
                                if (!auto.actualStates.Contains(to))
                                    auto.actualStates.Add(to);
                        }
                        // self loops are only allowed if no other states are active

                        // Console.WriteLine();

                        // display actual automaton data in AAS
                        foreach (var smw1 in auto.automatonControl.Value)
                        {
                            var sme1 = smw1;
                            if (sme1 is Property && sme1.IdShort == "getActualTime")
                            {
                                (sme1 as Property).Value = DateTime.UtcNow.ToString();
                            }
                            if (sme1 is Property && sme1.IdShort == "getStatus")
                            {
                                (sme1 as Property).Value = auto.getStatus;
                            }
                            if (sme1 is Property && sme1.IdShort == "getActualStates")
                            {
                                (sme1 as Property).Value = "";
                                foreach (var s in auto.actualStates)
                                {
                                    (sme1 as Property).Value += s + " ";
                                }
                            }
                            if (sme1 is Property && sme1.IdShort == "getTransitionsEnabled")
                            {
                                (sme1 as Property).Value = "";
                                foreach (var t in transitionsEnabled)
                                {
                                    (sme1 as Property).Value += t + " ";
                                }
                            }
                            if (sme1 is Property && sme1.IdShort == "getErrors")
                            {
                                (sme1 as Property).Value = auto.getErrors;
                            }
                            if (sme1 is Property && sme1.IdShort == "getMessages")
                            {
                                string value = "";
                                foreach (var text in auto.getMessages)
                                {
                                    if (value != "")
                                        value += ", ";
                                    value += text;
                                }
                                (sme1 as Property).Value = value;
                            }
                            /*
                            if (sme1 is Property && sme1.IdShort == "setMode")
                            {
                                (sme1 as Property).Value = "";
                            }
                            if (sme1 is Property && sme1.IdShort == "setForcedStates")
                            {
                                (sme1 as Property).Value = "";
                            }
                            if (sme1 is Property && sme1.IdShort == "setSingleStep")
                            {
                                (sme1 as Property).Value = "";
                            }
                            if (sme1 is Property && sme1.IdShort == "setSteppingTime")
                            {
                                string value = (sme1 as Property).Value;
                                Regex regex = new Regex(@"^\d+$");
                                if (regex.IsMatch(value))
                                {
                                    auto.setSteppingTime = value;
                                    auto.maxTick = Convert.ToInt32(auto.setSteppingTime);
                                }
                            }
                            */
                        }
                    }
                    auto.tick++;
                }

                int mode = 0;
                if (treeChanged)
                    mode = 2;
                Program.signalNewData(mode);
                Thread.Sleep(1000);
            }
        }

        public static bool operation_message(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property value = text
            foreach (var v in op.InputVariables)
            {
                if (v.Value is Property)
                {
                    var p = v.Value as Property;
                    if (auto.getMessages.Count < 100)
                        auto.getMessages.Add(p.Value);
                    if (auto.getMessages.Count == 100)
                        auto.getMessages.Add("+++");
                    // Console.WriteLine("operation message: " + p.IdShort + " = " + p.Value);
                }
            }
            return true;
        }

        public static bool operation_clearMessages(Operation op, i40LanguageAutomaton auto)
        {
            auto.getMessages.Clear();
            return true;
        }

        public static bool operation_wait(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable reference waitingTime: property value
            // outputVariable reference endTime: property value

            if (op.InputVariables.Count != 1 && op.OutputVariables.Count != 1)
            {
                return false;
            }

            var in1 = op.InputVariables.First();
            var r1 = in1.Value;
            if (!(r1 is ReferenceElement))
                return false;
            var ref1 = Program.env[0].AasEnv.FindReferableByReference((r1 as ReferenceElement).GetModelReference());
            if (ref1 == null)
                auto.getErrors += r1.IdShort + " not found! ";
            if (!(ref1 is Property))
                return false;
            var p1 = ref1 as Property;
            int waitingTime = Convert.ToInt32(p1.Value);

            var out1 = op.OutputVariables.First();
            var r2 = out1.Value;
            if (!(r2 is ReferenceElement))
                return false;
            var ref2 = Program.env[0].AasEnv.FindReferableByReference((r2 as ReferenceElement).GetModelReference());
            if (ref2 == null)
                auto.getErrors += r2.IdShort + " not found! ";
            if (!(ref2 is Property))
                return false;
            var p2 = ref2 as Property;

            DateTime localTime = DateTime.UtcNow;
            if (p2.Value == "") // start
            {
                var endTime = localTime.AddSeconds(waitingTime);
                p2.Value = endTime.ToString();
                // Console.WriteLine("endTime = " + p2.Value);
            }
            else // test if time has elapsed
            {
                // Console.WriteLine("localTime = " + localTime);
                var endTime = DateTime.Parse(p2.Value);
                if (DateTime.Compare(localTime, endTime) > 0)
                {
                    p2.Value = "";
                    return true;
                }
            }

            return false;
        }

        public static bool operation_receiveProposals(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property protocol: memory, connect
            // inputVariable reference frame proposal: collection
            // inputVariable reference submodel
            // outputVariable reference collected proposals: collection
            // outputVariable reference collected not understood proposals: collection
            // outputVariable reference collected refused proposals: collection
            // outputVariable reference property receivedFrameJSON

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count != 3 && op.OutputVariables.Count != 4)
            {
                return false;
            }

            Submodel refSubmodel = null;
            Property protocol = null;
            Property receivedFrameJSON = null;

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (inputRef is Property)
                {
                    protocol = (inputRef as Property);
                    continue;
                }
                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is Submodel)
                    refSubmodel = refElement as Submodel;
            }
            foreach (var output in op.OutputVariables)
            {
                var outputRef = output.Value;
                if (!(outputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += outputRef.IdShort + " not found! ";
                if (refElement is Property)
                    receivedFrameJSON = refElement as Property;
            }

            var out1 = op.OutputVariables.First();
            var r2 = out1.Value;
            if (!(r2 is ReferenceElement))
                return false;
            var ref2 = Program.env[0].AasEnv.FindReferableByReference((r2 as ReferenceElement).GetModelReference());
            if (ref2 == null)
                auto.getErrors += r2.IdShort + " not found! ";
            if (!(ref2 is SubmodelElementCollection))
                return false;
            var smc2 = ref2 as SubmodelElementCollection;

            if (protocol.Value != "memory" && protocol.Value != "connect")
                return false;
            while ((auto.name == "automatonServiceRequester" && receivedFrameJSONRequester.Count != 0)
                    || (auto.name == "automatonServiceProvider" && receivedFrameJSONProvider.Count != 0))
            {
                string receivedFrame = "";
                if (auto.name == "automatonServiceRequester")
                {
                    // receivedFrame = sendFrameJSONProvider;
                    // sendFrameJSONProvider = "";
                    if (receivedFrameJSONRequester.Count != 0)
                    {
                        receivedFrame = receivedFrameJSONRequester[0];
                        receivedFrameJSONRequester.RemoveAt(0);
                    }
                }

                if (auto.name == "automatonServiceProvider")
                {
                    // receivedFrame = sendFrameJSONRequester;
                    // sendFrameJSONRequester = "";
                    if (receivedFrameJSONProvider.Count != 0)
                    {
                        receivedFrame = receivedFrameJSONProvider[0];
                        receivedFrameJSONProvider.RemoveAt(0);
                    }
                }

                receivedFrameJSON.Value = receivedFrame;

                ISubmodel submodel = null;

                if (receivedFrame != "")
                {
                    try
                    {
                        if (auto.name == debugAutomaton)
                        {
                        }

                        I40Message_Interaction newBiddingMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<I40Message_Interaction>(
                            receivedFrame, new AdminShellConverters.JsonAasxConverter("modelType", "name"));

                        submodel = newBiddingMessage.interactionElements[0];
                        Console.WriteLine("A new Call for Proposal is received");
                        if (submodel != null)
                        {
                            if (submodel.IdShort == "Boring")
                            {
                                boringSubmodel = submodel;
                                boringSubmodelFrame = newBiddingMessage.frame;
                            }
                            SubmodelElementCollection smcSubmodel = new SubmodelElementCollection();
                            smcSubmodel.IdShort = submodel.IdShort;
                            foreach (var sme in submodel.SubmodelElements)
                            {
                                smcSubmodel.Value.Add(sme);
                                treeChanged = true;
                            }
                            smc2.Value.Add(smcSubmodel);

                        }

                    }
                    catch
                    {
                    }
                }

            }

            return true;
        }

        public static bool operation_receiveI40message(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property protocol: i40connect
            // inputVariable property protocolServer: URL
            // alternative 1
            // inputVariable reference submodel
            // outputVariable reference collected proposals: collection
            // alternative 2
            // outputVariable reference messageType
            // alternatives end
            // outputVariable reference property receivedFrameJSON

            if (auto.name == debugAutomaton)
            {
            }

            if ((op.InputVariables.Count < 2 && op.InoutputVariables.Count > 3) && op.OutputVariables.Count != 2)
            {
                return false;
            }

            Property protocol = null;
            Property protocolServer = null;
            SubmodelElementCollection refProposals = null;
            Submodel refSubmodel = null;
            Property messageType = null;
            Property receivedFrameJSON = null;

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is Property p)
                {
                    if (p.IdShort == "protocol")
                        protocol = p;
                    if (p.IdShort == "protocolServer")
                        protocolServer = p;
                }
                if (refElement is Submodel s)
                {
                    refSubmodel = s;
                }
            }

            foreach (var output in op.OutputVariables)
            {
                var outputRef = output.Value;
                if (!(outputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += outputRef.IdShort + " not found! ";
                if (refElement is SubmodelElementCollection smc)
                    refProposals = smc;
                if (refElement is Property p)
                {
                    if (refProposals == null && messageType == null)
                        messageType = p;
                    else
                        receivedFrameJSON = p;
                }
            }

            if (protocol.Value != "memory" && protocol.Value != "i40connect")
                return false;

            while ((auto.name == "automatonServiceRequester" && receivedFrameJSONRequester.Count != 0)
                    || (auto.name == "automatonServiceProvider" && receivedFrameJSONProvider.Count != 0))
            {
                string receivedFrame = "";
                if (auto.name == "automatonServiceRequester")
                {
                    // receivedFrame = sendFrameJSONProvider;
                    // sendFrameJSONProvider = "";
                    if (receivedFrameJSONRequester.Count != 0)
                    {
                        receivedFrame = receivedFrameJSONRequester[0];
                        receivedFrameJSONRequester.RemoveAt(0);
                    }
                }

                if (auto.name == "automatonServiceProvider")
                {
                    // receivedFrame = sendFrameJSONRequester;
                    // sendFrameJSONRequester = "";
                    if (receivedFrameJSONProvider.Count != 0)
                    {
                        receivedFrame = receivedFrameJSONProvider[0];
                        receivedFrameJSONProvider.RemoveAt(0);
                    }
                }

                receivedFrameJSON.Value = receivedFrame;

                ISubmodel submodel = null;

                if (receivedFrame != "")
                {
                    try
                    {
                        if (auto.name == debugAutomaton)
                        {
                        }

                        I40Message_Interaction newBiddingMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<I40Message_Interaction>(
                            receivedFrame, new AdminShellConverters.JsonAasxConverter("modelType", "name"));

                        if (newBiddingMessage.interactionElements.Count != 0)
                        {
                            submodel = newBiddingMessage.interactionElements[0];
                            Console.WriteLine("A new Call for Proposal is received");
                            if (submodel != null)
                            {
                                if (submodel.IdShort == "Boring")
                                {
                                    boringSubmodel = submodel;
                                    boringSubmodelFrame = newBiddingMessage.frame;
                                }
                                SubmodelElementCollection smcSubmodel = new SubmodelElementCollection();
                                smcSubmodel.IdShort = submodel.IdShort;
                                foreach (var sme in submodel.SubmodelElements)
                                {
                                    smcSubmodel.Value.Add(sme);
                                    treeChanged = true;
                                }
                                refProposals.Value.Add(smcSubmodel);
                            }
                        }
                        else
                        {
                            if (messageType != null)
                                messageType.Value = newBiddingMessage.frame.type;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return true;
        }

        public static bool operation_receiveSRAnswer(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property protocol: memory, connect
            // inputVariable reference frame proposal: collection
            // inputVariable reference submodel
            // outputVariable reference collected proposals: collection
            // outputVariable reference collected not understood proposals: collection
            // outputVariable reference collected refused proposals: collection
            // outputVariable reference property receivedFrameJSON

            if (auto.name == debugAutomaton)
            {
            }

            Console.WriteLine("Waiting for Service Requester Answer");

            while (auto.name == "automatonServiceProvider" && receivedFrameJSONProvider.Count != 0)
            {
                string receivedFrame = "";

                if (auto.name == "automatonServiceProvider")
                {
                    // receivedFrame = sendFrameJSONRequester;
                    // sendFrameJSONRequester = "";
                    if (receivedFrameJSONProvider.Count != 0)
                    {
                        receivedFrame = receivedFrameJSONProvider[0];
                        receivedFrameJSONProvider.RemoveAt(0);
                    }
                }
                if (receivedFrame != "")
                {
                    try
                    {
                        if (auto.name == debugAutomaton)
                        {
                        }

                        I40Message_Interaction newBiddingMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<I40Message_Interaction>(
                            receivedFrame, new AdminShellConverters.JsonAasxConverter("modelType", "name"));

                        srAnswerMessageType = newBiddingMessage.frame.type;

                    }
                    catch
                    {
                    }
                }


            }

            return true;
        }


        public static ISubmodel boringSubmodel = null;
        public static I40TransmitFrame boringSubmodelFrame = null;

        public static bool isRequester = false;
        public static bool isProvider = false;
        public static string sendProtocolRequester = "";
        public static string sendProtocolProvider = "";
        public static string receiveProtocolRequester = "";
        public static string receiveProtocolProvider = "";
        public static List<string> sendFrameJSONRequester = new List<string>();
        public static List<string> receivedFrameJSONProvider = new List<string>();
        public static List<string> sendFrameJSONProvider = new List<string>();
        public static List<string> receivedFrameJSONRequester = new List<string>();
        public static string srAnswerMessageType = "";
        public static ISubmodel returnBoringSbmodel()
        {
            //IIdentifiable _boringSMID = new IIdentifiable();
            string _boringSMID = "www.company.com/ids/sm/3145_4121_8002_1792";
            //Submodel _boringSubmodel = new Submodel();
            ISubmodel _boringSubmodel = Program.env[0].AasEnv.FindSubmodelById(_boringSMID);
            //return Program.env[0].AasEnv.FindSubmodelById(_boringSMID);
            return _boringSubmodel;
        }
        public static bool operation_sendFrame(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property protocol: memory, connect
            // inputVariable reference frame proposal: collection
            // inputVariable reference submodel
            // outputVariable reference property sendFrameJSON

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count != 3 && op.OutputVariables.Count != 1)
            {
                return false;
            }

            Property protocol = null;
            SubmodelElementCollection refFrame = null;
            Submodel refSubmodel = null;
            Property sendFrameJSON = null;

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (inputRef is Property)
                {
                    protocol = (inputRef as Property);
                    continue;
                }
                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is SubmodelElementCollection)
                    refFrame = refElement as SubmodelElementCollection;
                if (refElement is Submodel)
                    refSubmodel = refElement as Submodel;
            }

            foreach (var output in op.OutputVariables)
            {
                var outputRef = output.Value;
                if (!(outputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += outputRef.IdShort + " not found! ";
                if (refElement is Property)
                    sendFrameJSON = refElement as Property;
            }

            if (protocol.Value != "memory" && protocol.Value != "connect")
                return false;

            if (boringSubmodel == null)
                return false;
            I40MessageHelper _i40MessageHelper = new I40MessageHelper();
            I40Message_Interaction newBiddingMessage = _i40MessageHelper.createBiddingMessage(Program.connectNodeName,
                boringSubmodelFrame.sender.identification.id,
                boringSubmodelFrame.sender.role.name, "BoringProvider", "proposal",
                "RESTAPI", boringSubmodelFrame.replyBy, boringSubmodelFrame.conversationId, Program.count);

            Program.count = Program.count + 1;

            //Submodel _boringSubmodel = new Submodel();
            ISubmodel _boringSubmodel = returnBoringSbmodel();
            newBiddingMessage.interactionElements.Add(_boringSubmodel);

            string frame = JsonConvert.SerializeObject(newBiddingMessage, Newtonsoft.Json.Formatting.Indented);
            sendFrameJSON.Value = frame;

            boringSubmodel = null;

            // Console.WriteLine(frame);

            if (auto.name == "automatonServiceRequester")
            {
                switch (protocol.Value)
                {
                    case "memory":
                        receivedFrameJSONProvider.Add(frame);
                        break;
                    case "connect":
                        sendFrameJSONRequester.Add(frame);
                        break;
                }
            }
            if (auto.name == "automatonServiceProvider")
            {
                switch (protocol.Value)
                {
                    case "memory":
                        receivedFrameJSONRequester.Add(frame);
                        break;
                    case "connect":
                        sendFrameJSONProvider.Add(frame);
                        break;
                }
            }

            return true;
        }

        public static bool operation_sendI40message(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property protocol: i40connect
            // inputVariable property protocolServer: URL
            // alternative 1
            // inputVariable reference submodel
            // alternative 2
            // outputVariable reference messageType
            // alternatives end
            // outputVariable reference property sendFrameJSON

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count != 3 && op.OutputVariables.Count != 1)
            {
                return false;
            }

            Property protocol = null;
            Property protocolServer = null;
            Submodel refSubmodel = null;
            Property messageType = null;
            Property sendFrameJSON = null;

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (inputRef is Property p1)
                {
                    if (p1.IdShort == "messageType")
                        messageType = p1;
                    continue;
                }
                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is Property p)
                {
                    if (p.IdShort == "protocol")
                        protocol = p;
                    if (p.IdShort == "protocolServer")
                        protocolServer = p;
                }
                if (refElement is Submodel s)
                {
                    refSubmodel = s;
                }
            }

            foreach (var output in op.OutputVariables)
            {
                var outputRef = output.Value;
                if (!(outputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += outputRef.IdShort + " not found! ";
                if (refElement is Property p)
                {
                    sendFrameJSON = refElement as Property;
                }
            }

            if (protocol.Value != "memory" && protocol.Value != "i40connect")
                return false;

            string frame = "";
            if (refSubmodel != null)
            {
                if (boringSubmodel == null)
                    return false;
                I40MessageHelper _i40MessageHelper = new I40MessageHelper();
                I40Message_Interaction newBiddingMessage = _i40MessageHelper.createBiddingMessage(Program.connectNodeName,
                    boringSubmodelFrame.sender.identification.id,
                    boringSubmodelFrame.sender.role.name, "BoringProvider", "proposal",
                    "RESTAPI", boringSubmodelFrame.replyBy, boringSubmodelFrame.conversationId, Program.count);

                Program.count = Program.count + 1;

                //Submodel _boringSubmodel = new Submodel();
                ISubmodel _boringSubmodel = returnBoringSbmodel();
                newBiddingMessage.interactionElements.Add(_boringSubmodel);

                frame = JsonConvert.SerializeObject(newBiddingMessage, Newtonsoft.Json.Formatting.Indented);
                sendFrameJSON.Value = frame;

                boringSubmodel = null;
            }
            else
            {
                if (messageType.Value == "informConfirm")
                {
                    Console.WriteLine("The Service requester has sent the accept proposal");
                    I40MessageHelper _i40MessageHelper = new I40MessageHelper();
                    I40Message_Interaction newBiddingMessage = _i40MessageHelper.createBiddingMessage(Program.connectNodeName,
                        boringSubmodelFrame.sender.identification.id,
                        boringSubmodelFrame.sender.role.name, "BoringProvider", "informConfirm",
                        "RESTAPI", boringSubmodelFrame.replyBy, boringSubmodelFrame.conversationId, Program.count);

                    Program.count = Program.count + 1;

                    frame = JsonConvert.SerializeObject(newBiddingMessage, Newtonsoft.Json.Formatting.Indented);
                    sendFrameJSONProvider.Add(frame);
                    Console.WriteLine("The informConfirm is sent to the service Requesters");
                }
            }

            // Console.WriteLine(frame);
            if (frame != "")
            {
                if (auto.name == "automatonServiceRequester")
                {
                    switch (protocol.Value)
                    {
                        case "memory":
                            receivedFrameJSONProvider.Add(frame);
                            break;
                        case "i40connect":
                            sendFrameJSONRequester.Add(frame);
                            break;
                    }
                }
                if (auto.name == "automatonServiceProvider")
                {
                    switch (protocol.Value)
                    {
                        case "memory":
                            receivedFrameJSONRequester.Add(frame);
                            break;
                        case "i40connect":
                            sendFrameJSONProvider.Add(frame);
                            break;
                    }
                }
            }

            return true;
        }

        public static bool operation_processRequesterResponse(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property protocol: memory, connect
            // inputVariable reference frame proposal: collection
            // inputVariable reference submodel
            // outputVariable reference property sendFrameJSON

            if (auto.name == debugAutomaton)
            {
            }
            if (srAnswerMessageType == "acceptProposal")
            {
                Console.WriteLine("The Service requester has sent the accept proposal");
                I40MessageHelper _i40MessageHelper = new I40MessageHelper();
                I40Message_Interaction newBiddingMessage = _i40MessageHelper.createBiddingMessage(Program.connectNodeName,
                    boringSubmodelFrame.sender.identification.id,
                    boringSubmodelFrame.sender.role.name, "BoringProvider", "informConfirm",
                    "RESTAPI", boringSubmodelFrame.replyBy, boringSubmodelFrame.conversationId, Program.count);

                Program.count = Program.count + 1;

                string frame = JsonConvert.SerializeObject(newBiddingMessage, Newtonsoft.Json.Formatting.Indented);
                sendFrameJSONProvider.Add(frame);
                Console.WriteLine("The informConfirm is sent to the service Requesters");

            }
            else if (srAnswerMessageType == "rejectProposal")
            {
                Console.WriteLine("The Service requester has sent the reject proposal");
            }
            return true;
        }

        public static bool operation_clear(Operation op, i40LanguageAutomaton auto)
        {
            // outputVariables are references to collections
            // alle elements will be removed from collections

            if (auto.name == debugAutomaton)
            {
            }

            if (op.OutputVariables.Count == 0)
            {
                return false;
            }

            foreach (var output in op.OutputVariables)
            {
                var outputRef = output.Value;
                if (outputRef is ReferenceElement)
                {
                    var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as ReferenceElement).GetModelReference());
                    if (refElement == null)
                        auto.getErrors += outputRef.IdShort + " not found! ";
                    if (refElement is SubmodelElementCollection)
                    {
                        var refSMEC = refElement as SubmodelElementCollection;
                        List<ISubmodelElement> list = new List<ISubmodelElement>();
                        foreach (var sme in refSMEC.Value)
                        {
                            list.Add(sme);
                        }
                        foreach (var sme2 in list)
                        {
                            refSMEC.Value.Remove(sme2);
                            treeChanged = true;
                        }
                    }
                    if (refElement is Property p)
                    {
                        p.Value = "";
                    }
                }
            }

            return true;
        }

        public static bool operation_checkCollection(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property checkType: isEmpty, isNotEmpty;
            // inputVariable reference collection proposal

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count != 2 && op.OutputVariables.Count != 0)
            {
                return false;
            }

            Property checkType = null;
            SubmodelElementCollection refCollection = null;

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (inputRef is Property)
                {
                    checkType = (inputRef as Property);
                    continue;
                }
                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is SubmodelElementCollection)
                    refCollection = refElement as SubmodelElementCollection;
            }

            int count = refCollection.Value.Count;

            switch (checkType.IdShort)
            {
                case "isEmpty":
                    return (count == 0);
                case "isNotEmpty":
                    return (count != 0);
            }

            return false;
        }

        public static bool operation_check(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property checkType: isEmpty, isNotEmpty;
            // inputVariable reference collection proposal

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count != 3 && op.OutputVariables.Count != 0)
            {
                return false;
            }

            Property checkType = null;
            Property argument1 = null;
            Property argument2 = null;
            SubmodelElementCollection refCollection = null;
            Property refProperty = null;
            int count = 0;
            string value = "";

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (inputRef is Property)
                {
                    if (count == 0)
                        checkType = (inputRef as Property);
                    if (count == 1)
                        argument1 = (inputRef as Property);
                    if (count == 2)
                        argument2 = (inputRef as Property);
                    count++;
                    continue;
                }
                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is Property)
                {
                    refProperty = refElement as Property;
                    if (count == 1)
                        argument1 = (refProperty as Property);
                    if (count == 2)
                        argument2 = (refProperty as Property);
                }
                if (refElement is SubmodelElementCollection)
                {
                    refCollection = refElement as SubmodelElementCollection;
                }
                count++;
            }

            if (checkType == null || argument1 == null || argument2 == null)
                return false;

            switch (checkType.IdShort)
            {
                case "isEmpty":
                    if (refCollection != null)
                        return (count == 0);
                    if (argument1 != null)
                        return (value == "");
                    return false;
                case "isNotEmpty":
                    if (refCollection != null)
                        return (count != 0);
                    if (argument1 != null)
                        return (value != "");
                    return false;
                case "isEqual":
                    return (argument1.Value == argument2.Value);
                case "isNotEqual":
                    return (argument1.Value != argument2.Value);
            }

            return false;
        }

        public static bool operation_executeLogic(Operation op, i40LanguageAutomaton auto)
        {
            // parameter sequence
            // inputVariable property i40logic type: 1
            // inputVariable reference property protocol type: 1
            // inputVariable reference collection frame(s): 1..2
            // inputVariable reference collection inputQueue: 1
            // inputVariable reference submodel sub: 0..1
            // inputVariable reference property message: 0..1
            // outputVariable reference collection outputQueue(s): 0..2

            // alternative 1
            // inputVariable property i40Logic = callForProposal
            // inputVariable reference property protocol
            // inputVariable reference collection frameProposal
            // inputVariable reference submodel proposal
            // outputVariable reference collection queueProposal

            // alternative 2
            // inputVariable property i40Logic = evaluateProposal
            // inputVariable reference property protocol
            // inputVariable reference collection frameAcceptProposal
            // inputVariable reference collection frameRejectProposal
            // inputVariable reference collection queueProposal
            // outputVariable reference collection queueAcceptProposal
            // outputVariable reference collection queueRejectProposal

            // alternative 3
            // inputVariable property i40Logic = evaluateInformConfirm
            // inputVariable reference property protocol
            // inputVariable reference collection frameInformConfirm
            // inputVariable reference collection queueInformConfirm

            // alternative 4
            // inputVariable property i40Logic = capabiltyCheck
            // inputVariable reference property protocol
            // inputVariable reference collection frameNotUnderstood
            // inputVariable reference collection queueProposal
            // inputVariable reference property proposalMessage
            // outputVariable reference collection queueNotUnderstood

            // alternative 5
            // inputVariable property i40Logic = feasibilityCheck
            // inputVariable reference property protocol
            // inputVariable reference collection frameRefuse
            // inputVariable reference property proposalMessage
            // outputVariable reference collection queueRefuseProposal

            // alternative 6
            // inputVariable property i40Logic = checkingSchedule
            // inputVariable reference property protocol
            // inputVariable reference collection frameRefuse
            // inputVariable reference property proposalMessage
            // outputVariable reference collection queueRefuseProposal

            // alternative 7
            // inputVariable property i40Logic = PriceCalculation
            // inputVariable reference property protocol
            // inputVariable reference collection frameProposal
            // inputVariable reference property proposalMessage
            // outputVariable reference collection queueProposal

            // alternative 8
            // inputVariable property i40Logic = WaitingForServiceRequesterAnswer
            // inputVariable reference property protocol
            // inputVariable reference collection frameAcceptProposal
            // inputVariable reference collection frameRejectProposal
            // inputVariable reference collection queueSRAnswer
            // outputVariable reference collection queueAcceptProposal
            // outputVariable reference collection queueRejectProposal

            // alternative 9
            // inputVariable property i40Logic = ServiceProvision
            // inputVariable reference property protocol
            // inputVariable reference collection frameInfomrConfirm
            // outputVariable reference collection queueInfomrConfirm

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count < 4 && op.OutputVariables.Count > 2)
            {
                return false;
            }

            // inputVariable property i40logic type: 1
            // inputVariable reference property protocol type: 1
            // inputVariable reference collection frame(s): 1..2
            // inputVariable reference collection inputQueue: 1
            // inputVariable reference submodel sub: 0..1
            // inputVariable reference property message: 0..1
            // outputVariable reference collection outputQueue(s): 0..2
            Property i40Logic = null;
            Property protocol = null;
            SubmodelElementCollection frame1 = null;
            SubmodelElementCollection frame2 = null;
            SubmodelElementCollection inQueue = null;
            Submodel sub = null;
            SubmodelElementCollection proposalMessage = null;
            SubmodelElementCollection outQueue1 = null;
            SubmodelElementCollection outQueue2 = null;

            SubmodelElementCollection refCollection = null;
            Property refProperty = null;

            string state = "i40logic";

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (inputRef is Property p)
                {
                    switch (p.IdShort)
                    {
                        case "i40Logic":
                            i40Logic = p;
                            state = "protocol";
                            break;
                    }
                    // Debug
                    switch (i40Logic?.Value)
                    {
                        case "callForProposal":
                            break;
                        case "evaluateProposal":
                            break;
                        case "evaluateInformConfirm":
                            break;
                        case "capabilityCheck":
                            break;
                        case "feasibilityCheck":
                            break;
                        case "checkingSchedule":
                            break;
                        case "PriceCalculation":
                            break;
                        case "ServiceProvision":
                            break;
                        case "WaitingForServiceRequesterAnswer":
                            break;
                    }
                    continue;
                }

                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is Property)
                {
                    refProperty = refElement as Property;
                    switch (refProperty.IdShort)
                    {
                        case "protocol":
                            protocol = refProperty;
                            state = "frame1";
                            break;
                    }
                    continue;
                }
                if (refElement is Submodel)
                {
                    if (state == "submodel")
                    {
                        sub = refElement as Submodel;
                        state = "outQueue1";
                    }
                    continue;
                }
                if (refElement is SubmodelElementCollection)
                {
                    refCollection = refElement as SubmodelElementCollection;
                    if (refCollection.IdShort == "proposalMessage")
                    {
                        proposalMessage = refCollection;
                        state = "outQueue1";
                        continue;
                    }

                    switch (i40Logic?.Value)
                    {
                        case "callForProposal":
                            switch (state)
                            {
                                case "frame1":
                                    frame1 = refCollection;
                                    state = "submodel";
                                    break;
                                case "outQueue1":
                                    outQueue1 = refCollection;
                                    state = "";
                                    break;
                            }
                            break;
                        case "evaluateProposal":
                        case "WaitingForServiceRequesterAnswer":
                            switch (state)
                            {
                                case "frame1":
                                    frame1 = refCollection;
                                    state = "frame2";
                                    break;
                                case "frame2":
                                    frame2 = refCollection;
                                    state = "inQueue";
                                    break;
                                case "inQueue":
                                    inQueue = refCollection;
                                    state = "outQueue1";
                                    break;
                            }
                            break;
                        case "evaluateInformConfirm":
                            switch (state)
                            {
                                case "frame1":
                                    frame1 = refCollection;
                                    state = "inQueue";
                                    break;
                                case "inQueue":
                                    inQueue = refCollection;
                                    state = "";
                                    break;
                            }
                            break;
                        case "capabilityCheck":
                            switch (state)
                            {
                                case "frame1":
                                    frame1 = refCollection;
                                    state = "inQueue";
                                    break;
                                case "inQueue":
                                    inQueue = refCollection;
                                    state = "";
                                    break;
                            }
                            break;
                        case "feasibilityCheck":
                        case "checkingSchedule":
                        case "PriceCalculation":
                            switch (state)
                            {
                                case "frame1":
                                    frame1 = refCollection;
                                    state = "message";
                                    break;
                            }
                            break;
                        case "ServiceProvision":
                            inQueue = refCollection;
                            state = "outQueue1";
                            break;
                    }
                    continue;
                }
            }

            foreach (var output in op.OutputVariables)
            {
                var outputRef = output.Value;

                if (!(outputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += outputRef.IdShort + " not found! ";
                if (refElement is SubmodelElementCollection)
                {
                    refCollection = refElement as SubmodelElementCollection;
                    switch (i40Logic?.Value)
                    {
                        case "callForProposal":
                        case "capabilityCheck":
                        case "feasibilityCheck":
                        case "checkingSchedule":
                        case "PriceCalculation":
                        case "ServiceProvision":
                            switch (state)
                            {
                                case "outQueue1":
                                    outQueue1 = refCollection;
                                    state = "";
                                    break;
                            }
                            break;
                        case "evaluateProposal":
                        case "WaitingForServiceRequesterAnswer":
                            switch (state)
                            {
                                case "outQueue1":
                                    outQueue1 = refCollection;
                                    state = "outQueue2";
                                    break;
                                case "outQueue2":
                                    outQueue2 = refCollection;
                                    state = "";
                                    break;
                            }
                            break;
                    }
                    continue;
                }
            }

            // Execute operation
            SubmodelElementCollection smcSubmodel = null;

            switch (i40Logic?.Value)
            {
                case "callForProposal":
                    // Harish, please add correct code here
                    smcSubmodel = new SubmodelElementCollection();
                    smcSubmodel.IdShort = "callForProposal";
                    foreach (var sme in sub.SubmodelElements)
                    {
                        smcSubmodel.Value.Add(sme);
                        treeChanged = true;
                    }
                    outQueue1.Value.Add(smcSubmodel);
                    return true;
                case "evaluateProposal":
                    // Harish, please add correct code here
                    foreach (var sme in inQueue.Value)
                    {
                        outQueue1.Value.Add(sme);
                    }
                    inQueue.Value.Clear();
                    treeChanged = true;
                    return true;
                case "WaitingForServiceRequesterAnswer":
                    // Harish, please add correct code here
                    foreach (var sme in inQueue.Value)
                    {
                        outQueue1.Value.Add(smcSubmodel);
                    }
                    inQueue.Value.Clear();
                    treeChanged = true;
                    return true;
                case "ServiceProvision":
                    if (inQueue.Value.Count != 0)
                    {
                        outQueue1.Value.Add(inQueue);
                        // inQueue.Value.Clear();
                        treeChanged = true;
                    }
                    return true;
                case "evaluateInformConfirm":
                    return true;
                case "capabilityCheck":
                    return true;
                case "feasibilityCheck":
                    return true;
                case "checkingSchedule":
                    return true;
                case "PriceCalculation":
                    outQueue1.Value.Add(proposalMessage);
                    treeChanged = true;
                    return true;
            }

            return false;
        }

        public static List<string> i40frameRequesterSendBuffer = new List<string>();
        public static List<string> i40frameProviderSendBuffer = new List<string>();

        static void i40frameSend(string message, string protocol, i40LanguageAutomaton auto)
        {
            if (protocol == "memory")
            {
                if (auto.name == "automatonServiceRequester")
                {
                    i40frameRequesterSendBuffer.Add(message);
                }
                if (auto.name == "automatonServiceProvider")
                {
                    i40frameProviderSendBuffer.Add(message);
                }
            }
        }

        static string i40frameReceive(string protocol, i40LanguageAutomaton auto)
        {
            string message = "";

            if (protocol == "memory")
            {
                if (auto.name == "automatonServiceRequester")
                {
                    if (i40frameProviderSendBuffer.Count > 0)
                    {
                        message = i40frameProviderSendBuffer[0];
                        i40frameProviderSendBuffer.RemoveAt(0);
                    }
                }
                if (auto.name == "automatonServiceProvider")
                {
                    if (i40frameRequesterSendBuffer.Count > 0)
                    {
                        message = i40frameRequesterSendBuffer[0];
                        i40frameRequesterSendBuffer.RemoveAt(0);
                    }
                }
            }

            return message;
        }

        public static bool operation_sendI40frame(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable reference property protocol
            // inputVariable reference collection outputQueue

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count != 2)
            {
                return false;
            }

            Property protocol = null;
            SubmodelElementCollection outQueue = null;

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;

                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is Property p)
                {
                    protocol = p;
                    continue;
                }
                if (refElement is SubmodelElementCollection)
                {
                    outQueue = refElement as SubmodelElementCollection;
                    continue;
                }
            }

            if (protocol != null & outQueue != null)
            {
                // Harish, please add correct code here
                i40frameSend(JsonConvert.SerializeObject(outQueue, Newtonsoft.Json.Formatting.Indented), protocol.Value, auto);
            }

            return false;
        }

        public static bool operation_receiveI40frame(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable reference property protocol
            // inputVariable reference collection inputQueue

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count != 2)
            {
                return false;
            }

            Property protocol = null;
            SubmodelElementCollection inQueue = null;

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;

                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is Property p)
                {
                    protocol = p;
                    continue;
                }
                if (refElement is SubmodelElementCollection)
                {
                    inQueue = refElement as SubmodelElementCollection;
                    continue;
                }
            }

            if (protocol != null & inQueue != null)
            {
                // Harish, please add correct code here

                SubmodelElementCollection smc = null;
                try
                {
                    smc = Newtonsoft.Json.JsonConvert.DeserializeObject<SubmodelElementCollection>
                        (i40frameReceive(protocol.Value, auto), new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                    if (smc != null)
                    {
                        foreach (var sme in smc.Value)
                        {
                            inQueue.Value.Add(sme);
                            treeChanged = true;
                        }
                    }
                }
                catch
                { }
            }

            return false;
        }
        public static bool operation_calculate(Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property checkType: isEmpty, isNotEmpty;
            // inputVariable reference collection proposal

            if (auto.name == debugAutomaton)
            {
            }

            if (op.InputVariables.Count != 2 && op.OutputVariables.Count != 1)
            {
                return false;
            }

            Property operation = null;
            SubmodelElementCollection inputCollection = null;
            Property outputProperty = null;
            SubmodelElementCollection outputCollection = null;

            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (inputRef is Property)
                {
                    operation = (inputRef as Property);
                    continue;
                }
                if (!(inputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += inputRef.IdShort + " not found! ";
                if (refElement is SubmodelElementCollection)
                {
                    inputCollection = refElement as SubmodelElementCollection;
                }
            }

            foreach (var output in op.OutputVariables)
            {
                var outputRef = output.Value;
                if (!(outputRef is ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as ReferenceElement).GetModelReference());
                if (refElement == null)
                    auto.getErrors += outputRef.IdShort + " not found! ";
                if (refElement is Property)
                {
                    outputProperty = (refElement as Property);
                    continue;
                }
                if (refElement is SubmodelElementCollection)
                {
                    outputCollection = (refElement as SubmodelElementCollection);
                    continue;
                }
            }

            if (operation == null || inputCollection == null || (outputProperty == null && outputCollection == null))
                return false;

            switch (operation.IdShort)
            {
                case "length":
                    outputProperty.Value = inputCollection.Value.Count.ToString();
                    break;
                case "getFirst":
                    if (outputProperty != null)
                        outputProperty.Value = inputCollection.Value[0].ValueAsText();
                    if (outputCollection != null)
                    {
                        outputCollection.Value.Add(inputCollection.Value[0]);
                    }
                    inputCollection.Value.RemoveAt(0);
                    break;
            }

            return false;
        }
    }
}
