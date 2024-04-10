using AasCore.Aas3_0;
using AdminShellNS;
using System.Globalization;
using static AasCore.Aas3_0.Visitation;
using Extensions;
using System.IO.Compression;

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
                        string fcopyt = name + "__thumbnail";
                        fcopyt = fcopyt.Replace("/", "_");
                        fcopyt = fcopyt.Replace(".", "_");
                        Uri dummy = null;
                        using (var st = asp.GetLocalThumbnailStream(ref dummy, init: true))
                        {
                            Console.WriteLine("Copy " + AasContext._dataPath + "/files/" + fcopyt + ".dat");
                            var fst = System.IO.File.Create(AasContext._dataPath + "/files/" + fcopyt + ".dat");
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
            if (aasxDB == null || asp == null || asp.AasEnv == null || asp.AasEnv.AssetAdministrationShells == null || asp.AasEnv.AssetAdministrationShells.Count <= 0)
                return;

            foreach (IAssetAdministrationShell aas in asp.AasEnv.AssetAdministrationShells)
            {
                if (aas.IdShort != null && aas.IdShort != "" && aas.IdShort.ToLower().Contains("globalsecurity"))
                {
                    // AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
                    // AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
                    continue;
                }

                if (aas.Id == null || aas.Id == "" || aas.AssetInformation.GlobalAssetId == null || aas.AssetInformation.GlobalAssetId == "")
                    continue;
                new VisitorAASX(aasxDB: aasxDB).Visit(aas);

                if (asp.AasEnv.Submodels == null || asp.AasEnv.Submodels.Count <= 0)
                    continue;
                foreach (var sm in asp.AasEnv.Submodels)
                    new VisitorAASX(aasxDB: aasxDB).Visit(sm);
            }
        }

        private string shortType(ISubmodelElement sme)
        {
            if (sme is Capability)
                return "Cap";
            if (sme is Property)
                return "Prop";
            if (sme is MultiLanguageProperty)
                return "MLP";
            if (sme is AasCore.Aas3_0.Range)
                return "Range";
            if (sme is Entity)
                return "Ent";
            if (sme is AasCore.Aas3_0.File)
                return "File";
            if (sme is Blob)
                return "Blob";
            if (sme is Operation)
                return "Opr";
            if (sme is ReferenceElement)
                return ("Ref");
            if (sme is RelationshipElement)
                return "Rel";
            if (sme is AnnotatedRelationshipElement)
                return "RelA";
            if (sme is SubmodelElementCollection)
                return "SMC";
            if (sme is SubmodelElementList)
                return "SML";
            return null;
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
                if (v != "")
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
                IdShort = sme.IdShort
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
        }
        public override void VisitAdministrativeInformation(IAdministrativeInformation that)
        {
        }
        public override void VisitQualifier(IQualifier that)
        {
        }
        public override void VisitAssetAdministrationShell(IAssetAdministrationShell that)
        {
            var aasDB = new AASSet
            {
                Identifier = that.Id,
                IdShort = that.IdShort,
                AssetKind = that.AssetInformation.AssetKind.ToString(),
                GlobalAssetId = that.AssetInformation.GlobalAssetId,
            };
            _aasxDB.AASSets.Add(aasDB);
        }
        public override void VisitAssetInformation(IAssetInformation that)
        {
        }
        public override void VisitResource(IResource that)
        {
        }
        public override void VisitSpecificAssetId(ISpecificAssetId that)
        {
        }
        public override void VisitSubmodel(ISubmodel that)
        {
            var semanticId = that.SemanticId.GetAsIdentifier();
            if (semanticId == null)
                semanticId = "";
            _smDB = new SMSet
            {
                AASSet = _aasxDB.AASSets.First(),
                SemanticId = semanticId,
                Identifier = that.Id,
                IdShort = that.IdShort
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
        }
        public override void VisitBasicEventElement(IBasicEventElement that)
        {
        }
        public override void VisitOperation(IOperation that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitOperation(that);
        }
        public override void VisitOperationVariable(IOperationVariable that)
        {
        }
        public override void VisitCapability(ICapability that)
        {
            SMESet smeSet = collectSMEData(that);
            base.VisitCapability(that);
        }
        public override void VisitConceptDescription(IConceptDescription that)
        {
        }
        public override void VisitReference(AasCore.Aas3_0.IReference that)
        {
        }
        public override void VisitKey(IKey that)
        {
        }

        public override void VisitEnvironment(AasCore.Aas3_0.IEnvironment that)
        {
        }

        public override void VisitLangStringNameType(
            ILangStringNameType that
        )
        { }
        public override void VisitLangStringTextType(
            ILangStringTextType that
        )
        { }
        public override void VisitEmbeddedDataSpecification(
            IEmbeddedDataSpecification that
        )
        { }
        public override void VisitLevelType(
            ILevelType that
        )
        { }
        public override void VisitValueReferencePair(
            IValueReferencePair that
        )
        { }
        public override void VisitValueList(
            IValueList that
        )
        { }
        public override void VisitLangStringPreferredNameTypeIec61360(
            ILangStringPreferredNameTypeIec61360 that
        )
        { }
        public override void VisitLangStringShortNameTypeIec61360(
            ILangStringShortNameTypeIec61360 that
        )
        { }
        public override void VisitLangStringDefinitionTypeIec61360(
            ILangStringDefinitionTypeIec61360 that
        )
        { }
        public override void VisitDataSpecificationIec61360(
            IDataSpecificationIec61360 that
        )
        { }
    }
}
