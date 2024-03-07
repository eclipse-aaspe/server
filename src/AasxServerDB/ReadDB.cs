using AasCore.Aas3_0;
using static AasCore.Aas3_0.Visitation;
using Extensions;

namespace AasxServerDB
{
    public class DBRead
    {
        public DBRead() { }
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

                /*
                if (sme is RelationshipElement)
                    return "RE";
                if (sme is SubmodelElementList)
                    return "SEL";
                if (sme is SubmodelElementCollection)
                    return "SEC";
                if (sme is MultiLanguageProperty)
                    return "MLP";
                if (sme is ReferenceElement)
                    return ("RE");
                if (sme is AasCore.Aas3_0_RC02.Range)
                    return "R";
                if (sme is Blob)
                    return "B";
                if (sme is File)
                    return "F";
                if (sme is AnnotatedRelationshipElement)
                    return "ARE";
                if (sme is Entity)
                    return "E";
                if (sme is Operation)
                    return "O";
                if (sme is Capability)
                    return "C";
                */
            }
        }
    }
}
