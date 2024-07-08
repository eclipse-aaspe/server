using System.Text;
using AasCore.Aas3_0;
using AasxServerDB.Entities;
using AdminShellNS;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Nodes = System.Text.Json.Nodes;

namespace AasxServerDB
{
    public class Converter
    {
        public static AdminShellPackageEnv? GetPackageEnv(string path, AASSet? aasDB) 
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

        public static Submodel? GetSubmodel(SMSet? smDB = null, string smIdentifier = "")
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

        private static void LoadSME(Submodel submodel, ISubmodelElement? sme, SMESet? smeSet, List<SMESet> SMEList)
        {
            var smeSets = SMEList.Where(s => s.ParentSMEId == (smeSet != null ? smeSet.Id : null)).OrderBy(s => s.IdShort).ToList();

            foreach (var smel in smeSets)
            {
                // prefix of operation
                var split = !smel.SMEType.IsNullOrEmpty() ? smel.SMEType.Split(VisitorAASX.OPERATION_SPLIT) : [ string.Empty ];
                var oprPrefix = split.Length == 2 ? split[ 0 ] : string.Empty;
                smel.SMEType  = split.Length == 2 ? split[ 1 ] : split[ 0 ];

                // create SME from database
                var nextSME = CreateSME(smel);

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
                        case "Opr":
                            if (oprPrefix.Equals(VisitorAASX.OPERATION_INPUT))
                                (sme as Operation).InputVariables.Add(new OperationVariable(nextSME));
                            else if (oprPrefix.Equals(VisitorAASX.OPERATION_OUTPUT))
                                (sme as Operation).OutputVariables.Add(new OperationVariable(nextSME));
                            else if (oprPrefix.Equals(VisitorAASX.OPERATION_INOUTPUT))
                                (sme as Operation).InoutputVariables.Add(new OperationVariable(nextSME));
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
                    case "Opr":
                        LoadSME(submodel, nextSME, smel, SMEList);
                        break;
                }
            }
        }

        private static ISubmodelElement? CreateSME(SMESet smeSet)
        {
            ISubmodelElement? sme = null;
            var value = smeSet.GetValue();
            var oValue = smeSet.GetOValue();

            switch (smeSet.SMEType)
            {
                case "Rel":
                    sme = new RelationshipElement(
                        first: oValue.ContainsKey("First") ? CreateReferenceFromObject(oValue["First"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                        second: oValue.ContainsKey("Second") ? CreateReferenceFromObject(oValue["Second"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()));
                    break;
                case "RelA":
                    sme = new AnnotatedRelationshipElement(
                        first: oValue.ContainsKey("First") ? CreateReferenceFromObject(oValue["First"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                        second: oValue.ContainsKey("Second") ? CreateReferenceFromObject(oValue["Second"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                        annotations: new List<IDataElement>());
                    break;
                case "Prop":
                    sme = new Property(
                        valueType: Jsonization.Deserialize.DataTypeDefXsdFrom(value.First()[1]),
                        value: value.First()[0],
                        valueId: oValue.ContainsKey("ValueId") ? CreateReferenceFromObject(oValue["ValueId"]) : null);
                    break;
                case "MLP":
                    sme = new MultiLanguageProperty(
                        value: value.ConvertAll<ILangStringTextType>(val => new LangStringTextType(val[1], val[0])),
                        valueId: oValue.ContainsKey("ValueId") ? CreateReferenceFromObject(oValue["ValueId"]) : null);
                    break;
                case "Range":
                    var findMin = value.Find(val => val[1].Equals("Min"));
                    var findMax = value.Find(val => val[1].Equals("Max"));
                    sme = new AasCore.Aas3_0.Range(
                        valueType: Jsonization.Deserialize.DataTypeDefXsdFrom(oValue["ValueType"]),
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
                        value: oValue.ContainsKey("Value") ? CreateReferenceFromObject(oValue["Value"]) : null);
                    break;
                case "Cap":
                    sme = new Capability();
                    break;
                case "SML":
                    sme = new SubmodelElementList(
                        orderRelevant: oValue.ContainsKey("OrderRelevant") ? (bool?)oValue["OrderRelevant"] : true,
                        semanticIdListElement: oValue.ContainsKey("SemanticIdListElement") ? CreateReferenceFromObject(oValue["SemanticIdListElement"]) : null,
                        typeValueListElement: Jsonization.Deserialize.AasSubmodelElementsFrom(oValue["TypeValueListElement"]),
                        valueTypeListElement: oValue.ContainsKey("ValueTypeListElement") ? Jsonization.Deserialize.DataTypeDefXsdFrom(oValue["ValueTypeListElement"]) : null,
                        value: new List<ISubmodelElement>());
                    break;
                case "SMC":
                    sme = new SubmodelElementCollection(
                        value: new List<ISubmodelElement>());
                    break;
                case "Ent":
                    var spec = new List<ISpecificAssetId>();
                    if (oValue.ContainsKey("SpecificAssetIds"))
                        foreach (var item in (Nodes.JsonArray) oValue["SpecificAssetIds"])
                            spec.Add(Jsonization.Deserialize.SpecificAssetIdFrom(item));
                    sme = new Entity(
                        statements: new List<ISubmodelElement>(),
                        entityType: Jsonization.Deserialize.EntityTypeFrom(value.First()[1]),
                        globalAssetId: value.First()[0],
                        specificAssetIds: oValue.ContainsKey("SpecificAssetIds") ? spec : null);
                    break;
                case "Evt":
                    sme = new BasicEventElement(
                        observed: oValue.ContainsKey("Observed") ? CreateReferenceFromObject(oValue["Observed"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                        direction: Jsonization.Deserialize.DirectionFrom(oValue["Direction"]),
                        state: Jsonization.Deserialize.StateOfEventFrom(oValue["State"]),
                        messageTopic: oValue.ContainsKey("MessageTopic") ? oValue["MessageTopic"].ToString() : null,
                        messageBroker: oValue.ContainsKey("MessageBroker") ? CreateReferenceFromObject(oValue["MessageBroker"]) : null,
                        lastUpdate: oValue.ContainsKey("LastUpdate") ? oValue["LastUpdate"].ToString() : null,
                        minInterval: oValue.ContainsKey("MinInterval") ? oValue["MinInterval"].ToString() : null,
                        maxInterval: oValue.ContainsKey("MaxInterval") ? oValue["MaxInterval"].ToString() : null);
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
            sme.TimeStamp = smeSet.TimeStamp;
            sme.TimeStampCreate = smeSet.TimeStampCreate;
            sme.TimeStampTree = smeSet.TimeStampTree;
            if (!smeSet.SemanticId.IsNullOrEmpty())
                sme.SemanticId = new Reference(ReferenceTypes.ExternalReference,
                    new List<IKey>() { new Key(KeyTypes.GlobalReference, smeSet.SemanticId) });

            return sme;
        }

        private static Reference CreateReferenceFromObject(Nodes.JsonNode obj)
        {
            var result = Jsonization.Deserialize.ReferenceFrom(obj);
            if (result != null)
                return result;
            else
                return new Reference(ReferenceTypes.ExternalReference, new List<IKey>());
        }

        public static string GetAASXPath(string aasId = "", string submodelId = "")
        {
            using var db = new AasContext();
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