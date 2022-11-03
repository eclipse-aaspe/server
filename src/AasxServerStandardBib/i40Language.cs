using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Opc.Ua.Server;

/*
Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
Copyright (c) 2018-2020 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
*/

namespace AasxServer
{
    public class i40LanguageAutomaton
    {
        public AdminShell.SubmodelElementCollection automatonControl;

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
        public Dictionary<string, AdminShell.SubmodelElementCollection> variablesCollections;

        public HashSet<string> states = new HashSet<string>();
        public HashSet<string> initialStates = new HashSet<string>();
        public HashSet<string> actualStates = new HashSet<string>();

        public List<AdminShell.SubmodelElementCollection> transitions;

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
                        if (sm != null && sm.idShort != null)
                        {
                            bool withI40Language = false;
                            int count = sm.qualifiers.Count;
                            if (count != 0)
                            {
                                int j = 0;

                                while (j < count) // Scan qualifiers
                                {
                                    var p = sm.qualifiers[j] as AdminShell.Qualifier;

                                    if (p.type == "i40language")
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
                                auto.name = sm.idShort;
                                if (auto.name == "automatonServiceRequester")
                                    isRequester = true;
                                if (auto.name == "automatonServiceProvider")
                                    isProvider = true;

                                foreach (var smw1 in sm.submodelElements)
                                {
                                    var sme1 = smw1.submodelElement;

                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "automatonControl")
                                    {
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;
                                        auto.automatonControl = smc1;

                                        foreach (var smw2 in smc1.value)
                                        {
                                            var sme2 = smw2.submodelElement;
                                            if (sme2 is AdminShell.Property && sme2.idShort == "setSteppingTime")
                                            {
                                                string value = (sme2 as AdminShell.Property).value;
                                                Regex regex = new Regex(@"^\d+$");
                                                if (regex.IsMatch(value))
                                                {
                                                    auto.setSteppingTime = value;
                                                }
                                                else
                                                {
                                                    (sme2 as AdminShell.Property).value = auto.setSteppingTime;
                                                }
                                            }
                                        }
                                    }

                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "frames")
                                    {
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;
                                        auto.frameNames = new string[smc1.value.Count];
                                        auto.frameContent = new Dictionary<string, string>[smc1.value.Count];

                                        int i = 0;
                                        foreach (var smw2 in smc1.value)
                                        {
                                            var sme2 = smw2.submodelElement;
                                            if (sme2 is AdminShell.SubmodelElementCollection)
                                            {
                                                var smc2 = sme2 as AdminShell.SubmodelElementCollection;
                                                auto.frameNames[i] = smc2.idShort;
                                                auto.frameContent[i] = new Dictionary<string, string>();

                                                foreach (var smw3 in smc2.value)
                                                {
                                                    var sme3 = smw3.submodelElement;
                                                    if (sme3 is AdminShell.Property)
                                                    {
                                                        auto.frameContent[i].Add(sme3.idShort, (sme3 as AdminShell.Property).value);
                                                    }
                                                }
                                            }
                                            i++;
                                        }
                                    }

                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "constants")
                                    {
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;
                                        auto.constants = new Dictionary<string, string>();

                                        foreach (var smw2 in smc1.value)
                                        {
                                            var sme2 = smw2.submodelElement;
                                            if (sme2 is AdminShell.Property)
                                            {
                                                auto.constants.Add(sme2.idShort, (sme2 as AdminShell.Property).value);
                                            }
                                        }
                                    }

                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "variables")
                                    {
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;
                                        auto.variablesStrings = new Dictionary<string, string>();
                                        auto.variablesCollections = new Dictionary<string, AdminShell.SubmodelElementCollection>();

                                        foreach (var smw2 in smc1.value)
                                        {
                                            var sme2 = smw2.submodelElement;
                                            if (sme2 is AdminShell.Property)
                                            {
                                                auto.variablesStrings.Add(sme2.idShort, (sme2 as AdminShell.Property).value);
                                            }
                                            if (sme2 is AdminShell.SubmodelElementCollection)
                                            {
                                                auto.variablesCollections.Add(sme2.idShort, (sme2 as AdminShell.SubmodelElementCollection));
                                            }
                                        }
                                    }

                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "states")
                                    {
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;

                                        foreach (var smw2 in smc1.value)
                                        {
                                            var sme2 = smw2.submodelElement;
                                            if (sme2 is AdminShell.Property || (sme2 is AdminShell.SubmodelElementCollection && sme2.idShort != "initialStates"))
                                            {
                                                auto.states.Add(sme2.idShort);
                                            }
                                            if (sme2 is AdminShell.SubmodelElementCollection && sme2.idShort == "initialStates")
                                            {
                                                var smc2 = sme2 as AdminShell.SubmodelElementCollection;

                                                foreach (var smw3 in smc2.value)
                                                {
                                                    var sme3 = smw3.submodelElement;
                                                    if (sme3 is AdminShell.Property || (sme3 is AdminShell.SubmodelElementCollection && sme3.idShort != "initialStates"))
                                                    {
                                                        auto.initialStates.Add(sme3.idShort);
                                                        auto.states.Add(sme3.idShort);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "transitions")
                                    {
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;
                                        auto.transitions = new List<AdminShell.SubmodelElementCollection>();

                                        foreach (var smw2 in smc1.value)
                                        {
                                            var sme2 = smw2.submodelElement;
                                            if (sme2 is AdminShell.SubmodelElementCollection)
                                            {
                                                auto.transitions.Add(sme2 as AdminShell.SubmodelElementCollection);
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
                i40LanguageThread.Start();
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
                    foreach (var smw1 in auto.automatonControl.value)
                    {
                        var sme1 = smw1.submodelElement;
                        if (sme1 is AdminShell.Property && sme1.idShort == "setMode")
                        {
                            auto.setMode = (sme1 as AdminShell.Property).value;
                        }
                        if (sme1 is AdminShell.Property && sme1.idShort == "stopAtStates")
                        {
                            string stopAtStates = (sme1 as AdminShell.Property).value;
                            string[] states = stopAtStates.Split(' ');
                            foreach (var s in states)
                                if (auto.actualStates.Contains(s))
                                    auto.setMode = "stop";
                        }
                        if (sme1 is AdminShell.Property && sme1.idShort == "setForcedStates")
                        {
                            auto.setForcedStates = (sme1 as AdminShell.Property).value;
                            (sme1 as AdminShell.Property).value = "";
                        }
                        if (sme1 is AdminShell.Property && sme1.idShort == "setSingleStep")
                        {
                            auto.setSingleStep = (sme1 as AdminShell.Property).value;
                            (sme1 as AdminShell.Property).value = "";
                        }
                        if (sme1 is AdminShell.Property && sme1.idShort == "setSteppingTime")
                        {
                            string value = (sme1 as AdminShell.Property).value;
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
                            foreach (var smw1 in t.value)
                            {
                                var sme1 = smw1.submodelElement;
                                if (sme1 is AdminShell.SubmodelElementCollection && (sme1.idShort == "from" || sme1.idShort == "From"))
                                {
                                    var smc1 = sme1 as AdminShell.SubmodelElementCollection;

                                    foreach (var smw2 in smc1.value)
                                    {
                                        var sme2 = smw2.submodelElement;

                                        fromStates.Add(sme2.idShort);
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
                                transitionsEnabled.Add(t.idShort);
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
                            if (transitionsEnabled.Contains(t.idShort))
                            {
                                bool includesInput = false;
                                foreach (var smw1 in t.value)
                                {
                                    var sme1 = smw1.submodelElement;
                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "input")
                                    {
                                        includesInput = true;
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;

                                        foreach (var smw2 in smc1.value)
                                        {
                                            var sme2 = smw2.submodelElement;

                                            if (sme2 is AdminShell.Operation)
                                            {
                                                if (auto.name == debugAutomaton)
                                                {
                                                }

                                                var op = sme2 as AdminShell.Operation;
                                                // Console.WriteLine("Operation: " + op.idShort);
                                                bool opResult = false;
                                                switch (op.idShort)
                                                {
                                                    case "wait":
                                                        opResult = operation_wait(op, auto);
                                                        if (opResult)
                                                            transitionsActive.Add(t.idShort);
                                                        break;
                                                    case "message":
                                                        opResult = operation_message(op, auto);
                                                        break;
                                                    case "checkCollection":
                                                        opResult = operation_checkCollection(op, auto);
                                                        if (opResult)
                                                            transitionsActive.Add(t.idShort);
                                                        break;
                                                    case "check":
                                                        opResult = operation_check(op, auto);
                                                        if (opResult)
                                                            transitionsActive.Add(t.idShort);
                                                        break;
                                                    case "receiveProposals":
                                                        opResult = operation_receiveProposals(op, auto);
                                                        if (opResult)
                                                            transitionsActive.Add(t.idShort);
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
                                                        transitionsActive.Add(t.idShort);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (!includesInput)
                                    transitionsActive.Add(t.idShort);
                            }
                        }

                        // collect fromStates and toStates from enabled transitions
                        foreach (var t in auto.transitions)
                        {
                            if (transitionsActive.Contains(t.idShort))
                            {
                                // execute output operations
                                foreach (var smw1 in t.value)
                                {
                                    var sme1 = smw1.submodelElement;
                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "output")
                                    {
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;

                                        foreach (var smw2 in smc1.value)
                                        {
                                            var sme2 = smw2.submodelElement;

                                            if (sme2 is AdminShell.Operation)
                                            {
                                                if (auto.name == debugAutomaton)
                                                {
                                                }

                                                var op = sme2 as AdminShell.Operation;
                                                // Console.WriteLine("Operation: " + op.idShort);
                                                bool opResult = false;
                                                switch (op.idShort)
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
                                    foreach (var smw21 in t.value)
                                    {
                                        sme1 = smw21.submodelElement;
                                        if (sme1 is AdminShell.SubmodelElementCollection && (sme1.idShort == "from" || sme1.idShort == "From"))
                                        {
                                            var smc1 = sme1 as AdminShell.SubmodelElementCollection;

                                            foreach (var smw2 in smc1.value)
                                            {
                                                var sme2 = smw2.submodelElement;

                                                if (!fromStates.Contains(sme2.idShort))
                                                    fromStates.Add(sme2.idShort);
                                            }
                                        }
                                        if (sme1 is AdminShell.SubmodelElementCollection && (sme1.idShort == "to" || sme1.idShort == "To"))
                                        {
                                            var smc1 = sme1 as AdminShell.SubmodelElementCollection;

                                            foreach (var smw2 in smc1.value)
                                            {
                                                var sme2 = smw2.submodelElement;

                                                if (!toStates.Contains(sme2.idShort))
                                                    toStates.Add(sme2.idShort);
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
                        foreach (var smw1 in auto.automatonControl.value)
                        {
                            var sme1 = smw1.submodelElement;
                            if (sme1 is AdminShell.Property && sme1.idShort == "getActualTime")
                            {
                                (sme1 as AdminShell.Property).value = DateTime.UtcNow.ToString();
                            }
                            if (sme1 is AdminShell.Property && sme1.idShort == "getStatus")
                            {
                                (sme1 as AdminShell.Property).value = auto.getStatus;
                            }
                            if (sme1 is AdminShell.Property && sme1.idShort == "getActualStates")
                            {
                                (sme1 as AdminShell.Property).value = "";
                                foreach (var s in auto.actualStates)
                                {
                                    var p = sme1 as AdminShell.Property;
                                    p.value += s + " ";
                                    p.TimeStamp = DateTime.UtcNow;
                                }
                            }
                            if (sme1 is AdminShell.Property && sme1.idShort == "getTransitionsEnabled")
                            {
                                (sme1 as AdminShell.Property).value = "";
                                foreach (var t in transitionsEnabled)
                                {
                                    (sme1 as AdminShell.Property).value += t + " ";
                                }
                            }
                            if (sme1 is AdminShell.Property && sme1.idShort == "getErrors")
                            {
                                (sme1 as AdminShell.Property).value = auto.getErrors;
                            }
                            if (sme1 is AdminShell.Property && sme1.idShort == "getMessages")
                            {
                                string value = "";
                                foreach (var text in auto.getMessages)
                                {
                                    if (value != "")
                                        value += ", ";
                                    value += text;
                                }
                                (sme1 as AdminShell.Property).value = value;
                            }
                            /*
                            if (sme1 is AdminShell.Property && sme1.idShort == "setMode")
                            {
                                (sme1 as AdminShell.Property).value = "";
                            }
                            if (sme1 is AdminShell.Property && sme1.idShort == "setForcedStates")
                            {
                                (sme1 as AdminShell.Property).value = "";
                            }
                            if (sme1 is AdminShell.Property && sme1.idShort == "setSingleStep")
                            {
                                (sme1 as AdminShell.Property).value = "";
                            }
                            if (sme1 is AdminShell.Property && sme1.idShort == "setSteppingTime")
                            {
                                string value = (sme1 as AdminShell.Property).value;
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

        public static bool operation_message(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property value = text
            foreach (var v in op.inputVariable)
            {
                if (v.value.submodelElement is AdminShell.Property)
                {
                    var p = v.value.submodelElement as AdminShell.Property;
                    if (auto.getMessages.Count < 100)
                        auto.getMessages.Add(p.value);
                    if (auto.getMessages.Count == 100)
                        auto.getMessages.Add("+++");
                    // Console.WriteLine("operation message: " + p.idShort + " = " + p.value);
                }
            }
            return true;
        }

        public static bool operation_clearMessages(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            auto.getMessages.Clear();
            return true;
        }

        public static bool operation_wait(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable reference waitingTime: property value
            // outputVariable reference endTime: property value

            if (op.inputVariable.Count != 1 && op.outputVariable.Count != 1)
            {
                return false;
            }

            var in1 = op.inputVariable.First();
            var r1 = in1.value.submodelElement;
            if (!(r1 is AdminShell.ReferenceElement))
                return false;
            var ref1 = Program.env[0].AasEnv.FindReferableByReference((r1 as AdminShell.ReferenceElement).value);
            if (ref1 == null)
                auto.getErrors += r1.idShort + " not found! ";
            if (!(ref1 is AdminShell.Property))
                return false;
            var p1 = ref1 as AdminShell.Property;
            int waitingTime = Convert.ToInt32(p1.value);

            var out1 = op.outputVariable.First();
            var r2 = out1.value.submodelElement;
            if (!(r2 is AdminShell.ReferenceElement))
                return false;
            var ref2 = Program.env[0].AasEnv.FindReferableByReference((r2 as AdminShell.ReferenceElement).value);
            if (ref2 == null)
                auto.getErrors += r2.idShort + " not found! ";
            if (!(ref2 is AdminShell.Property))
                return false;
            var p2 = ref2 as AdminShell.Property;

            DateTime localTime = DateTime.UtcNow;
            if (p2.value == "") // start
            {
                var endTime = localTime.AddSeconds(waitingTime);
                p2.value = endTime.ToString();
                // Console.WriteLine("endTime = " + p2.value);
            }
            else // test if time has elapsed
            {
                // Console.WriteLine("localTime = " + localTime);
                var endTime = DateTime.Parse(p2.value);
                if (DateTime.Compare(localTime, endTime) > 0)
                {
                    p2.value = "";
                    return true;
                }
            }

            return false;
        }

        public static bool operation_receiveProposals(AdminShell.Operation op, i40LanguageAutomaton auto)
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

            if (op.inputVariable.Count != 3 && op.outputVariable.Count != 4)
            {
                return false;
            }

            AdminShell.Submodel refSubmodel = null;
            AdminShell.Property protocol = null;
            AdminShell.Property receivedFrameJSON = null;

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property)
                {
                    protocol = (inputRef as AdminShell.Property);
                    continue;
                }
                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.Submodel)
                    refSubmodel = refElement as AdminShell.Submodel;
            }
            foreach (var output in op.outputVariable)
            {
                var outputRef = output.value.submodelElement;
                if (!(outputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += outputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property)
                    receivedFrameJSON = refElement as AdminShell.Property;
            }

            var out1 = op.outputVariable.First();
            var r2 = out1.value.submodelElement;
            if (!(r2 is AdminShell.ReferenceElement))
                return false;
            var ref2 = Program.env[0].AasEnv.FindReferableByReference((r2 as AdminShell.ReferenceElement).value);
            if (ref2 == null)
                auto.getErrors += r2.idShort + " not found! ";
            if (!(ref2 is AdminShell.SubmodelElementCollection))
                return false;
            var smc2 = ref2 as AdminShell.SubmodelElementCollection;

            if (protocol.value != "memory" && protocol.value != "connect")
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

                receivedFrameJSON.value = receivedFrame;

                AdminShell.Submodel submodel = null;

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
                            if (submodel.idShort == "Boring")
                            {
                                boringSubmodel = submodel;
                                boringSubmodelFrame = newBiddingMessage.frame;
                            }
                            AdminShell.SubmodelElementCollection smcSubmodel = new AdminShell.SubmodelElementCollection();
                            smcSubmodel.idShort = submodel.idShort;
                            foreach (var sme in submodel.submodelElements)
                            {
                                smcSubmodel.Add(sme.submodelElement);
                                treeChanged = true;
                            }
                            smc2.Add(smcSubmodel);

                        }

                    }
                    catch
                    {
                    }
                }

            }

            return true;
        }

        public static bool operation_receiveI40message(AdminShell.Operation op, i40LanguageAutomaton auto)
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

            if ((op.inputVariable.Count < 2 && op.inputVariable.Count > 3) && op.outputVariable.Count != 2)
            {
                return false;
            }

            AdminShell.Property protocol = null;
            AdminShell.Property protocolServer = null;
            AdminShell.SubmodelElementCollection refProposals = null;
            AdminShell.Submodel refSubmodel = null;
            AdminShell.Property messageType = null;
            AdminShell.Property receivedFrameJSON = null;

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property p)
                {
                    if (p.idShort == "protocol")
                        protocol = p;
                    if (p.idShort == "protocolServer")
                        protocolServer = p;
                }
                if (refElement is AdminShell.Submodel s)
                {
                    refSubmodel = s;
                }
            }

            foreach (var output in op.outputVariable)
            {
                var outputRef = output.value.submodelElement;
                if (!(outputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += outputRef.idShort + " not found! ";
                if (refElement is AdminShell.SubmodelElementCollection smc)
                    refProposals = smc;
                if (refElement is AdminShell.Property p)
                {
                    if (refProposals == null && messageType == null)
                        messageType = p;
                    else
                        receivedFrameJSON = p;
                }
            }

            if (protocol.value != "memory" && protocol.value != "i40connect")
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

                receivedFrameJSON.value = receivedFrame;

                AdminShell.Submodel submodel = null;

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
                                if (submodel.idShort == "Boring")
                                {
                                    boringSubmodel = submodel;
                                    boringSubmodelFrame = newBiddingMessage.frame;
                                }
                                AdminShell.SubmodelElementCollection smcSubmodel = new AdminShell.SubmodelElementCollection();
                                smcSubmodel.idShort = submodel.idShort;
                                foreach (var sme in submodel.submodelElements)
                                {
                                    smcSubmodel.Add(sme.submodelElement);
                                    treeChanged = true;
                                }
                                refProposals.Add(smcSubmodel);
                            }
                        }
                        else
                        {
                            if (messageType != null)
                                messageType.value = newBiddingMessage.frame.type;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return true;
        }

        public static bool operation_receiveSRAnswer(AdminShell.Operation op, i40LanguageAutomaton auto)
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


        public static AdminShell.Submodel boringSubmodel = null;
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
        public static AdminShell.Submodel returnBoringSbmodel()
        {
            AdminShell.Identification _boringSMID = new AdminShell.Identification();
            _boringSMID.id = "www.company.com/ids/sm/3145_4121_8002_1792";
            _boringSMID.idType = "IRI";
            AdminShell.Submodel _boringSubmodel = new AdminShell.Submodel();
            _boringSubmodel = Program.env[0].AasEnv.FindSubmodel(_boringSMID);
            return Program.env[0].AasEnv.FindSubmodel(_boringSMID);
        }
        public static bool operation_sendFrame(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property protocol: memory, connect
            // inputVariable reference frame proposal: collection
            // inputVariable reference submodel
            // outputVariable reference property sendFrameJSON

            if (auto.name == debugAutomaton)
            {
            }

            if (op.inputVariable.Count != 3 && op.outputVariable.Count != 1)
            {
                return false;
            }

            AdminShell.Property protocol = null;
            AdminShell.SubmodelElementCollection refFrame = null;
            AdminShell.Submodel refSubmodel = null;
            AdminShell.Property sendFrameJSON = null;

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property)
                {
                    protocol = (inputRef as AdminShell.Property);
                    continue;
                }
                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.SubmodelElementCollection)
                    refFrame = refElement as AdminShell.SubmodelElementCollection;
                if (refElement is AdminShell.Submodel)
                    refSubmodel = refElement as AdminShell.Submodel;
            }

            foreach (var output in op.outputVariable)
            {
                var outputRef = output.value.submodelElement;
                if (!(outputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += outputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property)
                    sendFrameJSON = refElement as AdminShell.Property;
            }

            if (protocol.value != "memory" && protocol.value != "connect")
                return false;

            if (boringSubmodel == null)
                return false;
            I40MessageHelper _i40MessageHelper = new I40MessageHelper();
            I40Message_Interaction newBiddingMessage = _i40MessageHelper.createBiddingMessage(Program.connectNodeName,
                boringSubmodelFrame.sender.identification.id,
                boringSubmodelFrame.sender.role.name, "BoringProvider", "proposal",
                "RESTAPI", boringSubmodelFrame.replyBy, boringSubmodelFrame.conversationId, Program.count);

            Program.count = Program.count + 1;

            AdminShell.Submodel _boringSubmodel = new AdminShell.Submodel();
            _boringSubmodel = returnBoringSbmodel();
            newBiddingMessage.interactionElements.Add(_boringSubmodel);

            string frame = JsonConvert.SerializeObject(newBiddingMessage, Newtonsoft.Json.Formatting.Indented);
            sendFrameJSON.value = frame;

            boringSubmodel = null;

            // Console.WriteLine(frame);

            if (auto.name == "automatonServiceRequester")
            {
                switch (protocol.value)
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
                switch (protocol.value)
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

        public static bool operation_sendI40message(AdminShell.Operation op, i40LanguageAutomaton auto)
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

            if (op.inputVariable.Count != 3 && op.outputVariable.Count != 1)
            {
                return false;
            }

            AdminShell.Property protocol = null;
            AdminShell.Property protocolServer = null;
            AdminShell.Submodel refSubmodel = null;
            AdminShell.Property messageType = null;
            AdminShell.Property sendFrameJSON = null;

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property p1)
                {
                    if (p1.idShort == "messageType")
                        messageType = p1;
                    continue;
                }
                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property p)
                {
                    if (p.idShort == "protocol")
                        protocol = p;
                    if (p.idShort == "protocolServer")
                        protocolServer = p;
                }
                if (refElement is AdminShell.Submodel s)
                {
                    refSubmodel = s;
                }
            }

            foreach (var output in op.outputVariable)
            {
                var outputRef = output.value.submodelElement;
                if (!(outputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += outputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property p)
                {
                    sendFrameJSON = refElement as AdminShell.Property;
                }
            }

            if (protocol.value != "memory" && protocol.value != "i40connect")
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

                AdminShell.Submodel _boringSubmodel = new AdminShell.Submodel();
                _boringSubmodel = returnBoringSbmodel();
                newBiddingMessage.interactionElements.Add(_boringSubmodel);

                frame = JsonConvert.SerializeObject(newBiddingMessage, Newtonsoft.Json.Formatting.Indented);
                sendFrameJSON.value = frame;

                boringSubmodel = null;
            }
            else
            {
                if (messageType.value == "informConfirm")
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
                    switch (protocol.value)
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
                    switch (protocol.value)
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

        public static bool operation_processRequesterResponse(AdminShell.Operation op, i40LanguageAutomaton auto)
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

        public static bool operation_clear(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // outputVariables are references to collections
            // alle elements will be removed from collections

            if (auto.name == debugAutomaton)
            {
            }

            if (op.outputVariable.Count == 0)
            {
                return false;
            }

            foreach (var output in op.outputVariable)
            {
                var outputRef = output.value.submodelElement;
                if (outputRef is AdminShell.ReferenceElement)
                {
                    var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as AdminShell.ReferenceElement).value);
                    if (refElement == null)
                        auto.getErrors += outputRef.idShort + " not found! ";
                    if (refElement is AdminShell.SubmodelElementCollection)
                    {
                        var refSMEC = refElement as AdminShell.SubmodelElementCollection;
                        List<AdminShell.SubmodelElement> list = new List<AdminShell.SubmodelElement>();
                        foreach (var sme in refSMEC.value)
                        {
                            list.Add(sme.submodelElement);
                        }
                        foreach (var sme2 in list)
                        {
                            refSMEC.Remove(sme2);
                            treeChanged = true;
                        }
                    }
                    if (refElement is AdminShell.Property p)
                    {
                        p.value = "";
                    }
                }
            }

            return true;
        }

        public static bool operation_checkCollection(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property checkType: isEmpty, isNotEmpty;
            // inputVariable reference collection proposal

            if (auto.name == debugAutomaton)
            {
            }

            if (op.inputVariable.Count != 2 && op.outputVariable.Count != 0)
            {
                return false;
            }

            AdminShell.Property checkType = null;
            AdminShell.SubmodelElementCollection refCollection = null;

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property)
                {
                    checkType = (inputRef as AdminShell.Property);
                    continue;
                }
                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.SubmodelElementCollection)
                    refCollection = refElement as AdminShell.SubmodelElementCollection;
            }

            int count = refCollection.value.Count;

            switch (checkType.idShort)
            {
                case "isEmpty":
                    return (count == 0);
                case "isNotEmpty":
                    return (count != 0);
            }

            return false;
        }

        public static bool operation_check(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property checkType: isEmpty, isNotEmpty;
            // inputVariable reference collection proposal

            if (auto.name == debugAutomaton)
            {
            }

            if (op.inputVariable.Count != 3 && op.outputVariable.Count != 0)
            {
                return false;
            }

            AdminShell.Property checkType = null;
            AdminShell.Property argument1 = null;
            AdminShell.Property argument2 = null;
            AdminShell.SubmodelElementCollection refCollection = null;
            AdminShell.Property refProperty = null;
            int count = 0;
            string value = "";

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property)
                {
                    if (count == 0)
                        checkType = (inputRef as AdminShell.Property);
                    if (count == 1)
                        argument1 = (inputRef as AdminShell.Property);
                    if (count == 2)
                        argument2 = (inputRef as AdminShell.Property);
                    count++;
                    continue;
                }
                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property)
                {
                    refProperty = refElement as AdminShell.Property;
                    if (count == 1)
                        argument1 = (refProperty as AdminShell.Property);
                    if (count == 2)
                        argument2 = (refProperty as AdminShell.Property);
                }
                if (refElement is AdminShell.SubmodelElementCollection)
                {
                    refCollection = refElement as AdminShell.SubmodelElementCollection;
                }
                count++;
            }

            if (checkType == null || argument1 == null || argument2 == null)
                return false;

            switch (checkType.idShort)
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
                    return (argument1.value == argument2.value);
                case "isNotEqual":
                    return (argument1.value != argument2.value);
            }

            return false;
        }

        public static bool operation_executeLogic(AdminShell.Operation op, i40LanguageAutomaton auto)
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

            if (op.inputVariable.Count < 4 && op.outputVariable.Count > 2)
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
            AdminShell.Property i40Logic = null;
            AdminShell.Property protocol = null;
            AdminShell.SubmodelElementCollection frame1 = null;
            AdminShell.SubmodelElementCollection frame2 = null;
            AdminShell.SubmodelElementCollection inQueue = null;
            AdminShell.Submodel sub = null;
            AdminShell.SubmodelElementCollection proposalMessage = null;
            AdminShell.SubmodelElementCollection outQueue1 = null;
            AdminShell.SubmodelElementCollection outQueue2 = null;

            AdminShell.SubmodelElementCollection refCollection = null;
            AdminShell.Property refProperty = null;

            string state = "i40logic";

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property p)
                {
                    switch (p.idShort)
                    {
                        case "i40Logic":
                            i40Logic = p;
                            state = "protocol";
                            break;
                    }
                    // Debug
                    switch (i40Logic?.value)
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

                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property)
                {
                    refProperty = refElement as AdminShell.Property;
                    switch (refProperty.idShort)
                    {
                        case "protocol":
                            protocol = refProperty;
                            state = "frame1";
                            break;
                    }
                    continue;
                }
                if (refElement is AdminShell.Submodel)
                {
                    if (state == "submodel")
                    {
                        sub = refElement as AdminShell.Submodel;
                        state = "outQueue1";
                    }
                    continue;
                }
                if (refElement is AdminShell.SubmodelElementCollection)
                {
                    refCollection = refElement as AdminShell.SubmodelElementCollection;
                    if (refCollection.idShort == "proposalMessage")
                    {
                        proposalMessage = refCollection;
                        state = "outQueue1";
                        continue;
                    }

                    switch (i40Logic?.value)
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

            foreach (var output in op.outputVariable)
            {
                var outputRef = output.value.submodelElement;

                if (!(outputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += outputRef.idShort + " not found! ";
                if (refElement is AdminShell.SubmodelElementCollection)
                {
                    refCollection = refElement as AdminShell.SubmodelElementCollection;
                    switch (i40Logic?.value)
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
            AdminShell.SubmodelElementCollection smcSubmodel = null;

            switch (i40Logic?.value)
            {
                case "callForProposal":
                    // Harish, please add correct code here
                    smcSubmodel = new AdminShell.SubmodelElementCollection();
                    smcSubmodel.idShort = "callForProposal";
                    foreach (var sme in sub.submodelElements)
                    {
                        smcSubmodel.Add(sme.submodelElement);
                        treeChanged = true;
                    }
                    outQueue1.Add(smcSubmodel);
                    return true;
                case "evaluateProposal":
                    // Harish, please add correct code here
                    foreach (var sme in inQueue.value)
                    {
                        outQueue1.Add(sme.submodelElement);
                    }
                    inQueue.value.Clear();
                    treeChanged = true;
                    return true;
                case "WaitingForServiceRequesterAnswer":
                    // Harish, please add correct code here
                    foreach (var sme in inQueue.value)
                    {
                        outQueue1.Add(sme.submodelElement);
                    }
                    inQueue.value.Clear();
                    treeChanged = true;
                    return true;
                case "ServiceProvision":
                    if (inQueue.value.Count != 0)
                    {
                        outQueue1.Add(inQueue);
                        // inQueue.value.Clear();
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
                    outQueue1.Add(proposalMessage);
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

        public static bool operation_sendI40frame(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable reference property protocol
            // inputVariable reference collection outputQueue

            if (auto.name == debugAutomaton)
            {
            }

            if (op.inputVariable.Count != 2)
            {
                return false;
            }

            AdminShell.Property protocol = null;
            AdminShell.SubmodelElementCollection outQueue = null;

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;

                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property p)
                {
                    protocol = p;
                    continue;
                }
                if (refElement is AdminShell.SubmodelElementCollection)
                {
                    outQueue = refElement as AdminShell.SubmodelElementCollection;
                    continue;
                }
            }

            if (protocol != null & outQueue != null)
            {
                // Harish, please add correct code here
                i40frameSend(JsonConvert.SerializeObject(outQueue, Newtonsoft.Json.Formatting.Indented), protocol.value, auto);
            }

            return false;
        }

        public static bool operation_receiveI40frame(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable reference property protocol
            // inputVariable reference collection inputQueue

            if (auto.name == debugAutomaton)
            {
            }

            if (op.inputVariable.Count != 2)
            {
                return false;
            }

            AdminShell.Property protocol = null;
            AdminShell.SubmodelElementCollection inQueue = null;

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;

                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property p)
                {
                    protocol = p;
                    continue;
                }
                if (refElement is AdminShell.SubmodelElementCollection)
                {
                    inQueue = refElement as AdminShell.SubmodelElementCollection;
                    continue;
                }
            }

            if (protocol != null & inQueue != null)
            {
                // Harish, please add correct code here

                AdminShell.SubmodelElementCollection smc = null;
                try
                {
                    smc = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.SubmodelElementCollection>
                        (i40frameReceive(protocol.value, auto), new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                    if (smc != null)
                    {
                        foreach (var sme in smc.value)
                        {
                            inQueue.Add(sme.submodelElement);
                            treeChanged = true;
                        }
                    }
                }
                catch
                { }
            }

            return false;
        }
        public static bool operation_calculate(AdminShell.Operation op, i40LanguageAutomaton auto)
        {
            // inputVariable property checkType: isEmpty, isNotEmpty;
            // inputVariable reference collection proposal

            if (auto.name == debugAutomaton)
            {
            }

            if (op.inputVariable.Count != 2 && op.outputVariable.Count != 1)
            {
                return false;
            }

            AdminShell.Property operation = null;
            AdminShell.SubmodelElementCollection inputCollection = null;
            AdminShell.Property outputProperty = null;
            AdminShell.SubmodelElementCollection outputCollection = null;

            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property)
                {
                    operation = (inputRef as AdminShell.Property);
                    continue;
                }
                if (!(inputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += inputRef.idShort + " not found! ";
                if (refElement is AdminShell.SubmodelElementCollection)
                {
                    inputCollection = refElement as AdminShell.SubmodelElementCollection;
                }
            }

            foreach (var output in op.outputVariable)
            {
                var outputRef = output.value.submodelElement;
                if (!(outputRef is AdminShell.ReferenceElement))
                    return false;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((outputRef as AdminShell.ReferenceElement).value);
                if (refElement == null)
                    auto.getErrors += outputRef.idShort + " not found! ";
                if (refElement is AdminShell.Property)
                {
                    outputProperty = (refElement as AdminShell.Property);
                    continue;
                }
                if (refElement is AdminShell.SubmodelElementCollection)
                {
                    outputCollection = (refElement as AdminShell.SubmodelElementCollection);
                    continue;
                }
            }

            if (operation == null || inputCollection == null || (outputProperty == null && outputCollection == null))
                return false;

            switch (operation.idShort)
            {
                case "length":
                    outputProperty.value = inputCollection.value.Count.ToString();
                    break;
                case "getFirst":
                    if (outputProperty != null)
                        outputProperty.value = inputCollection.value[0].submodelElement.ValueAsText();
                    if (outputCollection != null)
                    {
                        outputCollection.Add(inputCollection.value[0].submodelElement);
                    }
                    inputCollection.value.RemoveAt(0);
                    break;
            }

            return false;
        }
    }
}
