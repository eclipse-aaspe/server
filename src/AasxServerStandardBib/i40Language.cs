using AdminShellNS;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;

namespace AasxServer
{
    public class i40LanguageAutomaton
    {
        public string name = "";
        public string getActualStates = "";
        public string getStatus = "stop";
        public string setMode = "";
        public string getErrors = "";
        public string setForcedStates = "";
        public string setSingleStep = "";
        public string setSteppingTime = "2";

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

                                foreach (var smw1 in sm.submodelElements)
                                {
                                    var sme1 = smw1.submodelElement;

                                    if (sme1 is AdminShell.SubmodelElementCollection && sme1.idShort == "automatonControl")
                                    {
                                        var smc1 = sme1 as AdminShell.SubmodelElementCollection;
                                        
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
                                auto.maxTick = Convert.ToInt32(auto.setSteppingTime);
                            }
                        }
                    }
                }
            }

            threadDelegate = new ThreadStart(nextTick);
            i40LanguageThread = new Thread(threadDelegate);
            i40LanguageThread.Start();
        }

        public static void nextTick()
        {
            while (true)
            {
                foreach (var auto in automatons)
                {
                    if (auto.name != "automatonServiceRequester")
                        continue;

                    if (auto.tick >= auto.maxTick)
                    {
                        auto.tick = 0;

                        Console.WriteLine(auto.name + ":");
                        string states = "";
                        foreach (var s in auto.actualStates)
                        {
                            states += s + " ";
                        }
                        Console.WriteLine("states = " + states);

                        List<string> transitionsEnabled = new List<string>();
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
                        Console.WriteLine("Transition enabled: " + enabled);

                        // collect fromStates and toStates from enabled transitions
                        foreach (var t in auto.transitions)
                        {
                            if (transitionsEnabled.Contains(t.idShort))
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

                        Console.WriteLine();
                    }
                    auto.tick++;
                }

                Thread.Sleep(1000);
            }
        }
    }
}
