using AasCore.Aas3_0;
using AdminShellNS;
using static AasCore.Aas3_0.Visitation;
using Extensions;
using System.IO.Compression;
using Microsoft.IdentityModel.Tokens;
using AasxServerDB.Entities;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Newtonsoft.Json;
using System.Data;

namespace AasxServerDB
{
    public class VisitorAASX : VisitorThrough
    {
        private AASXSet? _aasxDB;
        private SMSet? _smDB;
        private SMESet? _parSME;
        private string _oprPrefix = string.Empty;

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
            return _oprPrefix + sme switch
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

        private bool GetValueAndDataType(string value, ref DataTypeDefXsd dataType, out DataTypeDefXsd tableDataType, out string sValue, out long iValue, out double dValue)
        {
            tableDataType = ConverterDataType.DataTypeToTable[dataType];
            sValue = string.Empty;
            iValue = 0;
            dValue = 0;

            if (value.IsNullOrEmpty())
                return false;

            // correct table type
            switch (tableDataType)
            {
                case DataTypeDefXsd.String:
                    sValue = value;
                    return true;
                case DataTypeDefXsd.Integer:
                    if (Int64.TryParse(value, out iValue))
                        return true;
                    break;
                case DataTypeDefXsd.Double:
                    if (Double.TryParse(value, out dValue))
                        return true;
                    break;
            }

            // incorrect table type
            if (Int64.TryParse(value, out iValue))
            {
                dataType = DataTypeDefXsd.Integer;
                tableDataType = DataTypeDefXsd.Integer;
                return true;
            }

            if (Double.TryParse(value, out dValue))
            {
                dataType = DataTypeDefXsd.Double;
                tableDataType = DataTypeDefXsd.Double;
                return true;
            }

            sValue = value;
            dataType = DataTypeDefXsd.String;
            tableDataType = DataTypeDefXsd.String;
            return true;
        }

        private void SetValues(ISubmodelElement sme, SMESet smeDB)
        {
            if (sme is RelationshipElement rel)
            {
                smeDB.OValueSets.Add(new OValueSet { Attribute = "First", Value = rel.First });
                smeDB.OValueSets.Add(new OValueSet { Attribute = "Second", Value = rel.Second });
            }
            else if (sme is AnnotatedRelationshipElement relA)
            {
                smeDB.OValueSets.Add(new OValueSet { Attribute = "First", Value = relA.First });
                smeDB.OValueSets.Add(new OValueSet { Attribute = "Second", Value = relA.Second });
            }
            else if (sme is Property prop)
            {
                var dataType = prop.ValueType;
                var hasValue = GetValueAndDataType(prop.ValueAsText(), ref dataType, out var tableDataType, out var sValue, out var iValue, out var dValue);
                if (!hasValue)
                    return;

                smeDB.ValueType = dataType.ToString();

                if (tableDataType == DataTypeDefXsd.String)
                    smeDB.SValueSets.Add(new SValueSet { Value = sValue, Annotation = string.Empty });
                else if (tableDataType == DataTypeDefXsd.Integer)
                    smeDB.IValueSets.Add(new IValueSet { Value = iValue, Annotation = string.Empty });
                else if (tableDataType == DataTypeDefXsd.Double)
                    smeDB.DValueSets.Add(new DValueSet { Value = dValue, Annotation = string.Empty });

                if (prop.ValueId != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "ValueId", Value = prop.ValueId });
            }
            else if (sme is MultiLanguageProperty mlp)
            {
                if (mlp.Value == null || mlp.Value.Count == 0)
                    return;

                smeDB.ValueType = DataTypeDefXsd.String.ToString();
                if (mlp.Value != null)
                    foreach (var sValueMLP in mlp.Value)
                        smeDB.SValueSets.Add(new SValueSet() { Annotation = sValueMLP.Language, Value = sValueMLP.Text });

                if (mlp.ValueId != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "ValueId", Value = mlp.ValueId });
            }
            else if (sme is AasCore.Aas3_0.Range range)
            {
                var dataTypeMin = range.ValueType;
                var dataTypeMax = range.ValueType;
                var hasValueMin = GetValueAndDataType(range.Min, ref dataTypeMin, out var tableDataTypeMin, out var sValueMin, out var iValueMin, out var dValueMin);
                var hasValueMax = GetValueAndDataType(range.Max, ref dataTypeMax, out var tableDataTypeMax, out var sValueMax, out var iValueMax, out var dValueMax);

                // determine which data types apply
                var dataType = range.ValueType;
                var tableDataType = DataTypeDefXsd.String;
                if (!hasValueMin && !hasValueMax) // no value is given
                    return;
                else if (hasValueMin && !hasValueMax) // only min is given
                {
                    dataType = dataTypeMin;
                    tableDataType = tableDataTypeMin;
                }
                else if (!hasValueMin && hasValueMax) // only max is given
                {
                    dataType = dataTypeMax;
                    tableDataType = tableDataTypeMax;
                }
                else if (hasValueMin && hasValueMax) // both values are given
                {
                    if (dataTypeMin == dataTypeMax) // dataType did not change
                    {
                        dataType = dataTypeMin;
                        tableDataType = tableDataTypeMin;
                    }
                    else if (tableDataTypeMin != DataTypeDefXsd.String && tableDataTypeMax != DataTypeDefXsd.String) // both a number
                    {
                        dataType = DataTypeDefXsd.Double;
                        tableDataType = DataTypeDefXsd.Double;
                        if (tableDataTypeMin == DataTypeDefXsd.Integer)
                            dValueMin = Convert.ToDouble(iValueMin);
                        else if (tableDataTypeMax == DataTypeDefXsd.Integer)
                            dValueMax = Convert.ToDouble(iValueMax);
                    }
                    else // default: save in string
                    {
                        dataType = DataTypeDefXsd.String;
                        tableDataType = DataTypeDefXsd.String;
                        if (tableDataTypeMin != DataTypeDefXsd.String)
                            sValueMin = tableDataTypeMin == DataTypeDefXsd.Integer ? iValueMin.ToString() : dValueMin.ToString();
                        if (tableDataTypeMax != DataTypeDefXsd.String)
                            sValueMax = tableDataTypeMax == DataTypeDefXsd.Integer ? iValueMax.ToString() : dValueMax.ToString();
                    }
                }

                smeDB.ValueType = dataType.ToString();
                if (tableDataType == DataTypeDefXsd.String)
                {
                    if (hasValueMin)
                        smeDB.SValueSets.Add(new SValueSet { Value = sValueMin, Annotation = "Min" });
                    if (hasValueMax)
                        smeDB.SValueSets.Add(new SValueSet { Value = sValueMax, Annotation = "Max" });
                }
                else if (tableDataType == DataTypeDefXsd.Integer)
                {
                    if (hasValueMin)
                        smeDB.IValueSets.Add(new IValueSet { Value = iValueMin, Annotation = "Min" });
                    if (hasValueMax)
                        smeDB.IValueSets.Add(new IValueSet { Value = iValueMax, Annotation = "Max" });
                }
                else if (tableDataType == DataTypeDefXsd.Double)
                {
                    if (hasValueMin)
                        smeDB.DValueSets.Add(new DValueSet { Value = dValueMin, Annotation = "Min" });
                    if (hasValueMax)
                        smeDB.DValueSets.Add(new DValueSet { Value = dValueMax, Annotation = "Max" });
                }
            }
            else if (sme is Blob blob)
            {
                if (blob.Value.IsNullOrEmpty() && blob.ContentType.IsNullOrEmpty())
                    return;

                smeDB.ValueType = DataTypeDefXsd.String.ToString();
                smeDB.SValueSets.Add(new SValueSet { Value = blob.Value != null ? Encoding.ASCII.GetString(blob.Value) : string.Empty, Annotation = blob.ContentType });
            }
            else if (sme is AasCore.Aas3_0.File file)
            {
                if (file.Value.IsNullOrEmpty() && file.ContentType.IsNullOrEmpty())
                    return;

                smeDB.ValueType = DataTypeDefXsd.String.ToString();
                smeDB.SValueSets.Add(new SValueSet { Value = file.Value, Annotation = file.ContentType });
            }
            else if (sme is ReferenceElement refEle)
            {
                if (refEle.Value != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "Value", Value = refEle.Value });
            }
            else if (sme is SubmodelElementList sml)
            {
                if (sml.OrderRelevant != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "OrderRelevant", Value = sml.OrderRelevant });

                if (sml.SemanticIdListElement != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "SemanticIdListElement", Value = sml.SemanticIdListElement });

                smeDB.OValueSets.Add(new OValueSet { Attribute = "TypeValueListElement", Value = sml.TypeValueListElement });

                if (sml.ValueTypeListElement != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "ValueTypeListElement", Value = sml.ValueTypeListElement });
            }
            else if (sme is Entity ent)
            {
                smeDB.ValueType = DataTypeDefXsd.String.ToString();
                smeDB.SValueSets.Add(new SValueSet { Value = ent.GlobalAssetId, Annotation = ent.EntityType.ToString() });

                if (ent.SpecificAssetIds != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "SpecificAssetIds", Value = ent.SpecificAssetIds });
            }
            else if (sme is BasicEventElement evt)
            {
                smeDB.OValueSets.Add(new OValueSet { Attribute = "Observed", Value = evt.Observed });
                smeDB.OValueSets.Add(new OValueSet { Attribute = "Direction", Value = evt.Direction });
                smeDB.OValueSets.Add(new OValueSet { Attribute = "State", Value = evt.State });

                if (evt.MessageTopic != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "MessageTopic", Value = evt.MessageTopic });

                if (evt.MessageBroker != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "MessageBroker", Value = evt.MessageBroker });

                if (evt.LastUpdate != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "LastUpdate", Value = evt.LastUpdate });

                if (evt.MinInterval != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "MinInterval", Value = evt.MinInterval });

                if (evt.MaxInterval != null)
                    smeDB.OValueSets.Add(new OValueSet { Attribute = "MaxInterval", Value = evt.MaxInterval });
            }
        }

        private SMESet CollectSMEData(ISubmodelElement sme)
        {
            var semanticId = sme.SemanticId.GetAsIdentifier() ?? string.Empty;

            var currentDataTime = DateTime.UtcNow;
            var timeStamp = (sme.TimeStamp == default) ? currentDataTime : sme.TimeStamp;
            var timeStampCreate = (sme.TimeStampCreate == default) ? currentDataTime : sme.TimeStampCreate;
            var timeStampTree = (sme.TimeStampTree == default) ? currentDataTime : sme.TimeStampTree;

            var smeDB = new SMESet
                        {
                            ParentSME       = _parSME,
                            SMEType         = shortSMEType(sme),
                            ValueType       = string.Empty,
                            SemanticId      = semanticId,
                            IdShort         = sme.IdShort,
                            TimeStamp       = timeStamp,
                            TimeStampCreate = timeStampCreate,
                            TimeStampTree   = timeStampTree
                        };
            SetValues(sme, smeDB);
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
            var timeStamp = (that.TimeStamp == default) ? currentDataTime : that.TimeStamp;
            var timeStampCreate = (that.TimeStampCreate == default) ? currentDataTime : that.TimeStampCreate;
            var timeStampTree = (that.TimeStampTree == default) ? currentDataTime : that.TimeStampTree;

            var aasDB = new AASSet
                        {
                            Identifier      = that.Id,
                            IdShort         = that.IdShort,
                            AssetKind       = that.AssetInformation.AssetKind.ToString(),
                            GlobalAssetId   = that.AssetInformation.GlobalAssetId,
                            TimeStamp       = timeStamp,
                            TimeStampCreate = timeStampCreate,
                            TimeStampTree   = timeStampTree
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
                        SemanticId      = semanticId,
                        Identifier      = that.Id,
                        IdShort         = that.IdShort,
                        TimeStamp       = timeStamp,
                        TimeStampCreate = timeStampCreate,
                        TimeStampTree   = timeStampTree
                    };
            _aasxDB.SMSets.Add(_smDB);
            base.VisitSubmodel(that);
        }
        public override void VisitRelationshipElement(IRelationshipElement that)
        {
            CollectSMEData(that);
            base.VisitRelationshipElement(that);
        }
        public override void VisitSubmodelElementList(ISubmodelElementList that)
        {
            var smeSet = CollectSMEData(that);
            _parSME = smeSet;
            base.VisitSubmodelElementList(that);
            _parSME = smeSet.ParentSME;
        }

        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that)
        {
            var smeSet = CollectSMEData(that);
            _parSME = smeSet;
            base.VisitSubmodelElementCollection(that);
            _parSME = smeSet.ParentSME;
        }

        public override void VisitProperty(IProperty that)
        {
            CollectSMEData(that);
            base.VisitProperty(that);
        }
        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that)
        {
            CollectSMEData(that);
            base.VisitMultiLanguageProperty(that);
        }
        public override void VisitRange(IRange that)
        {
            CollectSMEData(that);
            base.VisitRange(that);
        }
        public override void VisitReferenceElement(IReferenceElement that)
        {
            CollectSMEData(that);
            base.VisitReferenceElement(that);
        }
        public override void VisitBlob(IBlob that)
        {
            CollectSMEData(that);
            base.VisitBlob(that);
        }
        public override void VisitFile(IFile that)
        {
            CollectSMEData(that);
            base.VisitFile(that);
        }
        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            var smeSet = CollectSMEData(that);
            _parSME = smeSet;
            base.VisitAnnotatedRelationshipElement(that);
            _parSME = smeSet.ParentSME;
        }
        public override void VisitEntity(IEntity that)
        {
            var smeSet = CollectSMEData(that);
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
            CollectSMEData(that);
            base.VisitBasicEventElement(that);
        }
        public override void VisitOperation(IOperation that)
        {
            var smeSet = CollectSMEData(that);
            _parSME = smeSet;

            if (that.InputVariables != null)
            {
                _oprPrefix = "In-";
                foreach (var item in that.InputVariables)
                    base.VisitOperationVariable(item);
            }

            if (that.OutputVariables != null)
            {
                _oprPrefix = "Out-";
                foreach (var item in that.OutputVariables)
                    base.VisitOperationVariable(item);
            }

            if (that.InoutputVariables != null)
            {
                _oprPrefix = "I/O-";
                foreach (var item in that.InoutputVariables)
                    base.VisitOperationVariable(item);
            }

            _oprPrefix = string.Empty;
            _parSME = smeSet.ParentSME;
        }
        public override void VisitOperationVariable(IOperationVariable that)
        {
            base.VisitOperationVariable(that);
        }
        public override void VisitCapability(ICapability that)
        {
            CollectSMEData(that);
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