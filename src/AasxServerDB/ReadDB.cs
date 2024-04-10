using AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

namespace AasxServerDB
{
    public class ReadDB
    {
        static public string GetAASXPath(string aasId = "", string submodelId = "")
        {
            using AasContext db = new AasContext();
            int? aasxId = null;
            if (!submodelId.Equals(""))
            {
                var submodelDBList = db.SMSets.Where(s => s.Identifier == submodelId);
                if (submodelDBList.Count() > 0)
                    aasxId = submodelDBList.First().AASXId;
            }
            if (!aasId.Equals(""))
            {
                var aasDBList = db.AASSets.Where(a => a.Identifier == aasId);
                if (aasDBList.Any())
                    aasxId = aasDBList.First().AASXId;
            }
            if (aasxId == null)
                return "";
            var aasxDBList = db.AASXSets.Where(a => a.Id == aasxId);
            if (!aasxDBList.Any())
                return "";
            var aasxDB = aasxDBList.First();
            return aasxDB.AASX;

        }

        static public AdminShellPackageEnv GetPackageEnv(string path, AASSet aasDB) 
        {
            using (AasContext db = new AasContext())
            {
                if (path == null || path.Equals("") || aasDB == null)
                    return null;

                AssetAdministrationShell aas = new AssetAdministrationShell(
                    id: aasDB.Identifier,
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
                    Submodel sm = ReadDB.GetSubmodel(smDB:submodelDB);
                    aas.Submodels.Add(sm.GetReference());
                    aasEnv.AasEnv.Submodels.Add(sm);
                }

                return aasEnv;
            }
        }

        static public Submodel GetSubmodel(SMSet? smDB = null, string? smIdentifier = "")
        {
            using (AasContext db = new AasContext())
            {
                if (!smIdentifier.Equals(""))
                {
                    var smList = db.SMSets.Where(sm => sm.Identifier == smIdentifier).ToList();
                    if (smList.Count == 0)
                        return null;
                    smDB = smList.First();
                }
                if (smDB == null)
                    return null;

                var SMEList = db.SMESets
                    .OrderBy(sme => sme.Id)
                    .Where(sme => sme.SMId == smDB.Id)
                    .ToList();

                Submodel submodel = new Submodel(smDB.Identifier);
                submodel.IdShort = smDB.IdShort;
                submodel.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                    new List<IKey>() { new Key(KeyTypes.GlobalReference, smDB.SemanticId) });

                LoadSME(submodel, null, null, SMEList, null);

                DateTime timeStamp = DateTime.Now;
                submodel.TimeStampCreate = timeStamp;
                submodel.SetTimeStamp(timeStamp);
                submodel.SetAllParents(timeStamp);

                return submodel;
            }           
        }

        static public string GetSubmodelJson(SMSet smSet)
        {
            var submodel = GetSubmodel(smDB: smSet);

            if (submodel != null)
            {
                var j = Jsonization.Serialize.ToJsonObject(submodel);
                string json = j.ToJsonString();
                return json;
            }

            return "";
        }

        static private void LoadSME(Submodel submodel, ISubmodelElement sme, string SMEType, List<SMESet> SMEList, int? smeId)
        {
            var smeLevel = SMEList.Where(s => s.ParentSMEId == smeId).OrderBy(s => s.IdShort).ToList();

            foreach (var smel in smeLevel)
            {
                ISubmodelElement nextSME = null;
                switch (smel.SMEType)
                {
                    case "Prop":
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
                                .Where(s => s.SMEId == smel.Id)
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
                    LoadSME(submodel, nextSME, smel.SMEType, SMEList, smel.Id);
                }
            }
        }
    }
}