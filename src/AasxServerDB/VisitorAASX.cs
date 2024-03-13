﻿using AasCore.Aas3_0;
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
        AASXSet _aasxDB = null;
        AASSet _aasDB = null;
        SMSet _smDB = null;
        List<SMESet> _parentSME = null;
        public static int CurrentAASXId { get; set; }
        public static int CurrentAASId { get; set; }
        public static int CurrentSMId { get; set; }
        public static int CurrentSMEId { get; set; }

        public VisitorAASX(AasContext db)
        {
            _db = db;
            _parentSME = new List<SMESet>();
        }

        public VisitorAASX(AasContext db, AASXSet aasxdb)
        {
            _db = db;
            _parentSME = new List<SMESet>();
            _aasxDB = aasxdb;
        }

        static public void LoadAASXInDB(string filePath, bool createFilesOnly, bool withDbFiles)
        {
            using (var asp = new AdminShellPackageEnv(filePath, false, true))
            {
                if (!createFilesOnly)
                {
                    using (AasContext db = new AasContext())
                    {
                        // AASX
                        var aasxDB = new AASXSet
                        {
                            AASX = filePath,
                            AASSets = new List<AASSet>(),
                            SMSets = new List<SMSet>()
                        };

                        // Check security
                        var aas = asp.AasEnv.AssetAdministrationShells[0];
                        if (!aas.IdShort.ToLower().Contains("globalsecurity") && aas.Id != null && aas.Id != "" && aas.AssetInformation.GlobalAssetId != null && aas.AssetInformation.GlobalAssetId != "")
                        {
                            VisitorAASX v = new VisitorAASX(db, aasxDB);
                            v.Visit(aas);
                            

                            // Iterate submodels
                            if (aas.Submodels != null && aas.Submodels.Count > 0)
                            {
                                foreach (var smr in aas.Submodels)
                                {
                                    var sm = asp.AasEnv.FindSubmodel(smr);
                                    if (sm != null)
                                    {
                                        v = new VisitorAASX(db, aasxDB);
                                        v.Visit(sm);
                                        /*var semanticId = sm.SemanticId.GetAsIdentifier();
                                        if (semanticId == null)
                                            semanticId = "";

                                        var smDB = new SMSet
                                        {
                                            AASXSet = aasxDB,
                                            AASSet = aasDB,
                                            SemanticId = semanticId,
                                            IdIdentifier = sm.Id,
                                            IdShort = sm.IdShort
                                        };
                                        aasxDB.SMSets.Add(smDB);
                                        aasDB.SMSets.Add(smDB);

                                        VisitorAASX v = new VisitorAASX(db);
                                        v.Visit(sm);*/
                                    }
                                }
                            }
                        }
                        db.Add(aasxDB);
                        db.SaveChanges();

                        if (aas.IdShort.ToLower().Contains("globalsecurity"))
                        {
                            // AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
                            // AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
                        }
                    }
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
            db.AASSets.Add(aasDB);
            if (VisitorAASX.CurrentAASId == 0)
            {
                db.SaveChanges();
                VisitorAASX.CurrentAASId = aasDB.Id;
            }
            else
                CurrentAASId++;
            // Iterate submodels
            /*if (aas.Submodels != null && aas.Submodels.Count > 0)
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
            }*/

            db.SaveChanges();
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

        private SMESet collectSMEData(ISubmodelElement sme)
        {
            string st = shortType(sme);
            SMESet pn = null;
            if (_parentSME.Count > 0)
                pn = _parentSME[_parentSME.Count - 1];
            var semanticId = sme.SemanticId.GetAsIdentifier();
            if (semanticId == null)
                semanticId = "";
            getValue(sme, out string vt, out string sValue, out long iValue, out double fValue);
            var smeDB = new SMESet
            {
                SMSet = _smDB,
                ParentSMESet = pn,
                SMEType = st,
                SemanticId = semanticId,
                IdShort = sme.IdShort,
                ValueType = vt,
                SMId = CurrentSMId,
                IValueSets = new List<IValueSet>(),
                DValueSets = new List<DValueSet>(),
                SValueSets = new List<SValueSet>()
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
                                SMESet = smeDB,
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
                    SMESet = smeDB,
                    Value = sValue,
                    Annotation = ""
                };
                smeDB.SValueSets.Add(ValueDB);
            }
            if (vt == "I")
            {
                var ValueDB = new IValueSet
                {
                    SMESet = smeDB,
                    Value = iValue,
                    Annotation = ""
                };
                smeDB.IValueSets.Add(ValueDB);
            }
            if (vt == "F")
            {
                var ValueDB = new DValueSet
                {
                    SMESet = smeDB,
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
                AASXSet = _aasxDB,
                IdIdentifier = that.Id,
                IdShort = that.IdShort,
                AssetKind = that.AssetInformation.AssetKind.ToString(),
                GlobalAssetId = that.AssetInformation.GlobalAssetId,
                SMSets = new List<SMSet>()
            };
            _aasxDB.AASSets.Add(aasDB);
            // base.VisitAssetAdministrationShell(that);
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
                AASXSet = _aasxDB,
                AASSet = _aasDB,
                SemanticId = semanticId,
                IdIdentifier = that.Id,
                IdShort = that.IdShort,
                SMESets = new List<SMESet>()
            };
            _aasxDB.SMSets.Add(_smDB);
            _aasxDB.AASSets.Last().SMSets.Add(_smDB);

            _parentSME = new List<SMESet>();

            base.VisitSubmodel(that);
        }
        public override void VisitRelationshipElement(IRelationshipElement that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitRelationshipElement(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitSubmodelElementList(ISubmodelElementList that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitSubmodelElementList(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitSubmodelElementCollection(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitProperty(IProperty that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitProperty(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitMultiLanguageProperty(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitRange(AasCore.Aas3_0.IRange that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitRange(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitReferenceElement(IReferenceElement that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitReferenceElement(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitBlob(IBlob that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitBlob(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitFile(AasCore.Aas3_0.IFile that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitFile(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitAnnotatedRelationshipElement(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitEntity(IEntity that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitEntity(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
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
            _parentSME.Add(smeSet);
            base.VisitOperation(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
        }
        public override void VisitOperationVariable(IOperationVariable that)
        {
        }
        public override void VisitCapability(ICapability that)
        {
            SMESet smeSet = collectSMEData(that);
            _parentSME.Add(smeSet);
            base.VisitCapability(that);
            _parentSME.RemoveAt(_parentSME.Count - 1);
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
