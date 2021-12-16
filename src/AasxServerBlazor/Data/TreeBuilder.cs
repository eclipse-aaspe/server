using AasxServer;
using System;
using System.Collections.Generic;
using System.Threading;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;

namespace AasxServerBlazor.Data
{
    public class TreeBuilder
    {
        private List<Item> Items = null;
        public List<Item> ViewItems = null;

        public void buildTree()
        {
            while (Program.isLoading)
            {
                Thread.Sleep(1000);
            }

            lock (Program.changeAasxFile)
            {
                Items = new List<Item>();
                for (int i = 0; i < Program.envimax; i++)
                {
                    Item root = new Item();
                    root.EnvIndex = i;
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
                                    smItem.EnvIndex = i;
                                    smItem.Text = sm.idShort;
                                    smItem.Tag = sm;
                                    childs.Add(smItem);
                                    List<Item> smChilds = new List<Item>();
                                    foreach (var sme in sm.submodelElements)
                                    {
                                        var smeItem = new Item();
                                        smeItem.EnvIndex = i;
                                        smeItem.Text = sme.submodelElement.idShort;
                                        smeItem.Tag = sme.submodelElement;
                                        smChilds.Add(smeItem);
                                        if (sme.submodelElement is SubmodelElementCollection)
                                        {
                                            var smec = sme.submodelElement as SubmodelElementCollection;
                                            createSMECItems(smeItem, smec, i);
                                        }
                                        if (sme.submodelElement is Operation)
                                        {
                                            var o = sme.submodelElement as Operation;
                                            createOperationItems(smeItem, o, i);
                                        }
                                        if (sme.submodelElement is Entity)
                                        {
                                            var e = sme.submodelElement as Entity;
                                            createEntityItems(smeItem, e, i);
                                        }
                                    }
                                    smItem.Children = smChilds;
                                    foreach (var c in smChilds)
                                        c.Parent = smItem;
                                }
                            }
                            root.Children = childs;
                            foreach (var c in childs)
                                c.Parent = root;
                            Items.Add(root);
                        }
                    }
                    if (Program.envSymbols[i] == "L")
                    {
                        root.Text = System.IO.Path.GetFileName(Program.envFileName[i]);
                        Items.Add(root);
                    }
                }
            }
            ViewItems = Items;
        }

        void createSMECItems(Item smeRootItem, SubmodelElementCollection smec, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var sme in smec.value)
            {
                if (sme != null && sme.submodelElement != null)
                {
                    var smeItem = new Item();
                    smeItem.EnvIndex = i;
                    smeItem.Text = sme.submodelElement.idShort;
                    smeItem.Tag = sme.submodelElement;
                    smChilds.Add(smeItem);
                    if (sme.submodelElement is SubmodelElementCollection)
                    {
                        var smecNext = sme.submodelElement as SubmodelElementCollection;
                        createSMECItems(smeItem, smecNext, i);
                    }
                    if (sme.submodelElement is Operation)
                    {
                        var o = sme.submodelElement as Operation;
                        createOperationItems(smeItem, o, i);
                    }
                    if (sme.submodelElement is Entity)
                    {
                        var e = sme.submodelElement as Entity;
                        createEntityItems(smeItem, e, i);
                    }
                }
            }
            smeRootItem.Children = smChilds;
            foreach (var c in smChilds)
                c.Parent = smeRootItem;
        }

        void createOperationItems(Item smeRootItem, Operation op, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var v in op.inputVariable)
            {
                var smeItem = new Item();
                smeItem.EnvIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "In";
                smeItem.Tag = v.value.submodelElement;
                smChilds.Add(smeItem);
            }
            foreach (var v in op.outputVariable)
            {
                var smeItem = new Item();
                smeItem.EnvIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "Out";
                smeItem.Tag = v.value.submodelElement;
                smChilds.Add(smeItem);
            }
            foreach (var v in op.inoutputVariable)
            {
                var smeItem = new Item();
                smeItem.EnvIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "InOut";
                smeItem.Tag = v.value.submodelElement;
                smChilds.Add(smeItem);
            }
            smeRootItem.Children = smChilds;
            foreach (var c in smChilds)
                c.Parent = smeRootItem;
        }

        void createEntityItems(Item smeRootItem, Entity e, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var s in e.statements)
            {
                if (s != null && s.submodelElement != null)
                {
                    var smeItem = new Item();
                    smeItem.EnvIndex = i;
                    smeItem.Text = s.submodelElement.idShort;
                    smeItem.Type = "In";
                    smeItem.Tag = s.submodelElement;
                    smChilds.Add(smeItem);
                }
            }
            smeRootItem.Children = smChilds;
            foreach (var c in smChilds)
                c.Parent = smeRootItem;
        }

        public List<Submodel> GetSubmodels()
        {
            return Program.env[0].AasEnv.Submodels;
        }
    }
}
