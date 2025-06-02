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

    public class Edit
    {
        static public void Update(AdminShellPackageEnv env)
        {
            using (var db = new AasContext())
            {
                var deleteEnvList = db.EnvSets.Where(e => e.Path == env.Filename);
                var deleteEnv = deleteEnvList.FirstOrDefault();
                var deleteAasList = db.AASSets.Where(a => a.EnvId == deleteEnv.Id);
                var deleteSmList = db.SMSets.Where(s => s.EnvId == deleteEnv.Id);
                var deleteCDList = db.EnvCDSets.Where(s => s.EnvId == deleteEnv.Id);

                foreach (var s in deleteSmList)
                {
                    if (s.Identifier != null)
                    {
                        CrudOperator.DeleteSubmodel(db,s.Identifier);
                    }
                }
                foreach (var a in deleteAasList)
                {
                    if (a.Identifier != null)
                    {
                        CrudOperator.DeleteAAS(db, a.Identifier);
                    }
                }
                deleteCDList.ExecuteDeleteAsync().Wait();
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
                finally
                {
                    db.Dispose();
                }
            }
            env.setWrite(false);
            Console.WriteLine("SAVE AASX TO DB: " + env.Filename);
        }

    }
}
