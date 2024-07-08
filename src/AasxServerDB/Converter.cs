using AasCore.Aas3_0;
using AasxServerDB.Entities;
using AdminShellNS;
using Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerDB
{
    public class Converter
    {
        static public AdminShellPackageEnv? GetPackageEnv(string path, AASSet? aasDB) 
        {
            using (AasContext db = new AasContext())
            {
                if (path.IsNullOrEmpty() || aasDB == null)
                    return null;

                AssetAdministrationShell aas = new AssetAdministrationShell(
                    id: aasDB.Identifier,
                    idShort: aasDB.IdShort,
                    assetInformation: new AssetInformation(AssetKind.Type, aasDB.GlobalAssetId),
                    submodels: new List<AasCore.Aas3_0.IReference>());
                aas.TimeStampCreate = aasDB.TimeStampCreate;
                aas.TimeStamp = aasDB.TimeStamp;
                aas.TimeStampTree = aasDB.TimeStampTree;

                AdminShellPackageEnv? aasEnv = new AdminShellPackageEnv();
                aasEnv.SetFilename(path);
                aasEnv.AasEnv.AssetAdministrationShells?.Add(aas);

                var submodelDBList = db.SMSets
                    .OrderBy(sm => sm.Id)
                    .Where(sm => sm.AASId == aasDB.Id)
                    .ToList();
                foreach (var sm in submodelDBList.Select(submodelDB => Converter.GetSubmodel(smDB:submodelDB)))
                {
                    aas.Submodels?.Add(sm.GetReference());
                    aasEnv.AasEnv.Submodels?.Add(sm);
                }

                return aasEnv;
            }
        }

        static public Submodel? GetSubmodel(SMSet? smDB = null, string smIdentifier = "")
        {
            using (AasContext db = new AasContext())
            {
                if (!smIdentifier.IsNullOrEmpty())
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
                submodel.SubmodelElements = new List<ISubmodelElement>();

                LoadSME(submodel, null, null, SMEList, null);

                submodel.TimeStampCreate = smDB.TimeStampCreate;
                submodel.TimeStamp = smDB.TimeStamp;
                submodel.TimeStampTree = smDB.TimeStampTree;
                submodel.SetAllParents();

                return submodel;
            }           
        }

        static private void LoadSME(Submodel submodel, ISubmodelElement? sme, string? SMEType, List<SMESet> SMEList, int? smeId)
        {
            var smeLevel = SMEList.Where(s => s.ParentSMEId == smeId).OrderBy(s => s.IdShort).ToList();

            foreach (var smel in smeLevel)
            {
                ISubmodelElement? nextSME = null;
                var value = smel.getValue();
                switch (smel.SMEType)
                {
                    case "Prop":
                        nextSME = new Property(DataTypeDefXsd.String, value: value.First()[0]);
                        break;
                    case "SMC":
                        nextSME = new SubmodelElementCollection(value: new List<ISubmodelElement>());
                        break;
                    case "MLP":
                        nextSME = new MultiLanguageProperty(
                            value: value.ConvertAll<ILangStringTextType>(val => new LangStringTextType(val[1], val[0])));
                        break;
                    case "File":
                        nextSME = new AasCore.Aas3_0.File("text", value: value.First()[0]);
                        break;
                    case "Ent":
                        nextSME = new Entity(
                            value.First()[1].Equals("SelfManagedEntity") ? EntityType.SelfManagedEntity : EntityType.CoManagedEntity,
                            globalAssetId: value.First()[0],
                            statements: new List<ISubmodelElement>());
                        break;
                }

                if (nextSME == null)
                    continue;

                nextSME.IdShort = smel.IdShort;
                if (!smel.SemanticId.IsNullOrEmpty())
                {
                    nextSME.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                        new List<IKey>() { new Key(KeyTypes.GlobalReference, smel.SemanticId) });
                }
                nextSME.TimeStamp = smel.TimeStamp;
                nextSME.TimeStampCreate = smel.TimeStampCreate;
                nextSME.TimeStampTree = smel.TimeStampTree;

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
                        case "Ent":
                            (sme as Entity).Statements.Add(nextSME);
                            break;
                    }
                }

                if (smel.SMEType.Equals("SMC") || smel.SMEType.Equals("Ent"))
                {
                    LoadSME(submodel, nextSME, smel.SMEType, SMEList, smel.Id);
                }
            }
        }

        static public string GetAASXPath(string aasId = "", string submodelId = "")
        {
            using AasContext db = new AasContext();
            int? aasxId = null;
            if (!submodelId.IsNullOrEmpty())
            {
                var submodelDBList = db.SMSets.Where(s => s.Identifier == submodelId);
                if (submodelDBList.Count() > 0)
                    aasxId = submodelDBList.First().AASXId;
            }
            if (!aasId.IsNullOrEmpty())
            {
                var aasDBList = db.AASSets.Where(a => a.Identifier == aasId);
                if (aasDBList.Any())
                    aasxId = aasDBList.First().AASXId;
            }
            if (aasxId == null)
                return string.Empty;
            var aasxDBList = db.AASXSets.Where(a => a.Id == aasxId);
            if (!aasxDBList.Any())
                return string.Empty;
            var aasxDB = aasxDBList.First();
            return aasxDB.AASX;

        }
    }
}