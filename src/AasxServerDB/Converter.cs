namespace AasxServerDB
{
    using System.Text;
    using AasCore.Aas3_0;
    using AasxServerDB.Entities;
    using AdminShellNS;
    using Extensions;
    using Microsoft.IdentityModel.Tokens;
    using Nodes = System.Text.Json.Nodes;

    public class Converter
    {
        public static AdminShellPackageEnv? GetPackageEnv(int envId)
        {
            // env
            var env = new AdminShellPackageEnv();
            env.AasEnv.ConceptDescriptions = new List<IConceptDescription>();
            env.AasEnv.AssetAdministrationShells = new List<IAssetAdministrationShell>();
            env.AasEnv.Submodels = new List<ISubmodel>();

            // db
            var db = new AasContext();

            // path
            env.SetFilename(fileName: GetAASXPath(envId));

            // cd
            var cdDBList = db.CDSets.Where(cd => cd.EnvId == envId).ToList();
            foreach (var cd in cdDBList.Select(selector: cdDB => GetConceptDescription(cdDB: cdDB)))
            {
                env.AasEnv.ConceptDescriptions?.Add(cd);
            }

            // aas
            var aasDBList = db.AASSets.Where(cd => cd.EnvId == envId).ToList();
            foreach (var aasDB in aasDBList)
            {
                var aas = GetAssetAdministrationShell(aasDB: aasDB);
                env.AasEnv.AssetAdministrationShells?.Add(aas);

                // sm
                var smAASDBList = db.SMSets.Where(sm => sm.EnvId == envId && sm.AASId == aasDB.Id).ToList();
                foreach (var sm in smAASDBList.Select(selector: smDB => GetSubmodel(smDB: smDB)))
                {
                    aas.Submodels?.Add(sm.GetReference());
                }
            }

            // sm
            var smDBList = db.SMSets.Where(cd => cd.EnvId == envId).ToList();
            foreach (var sm in smDBList.Select(selector: submodelDB => GetSubmodel(smDB: submodelDB)))
            {
                env.AasEnv.Submodels?.Add(sm);
            }

            return env;
        }

        public static ConceptDescription? GetConceptDescription(CDSet? cdDB = null, string cdIdentifier = "")
        {
            var db = new AasContext();
            if (!cdIdentifier.IsNullOrEmpty())
            {
                var cdList = db.CDSets.Where(cd => cd.Identifier == cdIdentifier).ToList();
                if (cdList.Count == 0)
                    return null;
                cdDB = cdList.First();
            }

            if (cdDB == null)
                return null;

            var cd = new ConceptDescription(
                idShort: cdDB.IdShort,
                displayName: cdDB.DisplayName,
                category: cdDB.Category,
                description: cdDB.Description,
                extensions: cdDB.Extensions,
                id: cdDB.Identifier,
                administration: cdDB.Administration,
                isCaseOf: cdDB.IsCaseOf,
                embeddedDataSpecifications: cdDB.EmbeddedDataSpecifications
            )
            {
                TimeStampCreate = cdDB.TimeStampCreate,
                TimeStamp = cdDB.TimeStamp,
                TimeStampTree = cdDB.TimeStampTree,
                TimeStampDelete = cdDB.TimeStampDelete
            };

            return cd;
        }

        public static AssetAdministrationShell? GetAssetAdministrationShell(AASSet? aasDB = null, string aasIdentifier = "")
        {
            var db = new AasContext();
            if (!aasIdentifier.IsNullOrEmpty())
            {
                var aasList = db.AASSets.Where(cd => cd.Identifier == aasIdentifier).ToList();
                if (aasList.Count == 0)
                    return null;
                aasDB = aasList.First();
            }

            if (aasDB == null)
                return null;

            var aas = new AssetAdministrationShell(
                idShort: aasDB.IdShort,
                displayName: aasDB.DisplayName,
                category: aasDB.Category,
                description: aasDB.Description,
                extensions: aasDB.Extensions,
                id: aasDB.Identifier,
                administration: aasDB.Administration,
                embeddedDataSpecifications: aasDB.EmbeddedDataSpecifications,
                derivedFrom: aasDB.DerivedFrom,
                assetInformation: new AssetInformation(
                    assetKind: aasDB.AssetKind != null ? (AssetKind)aasDB.AssetKind : AssetKind.Instance,
                    specificAssetIds: aasDB.SpecificAssetIds,
                    globalAssetId: aasDB.GlobalAssetId,
                    assetType: aasDB.AssetType,
                    defaultThumbnail: aasDB.DefaultThumbnail
                ),
                submodels: new List<IReference>()
            )
            {
                TimeStampCreate = aasDB.TimeStampCreate,
                TimeStamp = aasDB.TimeStamp,
                TimeStampTree = aasDB.TimeStampTree,
                TimeStampDelete = aasDB.TimeStampDelete
            };

            return aas;
        }

        public static Submodel? GetSubmodel(SMSet? smDB = null, string smIdentifier = "")
        {
            using (var db = new AasContext())
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

                var submodel = new Submodel(
                    idShort: smDB.IdShort,
                    displayName: smDB.DisplayName,
                    category: smDB.Category,
                    description: smDB.Description,
                    extensions: smDB.Extensions,
                    id: smDB.Identifier,
                    administration: smDB.Administration,
                    kind: smDB.Kind,
                    semanticId: !smDB.SemanticId.IsNullOrEmpty() ?
                        new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, smDB.SemanticId) }) : null,
                    supplementalSemanticIds: smDB.SupplementalSemanticIds,
                    qualifiers: smDB.Qualifiers,
                    embeddedDataSpecifications: smDB.EmbeddedDataSpecifications,
                    submodelElements: new List<ISubmodelElement>()
                );

                LoadSME(submodel, null, null, SMEList);

                submodel.TimeStampCreate = smDB.TimeStampCreate;
                submodel.TimeStamp       = smDB.TimeStamp;
                submodel.TimeStampTree   = smDB.TimeStampTree;
                submodel.TimeStampDelete = smDB.TimeStampDelete;
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
                        foreach (var item in (Nodes.JsonArray)oValue["SpecificAssetIds"])
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

            sme.IdShort         = smeSet.IdShort;
            sme.DisplayName     = smeSet.DisplayName;
            sme.Category        = smeSet.Category;
            sme.Description     = smeSet.Description;
            sme.Extensions      = smeSet.Extensions;
            sme.SemanticId      = !smeSet.SemanticId.IsNullOrEmpty() ?
                new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, smeSet.SemanticId) }) : null;
            sme.SupplementalSemanticIds = smeSet.SupplementalSemanticIds;
            sme.Qualifiers      = smeSet.Qualifiers;
            sme.EmbeddedDataSpecifications = smeSet.EmbeddedDataSpecifications;
            sme.TimeStampCreate = smeSet.TimeStampCreate;
            sme.TimeStamp       = smeSet.TimeStamp;
            sme.TimeStampTree   = smeSet.TimeStampTree;
            sme.TimeStampDelete = smeSet.TimeStampDelete;
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

        public static string GetAASXPath(int? envId = null, string cdId = "", string aasId = "", string smId = "")
        {
            using var db = new AasContext();
            if (!cdId.IsNullOrEmpty())
            {
                var cdDBList = db.CDSets.Where(s => s.Identifier == cdId);
                if (cdDBList.Count() > 0)
                    envId = cdDBList.First().EnvId;
            }

            if (!smId.IsNullOrEmpty())
            {
                var smDBList = db.SMSets.Where(s => s.Identifier == smId);
                if (smDBList.Count() > 0)
                    envId = smDBList.First().EnvId;
            }

            if (!aasId.IsNullOrEmpty())
            {
                var aasDBList = db.AASSets.Where(a => a.Identifier == aasId);
                if (aasDBList.Any())
                    envId = aasDBList.First().EnvId;
            }

            if (envId == null)
                return string.Empty;

            var path = db.EnvSets.Where(e => e.Id == envId).Select(e => e.Path).FirstOrDefault();
            return path ?? string.Empty;
        }
    }
}