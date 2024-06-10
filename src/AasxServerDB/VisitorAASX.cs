using AasCore.Aas3_0;
using AdminShellNS;
using System.Globalization;
using static AasCore.Aas3_0.Visitation;
using Extensions;
using System.IO.Compression;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerDB
{
    public class VisitorAASX : VisitorThrough
    {
        AASXSet _aasxDB;
        SMSet _smDB;
        SMESet _parSME = null;

        public VisitorAASX(AASXSet? aasxDB = null)
        {
            _aasxDB = aasxDB;
        }

        static public void LoadAASXInDB(string filePath, bool createFilesOnly, bool withDbFiles)
        {
            using (var asp = new AdminShellPackageEnv(filePath, false, true))
            {
                if (!createFilesOnly)
                {
                    var aasxDB = new AASXSet
                    {
                        AASX = filePath
                    };
                    LoadAASInDB(asp, aasxDB);

                    using AasContext db = new AasContext();
                    db.Add(aasxDB);
                    db.SaveChanges();
                }

                if (withDbFiles)
                {
                    string name = Path.GetFileName(filePath);
                    try
                    {
                        string temporaryFileName = name + "__thumbnail";
                        temporaryFileName = temporaryFileName.Replace("/", "_");
                        temporaryFileName = temporaryFileName.Replace(".", "_");
                        Uri dummy = null;
                        using (var st = asp.GetLocalThumbnailStream(ref dummy, init: true))
                        {
                            Console.WriteLine("Copy " + AasContext._dataPath + "/files/" + temporaryFileName + ".dat");
                            var fst = System.IO.File.Create(AasContext._dataPath + "/files/" + temporaryFileName + ".dat");
                            if (st != null)
                            {
                                st.CopyTo(fst);
                            }
                        }
                    }
                    catch { }

                    using (var fileStream = new FileStream(AasContext._dataPath + "/files/" + name + ".zip", FileMode.Create))
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                    {
                        var files = asp.GetListOfSupplementaryFiles();
                        foreach (var f in files)
                        {
                            try
                            {
                                using (var s = asp.GetLocalStreamFromPackage(f.Uri.OriginalString, init: true))
                                {
                                    var archiveFile = archive.CreateEntry(f.Uri.OriginalString);
                                    Console.WriteLine("Copy " + AasContext._dataPath + "/" + name + "/" + f.Uri.OriginalString);

                                    using var archiveStream = archiveFile.Open();
                                    s.CopyTo(archiveStream);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        public static void LoadAASInDB(AdminShellPackageEnv asp, AASXSet aasxDB)
        {
            if (aasxDB == null || asp == null || asp.AasEnv == null)
                return;

            if (asp.AasEnv.AssetAdministrationShells != null)
                foreach (var aas in asp.AasEnv.AssetAdministrationShells)
                    if (!aas.Id.IsNullOrEmpty() && !aas.AssetInformation.GlobalAssetId.IsNullOrEmpty() &&
                        !(!aas.IdShort.IsNullOrEmpty() && aas.IdShort.ToLower().Contains("globalsecurity")))
                        new VisitorAASX(aasxDB: aasxDB).Visit(aas);

            if (asp.AasEnv.Submodels != null)
                foreach (var sm in asp.AasEnv.Submodels)
                    if (!sm.Id.IsNullOrEmpty())
                        new VisitorAASX(aasxDB: aasxDB).Visit(sm);

            if (asp.AasEnv.AssetAdministrationShells != null && asp.AasEnv.Submodels != null)
                foreach (var aas in asp.AasEnv.AssetAdministrationShells)
                {
                    var aasDB = aasxDB.AASSets.FirstOrDefault(aasV => aas.Id == aasV.Identifier);
                    if (aasDB != null && aas.Submodels != null)
                        foreach (var smRef in aas.Submodels)
                        {
                            if (smRef.Keys != null && smRef.Keys.Count > 0)
                            {
                                var smDB = aasxDB.SMSets.FirstOrDefault(smV => smRef.Keys[0].Value == smV.Identifier);
                                if (smDB != null)
                                    smDB.AASSet = aasDB;
                            }
                        }
                }
        }

        private string shortType(ISubmodelElement sme)
        {
            switch (sme)
            {
                case Capability:
                    return "Cap";
                case Property:
                    return "Prop";
                case MultiLanguageProperty:
                    return "MLP";
                case AasCore.Aas3_0.Range:
                    return "Range";
                case Entity:
                    return "Ent";
                case AasCore.Aas3_0.File:
                    return "File";
                case Blob:
                    return "Blob";
                case Operation:
                    return "Opr";
                case ReferenceElement:
                    return "Ref";
                case RelationshipElement:
                    return "Rel";
                case AnnotatedRelationshipElement:
                    return "RelA";
                case SubmodelElementCollection:
                    return "SMC";
                case SubmodelElementList:
                    return "SML";
                default:
                    return null;
            }
        }

        private string getValueAndType(string v, out string sValue, out long iValue, out double fValue)
        {
            sValue = "";
            iValue = 0;
            fValue = 0;

            if (v.All(char.IsDigit) && v.Length <= 10)
            {
                try
                {
                    iValue = Convert.ToInt64(v);
                    return ("I");
                }
                catch
                {
                    sValue = v;
                    return "S";
                }
            }

            if (v.Contains("."))
            {
                string legal = "012345679.E";

                foreach (var c in v)
                {
                    if (Char.IsDigit(c))
                        continue;
                    if (c == '.')
                        continue;
                    if (!legal.Contains(c))
                    {
                        sValue = v;
                        return "S";
                    }
                }

                try
                {
                    var decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    v = v.Replace(".", decSep);
                    v = v.Replace(",", decSep);
                    fValue = Convert.ToDouble(v);
                    return "F";
                }
                catch { }
            }

            sValue = v;
            return "S";
        }

        private void getValue(ISubmodelElement sme, out string vt, out string sValue, out long iValue, out double fValue)
        {
            sValue = "";
            iValue = 0;
            fValue = 0;
            vt = "";
            string v = "";

            if (sme is Property p)
            {
                v = sme.ValueAsText();
                if (!v.IsNullOrEmpty())
                    vt = getValueAndType(v, out sValue, out iValue, out fValue);
            }

            if (sme is AasCore.Aas3_0.File f)
            {
                v = f.Value;
                vt = "S";
                sValue = v;
            }

            if (sme is MultiLanguageProperty mlp)
            {
                var ls = mlp.Value;
                if (ls != null)
                {
                    vt = "S";
                    sValue = v;
                }
            }

            if (sme is AasCore.Aas3_0.Range r)
            {
                v = r.Min;
                var v2 = r.Max;
                v += "$$" + v2;
                vt = "S";
                sValue = v;
            }
        }

        private SMESet collectSMEData(ISubmodelElement sme)
        {
            DateTime currentDataTime = DateTime.UtcNow;
            DateTime timeStamp = (sme.TimeStamp == default(DateTime)) ? currentDataTime : sme.TimeStamp;
            DateTime timeStampCreate = (sme.TimeStampCreate == default(DateTime)) ? currentDataTime : sme.TimeStampCreate;
            DateTime timeStampTree = (sme.TimeStampTree == default(DateTime)) ? currentDataTime : sme.TimeStampTree;
            string st = shortType(sme);
            var semanticId = sme.SemanticId.GetAsIdentifier();
            if (semanticId == null)
                semanticId = "";
            getValue(sme, out string vt, out string sValue, out long iValue, out double fValue);
            var smeDB = new SMESet
            {
                ParentSME = _parSME,
                SMEType = st,
                ValueType = vt,
                SemanticId = semanticId,
                IdShort = sme.IdShort,
                TimeStamp = timeStamp,
                TimeStampCreate = timeStampCreate,
                TimeStampTree = timeStampTree
            };
            _smDB.SMESets.Add(smeDB);

            if (vt == "S" && st == "MLP")
            {
                if (sme is MultiLanguageProperty mlp)
                {
                    var ls = mlp.Value;
                    if (ls != null)
                    {
                        for (int i = 0; i < ls.Count; i++)
                        {
                            var mlpval = new SValueSet()
                            {
                                Annotation = ls[i].Language,
                                Value = ls[i].Text
                            };
                            smeDB.SValueSets.Add(mlpval);
                        }
                    }
                }
            }
            if (vt == "S" && st != "MLP")
            {
                var ValueDB = new SValueSet
                {
                    Value = sValue,
                    Annotation = ""
                };
                smeDB.SValueSets.Add(ValueDB);
            }
            if (vt == "I")
            {
                var ValueDB = new IValueSet
                {
                    Value = iValue,
                    Annotation = ""
                };
                smeDB.IValueSets.Add(ValueDB);
            }
            if (vt == "F")
            {
                var ValueDB = new DValueSet
                {
                    Value = fValue,
                    Annotation = ""
                };
                smeDB.DValueSets.Add(ValueDB);
            }
            return smeDB;
        }
        
        public override void VisitExtension(IExtension that)
        {
            // not supported in the db yet
           /* Console.WriteLine("IExtension");
            base.VisitExtension(that);*/
        }
        public override void VisitAdministrativeInformation(IAdministrativeInformation that)
        {
            // not supported in the db yet
            /*Console.WriteLine("IAdministrativeInformation");
            base.VisitAdministrativeInformation(that);*/
        }
        public override void VisitQualifier(IQualifier that)
        {
            // not supported in the db yet
            /*Console.WriteLine("IQualifier");
            base.VisitQualifier(that);*/
        }
        public override void VisitAssetAdministrationShell(IAssetAdministrationShell that)
        {
            DateTime currentDataTime = DateTime.UtcNow;
            DateTime timeStamp = (that.TimeStamp == default(DateTime))? currentDataTime : that.TimeStamp;
            DateTime timeStampCreate = (that.TimeStampCreate == default(DateTime)) ? currentDataTime : that.TimeStampCreate;
            DateTime timeStampTree = (that.TimeStampTree == default(DateTime)) ? currentDataTime : that.TimeStampTree;
            var aasDB = new AASSet
            {
                Identifier = that.Id,
                IdShort = that.IdShort,
                AssetKind = that.AssetInformation.AssetKind.ToString(),
                GlobalAssetId = that.AssetInformation.GlobalAssetId,
                TimeStamp = timeStamp,
                TimeStampCreate = timeStampCreate,
                TimeStampTree = timeStampTree
            };
            _aasxDB.AASSets.Add(aasDB);
        }
        public override void VisitAssetInformation(IAssetInformation that)
        {
            // not supported in the db yet
            /*Console.WriteLine("AssetInformation");
            base.VisitAssetInformation(that);*/
        }
        public override void VisitResource(IResource that)
        {
            // not supported in the db yet
            /*Console.WriteLine("Resource");
            base.VisitResource(that);*/
        }
        public override void VisitSpecificAssetId(ISpecificAssetId that)
        {
            // not supported in the db yet
            /*Console.WriteLine("SpecificAssetId");
            base.VisitSpecificAssetId(that);*/
        }
        public override void VisitSubmodel(ISubmodel that)
        {
            DateTime currentDataTime = DateTime.UtcNow;
            DateTime timeStamp = (that.TimeStamp == default(DateTime)) ? currentDataTime : that.TimeStamp;
            DateTime timeStampCreate = (that.TimeStampCreate == default(DateTime)) ? currentDataTime : that.TimeStampCreate;
            DateTime timeStampTree = (that.TimeStampTree == default(DateTime)) ? currentDataTime : that.TimeStampTree;
            var semanticId = that.SemanticId.GetAsIdentifier();
            if (semanticId == null)
                semanticId = "";
            _smDB = new SMSet
            {
                SemanticId = semanticId,
                Identifier = that.Id,
                IdShort = that.IdShort,
                TimeStamp = timeStamp,
                TimeStampCreate = timeStampCreate,
                TimeStampTree = timeStampTree
            };
            _aasxDB.SMSets.Add(_smDB);
            base.VisitSubmodel(that);
        }
        public override void VisitRelationshipElement(IRelationshipElement that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitRelationshipElement(that);
        }
        public override void VisitSubmodelElementList(ISubmodelElementList that)
        {
            SMESet smeSet = collectSMEData(that);
            smeSet.ParentSME = _parSME;
            _parSME = smeSet;
            base.VisitSubmodelElementList(that);
            _parSME = smeSet.ParentSME;
        }
        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that)
        {
            SMESet smeSet = collectSMEData(that);
            smeSet.ParentSME = _parSME;
            _parSME = smeSet;
            base.VisitSubmodelElementCollection(that);
            _parSME = smeSet.ParentSME;
        }
        public override void VisitProperty(IProperty that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitProperty(that);
        }
        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitMultiLanguageProperty(that);
        }
        public override void VisitRange(AasCore.Aas3_0.IRange that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitRange(that);
        }
        public override void VisitReferenceElement(IReferenceElement that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitReferenceElement(that);
        }
        public override void VisitBlob(IBlob that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitBlob(that);
        }
        public override void VisitFile(AasCore.Aas3_0.IFile that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitFile(that);
        }
        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitAnnotatedRelationshipElement(that);
        }
        public override void VisitEntity(IEntity that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitEntity(that);
        }
        public override void VisitEventPayload(IEventPayload that)
        {
            // not supported in the db yet
            /*Console.WriteLine("EventPayload");
            base.VisitEventPayload(that);*/
        }
        public override void VisitBasicEventElement(IBasicEventElement that)
        {
            // not supported in the db yet
            /*Console.WriteLine("BasicEventElement");
            base.VisitBasicEventElement(that);*/
        }
        public override void VisitOperation(IOperation that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitOperation(that);
        }
        public override void VisitOperationVariable(IOperationVariable that)
        {
            // not supported in the db yet
            /*Console.WriteLine("OperationVariable");
            base.VisitOperationVariable(that);*/
        }
        public override void VisitCapability(ICapability that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitCapability(that);
        }
        public override void VisitConceptDescription(IConceptDescription that)
        {
            // not supported in the db yet
            /*Console.WriteLine("ConceptDescription");
            base.VisitConceptDescription(that);*/
        }
        public override void VisitReference(AasCore.Aas3_0.IReference that)
        {
            // not supported in the db yet
            /*Console.WriteLine("Reference");
            base.VisitReference(that);*/
        }
        public override void VisitKey(IKey that)
        {
            // not supported in the db yet
            /*Console.WriteLine("Key");
            base.VisitKey(that);*/
        }

        public override void VisitEnvironment(AasCore.Aas3_0.IEnvironment that)
        {
            // not supported in the db yet
            /*Console.WriteLine("Environment");
            base.VisitEnvironment(that);*/
        }

        public override void VisitLangStringNameType(ILangStringNameType that)
        {
            // not supported in the db yet
            /*Console.WriteLine("LangStringNameType");
            base.VisitLangStringNameType(that);*/
        }
        public override void VisitLangStringTextType(ILangStringTextType that)
        {
            // not supported in the db yet
            /*Console.WriteLine("LangStringTextType");
            base.VisitLangStringTextType(that);*/
        }
        public override void VisitEmbeddedDataSpecification(IEmbeddedDataSpecification that)
        {
            // not supported in the db yet
            /*Console.WriteLine("EmbeddedDataSpecification");
            base.VisitEmbeddedDataSpecification(that);*/
        }
        public override void VisitLevelType(ILevelType that)
        {
            // not supported in the db yet
            /*Console.WriteLine("LevelType");
            base.VisitLevelType(that);*/
        }
        public override void VisitValueReferencePair(IValueReferencePair that)
        {
            // not supported in the db yet
            /*Console.WriteLine("ValueReferencePair");
            base.VisitValueReferencePair(that);*/
        }
        public override void VisitValueList(IValueList that)
        {
            // not supported in the db yet
            /*Console.WriteLine("ValueList");
            base.VisitValueList(that);*/
        }
        public override void VisitLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that)
        {
            // not supported in the db yet
            /*Console.WriteLine("LangStringPreferredNameTypeIec61360");
            base.VisitLangStringPreferredNameTypeIec61360(that);*/
        }
        public override void VisitLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that)
        {
            // not supported in the db yet
            /*Console.WriteLine("LangStringShortNameTypeIec61360");
            base.VisitLangStringShortNameTypeIec61360(that);*/
        }
        public override void VisitLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that)
        {
            // not supported in the db yet
            /*Console.WriteLine("LangStringDefinitionTypeIec61360");
            base.VisitLangStringDefinitionTypeIec61360(that);*/
        }
        public override void VisitDataSpecificationIec61360(IDataSpecificationIec61360 that)
        {
            // not supported in the db yet
            /*Console.WriteLine("DataSpecificationIec61360");
            base.VisitDataSpecificationIec61360(that);*/
        }
    }
}
