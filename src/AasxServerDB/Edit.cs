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
    using AdminShellNS;
    using Microsoft.EntityFrameworkCore;
    using AasxServerDB.Entities;
    using System.Runtime.Intrinsics.X86;

    public class Edit
    {
        static public void Update(AdminShellPackageEnv env)
        {
            using (AasContext db = new AasContext())
            {
                // Deletes manually from DB
                var deleteEnvList = db.EnvSets.Where(e => e.Path == env.Filename);
                var deleteEnv = deleteEnvList.FirstOrDefault();
                var deleteAasList = db.AASSets.Where(a => a.EnvId == deleteEnv.Id);
                var deletSmList = db.SMSets.Where(s => s.EnvId == deleteEnv.Id);
                deletSmList.ExecuteDeleteAsync().Wait();
                deleteAasList.ExecuteDeleteAsync().Wait();
                deleteEnvList.ExecuteDeleteAsync().Wait();

                // Load Everything back in
                var envDB = new EnvSet
                {
                    Path = env.Filename
                };
                VisitorAASX.ImportAASIntoDB(env, envDB);
                db.Add(envDB);
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally { 
                    db.Dispose();
                }
            }
            env.setWrite(false);
            Console.WriteLine("SAVE AASX TO DB: " + env.Filename);
        }

        public static void DeleteAAS(string aasIdentifier)
        {
            using (AasContext db = new AasContext())
            {
                // Deletes automatically from DB
                db.AASSets
                    .Include(aas => aas.SMRefSets)
                    .Where(aas => aas.Identifier == aasIdentifier).ExecuteDelete();

                db.SaveChanges();
            }
        }

        public static void DeleteSubmodel(string submodelIdentifier)
        {
            using (AasContext db = new AasContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var smDB = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);
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
                        transaction.Rollback();
                    }
                }
            }
        }

        public static void DeleteSubmodelElement(SMESet sME)
        {
            using (AasContext db = new AasContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.SValueSets.Where(s => s.SMEId == sME.Id).ExecuteDelete();
                        db.IValueSets.Where(i => i.SMEId == sME.Id).ExecuteDelete();
                        db.DValueSets.Where(d => d.SMEId == sME.Id).ExecuteDelete();
                        db.OValueSets.Where(o => o.SMEId == sME.Id).ExecuteDelete();
                        db.SMESets.Where(sme => sme.Id == sME.Id).ExecuteDelete();

                        db.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                    }
                }
            }
        }

        public static void DeleteConceptDescription(string conceptDescriptionId)
        {
            using (AasContext db = new AasContext())
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
        }
    }
}
