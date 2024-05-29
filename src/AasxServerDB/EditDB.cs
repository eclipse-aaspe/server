using AasCore.Aas3_0;
using AdminShellNS;
using Microsoft.EntityFrameworkCore;

namespace AasxServerDB
{
    public class EditDB
    {
        static public void EditAAS(AdminShellPackageEnv env)
        {
            Dictionary<string, int> aasToDeleteAASXIdsDic; 

            var aasList = env.AasEnv.AssetAdministrationShells;
            var aasIds = aasList.Select(x => x.Id).ToList();

            using (AasContext db = new AasContext())
            {
                // Delets automtically from DB
                aasToDeleteAASXIdsDic = db.AASSets.Where(x => aasIds.Contains(x.Identifier)).ToDictionary(x => x.Identifier, x => x.AASXId);
                db.AASSets.Where(x => aasIds.Contains(x.Identifier)).ExecuteDelete();

                // Load Everything back in
                foreach (IAssetAdministrationShell aas in aasList)
                {
                    int aasxId = 0;
                    if (aasToDeleteAASXIdsDic.ContainsKey(aas.Id))
                        aasxId = aasToDeleteAASXIdsDic[aas.Id];

                    AASXSet aasxDBOld = db.AASXSets.Where(a => a.Id == aasxId).ToList<AASXSet>().First();
                    var aasxDB = new AASXSet
                    {
                        AASX = aasxDBOld.AASX
                    };
                    db.AASXSets.Where(a => a.Id == aasxId).ExecuteDelete();

                    VisitorAASX.LoadAASInDB(env, aasxDB);
                    db.Add(aasxDB);
                }
                db.SaveChanges();
            }
            env.setWrite(false);
        }
    }    
}
