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
        AasContext _db = null;
        List<int> _parentId = null;
        public static int CurrentAASXId { get; set; }
        public static int CurrentAASId { get; set; }
        public static int CurrentSMId { get; set; }
        public static int CurrentSMEId { get; set; }

        public VisitorAASX(AasContext db)
        {
            _db = db;
            _parentId = new List<int>();
        }

        static public void LoadAASXInDB(string filePath, bool createFilesOnly, bool withDbFiles)
        {
            using (AasContext db = new AasContext())
            { 
                using (var asp = new AdminShellPackageEnv(filePath, false, true))
                {
                    if (!createFilesOnly)
                    {
                        var aasxDB = new AASXSet
                        {
                            AASX = filePath
                        };
                        db.Add(aasxDB);
                        if (VisitorAASX.CurrentAASXId == 0)
                        {
                            db.SaveChanges();
                            VisitorAASX.CurrentAASXId = aasxDB.Id;
                        }
                        else
                            CurrentAASXId++;

                        var aas = asp.AasEnv.AssetAdministrationShells[0];

                        // Check security
                        if (aas.IdShort.ToLower().Contains("globalsecurity"))
                        {
                            // AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
                            // AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
                        }
                        else
                        {
                            if (aas.Id != null && aas.Id != "" && 
                                aas.AssetInformation.GlobalAssetId != null && aas.AssetInformation.GlobalAssetId != "")
                                VisitorAASX.LoadAASInDB(db, aas, asp);
                        }
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
        }

        public static void LoadAASInDB(AasContext db, IAssetAdministrationShell aas, AdminShellPackageEnv asp)
        {
            var aasDB = new AASSet
            {
                IdIdentifier = aas.Id,
                GlobalAssetId = aas.AssetInformation.GlobalAssetId,
                AASXId = CurrentAASXId,
                IdShort = aas.IdShort,
                AssetKind = aas.AssetInformation.AssetKind.ToString()
            };
            db.Add(aasDB);
            if (VisitorAASX.CurrentAASId == 0)
            {
                db.SaveChanges();
                VisitorAASX.CurrentAASId = aasDB.Id;
            }
            else
                CurrentAASId++;

            // Iterate submodels
            if (aas.Submodels != null && aas.Submodels.Count > 0)
            {
                foreach (var smr in aas.Submodels)
                {
                    var sm = asp.AasEnv.FindSubmodel(smr);
                    if (sm != null)
                    {
                        var semanticId = sm.SemanticId.GetAsIdentifier();
                        if (semanticId == null)
                            semanticId = "";

                        var smDB = new SMSet
                        {
                            IdIdentifier = sm.Id,
                            SemanticId = semanticId,
                            AASXId = CurrentAASXId,
                            AASId = CurrentAASId,
                            IdShort = sm.IdShort
                        };
                        db.Add(smDB);
                        if (VisitorAASX.CurrentSMId == 0)
                        {
                            db.SaveChanges();
                            VisitorAASX.CurrentSMId = smDB.Id;
                        }
                        else
                            CurrentSMId++;

                        VisitorAASX v = new VisitorAASX(db);
                        v.Visit(sm);
                    }
                }
            }
        }

        private string shortType(ISubmodelElement sme)
        {
            if (sme is Property)
                return ("P");
            if (sme is RelationshipElement)
                return "RE";
            if (sme is SubmodelElementList)
                return "SEL";
            if (sme is SubmodelElementCollection)
                return "SMC";
            if (sme is MultiLanguageProperty)
                return "MLP";
            if (sme is ReferenceElement)
                return ("RE");
            if (sme is AasCore.Aas3_0.Range)
                return "R";
            if (sme is Blob)
                return "B";
            if (sme is AasCore.Aas3_0.File)
                return "F";
            if (sme is AnnotatedRelationshipElement)
                return "ARE";
            if (sme is Entity)
                return "E";
            if (sme is Operation)
                return "O";
            if (sme is Capability)
                return "C";
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
                {
                    vt = getValueAndType(v, out sValue, out iValue, out fValue);
                }
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

        private int collectSMEData(ISubmodelElement sme)
        {
            string st = shortType(sme);
            int pn = 0;
            if (_parentId.Count > 0)
                pn = _parentId[_parentId.Count - 1];
            var semanticId = sme.SemanticId.GetAsIdentifier();
            if (semanticId == null)
                semanticId = "";
            getValue(sme, out string vt, out string sValue, out long iValue, out double fValue);
            var smeDB = new SMESet
            {
                SMEType = st,
                SemanticId = semanticId,
                IdShort = sme.IdShort,
                ValueType = vt,
                SMId = CurrentSMId,
                ParentSMEId = pn
            };
            _db.Add(smeDB);
            if (VisitorAASX.CurrentSMEId == 0)
            {
                _db.SaveChanges();
                VisitorAASX.CurrentSMEId = smeDB.Id;
            }
            else
                CurrentSMEId++;

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
                                Value = ls[i].Text,
                                ParentSMEId = CurrentSMEId
                            };
                            _db.Add(mlpval);
                        }
                    }
                }
            }
            if (vt == "S" && st != "MLP")
            {
                var ValueDB = new SValueSet
                {
                    ParentSMEId = CurrentSMEId,
                    Value = sValue,
                    Annotation = ""
                };
                _db.Add(ValueDB);
            }
            if (vt == "I")
            {
                var ValueDB = new IValueSet
                {
                    ParentSMEId = CurrentSMEId,
                    Value = iValue,
                    Annotation = ""
                };
                _db.Add(ValueDB);
            }
            if (vt == "F")
            {
                var ValueDB = new DValueSet
                {
                    ParentSMEId = CurrentSMEId,
                    Value = fValue,
                    Annotation = ""
                };
                _db.Add(ValueDB);
            }
            return smeDB.Id;
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
            base.VisitSubmodel(that);
        }
        public override void VisitRelationshipElement(IRelationshipElement that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitRelationshipElement(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitSubmodelElementList(ISubmodelElementList that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitSubmodelElementList(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitSubmodelElementCollection(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitProperty(IProperty that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitProperty(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitMultiLanguageProperty(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitRange(AasCore.Aas3_0.IRange that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitRange(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitReferenceElement(IReferenceElement that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitReferenceElement(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitBlob(IBlob that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitBlob(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitFile(AasCore.Aas3_0.IFile that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitFile(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitAnnotatedRelationshipElement(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitEntity(IEntity that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitEntity(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitEventPayload(IEventPayload that)
        {
        }
        public override void VisitBasicEventElement(IBasicEventElement that)
        {
        }
        public override void VisitOperation(IOperation that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitOperation(that);
            _parentId.RemoveAt(_parentId.Count - 1);
        }
        public override void VisitOperationVariable(IOperationVariable that)
        {
        }
        public override void VisitCapability(ICapability that)
        {
            int smeId = collectSMEData(that);
            _parentId.Add(smeId);
            base.VisitCapability(that);
            _parentId.RemoveAt(_parentId.Count - 1);
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
