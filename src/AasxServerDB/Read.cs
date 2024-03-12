using AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

namespace AasxServerDB
{
    public class DBRead
    {
        static public string GetAASXPath(string aasId = "", string submodelId = "")
        {
            using (AasContext db = new AasContext())
            { 
                long aasxNum = 0;
                if (!submodelId.Equals(""))
                {
                    var submodelDBList = db.SMSets.Where(s => s.IdIdentifier == submodelId);
                    if (submodelDBList.Count() > 0)
                    {
                        var submodelDB = submodelDBList.First();
                        aasxNum = submodelDB.AASXId;
                    }
                }
                if (!aasId.Equals(""))
                {
                    var aasDBList = db.AASSets.Where(a => a.IdIdentifier == aasId);
                    if (aasDBList.Any())
                    {
                        var aasDB = aasDBList.First();
                        aasxNum = aasDB.AASXId;
                    }
                }
                if (aasxNum == 0)
                    return null;
                var aasxDBList = db.AASXSets.Where(a => a.Id == aasxNum);
                if (!aasxDBList.Any())
                    return null;
                var aasxDB = aasxDBList.First();
                return aasxDB.AASX;
            }
                
        }

        static public AdminShellPackageEnv AASToPackageEnv(string path, AASSet aasDB)
        {
            using (AasContext db = new AasContext())
            {
                if (path == null || path.Equals("") || aasDB == null)
                    return null;

                AssetAdministrationShell aas = new AssetAdministrationShell(
                    id: aasDB.IdIdentifier,
                    idShort: aasDB.IdShort,
                    assetInformation: new AssetInformation(AssetKind.Type, aasDB.GlobalAssetId),
                    submodels: new List<AasCore.Aas3_0.IReference>());

                AdminShellPackageEnv aasEnv = new AdminShellPackageEnv();
                aasEnv.SetFilename(path);
                aasEnv.AasEnv.AssetAdministrationShells.Add(aas);

                var submodelDBList = db.SMSets
                    .OrderBy(sm => sm.Id)
                    .Where(sm => sm.AASId == aasDB.Id)
                    .ToList();
                foreach (var submodelDB in submodelDBList)
                {
                    var sm = DBRead.getSubmodel(submodelDB.IdIdentifier);
                    aas.Submodels.Add(sm.GetReference());
                    aasEnv.AasEnv.Submodels.Add(sm);
                }

                return aasEnv;
            }
        }

        static public Submodel getSubmodel(string submodelId)
        {
            using (AasContext db = new AasContext())
            {
                var subDB = db.SMSets
                    .OrderBy(s => s.Id)
                    .Where(s => s.IdIdentifier == submodelId)
                    .ToList()
                    .First();

                if (subDB != null)
                {
                    var SMEList = db.SMESets
                            .OrderBy(sme => sme.Id)
                            .Where(sme => sme.Id == subDB.Id)
                            .ToList();

                    Submodel submodel = new Submodel(submodelId);
                    submodel.IdShort = subDB.IdShort;
                    submodel.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                        new List<IKey>() { new Key(KeyTypes.GlobalReference, subDB.SemanticId) });

                    loadSME(submodel, null, null, SMEList, 0);

                    DateTime timeStamp = DateTime.Now;
                    submodel.TimeStampCreate = timeStamp;
                    submodel.SetTimeStamp(timeStamp);
                    submodel.SetAllParents(timeStamp);

                    return submodel;
                }
            }

            return null;
        }

        static public string getSubmodelJson(string submodelId)
        {
            var submodel = getSubmodel(submodelId);

            if (submodel != null)
            {
                var j = Jsonization.Serialize.ToJsonObject(submodel);
                string json = j.ToJsonString();
                return json;
            }

            return "";
        }

        static private void loadSME(Submodel submodel, ISubmodelElement sme, string SMEType, List<SMESet> SMEList, long smeNum)
        {
            var smeLevel = SMEList.Where(s => s.ParentSMEId == smeNum).OrderBy(s => s.IdShort).ToList();

            foreach (var smel in smeLevel)
            {
                ISubmodelElement nextSME = null;
                switch (smel.SMEType)
                {
                    case "P":
                        nextSME = new Property(DataTypeDefXsd.String, idShort: smel.IdShort, value: smel.getValue());
                        break;
                    case "SMC":
                        nextSME = new SubmodelElementCollection(idShort: smel.IdShort, value: new List<ISubmodelElement>());
                        break;
                    case "MLP":
                        var mlp = new MultiLanguageProperty(idShort: smel.IdShort);
                        var ls = new List<ILangStringTextType>();

                        using (AasContext db = new AasContext())
                        {
                            var SValueSetList = db.SValueSets
                                .Where(s => s.ParentSMEId == smel.Id)
                                .ToList();
                            foreach (var MLPValue in SValueSetList)
                            {
                                ls.Add(new LangStringTextType(MLPValue.Annotation, MLPValue.Value));
                            }
                        }

                        mlp.Value = ls;
                        nextSME = mlp;
                        break;
                    case "F":
                        nextSME = new AasCore.Aas3_0.File("text", idShort: smel.IdShort, value: smel.getValue());
                        break;
                }
                if (nextSME == null)
                    continue;

                if (smel.SemanticId != "")
                {
                    nextSME.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                        new List<IKey>() { new Key(KeyTypes.GlobalReference, smel.SemanticId) });
                }

                if (sme == null)
                {
                    submodel.Add(nextSME);
                }
                else
                {
                    switch (SMEType)
                    {
                        case "SMC":
                            (sme as SubmodelElementCollection).Value.Add(nextSME);
                            break;
                    }
                }

                if (smel.SMEType == "SMC")
                {
                    loadSME(submodel, nextSME, smel.SMEType, SMEList, smel.Id);
                }
            }
        }
    }
}