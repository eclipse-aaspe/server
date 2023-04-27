
using AasxServer;
using Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static AasxServerBlazor.Pages.TreePage;

namespace AasxServerBlazor.Data
{
    public class SubmodelText
    {
        public string text { get; set; }
    }

    public class AASService
    {

        public AASService()
        {
            // buildTree();
            // NewDataAvailable?.Invoke(this, EventArgs.Empty);

            Program.NewDataAvailable += (s, a) =>
            {
                // buildTree();
                NewDataAvailable?.Invoke(this, a);
            };
        }
        public event EventHandler NewDataAvailable;

        public static List<Item> items = null;
        public static List<Item> viewItems = null;

        public List<Item> GetTree(Item selectedNode, IList<Item> ExpandedNodes)
        {
            // buildTree();
            // Item.updateVisibleTree(viewItems, selectedNode, ExpandedNodes);
            return viewItems;
        }

        public void updateVisibleTree()
        {

        }

        public void syncSubTree(Item item)
        {
            if (item.Tag is SubmodelElementCollection)
            {
                var smec = item.Tag as SubmodelElementCollection;
                if (item.Childs.Count() != smec.Value.Count)
                {
                    createSMECItems(item, smec, item.envIndex);
                }
            }
        }
        public void buildTree()
        {
            while (Program.isLoading) ;

            lock (Program.changeAasxFile)
            {
                items = new List<Item>();

                // Check for README
                if (Directory.Exists("./readme"))
                {
                    var fileNames = Directory.GetFiles("./readme", "*.HTML");
                    Array.Sort(fileNames);
                    foreach (var fname in fileNames)
                    {
                        var fname2 = fname.Replace("\\", "/");
                        Item demo = new Item();
                        demo.envIndex = -1;
                        demo.Text = fname2;
                        demo.Tag = "README";
                        items.Add(demo);
                    }
                }

                for (int i = 0; i < Program.envimax; i++)
                {
                    Item root = new Item();
                    root.envIndex = i;
                    if (Program.env[i] != null)
                    {
                        if (Program.env[i].AasEnv.AssetAdministrationShells != null && Program.env[i].AasEnv.AssetAdministrationShells.Count > 0)
                        {
                            root.Text = Program.env[i].AasEnv.AssetAdministrationShells[0].IdShort;
                            root.Tag = Program.env[i].AasEnv.AssetAdministrationShells[0];
                            if (Program.envSymbols[i] != "L")
                            {
                                List<Item> childs = new List<Item>();
                                var env = AasxServer.Program.env[i];
                                var aas = env.AasEnv.AssetAdministrationShells[0];
                                if (env != null && aas.Submodels != null && aas.Submodels.Count > 0)
                                    foreach (var smr in aas.Submodels)
                                    {
                                        var sm = env.AasEnv.FindSubmodel(smr);
                                        if (sm != null && sm.IdShort != null)
                                        {
                                            var smItem = new Item();
                                            smItem.envIndex = i;
                                            smItem.Text = sm.IdShort;
                                            smItem.Tag = sm;
                                            childs.Add(smItem);
                                            List<Item> smChilds = new List<Item>();
                                            if (sm.SubmodelElements != null)
                                                foreach (var sme in sm.SubmodelElements)
                                                {
                                                    var smeItem = new Item();
                                                    smeItem.envIndex = i;
                                                    smeItem.Text = sme.IdShort;
                                                    smeItem.Tag = sme;
                                                    smChilds.Add(smeItem);
                                                    if (sme is SubmodelElementCollection)
                                                    {
                                                        var smec = sme as SubmodelElementCollection;
                                                        createSMECItems(smeItem, smec, i);
                                                    }
                                                    if (sme is Operation)
                                                    {
                                                        var o = sme as Operation;
                                                        createOperationItems(smeItem, o, i);
                                                    }
                                                    if (sme is Entity)
                                                    {
                                                        var e = sme as Entity;
                                                        createEntityItems(smeItem, e, i);
                                                    }
                                                    if (sme is AnnotatedRelationshipElement annotatedRelationshipElement)
                                                    {
                                                        CreateAnnotedRelationshipElementItems(smeItem, annotatedRelationshipElement, i);
                                                    }
                                                    if (sme is SubmodelElementList smeList)
                                                    {
                                                        CreateSMEListItems(smeItem, smeList, i);
                                                    }
                                                }
                                            smItem.Childs = smChilds;
                                            foreach (var c in smChilds)
                                                c.parent = smItem;
                                        }
                                    }
                                root.Childs = childs;
                                foreach (var c in childs)
                                    c.parent = root;
                                items.Add(root);
                            }
                        }
                    }
                    if (Program.envSymbols[i] == "L")
                    {
                        root.Text = System.IO.Path.GetFileName(Program.envFileName[i]);
                        items.Add(root);
                    }
                }
            }
            viewItems = items;
        }

        private void CreateSMEListItems(Item smeRootItem, SubmodelElementList smeList, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var s in smeList.Value)
            {
                if (s != null && s != null)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = s.IdShort;
                    //smeItem.Type = "In";
                    smeItem.Tag = s;
                    smChilds.Add(smeItem);
                    if (s is SubmodelElementCollection)
                    {
                        var smecNext = s as SubmodelElementCollection;
                        createSMECItems(smeItem, smecNext, i);
                    }
                    if (s is Operation)
                    {
                        var o = s as Operation;
                        createOperationItems(smeItem, o, i);
                    }
                    if (s is Entity)
                    {
                        var e = s as Entity;
                        createEntityItems(smeItem, e, i);
                    }
                    if (s is AnnotatedRelationshipElement annotatedRelationshipElement)
                    {
                        CreateAnnotedRelationshipElementItems(smeItem, annotatedRelationshipElement, i);
                    }
                    if (s is SubmodelElementList childSmeList)
                    {
                        CreateSMEListItems(smeItem, childSmeList, i);
                    }
                }
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        private void CreateAnnotedRelationshipElementItems(Item smeRootItem, AnnotatedRelationshipElement annotatedRelationshipElement, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var s in annotatedRelationshipElement.Annotations)
            {
                if (s != null && s != null)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = s.IdShort;
                    smeItem.Type = "In";
                    smeItem.Tag = s;
                    smChilds.Add(smeItem);
                }
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        void createSMECItems(Item smeRootItem, SubmodelElementCollection smec, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var sme in smec.Value)
            {
                if (sme != null && sme != null)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = sme.IdShort;
                    smeItem.Tag = sme;
                    smChilds.Add(smeItem);
                    if (sme is SubmodelElementCollection)
                    {
                        var smecNext = sme as SubmodelElementCollection;
                        createSMECItems(smeItem, smecNext, i);
                    }
                    if (sme is Operation)
                    {
                        var o = sme as Operation;
                        createOperationItems(smeItem, o, i);
                    }
                    if (sme is Entity)
                    {
                        var e = sme as Entity;
                        createEntityItems(smeItem, e, i);
                    }
                    if (sme is AnnotatedRelationshipElement annotatedRelationshipElement)
                    {
                        CreateAnnotedRelationshipElementItems(smeItem, annotatedRelationshipElement, i);
                    }
                    if (sme is SubmodelElementList smeList)
                    {
                        CreateSMEListItems(smeItem, smeList, i);
                    }
                }
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        void createOperationItems(Item smeRootItem, Operation op, int i)
        {
            List<Item> smChilds = new List<Item>();
            if (op.InputVariables != null)
            {
                foreach (var v in op.InputVariables)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = v.Value.IdShort;
                    smeItem.Type = "In";
                    smeItem.Tag = v.Value;
                    smChilds.Add(smeItem);
                }
            }
            if (op.OutputVariables != null)
            {
                foreach (var v in op.OutputVariables)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = v.Value.IdShort;
                    smeItem.Type = "Out";
                    smeItem.Tag = v.Value;
                    smChilds.Add(smeItem);
                }
            }
            if (op.InoutputVariables != null)
            {
                foreach (var v in op.InoutputVariables)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = v.Value.IdShort;
                    smeItem.Type = "InOut";
                    smeItem.Tag = v.Value;
                    smChilds.Add(smeItem);
                }
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        void createEntityItems(Item smeRootItem, Entity e, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var s in e.Statements)
            {
                if (s != null && s != null)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = s.IdShort;
                    smeItem.Type = "In";
                    smeItem.Tag = s;
                    smChilds.Add(smeItem);
                    if (s is SubmodelElementCollection collection)
                    {
                        createSMECItems(smeItem, collection, i);
                    }

                    if (s is SubmodelElementList smeList)
                    {
                        CreateSMEListItems(smeItem, smeList, i);
                    }
                }
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        public List<ISubmodel> GetSubmodels()
        {
            return Program.env[0].AasEnv.Submodels;
        }
    }
}
