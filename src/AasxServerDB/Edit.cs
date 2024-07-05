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
