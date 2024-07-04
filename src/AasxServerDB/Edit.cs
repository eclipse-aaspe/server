/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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

using AdminShellNS;
using Microsoft.EntityFrameworkCore;
using AasxServerDB.Entities;

namespace AasxServerDB
{
    public class Edit
    {
        static public void Update(AdminShellPackageEnv env)
        {
            using (AasContext db = new AasContext())
            {
                // Deletes automatically from DB
                db.AASXSets.Where(a => a.AASX == env.Filename).ExecuteDelete();

                // Load Everything back in
                var aasxDB = new AASXSet
                {
                    AASX = env.Filename
                };
                VisitorAASX.LoadAASInDB(env, aasxDB);
                db.Add(aasxDB);
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
        }
    }    
}
