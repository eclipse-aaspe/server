using System.ComponentModel.DataAnnotations;
using System.Text;
using AasCore.Aas3_0;
using AasxServerDB.Entities;
using AdminShellNS;
using Extensions;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

                var submodel = new Submodel(smDB.Identifier);
                submodel.IdShort = smDB.IdShort;
                submodel.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                    new List<IKey>() { new Key(KeyTypes.GlobalReference, smDB.SemanticId) });
                submodel.SubmodelElements = new List<ISubmodelElement>();

                LoadSME(submodel, null, null, SMEList);

                submodel.TimeStampCreate = smDB.TimeStampCreate;
                submodel.TimeStamp = smDB.TimeStamp;
                submodel.TimeStampTree = smDB.TimeStampTree;
                submodel.SetAllParents();

                return submodel;
            }
        }

        static private void LoadSME(Submodel submodel, ISubmodelElement? sme, SMESet? smeSet, List<SMESet> SMEList)
        {
            var smeSets = SMEList.Where(s => s.ParentSMEId == (smeSet != null ? smeSet.Id : null)).OrderBy(s => s.IdShort).ToList();

            foreach (var smel in smeSets)
            {
                // create SME from database
                var nextSME = createSME(smel);

                // add sme to sm or sme 
                if (sme == null)
                {
                    submodel.Add(nextSME);
                }
                else
                {
                    switch (smeSet.SMEType)
                    {
                        case "RelA":
                            (sme as AnnotatedRelationshipElement).Annotations.Add((IDataElement) nextSME);
                            break;
                        case "SML":
                            (sme as SubmodelElementList).Value.Add(nextSME);
                            break;
                        case "SMC":
                            (sme as SubmodelElementCollection).Value.Add(nextSME);
                            break;
                        case "Ent":
                            (sme as Entity).Statements.Add(nextSME);
                            break;
                        /* 1. Version 
                        case "Opr":
                            if (nextSME.IdShort.StartsWith("Input_"))
                                (sme as Operation).InputVariables.Add(new OperationVariable(nextSME));
                            else if (nextSME.IdShort.StartsWith("Output_"))
                                (sme as Operation).OutputVariables.Add(new OperationVariable(nextSME));
                            else if (nextSME.IdShort.StartsWith("Inoutput_"))
                                (sme as Operation).InoutputVariables.Add(new OperationVariable(nextSME));
                            break;*/
                        /* 2. Version */
                        case "OprVar":
                            switch (smeSet.IdShort)
                            {
                                case "InputVariables":
                                    (sme as Operation).InputVariables.Add(new OperationVariable(nextSME));
                                    break;
                                case "OutputVariables":
                                    (sme as Operation).OutputVariables.Add(new OperationVariable(nextSME));
                                    break;
                                case "InoutputVariables":
                                    (sme as Operation).InoutputVariables.Add(new OperationVariable(nextSME));
                                    break;
                            }
                            break;
                    }
                }

                // recursiv, call for child sme's
                switch (smel.SMEType)
                {
                    case "RelA":
                    case "SML":
                    case "SMC":
                    case "Ent":
                    /* case "Opr": 1. Version */
                        LoadSME(submodel, nextSME, smel, SMEList);
                        break;
                    /* 2. Version */
                    case "Opr":
                        var smeOprVar = SMEList.Where(s => s.ParentSMEId == smel.Id).ToList();
                        foreach (var oprVar in smeOprVar)
                            LoadSME(submodel, nextSME, oprVar, SMEList);
                        break;
                }
            }
        }

        static private ISubmodelElement? createSME(SMESet smeSet)
        {
            ISubmodelElement? sme = null;
            var value = smeSet.getValue();
            switch (smeSet.SMEType)
            {
                case "Rel":
                    sme = new RelationshipElement(
                        first: new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                        second: new Reference(ReferenceTypes.ExternalReference, new List<IKey>()));
                    break;
                case "RelA":
                    sme = new AnnotatedRelationshipElement(
                        first: new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                        second: new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                        annotations: new List<IDataElement>());
                    break;
                case "Prop":
                    sme = new Property(
                        valueType: GetDataTypeDefXsd(smeSet.ValueType),
                        value: value.First()[0]/*,
                        valueId: new Reference(ReferenceTypes.ExternalReference, new List<IKey>())*/);
                    break;
                case "MLP":
                    sme = new MultiLanguageProperty(
                        value: value.ConvertAll<ILangStringTextType>(val => new LangStringTextType(val[1], val[0]))/*,
                        valueId: new Reference(ReferenceTypes.ExternalReference, new List<IKey>())*/);
                    break;
                case "Range":
                    var findMin = value.Find(val => val[1].Equals("Min"));
                    var findMax = value.Find(val => val[1].Equals("Max"));
                    sme = new AasCore.Aas3_0.Range(
                        valueType: GetDataTypeDefXsd(smeSet.ValueType),
                        min: findMin != null ? findMin[0] : string.Empty,
                        max: findMax != null ? findMax[0] : string.Empty);
                    break;
                case "Blob":
                    sme = new Blob(
                        value: Encoding.ASCII.GetBytes(value.First()[0]),
                        contentType: value.First()[1]);
                    break;
                case "File":
                    sme = new AasCore.Aas3_0.File(
                        value: value.First()[0],
                        contentType: value.First()[1]);
                    break;
                case "Ref":
                    sme = new ReferenceElement(
                        value: new Reference(ReferenceTypes.ExternalReference, new List<IKey>()));
                    break;
                case "Cap":
                    sme = new Capability();
                    break;
                case "SML":
                    sme = new SubmodelElementList(
                        orderRelevant: true,
                        semanticIdListElement: null,
                        typeValueListElement: AasSubmodelElements.SubmodelElement,
                        valueTypeListElement: null,
                        value: new List<ISubmodelElement>());
                    break;
                case "SMC":
                    sme = new SubmodelElementCollection(
                        value: new List<ISubmodelElement>());
                    break;
                case "Ent":
                    sme = new Entity(
                        statements: new List<ISubmodelElement>(),
                        entityType: value.First()[1].Equals("SelfManagedEntity") ? EntityType.SelfManagedEntity : EntityType.CoManagedEntity,
                        globalAssetId: value.First()[0],
                        specificAssetIds: null);
                    break;
                case "Evt":
                    sme = new BasicEventElement(
                        observed: new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                        direction: Direction.Input,
                        state: StateOfEvent.Off,
                        messageTopic: null,
                        messageBroker: null,
                        lastUpdate: null,
                        minInterval: null,
                        maxInterval: null);
                    break;
                case "Opr":
                    sme = new Operation(
                        inputVariables: new List<IOperationVariable>(),
                        outputVariables: new List<IOperationVariable>(),
                        inoutputVariables: new List<IOperationVariable>());
                    break;
            }

            if (sme == null)
                return null;

            sme.IdShort = smeSet.IdShort;
            if (!smeSet.SemanticId.IsNullOrEmpty())
            {
                sme.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                    new List<IKey>() { new Key(KeyTypes.GlobalReference, smeSet.SemanticId) });
            }
            sme.TimeStamp = smeSet.TimeStamp;
            sme.TimeStampCreate = smeSet.TimeStampCreate;
            sme.TimeStampTree = smeSet.TimeStampTree;
            return sme;
        }

        static private DataTypeDefXsd GetDataTypeDefXsd(string dataTypeString)
        {
            return dataTypeString switch
            {
                "S" => DataTypeDefXsd.String,
                "D" => DataTypeDefXsd.Double,
                "I" => DataTypeDefXsd.Integer,
                _ => DataTypeDefXsd.String
            };
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