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

namespace AasxServerDB
{
    using System.Collections.Generic;
    using System.IO.Packaging;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Text;
    using AasCore.Aas3_0;
    using AasxServerDB.Entities;
    using AasxServerDB.Result;
    using AdminShellNS;
    using Contracts.Pagination;
    using Extensions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Tokens;
    using TimeStamp;
    using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


    public class Converter
    {
        public static List<string> GetFilteredPackages(string filterPath, List<AdminShellPackageEnv> list)
        {
            var paths = new List<string>();
            var db = new AasContext();

            var envList = db.EnvSets.Where(e => e.Path.Contains(filterPath));

            foreach (var env in envList)
            {
                var p = GetPackageEnv(env.Id);
                if (p != null)
                {
                    list.Add(p);
                    paths.Add(env.Path);
                }
            }

            return paths;
        }

        public static AdminShellPackageEnv? GetPackageEnv(string aasID, out string envFileName)
        {
            var db = new AasContext();
            envFileName = ";";

            var aasDBList = db.AASSets.Where(aas => aas.Identifier == aasID).ToList();

            if (aasDBList.Count != 1)
            {
                return null;
            }

            envFileName = GetAASXPath(aasDBList[0].EnvId);

            return GetPackageEnv(aasDBList[0].EnvId);
        }
        public static AdminShellPackageEnv? GetPackageEnv(int envId)
        {
            var timeStamp = DateTime.UtcNow;

            // env
            var env = new AdminShellPackageEnv();
            env.AasEnv.ConceptDescriptions = new List<IConceptDescription>();
            env.AasEnv.AssetAdministrationShells = new List<IAssetAdministrationShell>();
            env.AasEnv.Submodels = new List<ISubmodel>();

            // db
            var db = new AasContext();

            // path
            env.SetFilename(fileName: GetAASXPath(envId));

            // cd
            var cdDBList = db.EnvCDSets.Where(envcd => envcd.EnvId == envId).Join(db.CDSets, envcd => envcd.CDId, cd => cd.Id, (envcd, cd) => cd).ToList();
            foreach (var cd in cdDBList.Select(selector: cdDB => GetConceptDescription(cdDB: cdDB)))
            {
                env.AasEnv.ConceptDescriptions?.Add(cd);
            }

            // aas
            var aasDBList = db.AASSets.Where(cd => cd.EnvId == envId).ToList();
            foreach (var aasDB in aasDBList)
            {
                var aas = GetAssetAdministrationShell(aasDB: aasDB);
                if (aas.TimeStamp == DateTime.MinValue)
                {
                    aas.TimeStampCreate = timeStamp;
                    aas.SetTimeStamp(timeStamp);
                }
                env.AasEnv.AssetAdministrationShells?.Add(aas);

                // sm
                var smAASDBList = db.SMSets.Where(sm => sm.EnvId == envId && sm.AASId == aasDB.Id).ToList();
                foreach (var sm in smAASDBList.Select(selector: smDB => GetSubmodel(smDB: smDB)))
                {
                    aas.Submodels?.Add(sm.GetReference());
                }
            }

            // sm
            var smDBList = db.SMSets.Where(cd => cd.EnvId == envId).ToList();
            foreach (var sm in smDBList.Select(selector: submodelDB => GetSubmodel(smDB: submodelDB)))
            {
                if (sm.TimeStamp == DateTime.MinValue)
                {
                    sm.SetAllParentsAndTimestamps(null, timeStamp, timeStamp, DateTime.MinValue);
                    sm.SetTimeStamp(timeStamp);
                }
                env.AasEnv.Submodels?.Add(sm);
            }
            return env;
        }

        public static List<IAssetAdministrationShell> GetPagedAssetAdministrationShells(IPaginationParameters paginationParameters, List<ISpecificAssetId> assetIds, string idShort)
        {
            List<IAssetAdministrationShell> output = new List<IAssetAdministrationShell>();

            using (var db = new AasContext())
            {
                var timeStamp = DateTime.UtcNow;

                var aasDBList = db.AASSets
                    .Where(aas => idShort == null || aas.IdShort == idShort)
                    .OrderBy(aas => aas.Id)
                    .Skip(paginationParameters.Cursor)
                    .Take(paginationParameters.Limit)
                    .ToList();

                var aasIDs = aasDBList.Select(aas => aas.Id).ToList();
                var smDBList = db.SMSets.Where(sm => sm.AASId != null && aasIDs.Contains((int)sm.AASId)).ToList();

                foreach (var aasDB in aasDBList)
                {
                    var aas = GetAssetAdministrationShell(aasDB: aasDB);
                    if (aas.TimeStamp == DateTime.MinValue)
                    {
                        aas.TimeStampCreate = timeStamp;
                        aas.SetTimeStamp(timeStamp);
                    }

                    // sm
                    foreach (var sm in smDBList.Where(sm => sm.AASId == aasDB.Id))
                    {
                        aas.Submodels?.Add(new Reference(type: ReferenceTypes.ModelReference,
                            keys: new List<IKey>() { new Key(KeyTypes.Submodel, sm.Identifier) }
                        ));
                    }

                    output.Add(aas);
                }
            }

            return output;
        }

        public static List<ISubmodelElement>? GetPagedSubmodelElements(IPaginationParameters paginationParameters, string securityConditionSM, string securityConditionSME, string aasIdentifier, string submodelIdentifier)
        {
            bool result = false;
            var output = new List<ISubmodelElement>();

            using (var db = new AasContext())
            {
                var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

                if (!aasIdentifier.IsNullOrEmpty())
                {
                    var aasDB = db.AASSets
                        .Where(aas => aas.Identifier == aasIdentifier).ToList();
                    if (aasDB == null || aasDB.Count != 1)
                    {
                        return null;
                    }
                    var aasDBId = aasDB[0].Id;
                    smDBQuery = smDBQuery.Where(sm => sm.AASId == aasDBId);
                }

                if (!securityConditionSM.IsNullOrEmpty())
                {
                    smDBQuery = smDBQuery.Where(securityConditionSM);
                }
                var smDB = smDBQuery.ToList();
                if (smDB == null || smDB.Count != 1)
                {
                    return null;
                }
                var smDBId = smDB[0].Id;

                var smeSmTopQuery = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == null);
                if (securityConditionSME != "")
                {
                    smeSmTopQuery = smeSmTopQuery.Where(securityConditionSME);
                }
                var smeSmTop = smeSmTopQuery
                    .OrderBy(sme => sme.Id).Skip(paginationParameters.Cursor).Take(paginationParameters.Limit).ToList();
                var smeSmTopTree = Converter.GetTree(db, smDB[0], smeSmTop);
                var smeSmTopMerged = Converter.GetSmeMerged(db, smeSmTopTree);

                foreach (var smeDB in smeSmTop)
                {
                    var sme = Converter.GetSubmodelElement(smeDB, smeSmTopMerged);
                    if (sme != null)
                    {
                        output.Add(sme);
                    }
                }
                return output;
            }
        }

        public static ISubmodelElement? GetSubmodelElementByPath(string securityConditionSM, string securityConditionSME, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements)
        {
            bool result = false;

            using (var db = new AasContext())
            {
                var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

                if (!aasIdentifier.IsNullOrEmpty())
                {
                    var aasDB = db.AASSets
                        .Where(aas => aas.Identifier == aasIdentifier).ToList();
                    if (aasDB == null || aasDB.Count != 1)
                    {
                        return null;
                    }
                    var aasDBId = aasDB[0].Id;
                    smDBQuery = smDBQuery.Where(sm => sm.AASId == aasDBId);
                }

                if (!securityConditionSM.IsNullOrEmpty())
                {
                    smDBQuery = smDBQuery.Where(securityConditionSM);
                }
                var smDB = smDBQuery.ToList();
                if (smDB == null || smDB.Count != 1)
                {
                    return null;
                }
                var smDBId = smDB[0].Id;


                if (idShortPathElements.Count == 0)
                {
                    return null;
                }
                var idShort = idShortPathElements[0];
                var smeParent = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == null && sme.IdShort == idShort).ToList();
                if (smeParent == null || smeParent.Count != 1)
                {
                    return null;
                }
                var parentId = smeParent[0].Id;
                var smeFound = smeParent;

                for (int i = 1; i < idShortPathElements.Count; i++)
                {
                    idShort = idShortPathElements[i];
                    //ToDo SubmodelElementList with index (type: int) must be implemented
                    var smeFoundDB = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == parentId && sme.IdShort == idShort);
                    smeFound = smeFoundDB.ToList();
                    if (smeFound == null || smeFound.Count != 1)
                    {
                        return null;
                    }
                    parentId = smeFound[0].Id;
                }

                var smeFoundTree = Converter.GetTree(db, smDB[0], smeFound);
                var smeFoundMerged = Converter.GetSmeMerged(db, smeFoundTree);

                var sme = Converter.GetSubmodelElement(smeFound[0], smeFoundMerged);

                return sme;
            }

            return null;
        }


        public static List<ISubmodel> GetSubmodels(IPaginationParameters paginationParameters, string securityConditionSM, string securityConditionSME, Reference? reqSemanticId, string idShort)
        {
            List<ISubmodel> output = new List<ISubmodel>();

            using (var db = new AasContext())
            {
                var timeStamp = DateTime.UtcNow;

                var smDBList = db.SMSets
                    .Where(sm => idShort == null || sm.IdShort == idShort)
                    .OrderBy(aas => aas.Id)
                    .Skip(paginationParameters.Cursor)
                    .Take(paginationParameters.Limit)
                    .ToList();

                //ToDo: Verify whether this is correct
                foreach (var sm in smDBList.Select(selector: submodelDB => GetSubmodel(smDB: submodelDB)))
                {
                    if (sm.TimeStamp == DateTime.MinValue)
                    {
                        sm.SetAllParentsAndTimestamps(null, timeStamp, timeStamp, DateTime.MinValue);
                        sm.SetTimeStamp(timeStamp);
                    }
                    output.Add(sm);
                }
            }

            return output;
        }

        private static ConceptDescription? GetConceptDescription(CDSet? cdDB = null, string cdIdentifier = "")
        {
            var db = new AasContext();
            if (!cdIdentifier.IsNullOrEmpty())
            {
                var cdList = db.CDSets.Where(cd => cd.Identifier == cdIdentifier).ToList();
                if (cdList.Count == 0)
                    return null;
                cdDB = cdList.First();
            }

            if (cdDB == null)
                return null;

            var cd = new ConceptDescription(
                idShort: cdDB.IdShort,
                displayName: Serializer.DeserializeList<ILangStringNameType>(cdDB.DisplayName),
                category: cdDB.Category,
                description: Serializer.DeserializeList<ILangStringTextType>(cdDB.Description),
                extensions: Serializer.DeserializeList<IExtension>(cdDB.Extensions),
                id: cdDB.Identifier,
                isCaseOf: Serializer.DeserializeList<IReference>(cdDB.IsCaseOf),
                embeddedDataSpecifications: Serializer.DeserializeList<IEmbeddedDataSpecification>(cdDB.EmbeddedDataSpecifications),
                administration: new AdministrativeInformation(
                    version: cdDB.Version,
                    revision: cdDB.Revision,
                    creator: Serializer.DeserializeElement<IReference>(cdDB.Creator),
                    templateId: cdDB.TemplateId,
                    embeddedDataSpecifications: Serializer.DeserializeList<IEmbeddedDataSpecification>(cdDB.AEmbeddedDataSpecifications)
                )
            )
            {
                TimeStampCreate = cdDB.TimeStampCreate,
                TimeStamp = cdDB.TimeStamp,
                TimeStampTree = cdDB.TimeStampTree,
                TimeStampDelete = cdDB.TimeStampDelete
            };

            return cd;
        }

        public static AssetAdministrationShell? GetAssetAdministrationShell(AASSet? aasDB = null, string aasIdentifier = "")
        {
            var db = new AasContext();
            if (!aasIdentifier.IsNullOrEmpty())
            {
                var aasList = db.AASSets.Where(cd => cd.Identifier == aasIdentifier).ToList();
                if (aasList.Count == 0)
                    return null;
                aasDB = aasList.First();
            }

            if (aasDB == null)
                return null;

            var aas = new AssetAdministrationShell(
                idShort: aasDB.IdShort,
                displayName: Serializer.DeserializeList<ILangStringNameType>(aasDB.DisplayName),
                category: aasDB.Category,
                description: Serializer.DeserializeList<ILangStringTextType>(aasDB.Description),
                extensions: Serializer.DeserializeList<IExtension>(aasDB.Extensions),
                id: aasDB.Identifier,
                embeddedDataSpecifications: Serializer.DeserializeList<IEmbeddedDataSpecification>(aasDB.EmbeddedDataSpecifications),
                derivedFrom: Serializer.DeserializeElement<IReference>(aasDB.DerivedFrom),
                submodels: new List<IReference>(),
                administration: new AdministrativeInformation(
                    version: aasDB.Version,
                    revision: aasDB.Revision,
                    creator: Serializer.DeserializeElement<IReference>(aasDB.Creator),
                    templateId: aasDB.TemplateId,
                    embeddedDataSpecifications: Serializer.DeserializeList<IEmbeddedDataSpecification>(aasDB.AEmbeddedDataSpecifications)
                ),
                assetInformation: new AssetInformation(
                    assetKind: Serializer.DeserializeElement<AssetKind>(aasDB.AssetKind),
                    specificAssetIds: Serializer.DeserializeList<ISpecificAssetId>(aasDB.SpecificAssetIds),
                    globalAssetId: aasDB.GlobalAssetId,
                    assetType: aasDB.AssetType,
                    defaultThumbnail: new Resource(
                        path: aasDB.DefaultThumbnailPath,
                        contentType: aasDB.DefaultThumbnailContentType
                    )
                )
            )
            {
                TimeStampCreate = aasDB.TimeStampCreate,
                TimeStamp = aasDB.TimeStamp,
                TimeStampTree = aasDB.TimeStampTree,
                TimeStampDelete = aasDB.TimeStampDelete
            };

            return aas;
        }

        public static Submodel? GetSubmodel(SMSet? smDB = null, string submodelIdentifier = "", string securityConditionSM = "", string securityConditionSME = "")
        {
            using (var db = new AasContext())
            {
                if (!submodelIdentifier.IsNullOrEmpty())
                {
                    var smDBQuery = db.SMSets
                            .Where(sm => sm.Identifier == submodelIdentifier);
                    if (securityConditionSM != "")
                    {
                        smDBQuery = smDBQuery.Where(securityConditionSM);
                    }

                    var smDBList = smDBQuery.ToList();
                    if (smDBList != null && smDBList.Count > 0)
                    {
                        smDB = smDBList.First();
                    }
                }

                if (smDB == null)
                    return null;

                var SMEQuery = db.SMESets
                    .OrderBy(sme => sme.Id)
                    .Where(sme => sme.SMId == smDB.Id);

                var submodel = new Submodel(
                    idShort: smDB.IdShort,
                    displayName: Serializer.DeserializeList<ILangStringNameType>(smDB.DisplayName),
                    category: smDB.Category,
                    description: Serializer.DeserializeList<ILangStringTextType>(smDB.Description),
                    extensions: Serializer.DeserializeList<IExtension>(smDB.Extensions),
                    id: smDB.Identifier,
                    kind: Serializer.DeserializeElement<ModellingKind>(smDB.Kind),
                    semanticId: !smDB.SemanticId.IsNullOrEmpty() ? new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, smDB.SemanticId) }) : null,
                    supplementalSemanticIds: Serializer.DeserializeList<IReference>(smDB.SupplementalSemanticIds),
                    qualifiers: Serializer.DeserializeList<IQualifier>(smDB.Qualifiers),
                    embeddedDataSpecifications: Serializer.DeserializeList<IEmbeddedDataSpecification>(smDB.EmbeddedDataSpecifications),
                    administration: new AdministrativeInformation(
                        version: smDB.Version,
                        revision: smDB.Revision,
                        creator: Serializer.DeserializeElement<IReference>(smDB.Creator),
                        templateId: smDB.TemplateId,
                        embeddedDataSpecifications: Serializer.DeserializeList<IEmbeddedDataSpecification>(smDB.AEmbeddedDataSpecifications)
                    ),
                    submodelElements: new List<ISubmodelElement>()
                );

                // LoadSME(submodel, null, null, SMEList);
                var smeMerged = Converter.GetSmeMerged(db, SMEQuery);
                var SMEList = SMEQuery.ToList();
                LoadSME(submodel, null, null, SMEList, smeMerged);

                submodel.TimeStampCreate = smDB.TimeStampCreate;
                submodel.TimeStamp = smDB.TimeStamp;
                submodel.TimeStampTree = smDB.TimeStampTree;
                submodel.TimeStampDelete = smDB.TimeStampDelete;
                submodel.SetAllParents();

                return submodel;
            }
        }

        private static void LoadSME(Submodel submodel, ISubmodelElement? sme, SMESet? smeSet, List<SMESet> SMEList, List<SmeMerged> tree = null)
        {
            var smeSets = SMEList;
            if (tree != null)
            {
                smeSets = tree.Select(t => t.smeSet).Distinct().ToList();
            }
            smeSets = smeSets.Where(s => s.ParentSMEId == (smeSet != null ? smeSet.Id : null)).OrderBy(s => s.IdShort).ToList();

            foreach (var smel in smeSets)
            {
                // prefix of operation
                var split = !smel.SMEType.IsNullOrEmpty() ? smel.SMEType.Split(VisitorAASX.OPERATION_SPLIT) : [string.Empty];
                var oprPrefix = split.Length == 2 ? split[0] : string.Empty;
                smel.SMEType = split.Length == 2 ? split[1] : split[0];

                // create SME from database
                var nextSME = CreateSME(smel, tree);

                // add sme to sm or sme 
                if (sme == null)
                {
                    submodel.Add(nextSME);
                }
                else
                {
                    switch (smeSet.SMEType)
                    {
                        case "RelA":
                            (sme as AnnotatedRelationshipElement).Annotations.Add((IDataElement)nextSME);
                            break;
                        case "SML":
                            (sme as SubmodelElementList).Value.Add(nextSME);
                            break;
                        case "SMC":
                            (sme as SubmodelElementCollection).Value.Add(nextSME);
                            break;
                        case "Ent":
                            (sme as Entity).Statements.Add(nextSME);
                            break;
                        case "Opr":
                            if (oprPrefix.Equals(VisitorAASX.OPERATION_INPUT))
                                (sme as Operation).InputVariables.Add(new OperationVariable(nextSME));
                            else if (oprPrefix.Equals(VisitorAASX.OPERATION_OUTPUT))
                                (sme as Operation).OutputVariables.Add(new OperationVariable(nextSME));
                            else if (oprPrefix.Equals(VisitorAASX.OPERATION_INOUTPUT))
                                (sme as Operation).InoutputVariables.Add(new OperationVariable(nextSME));
                            break;
                    }
                }

                // recursiv, call for child sme's
                switch (smel.SMEType)
                {
                    case "RelA":
                    case "SML":
                    case "SMC":
                    case "Ent":
                    case "Opr":
                        LoadSME(submodel, nextSME, smel, SMEList, tree);
                        break;
                }
            }
        }

        public static List<SMESet>? GetTree(AasContext db, SMSet smSet, List<SMESet> rootSet)
        {
            var result = new List<SMESet>();
            if (rootSet == null)
            {
                result = db.SMESets.Where(sme => sme.SMId == smSet.Id).ToList();
                return result;
            }
            else
            {
                result.AddRange(rootSet);
                // Add all children SME to result
                List<int> parentIDs = rootSet.Select(sme => sme.Id).ToList();
                while (parentIDs.Count > 0)
                {
                    var smeSearch = db.SMESets.Where(sme => sme.SMId == smSet.Id && sme.ParentSMEId != null && parentIDs.Contains((int)sme.ParentSMEId)).ToList();
                    result.AddRange(smeSearch);
                    parentIDs = smeSearch.Select(sme => sme.Id).ToList();
                }
                return result;
            }
            return null;
        }

        public class smeREsult
        {
            public int Id { get; set; }
            public string? IdShortPath { get; set; }
            public int? ParentSMEId { get; set; }
        }

        public static void CreateIdShortPath1(AasContext db, List<SMESet> smeList)
        {
            /*
            // created idShortPath for result only
            var smeSearch = smeList.Select(sme => new smeREsult { Id = sme.Id, IdShortPath = sme.IdShort, ParentSMEId = sme.ParentSMEId }).ToList();
            var smeResult = smeSearch.Where(sme => sme.ParentSMEId == null).ToList();
            smeSearch = smeSearch.Where(sme => sme.ParentSMEId != null).ToList();
            while (smeSearch != null && smeSearch.Count != 0)
            {
                var parentIds = smeSearch.Where(sme => sme.ParentSMEId != null).Select(sme => sme.ParentSMEId).ToList();
                var smeWithIdShortPath = db.SMESets.Where(sme => parentIds.Contains(sme.Id))
                    .Select(sme => new smeREsult { Id = sme.Id, IdShortPath = sme.IdShort, ParentSMEId = sme.ParentSMEId }).ToList();
                if (smeParents != null && smeParents.Count != 0)
                {
                    smeResult.AddRange(smeParents.Where(sme => sme.ParentSMEId == null).ToList());
                    smeSearch = smeParents.Where(sme => sme.ParentSMEId != null).ToList();
                }
                else
                {
                    smeSearch = null;
                }
            };

            var smeResultDict = smeResult.ToDictionary(sme => sme.Id, sme => sme.IdShortPath);

            foreach (var sme in smeList)
            {
                if (smeResultDict.TryGetValue(sme.Id, out var idShortPath))
                {
                    sme.IdShortPath = idShortPath;
                }
            }
            */
        }

        public static void CreateIdShortPath(AasContext db, List<SMESet> smeList)
        {
            if (smeList == null)
            {
                return;
            }

            var smeIdList = smeList.Select(sme => sme.Id).ToList();
            var smeSearch = db.SMESets.Where(sme => smeIdList.Contains(sme.Id))
                .Select(sme => new smeREsult { Id = sme.Id, IdShortPath = sme.IdShort, ParentSMEId = sme.ParentSMEId });
            var smeResult = smeSearch.Where(sme => sme.ParentSMEId == null).ToList();
            smeSearch = smeSearch.Where(sme => sme.ParentSMEId != null);
            while (smeSearch != null && smeSearch.Any())
            {
                var joinedResult = smeSearch
                    .Join(db.SMESets,
                          sme => sme.ParentSMEId,
                          parentSme => parentSme.Id,
                          (sme, parentSme) => new smeREsult
                          {
                              Id = sme.Id,
                              IdShortPath = parentSme.IdShort + "." + sme.IdShortPath,
                              ParentSMEId = parentSme.ParentSMEId
                          });
                if (joinedResult != null && joinedResult.Any())
                {
                    smeResult.AddRange(joinedResult.Where(sme => sme.ParentSMEId == null).ToList());
                    smeSearch = joinedResult.Where(sme => sme.ParentSMEId != null);
                }
                else
                {
                    smeSearch = null;
                }
            };

            var smeResultDict = smeResult.ToDictionary(sme => sme.Id, sme => sme.IdShortPath);

            foreach (var sme in smeList)
            {
                if (smeResultDict.TryGetValue(sme.Id, out var idShortPath))
                {
                    sme.IdShortPath = idShortPath;
                }
            }
        }

        public class SmeMerged
        {
            public SMESet smeSet;
            public SValueSet? sValueSet;
            public IValueSet? iValueSet;
            public DValueSet? dValueSet;
            public OValueSet? oValueSet;
        }

        public static List<SmeMerged> GetSmeMerged(AasContext db, List<SMESet>? listSME)
        {
            if (listSME == null)
            {
                return null;
            }

            var smeIdList = listSME.Select(sme => sme.Id).ToList();
            var querySME = db.SMESets.Where(sme => smeIdList.Contains(sme.Id));

            return GetSmeMerged(db, querySME);
        }
        public static List<SmeMerged> GetSmeMerged(AasContext db, IQueryable<SMESet>? querySME)
        {
            if (querySME == null)
                return null;

            var joinSValue = querySME.Join(
                db.SValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new SmeMerged { smeSet = sme, sValueSet = sv, iValueSet = null, dValueSet = null, oValueSet = null })
                .ToList();

            var joinIValue = querySME.Join(
                db.IValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new SmeMerged { smeSet = sme, sValueSet = null, iValueSet = sv, dValueSet = null, oValueSet = null })
                .ToList();

            var joinDValue = querySME.Join(
                db.DValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new SmeMerged { smeSet = sme, sValueSet = null, iValueSet = null, dValueSet = sv, oValueSet = null })
                .ToList();

            var joinOValue = querySME.Join(
                db.OValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new SmeMerged { smeSet = sme, sValueSet = null, iValueSet = null, dValueSet = null, oValueSet = sv })
                .ToList();

            var result = joinSValue;
            result.AddRange(joinIValue);
            result.AddRange(joinDValue);
            result.AddRange(joinOValue);

            var smeIdList = result.Select(sme => sme.smeSet.Id).ToList();
            var noValue = querySME.Where(sme => !smeIdList.Contains(sme.Id))
                .Select(sme => new SmeMerged { smeSet = sme, sValueSet = null, iValueSet = null, dValueSet = null, oValueSet = null })
                .ToList();
            result.AddRange(noValue);

            return result;


            /*
            var joinSValue = querySME.GroupJoin(
                db.SValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new { sme, sv })
                .SelectMany(
                    x => x.sv.DefaultIfEmpty(),
                    (x, sv) => new SmeMerged { smeSet = x.sme, sValueSet = sv, iValueSet = null, dValueSet = null, oValueSet = null });
            var l1 = joinSValue.ToList();

            var joinIValue = querySME.GroupJoin(
                db.IValueSets,
                sme => sme.Id,
                iv => iv.SMEId,
                (sme, iv) => new { sme, iv })
                .SelectMany(
                    x => x.iv.DefaultIfEmpty(),
                    (x, iv) => new SmeMerged { smeSet = x.sme, sValueSet = null, iValueSet = iv, dValueSet = null, oValueSet = null });
            var l2 = joinIValue.ToList();

            var joinDValue = querySME.GroupJoin(
                db.DValueSets,
                sme => sme.Id,
                dv => dv.SMEId,
                (sme, dv) => new { sme, dv })
                .SelectMany(
                    x => x.dv.DefaultIfEmpty(),
                    (x, dv) => new SmeMerged { smeSet = x.sme, dValueSet = dv, sValueSet = null, iValueSet = null, oValueSet = null });
            var l3 = joinDValue.ToList();

            var joinOValue = querySME.GroupJoin(
                db.OValueSets,
                sme => sme.Id,
                ov => ov.SMEId,
                (sme, ov) => new { sme, ov })
                .SelectMany(
                    x => x.ov.DefaultIfEmpty(),
                    (x, ov) => new SmeMerged { smeSet = x.sme, oValueSet = ov, sValueSet = null, iValueSet = null, dValueSet = null });
            var l4 = joinOValue.ToList();

            var result = joinSValue
                .Union(joinIValue)
                .Union(joinDValue)
                .Union(joinOValue)
                .ToList();

            var joinSValue = querySME.GroupJoin(
                db.SValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new { sme, sv })
                .SelectMany(
                    x => x.sv.DefaultIfEmpty(),
                    (x, sv) => new { x.sme, sValueSet = sv });

            var joinIValue = querySME.GroupJoin(
                db.IValueSets,
                sme => sme.Id,
                iv => iv.SMEId,
                (sme, iv) => new { sme, iv })
                .SelectMany(
                    x => x.iv.DefaultIfEmpty(),
                    (x, iv) => new { x.sme, iValueSet = iv });

            var joinDValue = querySME.GroupJoin(
                db.DValueSets,
                sme => sme.Id,
                dv => dv.SMEId,
                (sme, dv) => new { sme, dv })
                .SelectMany(
                    x => x.dv.DefaultIfEmpty(),
                    (x, dv) => new { x.sme, dValueSet = dv });

            var joinOValue = querySME.GroupJoin(
                db.OValueSets,
                sme => sme.Id,
                ov => ov.SMEId,
                (sme, ov) => new { sme, ov })
                .SelectMany(
                    x => x.ov.DefaultIfEmpty(),
                    (x, ov) => new { x.sme, oValueSet = ov });

            var result = joinSValue.Select(x => new { x.sme, x.sValueSet, iValueSet = (IValueSet)null, dValueSet = (DValueSet)null, oValueSet = (OValueSet)null })
                .Union(joinIValue.Select(x => new { x.sme, sValueSet = (SValueSet)null, x.iValueSet, dValueSet = (DValueSet)null, oValueSet = (OValueSet)null }))
                .Union(joinDValue.Select(x => new { x.sme, sValueSet = (SValueSet)null, iValueSet = (IValueSet)null, x.dValueSet, oValueSet = (OValueSet)null }))
                .Union(joinOValue.Select(x => new { x.sme, sValueSet = (SValueSet)null, iValueSet = (IValueSet)null, dValueSet = (DValueSet)null, x.oValueSet }))
                .Select(x => new SmeMerged
                {
                    smeSet = x.sme,
                    sValueSet = x.sValueSet,
                    iValueSet = x.iValueSet,
                    dValueSet = x.dValueSet,
                    oValueSet = x.oValueSet
                })
                .ToList();
            */
        }
        public static List<SmeMerged> GetSmeMerged0(AasContext db, List<SMESet>? listSME)
        {
            if (listSME == null)
                return null;

            var join1 = listSME.Join(
                db.SValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new { sme, sv })
                .ToList();

            var join2 = listSME.Join(
                db.IValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new { sme, sv })
                .ToList();

            var join3 = listSME.Join(
                db.DValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new { sme, sv })
                .ToList();

            var join4 = listSME.Join(
                db.OValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new { sme, sv })
                .ToList();




            var joinSValue = listSME.GroupJoin(
                db.SValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new { sme, sv })
                .SelectMany(
                    x => x.sv.DefaultIfEmpty(),
                    (x, sv) => new SmeMerged { smeSet = x.sme, sValueSet = sv });
            var l1 = joinSValue.ToList();

            var joinIValue = listSME.GroupJoin(
                db.IValueSets,
                sme => sme.Id,
                iv => iv.SMEId,
                (sme, iv) => new { sme, iv })
                .SelectMany(
                    x => x.iv.DefaultIfEmpty(),
                    (x, iv) => new SmeMerged { smeSet = x.sme, iValueSet = iv });
            var l2 = joinIValue.ToList();

            var joinDValue = listSME.GroupJoin(
                db.DValueSets,
                sme => sme.Id,
                dv => dv.SMEId,
                (sme, dv) => new { sme, dv })
                .SelectMany(
                    x => x.dv.DefaultIfEmpty(),
                    (x, dv) => new SmeMerged { smeSet = x.sme, dValueSet = dv });
            var l3 = joinDValue.ToList();

            var joinOValue = listSME.GroupJoin(
                db.OValueSets,
                sme => sme.Id,
                ov => ov.SMEId,
                (sme, ov) => new { sme, ov })
                .SelectMany(
                    x => x.ov.DefaultIfEmpty(),
                    (x, ov) => new SmeMerged { smeSet = x.sme, oValueSet = ov });
            var l4 = joinOValue.ToList();

            var result = joinSValue
                .Union(joinIValue)
                .Union(joinDValue)
                .Union(joinOValue)
                .ToList();

            return result;
        }
        public static ISubmodelElement? GetSubmodelElement(SMESet smeSet)
        {
            return CreateSME(smeSet, null);
        }

        public static ISubmodelElement? GetSubmodelElement(SMESet smeSet, List<SmeMerged> tree)
        {
            var sme = CreateSME(smeSet, tree);
            LoadSME(null, sme, smeSet, null, tree);
            return sme;
        }
        public static void setTimeStamp(SMSet smDB, DateTime timeStamp)
        {
            if (smDB != null)
            {
                smDB.TimeStampCreate = timeStamp;
                smDB.TimeStamp = timeStamp;
                smDB.TimeStampTree = timeStamp;
                smDB.TimeStampDelete = DateTime.MinValue;
            }
        }
        public static void setTimeStamp(SMESet smeDB, DateTime timeStamp)
        {
            if (smeDB != null)
            {
                smeDB.TimeStampCreate = timeStamp;
                smeDB.TimeStamp = timeStamp;
                smeDB.TimeStampTree = timeStamp;
                smeDB.TimeStampDelete = DateTime.MinValue;
            }
        }
        public static void setTimeStampTree(AasContext db, SMSet smDB, SMESet smeDB, DateTime timeStamp)
        {
            if (smDB != null)
            {
                smDB.TimeStampTree = timeStamp;
            }
            if (smeDB != null)
            {
                while (smeDB != null && smeDB.ParentSMEId != null)
                {
                    smeDB = db.SMESets.Where(sme => sme.Id == smeDB.ParentSMEId).FirstOrDefault();
                    if (smeDB != null)
                    {
                        smeDB.TimeStampTree = timeStamp;
                    }
                }
            }
        }
        public static void setTimeStampValue(int smSetId, int smeSetId, DateTime timeStamp, string value = null)
        {
            using (var db = new AasContext())
            {
                var smDB = db.SMSets.Where(sm => sm.Id == smSetId).FirstOrDefault();

                var smeFound = db.SMESets.Where(sme => sme.SMId == smSetId && sme.Id == smeSetId).FirstOrDefault();
                if (smeFound != null)
                {
                    smeFound.TimeStamp = timeStamp;
                    smeFound.TimeStampTree = timeStamp;
                    if (value != null && smeFound.SMEType == "Prop")
                    {
                        var sValue = db.SValueSets.Where(v => v.SMEId == smeFound.Id).FirstOrDefault();
                        if (sValue != null)
                        {
                            sValue.Value = value;
                        }
                        else
                        {
                            var iValue = db.IValueSets.Where(v => v.SMEId == smeFound.Id).FirstOrDefault();
                            if (iValue != null)
                            {
                                iValue.Value = Convert.ToInt64(value);
                            }
                            else
                            {
                                var dValue = db.DValueSets.Where(v => v.SMEId == smeFound.Id).FirstOrDefault();
                                if (dValue != null)
                                {
                                    dValue.Value = Convert.ToDouble(value);
                                }
                            }
                        }
                    }
                    setTimeStampTree(db, smDB, smeFound, timeStamp);

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    };
                }
            }
        }
        public static void setTimeStampValue(string submodelId, string path, DateTime timeStamp, string value = null)
        {
            var idShortPathElements = path.Split('.');
            using (var db = new AasContext())
            {
                var smDB = db.SMSets.Where(sm => sm.Identifier == submodelId).FirstOrDefault();
                if (smDB == null || idShortPathElements.Length == 0)
                {
                    return;
                };

                var idShort = idShortPathElements[0];
                var smeParent = db.SMESets.Where(sme => sme.SMId == smDB.Id && sme.ParentSMEId == null && sme.IdShort == idShort).FirstOrDefault();
                if (smeParent == null)
                {
                    return;
                }
                var parentId = smeParent.Id;
                var smeFound = smeParent;

                for (int i = 1; i < idShortPathElements.Length; i++)
                {
                    idShort = idShortPathElements[i];
                    smeFound = db.SMESets.Where(sme => sme.SMId == smDB.Id && sme.ParentSMEId == parentId && sme.IdShort == idShort).FirstOrDefault();
                    if (smeFound == null)
                    {
                        return;
                    }
                    parentId = smeFound.Id;
                }

                setTimeStampValue(smeFound.SMId, smeFound.Id, timeStamp, value);
            };
        }

        public static List<string[]>? GetValue(SMESet smeSet, List<SValueSet> sValueList, List<IValueSet> iValueList, List<DValueSet> dValueList)
        {
            var TValue = smeSet.TValue;
            var SMEType = smeSet.SMEType;
            var Id = smeSet.Id;

            if (TValue == null)
                return [[string.Empty, string.Empty]];

            var list = new List<string[]>();
            switch (TValue)
            {
                case "S":
                    list = sValueList
                        .ConvertAll<string[]>(s => [s.Value ?? string.Empty, s.Annotation ?? string.Empty]);
                    break;
                case "I":
                    list = iValueList
                        .ConvertAll<string[]>(s => [s.Value == null ? string.Empty : s.Value.ToString(), s.Annotation ?? string.Empty]);
                    break;
                case "D":
                    list = dValueList
                        .ConvertAll<string[]>(s => [s.Value == null ? string.Empty : s.Value.ToString(), s.Annotation ?? string.Empty]);
                    break;
            }
            if (list.Count > 0 || (!SMEType.IsNullOrEmpty() && SMEType.Equals("MLP")))
                return list;

            return [[string.Empty, string.Empty]];
        }
        public static List<string[]>? GetValue(SMESet smeSet, List<SmeMerged> tree)
        {
            var TValue = smeSet.TValue;
            var SMEType = smeSet.SMEType;
            var Id = smeSet.Id;

            if (TValue == null)
                return [[string.Empty, string.Empty]];

            var list = new List<string[]>();
            switch (TValue)
            {
                case "S":
                    list = tree.Where(s => s.sValueSet?.SMEId == Id).ToList()
                        .ConvertAll<string[]>(s => [s.sValueSet.Value ?? string.Empty, s.sValueSet.Annotation ?? string.Empty]);
                    break;
                case "I":
                    list = tree.Where(s => s.iValueSet?.SMEId == Id).ToList()
                        .ConvertAll<string[]>(s => [s.iValueSet.Value == null ? string.Empty : s.iValueSet.Value.ToString(), s.iValueSet.Annotation ?? string.Empty]);
                    break;
                case "D":
                    list = tree.Where(s => s.dValueSet?.SMEId == Id).ToList()
                        .ConvertAll<string[]>(s => [s.dValueSet.Value == null ? string.Empty : s.dValueSet.Value.ToString(), s.dValueSet.Annotation ?? string.Empty]);
                    break;
            }
            if (list.Count > 0 || (!SMEType.IsNullOrEmpty() && SMEType.Equals("MLP")))
                return list;

            return [[string.Empty, string.Empty]];
        }

        public static Dictionary<string, string> GetOValue(SMESet smeSet, List<SmeMerged> tree)
        {
            var Id = smeSet.Id;

            var dic = tree.Where(s => s.oValueSet?.SMEId == Id).ToList().ToDictionary(s => s.oValueSet.Attribute, s => s.oValueSet.Value);
            if (dic != null)
            {
                return dic;
            }
            return new Dictionary<string, string>();
        }
        private static ISubmodelElement? CreateSME(SMESet smeSet, List<SmeMerged> tree = null)
        {
            ISubmodelElement? sme = null;

            var value = new List<string[]>();
            var oValue = new Dictionary<string, string>();
            if (tree == null)
            {
                value = smeSet.GetValue();
                oValue = smeSet.GetOValue();
            }
            else
            {
                value = GetValue(smeSet, tree);
                oValue = GetOValue(smeSet, tree);
            }

            switch (smeSet.SMEType)
            {
                case "Rel":
                    sme = new RelationshipElement(
                        first: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("First") ? oValue["First"] : null, true),
                        second: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("Second") ? oValue["Second"] : null, true));
                    break;
                case "RelA":
                    sme = new AnnotatedRelationshipElement(
                        first: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("First") ? oValue["First"] : null, true),
                        second: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("Second") ? oValue["Second"] : null, true),
                        annotations: new List<IDataElement>());
                    break;
                case "Prop":
                    sme = new Property(
                        value: value.First()[0],
                        valueType: Serializer.DeserializeElement<DataTypeDefXsd>(value.First()[1], true),
                        valueId: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("ValueId") ? oValue["ValueId"] : null));
                    break;
                case "MLP":
                    sme = new MultiLanguageProperty(
                        value: value.ConvertAll<ILangStringTextType>(val => new LangStringTextType(val[1], val[0])),
                        valueId: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("ValueId") ? oValue["ValueId"] : null));
                    break;
                case "Range":
                    sme = new AasCore.Aas3_0.Range(
                        valueType: Serializer.DeserializeElement<DataTypeDefXsd>(oValue["ValueType"], true),
                        min: value.Find(val => val[1].Equals("Min")).FirstOrDefault(string.Empty),
                        max: value.Find(val => val[1].Equals("Max")).FirstOrDefault(string.Empty));
                    break;
                case "Blob":
                    sme = new Blob(
                        value: Encoding.ASCII.GetBytes(value.First()[0]),
                        contentType: value.First()[1]);
                    break;
                case "File":
                    sme = new AasCore.Aas3_0.File(
                        value: value.First()[0],
                        contentType: value.First()[1]);
                    break;
                case "Ref":
                    sme = new ReferenceElement(
                        value: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("Value") ? oValue["Value"] : null));
                    break;
                case "Cap":
                    sme = new Capability();
                    break;
                case "SML":
                    sme = new SubmodelElementList(
                        orderRelevant: Serializer.DeserializeElement<bool>(oValue.ContainsKey("OrderRelevant") ? oValue["OrderRelevant"] : null, true),
                        semanticIdListElement: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("SemanticIdListElement") ? oValue["SemanticIdListElement"] : null),
                        typeValueListElement: Serializer.DeserializeElement<AasSubmodelElements>(oValue.ContainsKey("TypeValueListElement") ? oValue["TypeValueListElement"] : null, true),
                        valueTypeListElement: Serializer.DeserializeElement<DataTypeDefXsd>(oValue.ContainsKey("ValueTypeListElement") ? oValue["ValueTypeListElement"] : null),
                        value: new List<ISubmodelElement>());
                    break;
                case "SMC":
                    sme = new SubmodelElementCollection(
                        value: new List<ISubmodelElement>());
                    break;
                case "Ent":
                    sme = new Entity(
                        statements: new List<ISubmodelElement>(),
                        entityType: Serializer.DeserializeElement<EntityType>(value.First()[1], true),
                        globalAssetId: value.First()[0],
                        specificAssetIds: Serializer.DeserializeList<ISpecificAssetId>(oValue.ContainsKey("SpecificAssetIds") ? oValue["SpecificAssetIds"] : null));
                    break;
                case "Evt":
                    sme = new BasicEventElement(
                        observed: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("Observed") ? oValue["Observed"] : null),
                        direction: Serializer.DeserializeElement<Direction>(oValue.ContainsKey("Direction") ? oValue["Direction"] : null, true),
                        state: Serializer.DeserializeElement<StateOfEvent>(oValue.ContainsKey("State") ? oValue["State"] : null, true),
                        messageTopic: oValue.ContainsKey("MessageTopic") ? oValue["MessageTopic"] : null,
                        messageBroker: Serializer.DeserializeElement<IReference>(oValue.ContainsKey("MessageBroker") ? oValue["MessageBroker"] : null),
                        lastUpdate: oValue.ContainsKey("LastUpdate") ? oValue["LastUpdate"] : null,
                        minInterval: oValue.ContainsKey("MinInterval") ? oValue["MinInterval"] : null,
                        maxInterval: oValue.ContainsKey("MaxInterval") ? oValue["MaxInterval"] : null);
                    break;
                case "Opr":
                    sme = new Operation(
                        inputVariables: new List<IOperationVariable>(),
                        outputVariables: new List<IOperationVariable>(),
                        inoutputVariables: new List<IOperationVariable>());
                    break;
            }

            if (sme == null)
                return null;

            sme.IdShort = smeSet.IdShort;
            sme.DisplayName = Serializer.DeserializeList<ILangStringNameType>(smeSet.DisplayName);
            sme.Category = smeSet.Category;
            sme.Description = Serializer.DeserializeList<ILangStringTextType>(smeSet.Description);
            sme.Extensions = Serializer.DeserializeList<IExtension>(smeSet.Extensions);
            sme.SemanticId = !smeSet.SemanticId.IsNullOrEmpty() ? new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, smeSet.SemanticId) }) : null;
            sme.SupplementalSemanticIds = Serializer.DeserializeList<IReference>(smeSet.SupplementalSemanticIds);
            sme.Qualifiers = Serializer.DeserializeList<IQualifier>(smeSet.Qualifiers);
            sme.EmbeddedDataSpecifications = Serializer.DeserializeList<IEmbeddedDataSpecification>(smeSet.EmbeddedDataSpecifications);
            sme.TimeStampCreate = smeSet.TimeStampCreate;
            sme.TimeStamp = smeSet.TimeStamp;
            sme.TimeStampTree = smeSet.TimeStampTree;
            sme.TimeStampDelete = smeSet.TimeStampDelete;
            return sme;
        }

        public static string GetAASXPath(int? envId = null, string cdId = "", string aasId = "", string smId = "")
        {
            using var db = new AasContext();
            if (!cdId.IsNullOrEmpty())
            {
                var cdDBList = db.CDSets.Where(cd => cd.Identifier == cdId).Join(db.EnvCDSets, cd => cd.Id, envcd => envcd.CDId, (cd, envcd) => envcd);
                if (cdDBList.Any())
                    envId = cdDBList.First().EnvId;
            }

            if (!smId.IsNullOrEmpty())
            {
                var smDBList = db.SMSets.Where(s => s.Identifier == smId);
                if (smDBList.Any())
                    envId = smDBList.First().EnvId;
            }

            if (!aasId.IsNullOrEmpty())
            {
                var aasDBList = db.AASSets.Where(a => a.Identifier == aasId);
                if (aasDBList.Any())
                    envId = aasDBList.First().EnvId;
            }

            if (envId == null)
                return string.Empty;

            var path = db.EnvSets.Where(e => e.Id == envId).Select(e => e.Path).FirstOrDefault();
            return path ?? string.Empty;
        }
    }
}
