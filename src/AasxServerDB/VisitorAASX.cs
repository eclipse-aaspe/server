using AasCore.Aas3_0;
using AdminShellNS;
using System.Globalization;
using static AasCore.Aas3_0.Visitation;
using Extensions;
using System.IO.Compression;
using Microsoft.IdentityModel.Tokens;
using static System.Net.Mime.MediaTypeNames;
using AasxServerDB.Entities;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Collections;
using System.Security.AccessControl;
using TimeStamp;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AasxServerDB
{
    public class VisitorAASX : VisitorThrough
    {
        AASXSet? _aasxDB;
        SMSet? _smDB;
        SMESet? _parSME = null;
        /* 1. Version
        string _oprPrefix = string.Empty;*/

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
                    var aasxDB = new AASXSet {AASX = filePath};
                    LoadAASInDB(asp, aasxDB);

                    using AasContext db = new AasContext();
                    db.Add(aasxDB);
                    db.SaveChanges();
                }

                if (withDbFiles)
                {
                    var name = Path.GetFileName(filePath);
                    try
                    {
                        var temporaryFileName = name + "__thumbnail";
                        temporaryFileName = temporaryFileName.Replace("/", "_");
                        temporaryFileName = temporaryFileName.Replace(".", "_");
                        Uri? dummy = null;
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
                                var smDB = aasxDB.SMSets.FirstOrDefault(smV => smRef.Keys[ 0 ].Value == smV.Identifier);
                                if (smDB != null)
                                    smDB.AASSet = aasDB;
                            }
                        }
                }

            if (asp.AasEnv.ConceptDescriptions != null)
                foreach (var cd in asp.AasEnv.ConceptDescriptions)
                    if (cd != null)
                        new VisitorAASX(aasxDB: aasxDB).Visit(cd);
        }

        private string shortSMEType(ISubmodelElement sme)
        {
            return sme switch
                   {
                       RelationshipElement          => "Rel",
                       AnnotatedRelationshipElement => "RelA",
                       Property                     => "Prop",
                       MultiLanguageProperty        => "MLP",
                       AasCore.Aas3_0.Range         => "Range",
                       Blob                         => "Blob",
                       AasCore.Aas3_0.File          => "File",
                       ReferenceElement             => "Ref",
                       Capability                   => "Cap",
                       SubmodelElementList          => "SML",
                       SubmodelElementCollection    => "SMC",
                       Entity                       => "Ent",
                       BasicEventElement            => "Evt",
                       Operation                    => "Opr",
                       _                            => string.Empty
                   };
        }

        private string shortValueType(DataTypeDefXsd? dataType)
        {
            return dataType switch
                    {
                        DataTypeDefXsd.AnyUri => "S",
                        DataTypeDefXsd.Base64Binary => "S",
                        DataTypeDefXsd.Boolean => "S",
                        DataTypeDefXsd.Byte => "I",
                        DataTypeDefXsd.Date => "S",
                        DataTypeDefXsd.DateTime => "S",
                        DataTypeDefXsd.Decimal => "S",
                        DataTypeDefXsd.Double => "D",
                        DataTypeDefXsd.Duration => "S",
                        DataTypeDefXsd.Float => "D",
                        DataTypeDefXsd.GDay => "S",
                        DataTypeDefXsd.GMonth => "S",
                        DataTypeDefXsd.GMonthDay => "S",
                        DataTypeDefXsd.GYear => "S",
                        DataTypeDefXsd.GYearMonth => "S",
                        DataTypeDefXsd.HexBinary => "S",
                        DataTypeDefXsd.Int => "I",
                        DataTypeDefXsd.Integer => "I",
                        DataTypeDefXsd.Long => "I",
                        DataTypeDefXsd.NegativeInteger => "I",
                        DataTypeDefXsd.NonNegativeInteger => "I",
                        DataTypeDefXsd.NonPositiveInteger => "I",
                        DataTypeDefXsd.PositiveInteger => "I",
                        DataTypeDefXsd.Short => "I",
                        DataTypeDefXsd.String => "S",
                        DataTypeDefXsd.Time => "S",
                        DataTypeDefXsd.UnsignedByte => "I",
                        DataTypeDefXsd.UnsignedInt => "I",
                        DataTypeDefXsd.UnsignedLong => "I",
                        DataTypeDefXsd.UnsignedShort => "I",
                        _ => string.Empty
                    };
        }

        private string getValueAndType(string? value, DataTypeDefXsd? dataType, out string sValue, out long iValue, out double dValue)
        {
            sValue = string.Empty;
            iValue = 0;
            dValue = 0;

            if (value.IsNullOrEmpty())
                return string.Empty;

            if (shortValueType(dataType).Equals("S"))
            {
                sValue = value;
                return "S";
            }

            if (Int64.TryParse(value, out iValue))
                return "I";

            if (Double.TryParse(value, out dValue))
                return "D";

            sValue = value;
            return "S";
        }

        private void setValues(ISubmodelElement sme, SMESet smeDB)
        {
            if (sme is Property prop)
            {
                var value = prop.ValueAsText();
                if (value.IsNullOrEmpty())
                    return;

                smeDB.ValueType = getValueAndType(value, prop.ValueType, out var sValue, out var iValue, out var dValue);
                if (smeDB.ValueType.Equals("S"))
                    smeDB.SValueSets.Add(new SValueSet { Value = sValue, Annotation = string.Empty });
                else if (smeDB.ValueType.Equals("I"))
                    smeDB.IValueSets.Add(new IValueSet { Value = iValue, Annotation = string.Empty });
                else if (smeDB.ValueType.Equals("D"))
                    smeDB.DValueSets.Add(new DValueSet { Value = dValue, Annotation = string.Empty });
            }
            else if (sme is MultiLanguageProperty mlp)
            {
                if (mlp.Value == null || mlp.Value.Count == 0)
                    return;

                smeDB.ValueType = "S";
                if (mlp.Value != null)
                    foreach (var sValueMLP in mlp.Value)
                        smeDB.SValueSets.Add(new SValueSet() { Annotation = sValueMLP.Language, Value = sValueMLP.Text });
            }
            else if (sme is AasCore.Aas3_0.Range range)
            {
                var withMin = !range.Min.IsNullOrEmpty();
                var withMax = !range.Max.IsNullOrEmpty();
                if (!withMin && !withMax)
                    return;

                var valueTypeMin = getValueAndType(range.Min, range.ValueType, out var sValueMin, out var iValueMin, out var dValueMin);
                var valueTypeMax = getValueAndType(range.Max, range.ValueType, out var sValueMax, out var iValueMax, out var dValueMax);

                if (valueTypeMin.Equals(valueTypeMax))
                    smeDB.ValueType = valueTypeMin;
                else if (valueTypeMin.IsNullOrEmpty() || valueTypeMax.IsNullOrEmpty())
                    smeDB.ValueType = withMin ? valueTypeMin : valueTypeMax;
                else if (valueTypeMin.Equals("S") || valueTypeMax.Equals("S"))
                {
                    smeDB.ValueType = "S";
                    if (valueTypeMin.Equals("S"))
                        sValueMax = valueTypeMax.Equals("I") ? iValueMax.ToString() : dValueMax.ToString();
                    else if (valueTypeMax.Equals("S"))
                        sValueMin = valueTypeMin.Equals("I") ? iValueMin.ToString() : dValueMin.ToString();
                }
                else
                {
                    smeDB.ValueType = "D";
                    if (valueTypeMin.Equals("I"))
                        dValueMin = Convert.ToDouble(iValueMin);
                    else if (valueTypeMax.Equals("I"))
                        dValueMax = Convert.ToDouble(iValueMax);
                }


                if (smeDB.ValueType.Equals("S"))
                {
                    if (withMin)
                        smeDB.SValueSets.Add(new SValueSet { Value = sValueMin, Annotation = "Min" });
                    if (withMax)
                        smeDB.SValueSets.Add(new SValueSet { Value = sValueMax, Annotation = "Max" });
                }
                else if (smeDB.ValueType.Equals("I"))
                {
                    if (withMin)
                        smeDB.IValueSets.Add(new IValueSet { Value = iValueMin, Annotation = "Min" });
                    if (withMax)
                        smeDB.IValueSets.Add(new IValueSet { Value = iValueMax, Annotation = "Max" });
                }
                else if (smeDB.ValueType.Equals("D"))
                {
                    if (withMin)
                        smeDB.DValueSets.Add(new DValueSet { Value = dValueMin, Annotation = "Min" });
                    if (withMax)
                        smeDB.DValueSets.Add(new DValueSet { Value = dValueMax, Annotation = "Max" });
                }
            }
            else if (sme is Blob blob)
            {
                if (blob.Value.IsNullOrEmpty() && blob.ContentType.IsNullOrEmpty())
                    return;

                smeDB.ValueType = "S";
                smeDB.SValueSets.Add(new SValueSet { Value = blob.Value != null ? Encoding.ASCII.GetString(blob.Value) : string.Empty, Annotation = blob.ContentType });
            }
            else if (sme is AasCore.Aas3_0.File file)
            {
                if (file.Value.IsNullOrEmpty() && file.ContentType.IsNullOrEmpty())
                    return;

                smeDB.ValueType = "S";
                smeDB.SValueSets.Add(new SValueSet { Value = file.Value, Annotation = file.ContentType });
            }
            else if (sme is Entity entity)
            {
                smeDB.ValueType = "S";
                smeDB.SValueSets.Add(new SValueSet { Value = entity.GlobalAssetId, Annotation = entity.EntityType.ToString() });
            }
        }

        private SMESet collectSMEData(ISubmodelElement sme)
        {
            var semanticId = sme.SemanticId.GetAsIdentifier() ?? string.Empty;

            var currentDataTime = DateTime.UtcNow;
            var timeStamp = (sme.TimeStamp == default) ? currentDataTime : sme.TimeStamp;
            var timeStampCreate = (sme.TimeStampCreate == default) ? currentDataTime : sme.TimeStampCreate;
            var timeStampTree = (sme.TimeStampTree == default) ? currentDataTime : sme.TimeStampTree;

            var smeType = shortSMEType(sme);
            var smeDB = new SMESet
                        {
                            ParentSME  = _parSME,
                            SMEType    = smeType,
                            ValueType  = string.Empty,
                            SemanticId = semanticId,
                            IdShort = /* 1. Version _oprPrefix +*/ sme.IdShort,
                            TimeStamp = timeStamp,
                            TimeStampCreate = timeStampCreate,
                            TimeStampTree = timeStampTree
                        };
            setValues(sme, smeDB);
            _smDB?.SMESets.Add(smeDB);

            return smeDB;
        }

        public override void VisitExtension(IExtension that)
        {
            // base.VisitExtension(that);
        }
        public override void VisitAdministrativeInformation(IAdministrativeInformation that)
        {
            // base.VisitAdministrativeInformation(that);
        }
        public override void VisitQualifier(IQualifier that)
        {
            // base.VisitQualifier(that);
        }
        public override void VisitAssetAdministrationShell(IAssetAdministrationShell that)
        {
            var currentDataTime = DateTime.UtcNow;
            var timeStamp = (that.TimeStamp == default)? currentDataTime : that.TimeStamp;
            var timeStampCreate = (that.TimeStampCreate == default) ? currentDataTime : that.TimeStampCreate;
            var timeStampTree = (that.TimeStampTree == default) ? currentDataTime : that.TimeStampTree;

            var aasDB = new AASSet
                        {
                            Identifier    = that.Id,
                            IdShort       = that.IdShort,
                            AssetKind     = that.AssetInformation.AssetKind.ToString(),
                            GlobalAssetId = that.AssetInformation.GlobalAssetId,
                            TimeStamp = timeStamp,
                            TimeStampCreate = timeStampCreate,
                            TimeStampTree = timeStampTree
                        };
            _aasxDB.AASSets.Add(aasDB);
            base.VisitAssetAdministrationShell(that);
        }
        public override void VisitAssetInformation(IAssetInformation that)
        {
            // base.VisitAssetInformation(that);
        }
        public override void VisitResource(IResource that)
        {
            // base.VisitResource(that);
        }
        public override void VisitSpecificAssetId(ISpecificAssetId that)
        {
            //base.VisitSpecificAssetId(that);
        }
        public override void VisitSubmodel(ISubmodel that)
        {
            var currentDataTime = DateTime.UtcNow;
            var timeStamp = (that.TimeStamp == default) ? currentDataTime : that.TimeStamp;
            var timeStampCreate = (that.TimeStampCreate == default) ? currentDataTime : that.TimeStampCreate;
            var timeStampTree = (that.TimeStampTree == default) ? currentDataTime : that.TimeStampTree;

            var semanticId = that.SemanticId.GetAsIdentifier();
            if (semanticId.IsNullOrEmpty())
                semanticId = string.Empty;
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
            collectSMEData(that);
            base.VisitRelationshipElement(that);
        }
        public override void VisitSubmodelElementList(ISubmodelElementList that)
        {
            var smeSet = collectSMEData(that);
            _parSME = smeSet;
            base.VisitSubmodelElementList(that);
            _parSME = smeSet.ParentSME;
        }

        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that)
        {
            var smeSet = collectSMEData(that);
            _parSME = smeSet;
            base.VisitSubmodelElementCollection(that);
            _parSME = smeSet.ParentSME;
        }

        public override void VisitProperty(IProperty that)
        {
            collectSMEData(that);
            base.VisitProperty(that);
        }
        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that)
        {
            collectSMEData(that);
            base.VisitMultiLanguageProperty(that);
        }
        public override void VisitRange(IRange that)
        {
            collectSMEData(that);
            base.VisitRange(that);
        }
        public override void VisitReferenceElement(IReferenceElement that)
        {
            collectSMEData(that);
            base.VisitReferenceElement(that);
        }
        public override void VisitBlob(IBlob that)
        {
            collectSMEData(that);
            base.VisitBlob(that);
        }
        public override void VisitFile(IFile that)
        {
            collectSMEData(that);
            base.VisitFile(that);
        }
        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            var smeSet = collectSMEData(that);
            _parSME = smeSet;
            base.VisitAnnotatedRelationshipElement(that);
            _parSME = smeSet.ParentSME;
        }
        public override void VisitEntity(IEntity that)
        {
            var smeSet = collectSMEData(that);
            _parSME = smeSet;
            base.VisitEntity(that);
            _parSME = smeSet.ParentSME;
        }
        public override void VisitEventPayload(IEventPayload that)
        {
            // base.VisitEventPayload(that);
        }
        public override void VisitBasicEventElement(IBasicEventElement that)
        {
            collectSMEData(that);
            base.VisitBasicEventElement(that);
        }
        public override void VisitOperation(IOperation that)
        {
            /* 1. Version
            var smeSet = collectSMEData(that);
            _parSME = smeSet;
            _oprPrefix = "Input_";
            foreach (var item in that.InputVariables)
                base.VisitOperationVariable(item);
            _oprPrefix = "Output_";
            foreach (var item in that.OutputVariables)
                base.VisitOperationVariable(item);
            _oprPrefix = "Inoutput_";
            foreach (var item in that.InoutputVariables)
                base.VisitOperationVariable(item);
            _oprPrefix = string.Empty;
            _parSME = smeSet.ParentSME;
            */

            /* 2. Version */
            var smeSet = collectSMEData(that);
            _parSME = smeSet;
            SetOperationVariable(smeSet, that.InputVariables, "InputVariables");
            SetOperationVariable(smeSet, that.OutputVariables, "OutputVariables");
            SetOperationVariable(smeSet, that.InoutputVariables, "InoutputVariables");
            _parSME = smeSet.ParentSME;
        }

        public void SetOperationVariable(SMESet smeOpr, List<IOperationVariable>? listOpr, string name)
        {
            if (listOpr == null || listOpr.Count == 0)
                return;

            var currentDataTime = DateTime.UtcNow;
            var timeStamp = (smeOpr.TimeStamp == default) ? currentDataTime : smeOpr.TimeStamp;
            var timeStampCreate = (smeOpr.TimeStampCreate == default) ? currentDataTime : smeOpr.TimeStampCreate;
            var timeStampTree = (smeOpr.TimeStampTree == default) ? currentDataTime : smeOpr.TimeStampTree;

            var smeDB = new SMESet
            {
                ParentSME = _parSME,
                IdShort = name,
                SMEType = "OprVar",
                TimeStamp = timeStamp,
                TimeStampCreate = timeStampCreate,
                TimeStampTree = timeStampTree
            };
            _smDB?.SMESets.Add(smeDB);

            _parSME = smeDB;

            foreach (var item in listOpr)
                base.VisitOperationVariable(item);

            _parSME = smeDB.ParentSME;
        }

        public override void VisitOperationVariable(IOperationVariable that)
        {
            // base.VisitOperationVariable(that);
        }
        public override void VisitCapability(ICapability that)
        {
            collectSMEData(that);
            base.VisitCapability(that);
        }
        public override void VisitConceptDescription(IConceptDescription that)
        {
            // base.VisitConceptDescription(that);
        }
        public override void VisitReference(AasCore.Aas3_0.IReference that)
        {
            // base.VisitReference(that);
        }
        public override void VisitKey(IKey that)
        {
            // base.VisitKey(that);
        }

        public override void VisitEnvironment(AasCore.Aas3_0.IEnvironment that)
        {
            // base.VisitEnvironment(that);
        }

        public override void VisitLangStringNameType(ILangStringNameType that)
        {
            // base.VisitLangStringNameType(that);
        }
        public override void VisitLangStringTextType(ILangStringTextType that)
        {
            // base.VisitLangStringTextType(that);
        }

        public override void VisitEmbeddedDataSpecification(IEmbeddedDataSpecification that)
        {
            // if (that != null && that.DataSpecification != null)
            // base.VisitEmbeddedDataSpecification(that);
        }

        public override void VisitLevelType(ILevelType that)
        {
            // base.VisitLevelType(that);
        }
        public override void VisitValueReferencePair(IValueReferencePair that)
        {
            // base.VisitValueReferencePair(that);
        }
        public override void VisitValueList(IValueList that)
        {
            // base.VisitValueList(that);
        }
        public override void VisitLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that)
        {
            // base.VisitLangStringPreferredNameTypeIec61360(that);
        }
        public override void VisitLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that)
        {
            // base.VisitLangStringShortNameTypeIec61360(that);
        }
        public override void VisitLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that)
        {
            // base.VisitLangStringDefinitionTypeIec61360(that);
        }
        public override void VisitDataSpecificationIec61360(IDataSpecificationIec61360 that)
        {
            // base.VisitDataSpecificationIec61360(that);
        }
    }
}