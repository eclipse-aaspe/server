using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AdminShellNS;
using static AdminShellNS.AdminShellV10;
// using static AasxBlazor.Pages.__generated__TreePage;
using static AasxBlazor.Pages.TreePage;
// using static AasxBlazor.Pages.TreeSample;
using Net46ConsoleServer;

namespace AasxBlazor.Data
{
    public class SubmodelText
    {
        public string text { get; set; }
    }

    public class AASService
    {
        // public AdminShell.PackageEnv env;
        
        public AASService()
        {
            loadAAS();

            Program.NewOpcDataAvailable += (s, a) =>
            {
                NewOpcDataAvailable?.Invoke(this, EventArgs.Empty);
            };
        }
        public event EventHandler NewOpcDataAvailable;
        public void loadAAS()
        {
            // env = new AdminShell.PackageEnv("Example_AAS_ServoDCMotor_21.aasx");
            // env = new AdminShell.PackageEnv("BoschRexroth_HCS-TypePlate.aasx");
        }

        public List<Item> GetTree()
        {
            List<Item> items = new List<Item>();
            for (int i = 0; i < Program.envimax; i++)
            {
                Item root = new Item();
                root.envIndex = i;
                if (Program.env[i] != null)
                {
                    root.Text = Program.env[i].AasEnv.AdministrationShells[0].idShort;
                    root.Tag = Program.env[i].AasEnv.AdministrationShells[0];
                    if (Program.envSymbols[i] != "L")
                    {
                        List<Item> childs = new List<Item>();
                        foreach (var sm in Program.env[i].AasEnv.Submodels)
                        {
                            if (sm != null && sm.idShort != null)
                            {
                                var smItem = new Item();
                                smItem.envIndex = i;
                                smItem.Text = sm.idShort;
                                smItem.Tag = sm;
                                childs.Add(smItem);
                                List<Item> smChilds = new List<Item>();
                                foreach (var sme in sm.submodelElements)
                                {
                                    var smeItem = new Item();
                                    smeItem.envIndex = i;
                                    smeItem.Text = sme.submodelElement.idShort;
                                    smeItem.Tag = sme.submodelElement;
                                    smChilds.Add(smeItem);
                                    if (sme.submodelElement is SubmodelElementCollection)
                                    {
                                        var smec = sme.submodelElement as SubmodelElementCollection;
                                        createSMECItems(smeItem, smec, i);
                                    }
                                }
                                smItem.Childs = smChilds;
                            }
                        }
                        root.Childs = childs;
                        items.Add(root);
                    }
                }
                if (Program.envSymbols[i] == "L")
                {
                    root.Text = System.IO.Path.GetFileName(Program.envFileName[i]);
                    items.Add(root);
                }
            }
            return items;
        }

        void createSMECItems(Item smeRootItem, SubmodelElementCollection smec, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var sme in smec.value)
            {
                var smeItem = new Item();
                smeItem.envIndex = i;
                smeItem.Text = sme.submodelElement.idShort;
                smeItem.Tag = sme.submodelElement;
                smChilds.Add(smeItem);
                if (sme.submodelElement is SubmodelElementCollection)
                {
                    var smecNext = sme.submodelElement as SubmodelElementCollection;
                    createSMECItems(smeItem, smecNext, i);
                }
            }
            smeRootItem.Childs = smChilds;
        }

        public List<Submodel> GetSubmodels()
        {
            return Program.env[0].AasEnv.Submodels;
        }
    }
}
