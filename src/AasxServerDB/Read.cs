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
                    var submodelDBList = db.SubmodelSets.Where(s => s.SubmodelId == submodelId);
                    if (submodelDBList.Count() > 0)
                    {
                        var submodelDB = submodelDBList.First();
                        aasxNum = submodelDB.AASXNum;
                    }
                }
                if (!aasId.Equals(""))
                {
                    var aasDBList = db.AasSets.Where(a => a.AasId == aasId);
                    if (aasDBList.Any())
                    {
                        var aasDB = aasDBList.First();
                        aasxNum = aasDB.AASXNum;
                    }
                }
                if (aasxNum == 0)
                    return null;
                var aasxDBList = db.AASXSets.Where(a => a.AASXNum == aasxNum);
                if (!aasxDBList.Any())
                    return null;
                var aasxDB = aasxDBList.First();
                return aasxDB.AASX;
            }
                
        }

        static public AdminShellPackageEnv AASToPackageEnv(string path, AasSet aasDB)
        {
            using (AasContext db = new AasContext())
            {
                if (path == null || path.Equals("") || aasDB == null)
                    return null;

                AssetAdministrationShell aas = new AssetAdministrationShell(
                    id: aasDB.AasId,
                    idShort: aasDB.Idshort,
                    assetInformation: new AssetInformation(AssetKind.Type, aasDB.AssetId),
                    submodels: new List<AasCore.Aas3_0.IReference>());

                AdminShellPackageEnv aasEnv = new AdminShellPackageEnv();
                aasEnv.SetFilename(path);
                aasEnv.AasEnv.AssetAdministrationShells.Add(aas);

                var submodelDBList = db.SubmodelSets
                    .OrderBy(sm => sm.SubmodelNum)
                    .Where(sm => sm.AasNum == aasDB.AasNum)
                    .ToList();
                foreach (var submodelDB in submodelDBList)
                {
                    var sm = DBRead.getSubmodel(submodelDB.SubmodelId);
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
                var subDB = db.SubmodelSets
                    .OrderBy(s => s.SubmodelNum)
                    .Where(s => s.SubmodelId == submodelId)
                    .ToList()
                    .First();

                if (subDB != null)
                {
                    var SMEList = db.SMESets
                            .OrderBy(sme => sme.SMENum)
                            .Where(sme => sme.SubmodelNum == subDB.SubmodelNum)
                            .ToList();

                    Submodel submodel = new Submodel(submodelId);
                    submodel.IdShort = subDB.Idshort;
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
            var smeLevel = SMEList.Where(s => s.ParentSMENum == smeNum).OrderBy(s => s.Idshort).ToList();

            foreach (var smel in smeLevel)
            {
                ISubmodelElement nextSME = null;
                switch (smel.SMEType)
                {
                    case "P":
                        nextSME = new Property(DataTypeDefXsd.String, idShort: smel.Idshort, value: smel.getValue());
                        break;
                    case "SEC":
                        nextSME = new SubmodelElementCollection(idShort: smel.Idshort, value: new List<ISubmodelElement>());
                        break;
                    case "MLP":
                        var mlp = new MultiLanguageProperty(idShort: smel.Idshort);
                        var ls = new List<ILangStringTextType>();

                        using (AasContext db = new AasContext())
                        {
                            var SValueSetList = db.SValueSets
                                .Where(s => s.ParentSMENum == smel.SMENum)
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
                        nextSME = new AasCore.Aas3_0.File("text", idShort: smel.Idshort, value: smel.getValue());
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
                        case "SEC":
                            (sme as SubmodelElementCollection).Value.Add(nextSME);
                            break;
                    }
                }

                if (smel.SMEType == "SEC")
                {
                    loadSME(submodel, nextSME, smel.SMEType, SMEList, smel.SMENum);
                }
            }
        }
    }
}