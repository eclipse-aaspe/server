/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using AasxServer;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static AasxServerBlazor.Pages.TreePage;

namespace AasxServerBlazor.Data;

public class AASService
{
    public AASService()
    {
        Program.NewDataAvailable += (s, a) => { NewDataAvailable?.Invoke(this, a); };
    }

    public event EventHandler NewDataAvailable;

    public List<Item> items;
    public List<Item> viewItems;

    public List<Item> GetTree(Item selectedNode, IList<Item> ExpandedNodes)
    {
        return viewItems;
    }

    public void SyncSubTree(Item item)
    {
        if (item.Tag is not SubmodelElementCollection smec) return;
        if (item.Children.Count() != smec.Value.Count)
        {
            createSMECItems(item, smec, item.envIndex);
        }
    }

    public void BuildTree()
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
                    var demo = new Item
                    {
                        envIndex = -1,
                        Text = fname2,
                        Tag = "README"
                    };
                    items.Add(demo);
                }
            }

            for (var i = 0; i < Program.envimax; i++)
            {
                if (Program.env[i] != null && Program.env[i].AasEnv.AssetAdministrationShells != null && Program.env[i].AasEnv.AssetAdministrationShells.Count > 0)
                {
                    foreach (var aas in Program.env[i].AasEnv.AssetAdministrationShells)
                    {
                        var root = new Item
                        {
                            envIndex = i
                        };
                        root.Text = aas.IdShort;
                        root.Tag = aas;
                        if (Program.envSymbols[i] != "L")
                        {
                            var children = new List<Item>();
                            var env = Program.env[i];
                            //var aas = env.AasEnv.AssetAdministrationShells[0];
                            if (env != null && aas.Submodels is { Count: > 0 })
                                foreach (var smr in aas.Submodels)
                                {
                                    var sm = env.AasEnv.FindSubmodel(smr);
                                    if (sm is not { IdShort: not null })
                                    {
                                        continue;
                                    }

                                    var smItem = new Item
                                    {
                                        envIndex = i,
                                        Text = sm.IdShort,
                                        Tag = sm
                                    };
                                    children.Add(smItem);
                                    var smItemChildren = new List<Item>();
                                    if (sm.SubmodelElements != null)
                                        foreach (var sme in sm.SubmodelElements)
                                        {
                                            var smeItem = new Item
                                            {
                                                envIndex = i,
                                                Text = sme.IdShort,
                                                Tag = sme
                                            };
                                            smItemChildren.Add(smeItem);
                                            switch (sme)
                                            {
                                                case SubmodelElementCollection collection:
                                                {
                                                    createSMECItems(smeItem, collection, i);
                                                    break;
                                                }
                                                case Operation operation:
                                                {
                                                    createOperationItems(smeItem, operation, i);
                                                    break;
                                                }
                                                case Entity entity:
                                                {
                                                    CreateEntityItems(smeItem, entity, i);
                                                    break;
                                                }
                                                case AnnotatedRelationshipElement annotatedRelationshipElement:
                                                    CreateAnnotatedRelationshipElementItems(smeItem, annotatedRelationshipElement, i);
                                                    break;
                                                case SubmodelElementList smeList:
                                                    CreateSMEListItems(smeItem, smeList, i);
                                                    break;
                                            }
                                        }

                                    smItem.Children = smItemChildren;
                                    foreach (var c in smItemChildren)
                                        c.parent = smItem;
                                }

                            root.Children = children;
                            foreach (var c in children)
                                c.parent = root;
                            items.Add(root);
                        }

                        if (Program.envSymbols[i] != "L")
                        {
                            continue;
                        }

                        root.Text = Path.GetFileName(Program.envFileName[i]);
                        items.Add(root);
                    }
                }
            }
        }

        viewItems = items;
    }

    private void CreateSMEListItems(Item smeRootItem, ISubmodelElementList smeList, int i)
    {
        if (smeList == null || smeList.Value.IsNullOrEmpty())
        {
            return;
        }

        var smChilds = new List<Item>();
        foreach (var s in smeList.Value)
        {
            var smeItem = new Item
            {
                envIndex = i,
                Text = s.IdShort,
                Tag = s
            };
            smChilds.Add(smeItem);
            switch (s)
            {
                case SubmodelElementCollection collection:
                {
                    createSMECItems(smeItem, collection, i);
                    break;
                }
                case Operation operation:
                {
                    createOperationItems(smeItem, operation, i);
                    break;
                }
                case Entity entity:
                {
                    CreateEntityItems(smeItem, entity, i);
                    break;
                }
                case AnnotatedRelationshipElement annotatedRelationshipElement:
                    CreateAnnotatedRelationshipElementItems(smeItem, annotatedRelationshipElement, i);
                    break;
                case SubmodelElementList childSmeList:
                    CreateSMEListItems(smeItem, childSmeList, i);
                    break;
            }
        }

        smeRootItem.Children = smChilds;
        foreach (var c in smChilds)
            c.parent = smeRootItem;
    }

    private void CreateAnnotatedRelationshipElementItems(Item smeRootItem, IAnnotatedRelationshipElement annotatedRelationshipElement, int i)
    {
        if (annotatedRelationshipElement == null || annotatedRelationshipElement.Annotations.IsNullOrEmpty())
        {
            return;
        }

        var smChilds = new List<Item>();
        foreach (var s in annotatedRelationshipElement.Annotations)
        {
            {
                var smeItem = new Item
                {
                    envIndex = i,
                    Text = s.IdShort,
                    Tag = s
                };
                smChilds.Add(smeItem);
            }
        }

        smeRootItem.Children = smChilds;
        foreach (var c in smChilds)
            c.parent = smeRootItem;
    }

    void createSMECItems(Item smeRootItem, ISubmodelElementCollection submodelElementCollection, int i)
    {
        if (submodelElementCollection == null || submodelElementCollection.Value.IsNullOrEmpty())
        {
            return;
        }

        var smChildren = new List<Item>();
        if (submodelElementCollection.Value != null)
            foreach (var sme in submodelElementCollection.Value)
            {
                {
                    var smeItem = new Item
                    {
                        envIndex = i,
                        Text = sme.IdShort,
                        Tag = sme
                    };
                    smChildren.Add(smeItem);

                    switch (sme)
                    {
                        case SubmodelElementCollection collection:
                        {
                            createSMECItems(smeItem, collection, i);
                            break;
                        }
                        case Operation operation:
                        {
                            createOperationItems(smeItem, operation, i);
                            break;
                        }
                        case Entity entity:
                        {
                            CreateEntityItems(smeItem, entity, i);
                            break;
                        }
                        case AnnotatedRelationshipElement annotatedRelationshipElement:
                            CreateAnnotatedRelationshipElementItems(smeItem, annotatedRelationshipElement, i);
                            break;
                        case SubmodelElementList smeList:
                            CreateSMEListItems(smeItem, smeList, i);
                            break;
                    }
                }
            }

        smeRootItem.Children = smChildren;
        foreach (var c in smChildren)
            c.parent = smeRootItem;
    }

    void createOperationItems(Item smeRootItem, IOperation op, int i)
    {
        var smChildren = new List<Item>();
        if (!op.InputVariables.IsNullOrEmpty() && op.InputVariables != null)
        {
            smChildren.AddRange(op.InputVariables.Select(v => new Item {envIndex = i, Text = v.Value.IdShort, Type = "In", Tag = v.Value}));
        }

        if (!op.OutputVariables.IsNullOrEmpty())
        {
            smChildren.AddRange(op.OutputVariables.Select(v => new Item {envIndex = i, Text = v.Value.IdShort, Type = "Out", Tag = v.Value}));
        }

        if (!op.InoutputVariables.IsNullOrEmpty())
        {
            smChildren.AddRange(op.InoutputVariables.Select(v => new Item {envIndex = i, Text = v.Value.IdShort, Type = "InOut", Tag = v.Value}));
        }

        smeRootItem.Children = smChildren;
        foreach (var c in smChildren)
        {
            c.parent = smeRootItem;
            switch (c.Tag)
            {
                case SubmodelElementCollection collection:
                {
                    createSMECItems(c, collection, i);
                    break;
                }
                case Entity entity:
                {
                    CreateEntityItems(c, entity, i);
                    break;
                }
                case AnnotatedRelationshipElement annotatedRelationshipElement:
                    CreateAnnotatedRelationshipElementItems(c, annotatedRelationshipElement, i);
                    break;
                case SubmodelElementList smeList:
                    CreateSMEListItems(c, smeList, i);
                    break;
            }
        }
    }

    void CreateEntityItems(Item smeRootItem, IEntity e, int i)
    {
        var smChildren = new List<Item>();
        if (e.Statements != null && e.Statements.Count > 0)
        {
            foreach (var s in e.Statements)
            {
                var smeItem = new Item
                {
                    envIndex = i,
                    Text = s.IdShort,
                    Tag = s
                };
                smChildren.Add(smeItem);
                switch (s)
                {
                    case SubmodelElementCollection collection:
                        createSMECItems(smeItem, collection, i);
                        break;
                    case SubmodelElementList smeList:
                        CreateSMEListItems(smeItem, smeList, i);
                        break;
                }
            }
        }

        smeRootItem.Children = smChildren;
        foreach (var c in smChildren)
            c.parent = smeRootItem;
    }

    public List<ISubmodel> GetSubmodels()
    {
        return Program.env[0].AasEnv.Submodels;
    }
}