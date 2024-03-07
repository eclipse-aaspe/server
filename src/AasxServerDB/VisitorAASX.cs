using AasCore.Aas3_0;
using AdminShellNS;
using System.Globalization;
using static AasCore.Aas3_0.Visitation;
using Extensions;

namespace AasxServerDB
{
    public class VisitorAASX : VisitorThrough
    {
        AasContext _db = null;
        DbConfigSet _dbConfig = null;
        long _smNum = 0;
        List<long> _parentNum = null;

        public VisitorAASX(AasContext db, DbConfigSet dbConfigSet, long smNum)
        {
            _db = db;
            _dbConfig = dbConfigSet;
            _smNum = smNum;
            _parentNum = new List<long>();
        }

        public static void LoadAASInDB(AasContext db, IAssetAdministrationShell aas, long aasxNum, AdminShellPackageEnv asp)
        {
            var dbConfig = db.DbConfigSets.FirstOrDefault();
            LoadAASInDB(db, aas, aasxNum, asp, dbConfig);
        }

        public static void LoadAASInDB(AasContext db, IAssetAdministrationShell aas, long aasxNum, AdminShellPackageEnv asp, DbConfigSet dbConfig)
        {

            long aasNum = ++dbConfig.AasCount;
            var aasDB = new AasSet
            {
                AasNum = aasNum,
                AasId = aas.Id,
                AssetId = aas.AssetInformation.GlobalAssetId,
                AASXNum = aasxNum,
                Idshort = aas.IdShort,
                AssetKind = aas.AssetInformation.AssetKind.ToString()
            };
            db.Add(aasDB);

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

                        long submodelNum = ++dbConfig.SubmodelCount;

                        var submodelDB = new SubmodelSet
                        {
                            SubmodelNum = submodelNum,
                            SubmodelId = sm.Id,
                            SemanticId = semanticId,
                            AASXNum = aasxNum,
                            AasNum = aasNum,
                            Idshort = sm.IdShort
                        };
                        db.Add(submodelDB);

                        VisitorAASX v = new VisitorAASX(db, dbConfig, submodelNum);
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
                return "SEC";
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
        private void getValue(ISubmodelElement sme, long smeNum, out string vt, out string sValue, out long iValue, out double fValue)
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
                    for (int i = 0; i < ls.Count; i++)
                    {
                        var mlpval = new StringValue()
                        {
                            Annotation = ls[i].Language,
                            Value = ls[i].Text,
                            ParentSMENum = smeNum
                        };
                        _db.Add(mlpval);
                    }
                    vt = "S";
                    sValue = v;
                }
            }
            if (sme is AasCore.Aas3_0.Range r)
            {
                v = r.Min;
                // if (v != "")
                //    vt = getValueType(v);

                var v2 = r.Max;
                // var vt2 = "";
                // if (v2 != "")
                //    vt2 = getValueType(v2);

                v += "$$" + v2;
                vt = "S";
                sValue = v;

                /*
                if (vt == "S" || vt2 == "S")
                    vt = "S";
                else if (vt == "I" && vt2 == "I")
                    vt = "I";
                else
                    vt = "F";
                */
            }
        }
        private long collectSMEData(ISubmodelElement sme)
        {
            string st = shortType(sme);
            // Console.WriteLine(st + " idshort " + sme.IdShort);

            long smeNum = ++_dbConfig.SMECount;
            long pn = 0;
            if (_parentNum.Count > 0)
                pn = _parentNum[_parentNum.Count - 1];
            var semanticId = sme.SemanticId.GetAsIdentifier();
            if (semanticId == null)
                semanticId = "";

            string vt = "";
            string sValue = "";
            long iValue = 0;
            double fValue = 0;
            getValue(sme, smeNum, out vt, out sValue, out iValue, out fValue);

            if (vt == "S" && st != "MLP")
            {
                var ValueDB = new StringValue
                {
                    ParentSMENum = smeNum,
                    Value = sValue,
                    Annotation = ""
                };
                _db.Add(ValueDB);
            }
            if (vt == "I")
            {
                var ValueDB = new IntValue
                {
                    ParentSMENum = smeNum,
                    Value = iValue,
                    Annotation = ""
                };
                _db.Add(ValueDB);
            }
            if (vt == "F")
            {
                var ValueDB = new DoubleValue
                {
                    ParentSMENum = smeNum,
                    Value = fValue,
                    Annotation = ""
                };
                _db.Add(ValueDB);
            }

            var smeDB = new SMESet
            {
                SMENum = smeNum,
                SMEType = st,
                SemanticId = semanticId,
                Idshort = sme.IdShort,
                ValueType = vt,
                SubmodelNum = _smNum,
                ParentSMENum = pn
            };
            _db.Add(smeDB);
            return smeNum;
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
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitRelationshipElement(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitSubmodelElementList(ISubmodelElementList that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitSubmodelElementList(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitSubmodelElementCollection(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitProperty(IProperty that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitProperty(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitMultiLanguageProperty(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitRange(AasCore.Aas3_0.IRange that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitRange(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitReferenceElement(IReferenceElement that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitReferenceElement(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitBlob(IBlob that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitBlob(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitFile(AasCore.Aas3_0.IFile that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitFile(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitAnnotatedRelationshipElement(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitEntity(IEntity that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitEntity(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitEventPayload(IEventPayload that)
        {
        }
        public override void VisitBasicEventElement(IBasicEventElement that)
        {
        }
        public override void VisitOperation(IOperation that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitOperation(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitOperationVariable(IOperationVariable that)
        {
        }
        public override void VisitCapability(ICapability that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitCapability(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
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
