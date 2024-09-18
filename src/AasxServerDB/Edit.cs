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
                db.EnvSets.Where(a => a.Path == env.Filename).ExecuteDelete();

                // Load Everything back in
                var envDB = new EnvSet
                {
                    Path = env.Filename
                };
                VisitorAASX.LoadAASInDB(env, envDB);
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
        }
    }
}
