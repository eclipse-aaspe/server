using AdminShellNS;
using Microsoft.EntityFrameworkCore;

namespace AasxServerDB
{
    public class EditDB
    {
        static public void EditAAS(AdminShellPackageEnv env)
        {
            using (AasContext db = new AasContext())
            {
                // Delets automtically from DB
                db.AASXSets.Where(a => a.AASX == env.Filename).ExecuteDelete();

                // Load Everything back in
                var aasxDB = new AASXSet
                {
                    AASX = env.Filename
                };
                VisitorAASX.LoadAASInDB(env, aasxDB);
                db.Add(aasxDB);
                db.SaveChanges();
            }
            env.setWrite(false);
        }
    }    
}
