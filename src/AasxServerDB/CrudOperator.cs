/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Runtime.Intrinsics.X86;
    using System.Text;
    using AasCore.Aas3_0;
    using AasxServerDB.Entities;
    using AdminShellNS;
    using AdminShellNS.Models;
    using Contracts.Pagination;
    using Contracts.QueryResult;
    using Extensions;
    using Microsoft.EntityFrameworkCore;
    // using Microsoft.EntityFrameworkCore.Metadata.Internal;
    using Microsoft.IdentityModel.Tokens;

    public class CrudOperator
    {
        public static List<string> GetFilteredPackages(string filterPath, List<AdminShellPackageEnv> list)
        {
            var paths = new List<string>();
            using (var db = new AasContext())
            {
                var envList = db.EnvSets.Where(e => e.Path.Contains(filterPath));

                foreach (var env in envList)
                {
                    var p = GetPackageEnv(db, null, env.Id);
                    if (p != null)
                    {
                        list.Add(p);
                        paths.Add(env.Path);
                    }
                }
            }
            return paths;
        }

        internal static Environment GetEnvironment(AasContext db, Dictionary<string, string>? securityCondition = null, int envId = -1,
            List<string> aasIds = null, List<string> smIds = null, AASSet aasDb = null, SMSet smDb = null, bool includeCD = false)
        {
            var timeStamp = DateTime.UtcNow;

            // env
            var env = new Environment();

            env.AssetAdministrationShells = new List<IAssetAdministrationShell>();
            env.Submodels = new List<ISubmodel>();

            var smWithCDs = new List<String>();

            // aas
            var loadedSMs = new List<String>();
            var aasDBList = new List<AASSet>();
            if (envId != -1)
            {
                aasDBList = db.AASSets.Where(aas => aas.EnvId == envId).ToList();
            }
            else
            {
                if (aasDb != null)
                {
                    aasDBList.Add(aasDb);
                }

                if (aasIds != null)
                {
                    foreach (var aasId in aasIds)
                    {
                        aasDBList = db.AASSets.Where(aas => aas.Identifier == aasId).ToList();
                    }
                }
            }

            foreach (var aasDB in aasDBList)
            {
                var helper = aasDB;
                var aas = ReadAssetAdministrationShell(db, ref helper);
                if (aas.TimeStamp == DateTime.MinValue)
                {
                    aas.TimeStampCreate = timeStamp;
                    aas.SetTimeStamp(timeStamp);
                }
                env.AssetAdministrationShells?.Add(aas);

                // sm
                /*
                var smAASDBList = db.SMSets.Where(sm => sm.EnvId == envId && sm.AASId == aasDB.Id).ToList();
                foreach (var sm in smAASDBList.Select(selector: smDB => GetSubmodel(smDB: smDB)))
                {
                    aas.Submodels?.Add(sm.GetReference());
                }
                */
                var smAASDBList = db.SMRefSets.Where(sm => sm.AASId == helper.Id).ToList();
                foreach (var smRef in smAASDBList)
                {
                    if (smRef.Identifier != null)
                    {
                        smWithCDs.Add(smRef.Identifier);
                        var sm = ReadSubmodel(db, submodelIdentifier: smRef.Identifier, securityCondition: securityCondition);
                        if (sm != null)
                        {
                            loadedSMs.Add(sm.Id);
                            //aas.Submodels?.Add(sm.GetReference());
                            env.Submodels?.Add(sm);
                        }
                    }
                }
            }


            // sm
            var smDBList = new List<SMSet>();
            if (envId != -1)
            {
                smDBList = db.SMSets.Where(sm => sm.EnvId == envId).ToList();
            }
            else
            {
                if (smDb != null)
                {
                    smDBList.Add(smDb);
                }

                if (smIds != null)
                {
                    foreach (var smId in smIds)
                    {
                        smDBList = db.SMSets.Where(sm => sm.Identifier == smId).ToList();
                    }
                }
            }

            // foreach (var sm in smDBList.Select(selector: submodelDB => ReadSubmodel(db, smDB: submodelDB)))
            foreach (var smDB in smDBList)
            {
                if (smDB.Identifier != null && !loadedSMs.Contains(smDB.Identifier))
                {
                    smWithCDs.Add(smDB.Identifier);
                    var sm = ReadSubmodel(db, submodelIdentifier: smDB.Identifier, securityCondition: securityCondition);
                    if (sm != null)
                    {
                        //ToDo: Remove?
                        if (sm.TimeStamp == DateTime.MinValue)
                        {
                            sm.SetAllParentsAndTimestamps(null, timeStamp, timeStamp, DateTime.MinValue);
                            sm.SetTimeStamp(timeStamp);
                        }

                        env.Submodels?.Add(sm);
                    }
                }
            }

            // cd
            if (includeCD)
            {
                env.ConceptDescriptions = new List<IConceptDescription>();

                List<CDSet> cdDBList = new List<CDSet>();
                if (envId != -1)
                {
                    cdDBList = db.EnvCDSets.Where(envcd => envcd.EnvId == envId).Join(db.CDSets, envcd => envcd.CDId, cd => cd.Id, (envcd, cd) => cd).ToList();
                }
                else
                {
                    //ToDo: Only get Concept Descriptions of specified aas and sm?
                    // cdDBList = db.CDSets.ToList();
                    var smDBneeded = db.SMSets.Where(sm => smWithCDs.Contains(sm.Identifier));
                    var smIDs = smDBneeded.Select(sm => sm.Id).ToList();
                    var semanticIDs = smDBneeded.Select(sm => sm.SemanticId).Distinct().ToList();
                    var smeDBneeded = db.SMESets.Where(sme => smIDs.Contains(sme.SMId));
                    semanticIDs.AddRange(smeDBneeded.Select(sme => sme.SemanticId).Distinct().ToList());
                    cdDBList = db.CDSets.Where(cd => semanticIDs.Contains(cd.Identifier)).ToList();
                }

                foreach (var cd in cdDBList.Select(selector: cdDB => ReadConceptDescription(db, cdDB: cdDB)))
                {
                    env.ConceptDescriptions?.Add(cd);
                }

            }

            if (env.AssetAdministrationShells?.Count == 0)
            {
                env.AssetAdministrationShells = null;
            }
            if (env.Submodels?.Count == 0)
            {
                env.Submodels = null;
            }
            if (env.ConceptDescriptions?.Count == 0)
            {
                env.ConceptDescriptions = null;
            }

            return env;
        }

        public static AdminShellPackageEnv? GetPackageEnv(AasContext db, Dictionary<string, string>? securityCondition = null, int envId = -1, AASSet aas = null, SMSet sm = null)
        {
            // env
            var env = new AdminShellPackageEnv();
            var environment = GetEnvironment(db, securityCondition, envId, aasDb: aas, smDb: sm, includeCD: true);

            // path
            if (envId != -1)
            {
                env.SetFilename(fileName: GetAASXPath(envId));
            }

            env.AasEnv = environment;

            return env;
        }

        public static List<IAssetAdministrationShell> ReadPagedAssetAdministrationShells(AasContext db, IPaginationParameters paginationParameters, List<ISpecificAssetId> assetIds, string idShort)
        {
            var output = new List<IAssetAdministrationShell>();

            var globalAssetId = assetIds?.Where(a => a.Name == "globalAssetId").Select(a => a.Value).FirstOrDefault();

            var aasDBList = db.AASSets
                .Where(aas => idShort == null || aas.IdShort == idShort)
                .Where(aas => globalAssetId == null || aas.GlobalAssetId == globalAssetId)
                .OrderBy(aas => aas.Id)
                .Skip(paginationParameters.Cursor)
                .Take(paginationParameters.Limit)
                .ToList();

            var aasIDs = aasDBList.Select(aas => aas.Id).ToList();
            var smDBList = db.SMRefSets.Where(sm => sm.AASId != null && aasIDs.Contains((int)sm.AASId)).ToList();

            foreach (var aasDB in aasDBList)
            {
                var helper = aasDB;
                var aas = ReadAssetAdministrationShell(db, ref helper, smDBList);


                output.Add(aas);
            }

            return output;
        }

        public static AssetAdministrationShell? ReadAssetAdministrationShell(AasContext db, ref AASSet? aasDB, List<SMRefSet>? smRefSets = null, string aasIdentifier = "")
        {
            AssetAdministrationShell aas = null;

            if (!aasIdentifier.IsNullOrEmpty())
            {
                aasDB = db.AASSets.FirstOrDefault(cd => cd.Identifier == aasIdentifier);
                /*
                var aasList = db.AASSets.Where(cd => cd.Identifier == aasIdentifier).ToList();
                if (aasList.Count == 0)
                    return null;
                aasDB = aasList.First();
                */
            }

            if (aasDB == null)
                return null;


            aas = new AssetAdministrationShell(
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
                    defaultThumbnail: aasDB.DefaultThumbnailPath != null ?
                        new Resource(
                        path: aasDB.DefaultThumbnailPath,
                        contentType: aasDB.DefaultThumbnailContentType
                        ) : null
                )
            )
            {
                TimeStampCreate = aasDB.TimeStampCreate,
                TimeStamp = aasDB.TimeStamp,
                TimeStampTree = aasDB.TimeStampTree,
                TimeStampDelete = aasDB.TimeStampDelete
            };


            var timeStamp = DateTime.UtcNow;
            if (aas?.TimeStamp == DateTime.MinValue)
            {
                aas.TimeStampCreate = timeStamp;
                aas.SetTimeStamp(timeStamp);
            }
            var aasDBId = aasDB.Id;

            var smDBList = new List<SMRefSet>();

            if (smRefSets != null)
            {
                smDBList = smRefSets;
            }
            else
            {
                smDBList = db.SMRefSets.Where(sm => sm.AASId == aasDBId).ToList();
            }

            // sm
            foreach (var sm in smDBList.Where(sm => sm.AASId == aasDBId))
            {
                aas?.Submodels?.Add(new Reference(type: ReferenceTypes.ModelReference,
                    keys: new List<IKey>() { new AasCore.Aas3_0.Key(KeyTypes.Submodel, sm.Identifier) }
                ));
            }

            return aas;
        }

        public static void ReplaceAssetAdministrationShellById(AasContext db, AASSet? aasDB, IAssetAdministrationShell newAas)
        {
            if (aasDB != null)
            {
                db.SMRefSets.RemoveRange(aasDB.SMRefSets);

                CrudOperator.SetAas(aasDB, newAas);
                db.SaveChanges();
            }
        }

        internal static IAssetAdministrationShell CreateAas(AasContext db, IAssetAdministrationShell newAas)
        {
            IAssetAdministrationShell createdAas = null;

            var aasDB = new AASSet();

            // dictionary to save connection between aas and sm
            var aasToSm = new Dictionary<string, AASSet?>();

            //if (!newAas.IdShort.IsNullOrEmpty() && newAas.IdShort.ToLower().Contains("globalsecurity"))
            //{
            //    if (newAas.Submodels != null)
            //    {
            //        foreach (var refSm in newAas.Submodels)
            //            if (refSm.Keys != null && refSm.Keys.Count() > 0 && !refSm.Keys[0].Value.IsNullOrEmpty())
            //                aasToSm.TryAdd(refSm.Keys[0].Value, null);
            //    }
            //}
            SetAas(aasDB, newAas);


            if (newAas.Submodels != null)
            {
                foreach (var refSm in newAas.Submodels)
                    if (refSm.Keys != null && refSm.Keys.Count() > 0 && !refSm.Keys[0].Value.IsNullOrEmpty())
                    {
                        aasDB.SMRefSets.Add(new SMRefSet { Identifier = refSm.Keys[0].Value });
                    }
                //aasToSm.TryAdd(refSm.Keys[0].Value, aasDB);
            }

            var aasDBQuery = db.Add(aasDB);

            db.SaveChanges();

            var addedInDb = aasDBQuery.Entity;

            createdAas = ReadAssetAdministrationShell(db, ref addedInDb);

            return createdAas;
        }

        private static void SetAas(AASSet aasDB, IAssetAdministrationShell newAas)
        {
            var currentDataTime = DateTime.UtcNow;

            aasDB.IdShort = newAas.IdShort;
            aasDB.DisplayName = Serializer.SerializeList(newAas.DisplayName);
            aasDB.Category = newAas.Category;
            aasDB.Description = Serializer.SerializeList(newAas.Description);
            aasDB.Extensions = Serializer.SerializeList(newAas.Extensions);
            aasDB.Identifier = newAas.Id;
            aasDB.EmbeddedDataSpecifications = Serializer.SerializeList(newAas.EmbeddedDataSpecifications);
            aasDB.DerivedFrom = Serializer.SerializeElement(newAas.DerivedFrom);
            aasDB.Version = newAas.Administration?.Version;
            aasDB.Revision = newAas.Administration?.Revision;
            aasDB.Creator = Serializer.SerializeElement(newAas.Administration?.Creator);
            aasDB.TemplateId = newAas.Administration?.TemplateId;
            aasDB.AEmbeddedDataSpecifications = Serializer.SerializeList(newAas.Administration?.EmbeddedDataSpecifications);
            aasDB.AssetKind = Serializer.SerializeElement(newAas.AssetInformation?.AssetKind);
            aasDB.SpecificAssetIds = Serializer.SerializeList(newAas.AssetInformation?.SpecificAssetIds);
            aasDB.GlobalAssetId = newAas.AssetInformation?.GlobalAssetId;
            aasDB.AssetType = newAas.AssetInformation?.AssetType;
            aasDB.DefaultThumbnailPath = newAas.AssetInformation?.DefaultThumbnail?.Path;
            aasDB.DefaultThumbnailContentType = newAas.AssetInformation?.DefaultThumbnail?.ContentType;

            aasDB.TimeStampCreate = newAas.TimeStampCreate == default ? currentDataTime : newAas.TimeStampCreate;
            aasDB.TimeStamp = newAas.TimeStamp == default ? currentDataTime : newAas.TimeStamp;
            aasDB.TimeStampTree = newAas.TimeStampTree == default ? currentDataTime : newAas.TimeStampTree;
            aasDB.TimeStampDelete = newAas.TimeStampDelete;
        }

        public static void DeleteAAS(AasContext db, string aasIdentifier)
        {
            try
            {
                // Deletes automatically from DB
                var aas = db.AASSets
                    .Include(aas => aas.SMRefSets)
                    .FirstOrDefault(aas => aas.Identifier == aasIdentifier);

                if (aas != null)
                {
                    aas?.SMRefSets.Clear();
                    db.AASSets.Remove(aas);

                    db.SaveChanges();
                }
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                Console.WriteLine($"SQLite Error: {ex.Message}");
                Console.WriteLine($"Foreign Key Constraint: {ex.SqliteErrorCode}");
            }
        }


        public static void ReplaceAssetInformation(AasContext db, AASSet aasDB, IAssetInformation newAssetInformation)
        {
            var currentDataTime = DateTime.UtcNow;

            aasDB.AssetKind = Serializer.SerializeElement(newAssetInformation.AssetKind);
            aasDB.SpecificAssetIds = Serializer.SerializeList(newAssetInformation.SpecificAssetIds);
            aasDB.GlobalAssetId = newAssetInformation.GlobalAssetId;
            aasDB.AssetType = newAssetInformation.AssetType;
            aasDB.DefaultThumbnailPath = newAssetInformation.DefaultThumbnail?.Path;
            aasDB.DefaultThumbnailContentType = newAssetInformation.DefaultThumbnail?.ContentType;
            aasDB.TimeStamp = currentDataTime;
            aasDB.TimeStampTree = currentDataTime;

            db.SaveChanges();
        }

        internal static IReference CreateSubmodelReferenceInAAS(AasContext db, IReference body, string aasIdentifier)
        {
            var aasDB = db.AASSets
                .Include(aas => aas.SMRefSets)
                .FirstOrDefault(aas => aas.Identifier == aasIdentifier);
            var identifier = body.GetAsIdentifier();
            if (aasDB != null && identifier != null)
            {
                aasDB.SMRefSets.Add(new SMRefSet { Identifier = identifier });
                var currentDataTime = DateTime.UtcNow;
                aasDB.TimeStamp = currentDataTime;
                aasDB.TimeStampTree = currentDataTime;
                db.SaveChanges();
            }

            // TODO: read reference from DB
            return body;
        }

        internal static void DeleteSubmodelReferenceInAAS(AasContext db, string aasIdentifier, string submodelIdentifier)
        {
            var aasDB = db.AASSets
                .Include(aas => aas.SMRefSets)
                .FirstOrDefault(aas => aas.Identifier == aasIdentifier);
            if (aasDB != null)
            {
                var smRefDB = aasDB.SMRefSets.FirstOrDefault(s => s.Identifier == submodelIdentifier);
                if (smRefDB != null)
                {
                    aasDB.SMRefSets.Remove(smRefDB);
                    var currentDataTime = DateTime.UtcNow;
                    aasDB.TimeStamp = currentDataTime;
                    aasDB.TimeStampTree = currentDataTime;
                    db.SaveChanges();
                }
            }
        }

        public static List<SmeMerged> GetSmeMerged(AasContext db, Dictionary<string, string>? securityCondition, List<SMESet>? listSME, SMSet? smSet)
        {
            if (listSME == null)
            {
                return null;
            }

            var smeIdList = listSME.Select(sme => sme.Id).ToList();
            var querySME = db.SMESets.Where(sme => smeIdList.Contains(sme.Id));

            return GetSmeMerged(db, securityCondition, querySME, smSet);
        }

        private static List<SmeMerged> GetSmeMerged(AasContext db, Dictionary<string, string>? securityCondition, IQueryable<SMESet>? querySME, SMSet? smSet)
        {
            if (querySME == null)
                return null;

            IQueryable<SValueSet> sValueSets = db.SValueSets;
            if (securityCondition != null && securityCondition["svalue"] != "")
            {
                sValueSets = sValueSets.Where(securityCondition["svalue"]);
            }

            IQueryable<IValueSet> iValueSets = db.IValueSets;
            IQueryable<DValueSet> dValueSets = db.DValueSets;
            if (securityCondition != null && securityCondition["mvalue"] != "")
            {
                iValueSets = iValueSets.Where(securityCondition["mvalue"]);
                dValueSets = dValueSets.Where(securityCondition["mvalue"]);
            }

            var joinSValue = querySME.Join(
                sValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new SmeMerged { smSet = smSet, smeSet = sme, sValueSet = sv, iValueSet = null, dValueSet = null, oValueSet = null })
                .ToList();

            var joinIValue = querySME.Join(
                iValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new SmeMerged { smSet = smSet, smeSet = sme, sValueSet = null, iValueSet = sv, dValueSet = null, oValueSet = null })
                .ToList();

            var joinDValue = querySME.Join(
                dValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new SmeMerged { smSet = smSet, smeSet = sme, sValueSet = null, iValueSet = null, dValueSet = sv, oValueSet = null })
                .ToList();

            var joinOValue = querySME.Join(
                db.OValueSets,
                sme => sme.Id,
                sv => sv.SMEId,
                (sme, sv) => new SmeMerged { smSet = smSet, smeSet = sme, sValueSet = null, iValueSet = null, dValueSet = null, oValueSet = sv })
                .ToList();

            var result = joinSValue;
            result.AddRange(joinIValue);
            result.AddRange(joinDValue);
            result.AddRange(joinOValue);

            var smeIdList = result.Select(sme => sme.smeSet.Id).ToList();
            var noValue = querySME.Where(sme => !smeIdList.Contains(sme.Id))
                .Select(sme => new SmeMerged { smSet = smSet, smeSet = sme, sValueSet = null, iValueSet = null, dValueSet = null, oValueSet = null })
                .ToList();
            result.AddRange(noValue);

            return result;
        }

        public static List<ISubmodel> ReadPagedSubmodels(AasContext db, Query? querySM, IPaginationParameters paginationParameters, Dictionary<string, string>? securityCondition, IReference reqSemanticId, string idShort)
        {
            List<ISubmodel> output = new List<ISubmodel>();

            var qresult = new QResult();
            if (querySM != null)
            {
                Dictionary<string, string> condition = [];
                if (securityCondition != null)
                {
                    foreach (var s in securityCondition)
                    {
                        condition.Add(s.Key, s.Value);
                    }
                    if (condition.TryGetValue("sm.", out var smvalue) && condition.TryGetValue("filter-sm.", out var smfilter))
                    {
                        if (smvalue != "" && smfilter != "")
                        {
                            condition["sm."] = smvalue + " && " + smfilter;
                        }
                        else if (smfilter != "")
                        {
                            condition["sm."] = smfilter;
                        }
                    }
                }
                else
                {
                    condition.Add("all", "true");
                    condition.Add("sm.", "true");
                }

                if (condition.TryGetValue("sm.", out _))
                {
                    string? semanticId = null;
                    if (reqSemanticId != null)
                    {
                        var keys = reqSemanticId.Keys;
                        if (keys != null && keys.Count > 0)
                        {
                            semanticId = keys[0].Value;

                            condition["sm."] = "(" + condition["sm."] + $" && sm.semanticId == \"{semanticId}\"" + ")";
                        }
                    }
                    if (!string.IsNullOrEmpty(idShort))
                    {
                        condition["sm."] = "(" + condition["sm."] + $" && sm.idShort == \"{idShort}\"" + ")";
                    }
                }

                qresult = querySM.SearchSMs(condition, db, withTotalCount: false, withLastId: false, semanticId: "",
                identifier: "", diff: "", pageFrom: paginationParameters.Cursor, pageSize: paginationParameters.Limit, expression: "$all");
            }

            var smList = qresult.SMResults.Select(sm => sm.smId).ToList();
            var smDBList = db.SMSets.Where(sm => smList.Contains(sm.Id)).ToList();

            var timeStamp = DateTime.UtcNow;

            foreach (var sm in smDBList.Select(selector: submodelDB => ReadSubmodel(db, smDB: submodelDB, "", securityCondition)))
            {
                if (sm != null)
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

        public static Submodel? ReadSubmodel(AasContext db, SMSet? smDB = null, string submodelIdentifier = "",
            Dictionary<string, string>? securityCondition = null, Dictionary<string, string>? condition = null)
        {
            if (!submodelIdentifier.IsNullOrEmpty())
            {
                var smDBQuery = db.SMSets
                        .Where(sm => sm.Identifier == submodelIdentifier);
                if (securityCondition?["sm."] is not null and not "")
                {
                    smDBQuery = smDBQuery.Where(securityCondition["sm."]);
                }

                var smDBList = smDBQuery.ToList();
                if (smDBList != null && smDBList.Count > 0)
                {
                    smDB = smDBList.First();
                }
            }

            if (smDB == null)
                return null;

            var SMEQueryAll = db.SMESets
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

            var smeMerged = GetSmeMerged(db, null, SMEQueryAll, smDB);

            var sCondition = "true";
            if (securityCondition?["all"] != null && securityCondition?["all"] != "")
            {
                sCondition = securityCondition["all"];
            }

            if (smeMerged != null && smeMerged.Count != 0)
            {
                // at least 1 exists
                var mergeForCondition = smeMerged.Select(sme => new
                {
                    sm = sme.smSet,
                    sme = sme.smeSet,
                    svalue = (sme.smeSet.TValue == "S" && sme.sValueSet != null && sme.sValueSet.Value != null) ? sme.sValueSet.Value : "",
                    mvalue = (sme.smeSet.TValue == "I" && sme.iValueSet != null && sme.iValueSet.Value != null) ? sme.iValueSet.Value :
                        (sme.smeSet.TValue == "D" && sme.dValueSet != null && sme.dValueSet.Value != null) ? sme.dValueSet.Value : 0
                }).Distinct();
                // at least 1 must exist to also approve security condition for SME
                var resultCondition = mergeForCondition.AsQueryable().Where(sCondition);
                if (!resultCondition.Any())
                {
                    return null;
                }

                var filter = "true";
                if (securityCondition != null && securityCondition.TryGetValue("filter-all", out _))
                {
                    if (securityCondition?["filter-all"] != null && securityCondition?["filter-all"] != "")
                    {
                        filter = securityCondition?["filter-all"];
                    }
                }
                if (condition != null && condition.TryGetValue("filter-all", out var filter2))
                {
                    if (filter2 != null)
                    {
                        if (filter == "true")
                        {
                            filter = filter2;
                        }
                        else
                        {
                            filter = $"({filter} && {filter2})";
                        }
                    }
                }

                resultCondition = mergeForCondition.AsQueryable().Where(filter);
                var resultConditionIDs = resultCondition.Select(s => s.sme.Id).Distinct().ToList();
                smeMerged = smeMerged.Where(m => resultConditionIDs.Contains(m.smeSet.Id)).ToList();

                LoadSME(submodel, null, null, null, smeMerged);
            }

            submodel.TimeStampCreate = smDB.TimeStampCreate;
            submodel.TimeStamp = smDB.TimeStamp;
            submodel.TimeStampTree = smDB.TimeStampTree;
            submodel.TimeStampDelete = smDB.TimeStampDelete;
            submodel.SetAllParents();

            return submodel;
        }

        public static ISubmodel CreateSubmodel(AasContext db, ISubmodel newSubmodel, string aasIdentifier = null)
        {
            ISubmodel submodel = null;

            int? aasDB = null;
            int? envDB = null;

            //ToDo: For EventService always null?
            if (!String.IsNullOrEmpty(aasIdentifier))
            {
                var aasDBQuery = db.AASSets.Where(sm => sm.Identifier == aasIdentifier);
                if (aasDBQuery != null && aasDBQuery.Any())
                {
                    aasDB = aasDBQuery.First().Id;
                    envDB = aasDBQuery.First().EnvId;
                }
            }

            var visitor = new VisitorAASX(db);

            //ToDo: For EventService
            visitor.currentDataTime = DateTime.UtcNow;
            visitor.VisitSubmodel(newSubmodel);
            visitor._smDB.AASId = aasDB;
            visitor._smDB.EnvId = envDB;
            db.Add(visitor._smDB);
            db.SaveChanges();

            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == newSubmodel.Id);
            if (smDBQuery != null && smDBQuery.Count() > 0)
            {
                submodel = ReadSubmodel(db, smDBQuery.First());
            }

            return submodel;
        }

        public static void ReplaceSubmodelById(AasContext db, string aasIdentifier, string submodelIdentifier, ISubmodel newSubmodel)
        {
            var visitor = new VisitorAASX(db);
            visitor.update = true;
            visitor.currentDataTime = DateTime.UtcNow;
            visitor.VisitSubmodel(newSubmodel);
            // Delete no more exisiting SMEs in SM
            var smDB = visitor._smDB;
            db.SMESets.Where(sme => sme.SMId == smDB.Id && !visitor.keepSme.Contains(sme.Id)).ExecuteDeleteAsync();
            db.SaveChanges();
        }
        internal static Submodel DeleteSubmodel(AasContext db, string submodelIdentifier, bool isLoadingSubmodel = false)
        {
            Submodel submodel = null;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var smDB = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

                    if (isLoadingSubmodel)
                    {
                        var submodeInDb = smDB.FirstOrDefault();
                        if (submodeInDb != null) {
                            submodel = ReadSubmodel(db, submodeInDb);
                        }
                    }

                    var smDBID = smDB.FirstOrDefault().Id;
                    var smeDB = db.SMESets.Where(sme => sme.SMId == smDBID);
                    var smeDBIDList = smeDB.Select(sme => sme.Id).ToList();

                    db.SValueSets.Where(s => smeDBIDList.Contains(s.SMEId)).ExecuteDelete();
                    db.IValueSets.Where(i => smeDBIDList.Contains(i.SMEId)).ExecuteDelete();
                    db.DValueSets.Where(d => smeDBIDList.Contains(d.SMEId)).ExecuteDelete();
                    db.OValueSets.Where(o => smeDBIDList.Contains(o.SMEId)).ExecuteDelete();
                    smeDB.ExecuteDelete();
                    smDB.ExecuteDelete();

                    db.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    submodel = null;
                    transaction.Rollback();
                }
            }

            return submodel;
        }

        public static List<ISubmodelElement>? ReadPagedSubmodelElements(AasContext db, IPaginationParameters paginationParameters, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier)
        {
            bool result = false;
            var output = new List<ISubmodelElement>();

            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

            if (!aasIdentifier.IsNullOrEmpty())
            {
                var aasDB = db.AASSets
                    .Where(aas => aas.Identifier == aasIdentifier).ToList();
                if (aasDB.Count != 1)
                {
                    return null;
                }
                var aasDBId = aasDB[0].Id;
                var smRefDBQuery = db.SMRefSets.Where(sm => sm.Identifier == submodelIdentifier && sm.AASId == aasDBId).ToList();
                if (smRefDBQuery.Count != 1)
                {
                    return null;
                }
            }

            if (securityCondition != null)
            {
                smDBQuery = smDBQuery.Where(securityCondition["sm."]);
            }
            var smDB = smDBQuery.ToList();
            if (smDB == null || smDB.Count != 1)
            {
                return null;
            }
            var smDBId = smDB[0].Id;

            var smeSmTopQuery = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == null);
            if (securityCondition != null && !securityCondition["sme."].IsNullOrEmpty())
            {
                smeSmTopQuery = smeSmTopQuery.Where(securityCondition["sme."]);
            }
            var smeSmTop = smeSmTopQuery
                .OrderBy(sme => sme.Id).Skip(paginationParameters.Cursor).Take(paginationParameters.Limit).ToList();
            var smeSmTopTree = CrudOperator.GetTree(db, smDB[0], smeSmTop);
            var smeSmTopMerged = CrudOperator.GetSmeMerged(db, securityCondition, smeSmTopTree, smDB[0]);

            foreach (var smeDB in smeSmTop)
            {
                var sme = CrudOperator.ReadSubmodelElement(smeDB, smeSmTopMerged);
                if (sme != null)
                {
                    output.Add(sme);
                }
            }
            return output;
        }
        public static ISubmodelElement? CreateSubmodelElement(AasContext db, string aasIdentifier, string submodelIdentifier, ISubmodelElement newSubmodelElement, string idShortPath, bool first)
        {
            // first is not possible any more
            // SME read from DB are now ordered by TimeStampTree
            // new SME with new time can only be at the end

            ISubmodelElement? submodelElement = null;

            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

            if (!string.IsNullOrEmpty(aasIdentifier))
            {
                var aasDB = db.AASSets
                    .Where(aas => aas.Identifier == aasIdentifier).ToList();
                if (aasDB.Count != 1)
                {
                    return null;
                }
                var aasDBId = aasDB[0].Id;
                var smRefDBQuery = db.SMRefSets.Where(sm => sm.Identifier == submodelIdentifier && sm.AASId == aasDBId).ToList();
                if (smRefDBQuery.Count != 1)
                {
                    return null;
                }
            }

            var smDB = smDBQuery.FirstOrDefault();
            var visitor = new VisitorAASX(db);
            visitor._smDB = smDB;
            visitor.currentDataTime = DateTime.UtcNow;
            var smDBId = smDB.Id;
            var smeSmList = db.SMESets.Where(sme => sme.SMId == smDBId).ToList();
            CrudOperator.CreateIdShortPath(db, smeSmList);
            var smeSmMerged = CrudOperator.GetSmeMerged(db, null, smeSmList, smDB);
            visitor.smSmeMerged = smeSmMerged;

            if (!String.IsNullOrEmpty(idShortPath))
            {
                visitor.parentPath = idShortPath;
            }
            visitor.update = false;
            // continue debug here
            var receiveSmeDB = visitor.VisitSMESet(newSubmodelElement);
            if (receiveSmeDB == null)
            {
                return null;
            }

            receiveSmeDB.SMId = smDBId;
            CrudOperator.setTimeStampTree(db, smDB, receiveSmeDB, receiveSmeDB.TimeStamp);
            try
            {
                db.SMESets.Add(receiveSmeDB);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
            }

            submodelElement = CrudOperator.ReadSubmodelElement(receiveSmeDB);

            return submodelElement;
        }

        public static ISubmodelElement? ReadSubmodelElementByPath(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath, out SMESet smeEntity)
        {
            smeEntity = null;
            bool result = false;

            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

            if (!aasIdentifier.IsNullOrEmpty())
            {
                var aasDB = db.AASSets
                    .Where(aas => aas.Identifier == aasIdentifier).ToList();
                if (aasDB.Count != 1)
                {
                    return null;
                }
                var aasDBId = aasDB[0].Id;
                var smRefDBQuery = db.SMRefSets.Where(sm => sm.Identifier == submodelIdentifier && sm.AASId == aasDBId).ToList();
                if (smRefDBQuery.Count != 1)
                {
                    return null;
                }
            }

            if (securityCondition != null)
            {
                smDBQuery = smDBQuery.Where(securityCondition["sm."]);
            }
            var smDB = smDBQuery.ToList();
            if (smDB == null || smDB.Count != 1)
            {
                return null;
            }
            var smDBId = smDB[0].Id;


            if (String.IsNullOrEmpty(idShortPath))
            {
                return null;
            }

            var smeFoundDB = db.SMESets.Where(sme => sme.SMId == smDBId && sme.IdShortPath == idShortPath);
            var smeFound = smeFoundDB.ToList();

            //for (int i = 1; i < idShortPathElements.Count; i++)
            //{
            //    idShort = idShortPathElements[i];
            //    //ToDo SubmodelElementList with index (type: int) must be implemented
            //    var smeFoundDB = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == parentId && sme.IdShort == idShort);
            //    smeFound = smeFoundDB.ToList();
            //    if (smeFound == null || smeFound.Count != 1)
            //    {
            //        return null;
            //    }
            //    parentId = smeFound[0].Id;
            //}

            var smeFoundTree = CrudOperator.GetTree(db, smDB[0], smeFound);
            var smeMerged = CrudOperator.GetSmeMerged(db, securityCondition, smeFoundTree, smDB[0]);

            if (smeMerged != null && smeMerged.Count != 0 && securityCondition?["all"] != null)
            {
                // at least 1 exists
                var mergeForCondition = smeMerged.Select(sme => new
                {
                    sm = sme.smSet,
                    sme = sme.smeSet,
                    svalue = (sme.smeSet.TValue == "S" && sme.sValueSet != null && sme.sValueSet.Value != null) ? sme.sValueSet.Value : "",
                    mvalue = (sme.smeSet.TValue == "I" && sme.iValueSet != null && sme.iValueSet.Value != null) ? sme.iValueSet.Value :
                        (sme.smeSet.TValue == "D" && sme.dValueSet != null && sme.dValueSet.Value != null) ? sme.dValueSet.Value : 0
                }).Distinct();
                // at least 1 must exist to approve security condition
                var resultCondition = mergeForCondition.AsQueryable().Where(securityCondition["all"]);
                if (!resultCondition.Any())
                {
                    return null;
                }
                if (securityCondition.TryGetValue("filter-all", out _))
                {
                    resultCondition = mergeForCondition.AsQueryable().Where(securityCondition["filter-all"]);
                    var resultConditionIDs = resultCondition.Select(s => s.sme.Id).Distinct().ToList();
                    smeMerged = smeMerged.Where(m => resultConditionIDs.Contains(m.smeSet.Id)).ToList();
                }
            }

            smeEntity = smeFound[0];

            var sme = CrudOperator.ReadSubmodelElement(smeFound[0], smeMerged);

            return sme;
        }

        public static void ReplaceSubmodelElementByPath(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body)
        {
            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

            if (!aasIdentifier.IsNullOrEmpty())
            {
                var aasDB = db.AASSets
                    .Where(aas => aas.Identifier == aasIdentifier).ToList();
                if (aasDB.Count != 1)
                {
                    return;
                }
                var aasDBId = aasDB[0].Id;
                var smRefDBQuery = db.SMRefSets.Where(sm => sm.Identifier == submodelIdentifier && sm.AASId == aasDBId).ToList();
                if (smRefDBQuery.Count != 1)
                {
                    return;
                }
            }

            if (securityCondition != null)
            {
                smDBQuery = smDBQuery.Where(securityCondition["sm."]);
            }

            var smDB = smDBQuery.FirstOrDefault();
            var visitor = new VisitorAASX(db);
            visitor._smDB = smDB;
            visitor.currentDataTime = DateTime.UtcNow;
            var smDBId = smDB.Id;
            var smeSmList = db.SMESets.Where(sme => sme.SMId == smDBId).ToList();
            CrudOperator.CreateIdShortPath(db, smeSmList);
            var smeSmMerged = CrudOperator.GetSmeMerged(db, securityCondition, smeSmList, null);
            visitor.smSmeMerged = smeSmMerged;
            visitor.idShortPath = idShortPath;
            visitor.update = true;
            var receiveSmeDB = visitor.VisitSMESet(body);
            receiveSmeDB.SMId = smDBId;
            CrudOperator.setTimeStampTree(db, smDB, receiveSmeDB, receiveSmeDB.TimeStamp);
            try
            {
                // db.SMESets.Add(receiveSmeDB);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            var smeDB = smeSmMerged.Where(sme =>
                    !visitor.keepSme.Contains(sme.smeSet.Id) &&
                    visitor.deleteSme.Contains(sme.smeSet.Id)
                ).ToList();
            var smeDelete = smeDB.Select(sme => sme.smeSet.Id).Distinct().ToList();

            if (smeDelete.Count > 0)
            {
                db.SMESets.Where(sme => smeDelete.Contains(sme.Id)).ExecuteDeleteAsync().Wait();
                db.SaveChanges();
            }
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //ToDo submodel service solution, do we really need a different solution for replace and update?
            //_submodelService.UpdateSubmodelElementByPath(submodelIdentifier, idShortPath, newSme);
        }

        public static void DeleteSubmodelElement(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath)
        {
            var now = DateTime.UtcNow;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

                    if (!aasIdentifier.IsNullOrEmpty())
                    {
                        var aasDB = db.AASSets
                            .Where(aas => aas.Identifier == aasIdentifier).ToList();
                        if (aasDB.Count != 1)
                        {
                            return;
                        }
                        var aasDBId = aasDB[0].Id;
                        var smRefDBQuery = db.SMRefSets.Where(sm => sm.Identifier == submodelIdentifier && sm.AASId == aasDBId).ToList();
                        if (smRefDBQuery.Count != 1)
                        {
                            return;
                        }
                    }

                    if (securityCondition != null)
                    {
                        smDBQuery = smDBQuery.Where(securityCondition["sm."]);
                    }
                    var smDB = smDBQuery.ToList();
                    if (smDB == null || smDB.Count != 1)
                    {
                        return;
                    }
                    var smDBId = smDB[0].Id;

                    var idShortPathElements = idShortPath.Split(".");
                    if (idShortPathElements.Length == 0)
                    {
                        return;
                    }
                    var idShort = idShortPathElements[0];
                    var smeParent = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == null && sme.IdShort == idShort).ToList();
                    if (smeParent == null || smeParent.Count != 1)
                    {
                        return;
                    }
                    var parentId = smeParent[0].Id;
                    var smeFound = smeParent;

                    for (var i = 1; i < idShortPathElements.Length; i++)
                    {
                        if (i < idShortPathElements.Length - 1)
                        {
                            smeFound[0].TimeStampTree = now;
                        }
                        else
                        {
                            smeFound[0].TimeStampDelete = now;
                        }
                        idShort = idShortPathElements[i];
                        //ToDo SubmodelElementList with index (type: int) must be implemented
                        var smeFoundDB = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == parentId && sme.IdShort == idShort);
                        smeFound = smeFoundDB.ToList();
                        if (smeFound == null || smeFound.Count != 1)
                        {
                            return;
                        }
                        parentId = smeFound[0].Id;
                    }

                    var smeFoundTreeIds = CrudOperator.GetTree(db, smDB[0], smeFound)?.Select(s => s.Id);
                    if (smeFoundTreeIds?.Count() > 0)
                    {
                        smDB[0].TimeStampTree = now;
                        if (idShortPathElements.Length == 1)
                        {
                            smDB[0].TimeStampDelete = now;
                        }
                        db.SMESets.Where(sme => smeFoundTreeIds.Contains(sme.Id)).ExecuteDeleteAsync().Wait();
                        db.SaveChanges();
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
        }

        public static ISubmodelElement? ReadSubmodelElement(SMESet smeSet)
        {
            return CreateSME(smeSet, null);
        }

        public static ISubmodelElement? ReadSubmodelElement(SMESet smeSet, List<SmeMerged> tree)
        {
            var sme = CreateSME(smeSet, tree);
            LoadSME(null, sme, smeSet, null, tree);
            return sme;
        }

        internal static void LoadSME(Submodel submodel, ISubmodelElement? sme, SMESet? smeSet, List<SMESet>? SMEList, List<SmeMerged>? tree = null)
        {
            var smeSets = SMEList;
            if (tree != null)
            {
                smeSets = tree.Select(t => t.smeSet).Distinct().ToList();
            }
            // smeSets = smeSets.Where(s => s.ParentSMEId == (smeSet != null ? smeSet.Id : null)).OrderBy(s => s.IdShort).ToList();
            smeSets = smeSets.Where(s => s.ParentSMEId == (smeSet != null ? smeSet.Id : null)).OrderBy(s => s.TimeStampTree).ToList();

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
                            else
                                Console.WriteLine("Prefix missing");
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
        }

        public class smeREsult
        {
            public int Id { get; set; }
            public string? IdShortPath { get; set; }
            public int? ParentSMEId { get; set; }
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
            public SMSet? smSet;
            public SMESet? smeSet;
            public SValueSet? sValueSet;
            public IValueSet? iValueSet;
            public DValueSet? dValueSet;
            public OValueSet? oValueSet;
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
        public static void setTimeStampValue(string submodelId, string idShortPath, DateTime timeStamp, string value = null)
        {
            using (var db = new AasContext())
            {
                var smDB = db.SMSets.Where(sm => sm.Identifier == submodelId).FirstOrDefault();
                if (smDB == null)
                {
                    return;
                };

                var smeFound = db.SMESets.Where(sme => sme.SMId == smDB.Id && sme.IdShortPath == idShortPath).FirstOrDefault();
                if (smeFound == null)
                {
                    return;
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

        public static string? GetModelType(string smeType)
        {
            switch (smeType)
            {
                case "Rel":
                    return "RelationshipElement";
                case "RelA":
                    return "AnnotatedRelationshipElement";
                case "Prop":
                    return "Property";
                case "MLP":
                    return "MultiLanguageProperty";
                case "Range":
                    return "Range";
                case "Blob":
                    return "Blob";
                case "File":
                    return "File";
                case "Ref":
                    return "ReferenceElement";
                case "Cap":
                    return "Capability";
                case "SML":
                    return "SubmodelElementList";
                case "SMC":
                    return "SubmodelElementCollection";
                case "Ent":
                    return "Entity";
                case "Evt":
                    return "BasicEventElement";
                case "Opr":
                    return "Operation";
                default:
                    return null;
            }
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
                    sme = new AasCore.Aas3_0.Property(
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
                        entityType: Serializer.DeserializeElement<AasCore.Aas3_0.EntityType>(value.First()[1], true),
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
            sme.SemanticId = !smeSet.SemanticId.IsNullOrEmpty() ? new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new AasCore.Aas3_0.Key(KeyTypes.GlobalReference, smeSet.SemanticId) }) : null;
            sme.SupplementalSemanticIds = Serializer.DeserializeList<IReference>(smeSet.SupplementalSemanticIds);
            sme.Qualifiers = Serializer.DeserializeList<IQualifier>(smeSet.Qualifiers);
            sme.EmbeddedDataSpecifications = Serializer.DeserializeList<IEmbeddedDataSpecification>(smeSet.EmbeddedDataSpecifications);
            sme.TimeStampCreate = smeSet.TimeStampCreate;
            sme.TimeStamp = smeSet.TimeStamp;
            sme.TimeStampTree = smeSet.TimeStampTree;
            sme.TimeStampDelete = smeSet.TimeStampDelete;
            return sme;
        }

        internal static List<IConceptDescription> GetPagedConceptDescriptions(AasContext db, IPaginationParameters paginationParameters, string idShort, IReference isCaseOf, IReference dataSpecificationRef)
        {
            var output = new List<IConceptDescription>();

            var timeStamp = DateTime.UtcNow;

            var cdDBList = db.CDSets
                .Where(cd => idShort == null || cd.IdShort == idShort)
                .OrderBy(cd => cd.Id)
                .Skip(paginationParameters.Cursor)
                .Take(paginationParameters.Limit)
                .ToList();

            foreach (var cdDB in cdDBList)
            {
                var cd = ReadConceptDescription(db, cdDB: cdDB);
                if (cd?.TimeStamp == DateTime.MinValue)
                {
                    cd.TimeStampCreate = timeStamp;
                    cd.SetTimeStamp(timeStamp);
                }

                output.Add(cd);
            }
            return output;
        }

        internal static ConceptDescription? ReadConceptDescription(AasContext db, CDSet? cdDB = null, string cdIdentifier = "")
        {
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

        internal static IConceptDescription CreateConceptDescription(AasContext db, IConceptDescription newCd)
        {
            IConceptDescription cd = null;

            var cdDB = new CDSet();
            SetConceptDescription(cdDB, newCd);

            var cdDBQuery = db.Add(cdDB);
            db.SaveChanges();

            cd = ReadConceptDescription(db, cdDBQuery.Entity);

            return cd;
        }

        internal static void ReplaceConceptdescription(AasContext db, string cdIdentifier, IConceptDescription newCd)
        {
            var cdList = db.CDSets.Where(cd => cd.Identifier == cdIdentifier).ToList();
            if (cdList.Count == 0)
                return;
            var cdDB = cdList.First();

            if (cdDB != null)
            {
                CrudOperator.SetConceptDescription(cdDB, newCd);
                db.SaveChanges();
            }
        }

        internal static void SetConceptDescription(CDSet cdDB, IConceptDescription newCd)
        {
            var currentDataTime = DateTime.UtcNow;

            cdDB.IdShort = newCd.IdShort;
            cdDB.DisplayName = Serializer.SerializeList(newCd.DisplayName);
            cdDB.Category = newCd.Category;
            cdDB.Description = Serializer.SerializeList(newCd.Description);
            cdDB.Extensions = Serializer.SerializeList(newCd.Extensions);
            cdDB.Identifier = newCd.Id;
            cdDB.EmbeddedDataSpecifications = Serializer.SerializeList(newCd.EmbeddedDataSpecifications);
            cdDB.IsCaseOf = Serializer.SerializeList(newCd.IsCaseOf);

            cdDB.Version = newCd.Administration?.Version;
            cdDB.Revision = newCd.Administration?.Revision;
            cdDB.Creator = Serializer.SerializeElement(newCd.Administration?.Creator);
            cdDB.TemplateId = newCd.Administration?.TemplateId;
            cdDB.AEmbeddedDataSpecifications = Serializer.SerializeList(newCd.Administration?.EmbeddedDataSpecifications);

            cdDB.TimeStampCreate = newCd.TimeStampCreate == default ? currentDataTime : newCd.TimeStampCreate;
            cdDB.TimeStamp = newCd.TimeStamp == default ? currentDataTime : newCd.TimeStamp;
            cdDB.TimeStampTree = newCd.TimeStampTree == default ? currentDataTime : newCd.TimeStampTree;
            cdDB.TimeStampDelete = newCd.TimeStampDelete;
        }
        internal static void DeleteConceptDescription(AasContext db, string conceptDescriptionId)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    db.CDSets.Where(cd => cd.Identifier == conceptDescriptionId).ExecuteDelete();

                    db.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
        }

        public static string GetAASXPath(int? envId = null, string cdId = "", string aasId = "", string smId = "")
        {
            using (var db = new AasContext())
            {
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

        internal static List<PackageDescription> ReadPagedPackageDescriptions(AasContext db, IPaginationParameters paginationParameters, Dictionary<string, string>? securityCondition, string aasIdentifier)
        {
            var output = new List<PackageDescription>();

            var timeStamp = DateTime.UtcNow;

            List<int> envIds = new List<int>();
            List<string> unpackedAasIds = new List<string>();

            if (aasIdentifier != null)
            {
                var aas = db.AASSets.FirstOrDefault(aas => aas.Identifier == aasIdentifier);

                if (aas != null)
                {
                    if (aas.EnvId.HasValue && aas.EnvId >= 0)
                    {
                        envIds.Add(aas.EnvId.Value);
                    }
                    else
                    {
                        unpackedAasIds.Add(aasIdentifier);
                    }
                }
            }
            else
            {
                var envDbSet = db.EnvSets
                    .Skip(paginationParameters.Cursor)
                    .Take(paginationParameters.Limit);
                envIds = [.. envDbSet.Select(env => env.Id)];

                if (envIds.Count < paginationParameters.Limit)
                {
                    var newLimit = paginationParameters.Limit - envIds.Count;

                    var newCursor = paginationParameters.Cursor - db.EnvSets.Count() < 0 ? 0 : paginationParameters.Cursor - db.EnvSets.Count();

                    var aasDbSet = db.AASSets.Where(aas => !aas.EnvId.HasValue)
                        .Skip(newCursor)
                        .Take(newLimit);
                    unpackedAasIds = [.. aasDbSet.Select(aas => aas.Identifier)];
                }
            }

            foreach (var envId in envIds)
            {
                var packageDescription = new PackageDescription();
                packageDescription.PackageId = envId.ToString();
                var aasIdList = new List<string>();

                var aasSets = db.AASSets.Where(aas => aas.EnvId == envId);
                foreach (var aas in aasSets)
                {
                    aasIdList.Add(aas.Identifier ?? "");
                }
                packageDescription.AasIds = aasIdList;
                output.Add(packageDescription);
            }

            foreach(var unpackedAasId in unpackedAasIds)
            {
                var packageDescription = new PackageDescription();
                packageDescription.PackageId = unpackedAasId;
                packageDescription.AasIds = new List<string> {
                    unpackedAasId };
                output.Add(packageDescription);
            }

            return output;
        }
    }
}

