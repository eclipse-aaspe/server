
using AasxTimeSeries;
using Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace AasxDemonstration
{
    /// <summary>
    /// This class holds information and provides functions to "automate" the energy model
    /// used by the CESMII / LNI4.0 demonstrator.
    /// It consists of Properties, which shall be synchronized with Azure IoTHub. It
    /// includes a time series (according the SM template spec) as well.
    /// </summary>
    public static class EnergyModel
    {
        /// <summary>
        /// Associated class can release trigger events
        /// </summary>
        public interface ITrackHasTrigger
        {
            bool IsTrigger(SourceSystemBase sosy);
        }

        /// <summary>
        /// Associated class provides a data vlue
        /// </summary>
        public interface ITrackHasValue
        {
            double GetValue(SourceSystemBase sosy);
        }

        /// <summary>
        /// Associated class renders a value blob according to the time series spec
        /// </summary>
        public interface ITrackRenderValueBlob
        {
            string RenderValueBlob(SourceSystemBase sosy, int totalSamples);
        }

        /// <summary>
        /// Base class for the source system and its context. Can be used to transport 
        /// context and global status data w.r.t to the online connect to a source system
        /// </summary>
        public class SourceSystemBase
        {

            public static SourceSystemBase FactoryNewSystem(
                string sourceType,
                string sourceAddress,
                string user, string password,
                string credentials)
            {
                // init
                sourceType = ("" + sourceType).Trim().ToLower();

                // debug?
                if (sourceType == "debug")
                    return new SourceSystemDebug();

                // debug?
                if (sourceType == "azure-iothub")
                    return new SourceSystemAzureHub(sourceAddress, user, password, credentials);

                // no, default
                return new SourceSystemBase();
            }
        }

        /// <summary>
        /// Implements a source system, which provides random values to random times
        /// </summary>
        public class SourceSystemDebug : SourceSystemBase
        {
            public Random Rnd = new Random();
        }

        /// <summary>
        /// Implements a source system, which gets data from Azure IoTHub 
        /// </summary>
        public class SourceSystemAzureHub : SourceSystemBase
        {
            public SourceSystemAzureHub() : base() { }

            public SourceSystemAzureHub(
                string sourceAddress,
                string user, string password,
                string credentials) : base()
            {
                // TODO ERICH
            }
        }

        /// <summary>
        /// Tracking of a single data point, which is may be online connected to simulation or Azure ..
        /// </summary>
        public class TrackInstanceDataPoint : ITrackHasTrigger, ITrackHasValue
        {
            /// <summary>
            /// Link to an EXISTING SME in the associated Submodel instance
            /// </summary>
            public ISubmodelElement Sme;

            /// <summary>
            /// Link to the online source, e.g. Azure IoTHub
            /// </summary>
            public string SourceId;

            /// <summary>
            /// Evaluates, if the trigger condition is met, where new data exists
            /// </summary>
            public bool IsTrigger(SourceSystemBase sosy)
            {
                if (sosy is SourceSystemDebug dbg)
                    return dbg.Rnd.Next(0, 9) >= 8;

                if (sosy is SourceSystemAzureHub azure)
                    // TODO ERICH
                    return false;

                return false;
            }

            /// <summary>
            /// depending on a trigger, gets the actual value
            /// </summary>
            public double GetValue(SourceSystemBase sosy)
            {
                if (sosy is SourceSystemDebug dbg)
                    return dbg.Rnd.NextDouble() * 99.9;

                if (sosy is SourceSystemAzureHub azure)
                    // TODO ERICH
                    return 0.0;

                return 0.0;
            }
        }

        private static T AddToSMC<T>(
            DateTime timestamp,
            SubmodelElementCollection parent,
            string idShort,
            string semanticIdKey,
            string smeValue = null) where T : ISubmodelElement
        {
            //var newElem = SubmodelElementWrapper.CreateAdequateType(typeof(T));
            var newElem = CreateSubmodelElementInstance(typeof(T));

            newElem.IdShort = idShort;
            newElem.SemanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, semanticIdKey) });
            newElem.SetTimeStamp(timestamp);
            newElem.TimeStampCreate = timestamp;
            if (parent?.Value != null)
            {
                parent.Value.Add(newElem);
                parent.SetTimeStamp(timestamp);
            }
            if (smeValue != null && newElem is Property newP)
                newP.Value = smeValue;
            if (smeValue != null && newElem is Blob newB)
                newB.Value = Encoding.ASCII.GetBytes(smeValue);
            return (T)newElem;
        }

        private static ISubmodelElement CreateSubmodelElementInstance(Type type)
        {
            if (type == null || !type.IsSubclassOf(typeof(ISubmodelElement)))
                return null;
            var sme = Activator.CreateInstance(type) as ISubmodelElement;
            return sme;
        }

        private static void CopySmeFeatures(
            ISubmodelElement dst, ISubmodelElement src,
            bool copyIdShort = false,
            bool copyDescription = false,
            bool copySemanticId = false,
            bool copyQualifers = false)
        {
            // access
            if (dst == null || src == null)
                return;

            // feature wise
            if (copyIdShort)
                dst.IdShort = src.IdShort;

            if (copyDescription)
                dst.Description = src.Description;

            if (copySemanticId)
                dst.SemanticId = src.SemanticId;

            if (copyQualifers)
            {
                //dst.Qualifiers = new QualifierCollection();
                dst.Qualifiers = new List<IQualifier>();
                foreach (var q in src.Qualifiers)
                    dst.Qualifiers.Add(q);
            }
        }

        private static void UpdateSME(
            ISubmodelElement sme,
            string value,
            DateTime timestamp)
        {
            // update
            if (sme is Property prop)
            {
                prop.Value = value;
            }
            if (sme is Blob blob)
            {
                blob.Value = Encoding.ASCII.GetBytes(value);
            }

            // time stamping
            sme.SetTimeStamp(timestamp);
        }

        /// <summary>
        /// Tracking of a single time series variable; in accordance to a time axis (trigger) 
        /// multiple values will aggregated
        /// </summary>
        public class TrackInstanceTimeSeriesVariable : ITrackHasValue, ITrackRenderValueBlob
        {
            /// <summary>
            /// Link to the CURRENTLY MAINTAINED time series variable in the associated time series segment
            /// </summary>
            public SubmodelElementCollection VariableSmc;

            /// <summary>
            /// Link to the CURRENTLY MAINTAINED ValueArray in the associated time series segment
            /// </summary>
            public Blob ValueArray;

            /// <summary>
            /// Link to the online source, e.g. Azure IoTHub
            /// </summary>
            public string SourceId;

            /// <summary>
            /// Record ID given by the template
            /// </summary>
            public string TemplateRecordId;

            /// <summary>
            /// Links to respective SME from the providing time series segment TEMPLATE in the originally
            /// loaded AASX.
            /// </summary>
            public Property TemplateDataPoint;

            /// <summary>
            /// Links to respective SMC for the variable from the providing time series segment TEMPLATE in the originally
            /// loaded AASX.
            /// </summary>
            public SubmodelElementCollection TemplateVariable;

            /// <summary>
            /// Maintains the list of values already stored in the variable.
            /// Is used to always be able to render a current state of the ValueArray
            /// </summary>
            public List<double> Values = new List<double>();

            /// <summary>
            /// Reset the values, clear the runtime associations
            /// </summary>
            public void ClearRuntime()
            {
                VariableSmc = null;
                Values.Clear();
            }

            /// <summary>
            /// depending on a trigger, gets the actual value
            /// </summary>
            public double GetValue(SourceSystemBase sosy)
            {
                if (sosy is SourceSystemDebug dbg)
                    return dbg.Rnd.NextDouble() * 99.9;

                if (sosy is SourceSystemAzureHub azure)
                    // TODO ERICH
                    return 0.0;

                return 0.0;
            }

            /// <summary>
            /// Renders list of time stamps according to time series spec
            /// </summary>
            public string RenderValueBlob(SourceSystemBase sosy, int totalSamples)
            {
                // access
                if (Values == null)
                    return "";

                // build
                return string.Join(", ", Values.Select(
                    v => String.Format(CultureInfo.InvariantCulture, "[{0}, {1}]", totalSamples++, v)
                ));
            }

            /// <summary>
            /// Create a new set of SubmodelElements for the segment.
            /// The <c>SegmentSmc</c> and <c>ValueArray</c> will be updated!
            /// </summary>
            public void CreateVariableSmc(
                SourceSystemBase sosy,
                SubmodelElementCollection segmentSmc,
                int totalSamples,
                DateTime timeStamp)
            {
                VariableSmc = AddToSMC<SubmodelElementCollection>(
                    timeStamp, segmentSmc,
                    "TSvariable_" + TemplateRecordId,
                    semanticIdKey: PrefTimeSeries10.CD_TimeSeriesVariable.Value);

                CopySmeFeatures(VariableSmc, TemplateVariable,
                    copyDescription: true, copyQualifers: true);

                AddToSMC<Property>(timeStamp, VariableSmc,
                    "RecordId", semanticIdKey: PrefTimeSeries10.CD_RecordId.Value,
                    smeValue: "" + TemplateRecordId);

                var p = AddToSMC<Property>(timeStamp, VariableSmc,
                    "" + TemplateDataPoint?.IdShort, semanticIdKey: null);

                CopySmeFeatures(p, TemplateDataPoint,
                    copySemanticId: true, copyDescription: true, copyQualifers: true);

                ValueArray = AddToSMC<Blob>(timeStamp, VariableSmc,
                    "ValueArray", semanticIdKey: PrefTimeSeries10.CD_ValueArray.Value,
                    smeValue: RenderValueBlob(sosy, totalSamples));
            }

            /// <summary>
            /// Updates the currently tracked set of SubmodelElements for the segment.
            /// </summary>
            public void UpdateVariableSmc(
                SourceSystemBase sosy,
                int totalSamples,
                DateTime timeStamp)
            {
                // access
                if (ValueArray == null)
                    return;

                // render
                UpdateSME(
                    ValueArray,
                    RenderValueBlob(sosy, totalSamples),
                    timeStamp);
            }
        }

        /// <summary>
        /// Tracking of a time series segement; if values of the time series 
        /// </summary>
        public class TrackInstanceTimeSeriesSegment : ITrackHasTrigger, ITrackRenderValueBlob
        {
            /// <summary>
            /// Link to the CURRENTLY MAINTAINED time series segment in the associated time series
            /// </summary>
            public SubmodelElementCollection SegmentSmc;

            /// <summary>
            /// Link to the CURRENTLY MAINTAINED ValueArray for the timestamps in the associated time series segment
            /// </summary>
            public Blob ValueArray;

            /// <summary>
            /// List of variables to always by MAINTAINED in the segment
            /// </summary>
            public List<TrackInstanceTimeSeriesVariable> Variables = new List<TrackInstanceTimeSeriesVariable>();

            /// <summary>
            /// Holds the timestamp of the samples currently represented in the different variables.
            /// This list's length should equal the length of the variable's value lists
            /// Note: obviously this means, that this code can only represent time series segments with
            ///       exactly one time axis.
            /// </summary>
            public List<DateTime> TimeStamps = new List<DateTime>();

            /// <summary>
            /// Evaluates, if the trigger condition is met, where new data exists
            /// </summary>
            public bool IsTrigger(SourceSystemBase sosy)
            {
                if (sosy is SourceSystemDebug dbg)
                    return dbg.Rnd.Next(0, 9) >= 8;

                if (sosy is SourceSystemAzureHub azure)
                    // TODO ERICH
                    return false;

                return false;
            }

            /// <summary>
            /// Reset the values, clear the runtime associations
            /// </summary>
            public void ClearRuntime()
            {
                SegmentSmc = null;
                TimeStamps.Clear();
                foreach (var vr in Variables)
                    vr.ClearRuntime();
            }

            /// <summary>
            /// Renders list of time stamps according to time series spec
            /// </summary>
            public string RenderValueBlob(SourceSystemBase sosy, int totalSamples)
            {
                // access
                if (TimeStamps == null)
                    return "";

                // build
                return string.Join(", ", TimeStamps.Select(
                    dt => String.Format(
                        CultureInfo.InvariantCulture, "[{0}, {1}]",
                        totalSamples++, dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
                ));
            }

            /// <summary>
            /// Create a new set of SubmodelElements for the segment.
            /// The <c>SegmentSmc</c> and <c>ValueArray</c> will be updated!
            /// </summary>
            public SubmodelElementCollection CreateSegmentSmc(
                SourceSystemBase sosy,
                SubmodelElementCollection root,
                int segmentIndex,
                int totalSamples,
                DateTime timeStamp)
            {
                // segment ifself
                SegmentSmc = AddToSMC<SubmodelElementCollection>(
                    timeStamp, root,
                    "Segment_" + segmentIndex,
                    semanticIdKey: PrefTimeSeries10.CD_TimeSeriesSegment.Value);

                // timestamp variables

                var smcVarTS = AddToSMC<SubmodelElementCollection>(
                    timeStamp, SegmentSmc,
                    "TSvariable_timeStamp", semanticIdKey: PrefTimeSeries10.CD_TimeSeriesVariable.Value);

                AddToSMC<Property>(timeStamp, smcVarTS,
                    "RecordId", semanticIdKey: PrefTimeSeries10.CD_RecordId.Value,
                    smeValue: "timeStamp");

                AddToSMC<Property>(timeStamp, smcVarTS,
                    "UtcTime", semanticIdKey: PrefTimeSeries10.CD_UtcTime.Value);

                ValueArray = AddToSMC<Blob>(timeStamp, smcVarTS,
                    "timeStamp", semanticIdKey: PrefTimeSeries10.CD_ValueArray.Value,
                    smeValue: RenderValueBlob(sosy, totalSamples));

                // the rest of the variables

                foreach (var vr in Variables)
                    vr.CreateVariableSmc(sosy, SegmentSmc, totalSamples, timeStamp);

                // ok
                return SegmentSmc;
            }

            /// <summary>
            /// Updates the currently tracked set of SubmodelElements for the segment.
            /// </summary>
            public void UpdateSegmentSmc(
                SourceSystemBase sosy,
                int totalSamples,
                DateTime timeStamp)
            {
                // access
                if (ValueArray == null)
                    return;

                // render
                UpdateSME(
                    ValueArray,
                    RenderValueBlob(sosy, totalSamples),
                    timeStamp);

                // the rest of the variables

                foreach (var vr in Variables)
                    vr.UpdateVariableSmc(sosy, totalSamples, timeStamp);
            }
        }

        public class EnergyModelInstance
        {
            //
            // Overall Submodel instance
            //

            protected ISubmodel _submodel;

            //
            // Managing of the actual value propertes
            //

            protected List<TrackInstanceDataPoint> _dataPoint = new List<TrackInstanceDataPoint>();

            //
            // The following entities serve as directs points to the found instance
            // of the energy model. Taken over from TimeSeries.cs
            //

            protected SubmodelElementCollection _block, _data;

            protected Property
                sampleStatus, sampleMode, sampleRate, lowDataIndex, highDataIndex,
                actualSamples, actualSamplesInCollection,
                actualCollections;

            protected int
                maxSamples = 200, maxSamplesInCollection = 20;

            protected TimeSeriesDestFormat destFormat;

            protected SourceSystemBase _sourceSystem = null;

            protected TrackInstanceTimeSeriesSegment _trackSegment = null;

            protected List<SubmodelElementCollection> _existingSegements
                = new List<SubmodelElementCollection>();

            protected int threadCounter = 0;
            protected int samplesCollectionsCount = 0;
            protected List<Property> samplesProperties = null;
            protected List<string> samplesValues = null;
            protected string samplesTimeStamp = "";
            protected int samplesValuesCount = 0;
            protected int totalSamples = 0;

            //
            // Initialize
            //

            protected void ScanSubmodelForIoTDataPoints(ISubmodel sm)
            {
                // access
                if (sm == null)
                    return;
                _dataPoint = new List<TrackInstanceDataPoint>();

                // find all elements with required qualifier
                sm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                {
                    var q = sme.FindQualifierOfType(PrefEnergyModel10.QualiIoTHubDataPoint);
                    if (q != null && q.Value != null && q.Value.Length > 0)
                        _dataPoint.Add(new TrackInstanceDataPoint()
                        {
                            Sme = sme,
                            SourceId = q.Value
                        });

                    //TODO:JT: Need to check again
                    return true;
                });
            }

            /// <summary>
            /// In Andreas' original code, all AAS and SM need to be tagged for time stamping
            /// </summary>
            public static void TagAllAasAndSm(
                AasCore.Aas3_0.Environment env,
                DateTime timeStamp)
            {
                if (env == null)
                    return;
                foreach (var x in env.FindAllSubmodelsGroupedByAAS((aas, sm) =>
                {
                    // mark aas
                    aas.TimeStampCreate = timeStamp;
                    aas.SetTimeStamp(timeStamp);

                    // mark sm
                    sm.TimeStampCreate = timeStamp;
                    sm.SetAllParents(timeStamp);

                    // need no results
                    return false;
                })) ;
            }

            public static IEnumerable<EnergyModelInstance> FindAllSmInstances(
                AasCore.Aas3_0.Environment env)
            {
                if (env == null)
                    yield break;

                foreach (var sm in env.FindAllSubmodelBySemanticId(
                    //SemanticId.CreateFromKey(PrefEnergyModel10.SM_EnergyModel).GetAsIdentifier(), AdminShell.Key.MatchMode.Relaxed))
                    PrefEnergyModel10.SM_EnergyModel.Value))
                {
                    var emi = new EnergyModelInstance();
                    emi.ScanSubmodelForIoTDataPoints(sm);
                    emi.ScanSubmodelForTimeSeriesParameters(sm);
                    yield return emi;
                }
            }

            protected void ScanSubmodelForTimeSeriesParameters(ISubmodel sm)
            {
                // access
                if (sm?.SubmodelElements == null)
                    return;
                //var mm = AdminShell.Key.MatchMode.Relaxed;
                int i;

                // track of SM
                _submodel = sm;

                // find time series models in SM
                var smctsCollection = sm.SubmodelElements.FindAllSemanticIdAs<SubmodelElementCollection>(PrefTimeSeries10.CD_TimeSeries.Value);
                foreach (var smcts in smctsCollection)
                {
                    // access
                    if (smcts?.Value == null)
                        continue;

                    // basic SMC references
                    _block = smcts;
                    _data = smcts;

                    var d2 = smcts.FindFirstIdShortAs<SubmodelElementCollection>("data");
                    if (d2 != null)
                        _data = d2;

                    // initialize the source system

                    _sourceSystem = SourceSystemBase.FactoryNewSystem(
                        "" + smcts.FindFirstIdShortAs<Property>("sourceType")?.Value,
                        "" + smcts.FindFirstIdShortAs<Property>("sourceAddress")?.Value,
                        "" + smcts.FindFirstIdShortAs<Property>("user")?.Value,
                        "" + smcts.FindFirstIdShortAs<Property>("password")?.Value,
                        "" + smcts.FindFirstIdShortAs<Property>("credentials")?.Value
                        );

                    // rest of the necessary properties

                    sampleStatus = smcts.FindFirstIdShortAs<Property>("sampleStatus");
                    sampleMode = smcts.FindFirstIdShortAs<Property>("sampleMode");
                    sampleRate = smcts.FindFirstIdShortAs<Property>("sampleRate");
                    if (int.TryParse(sampleRate?.Value, out i))
                        threadCounter = i;

                    if (int.TryParse(smcts.FindFirstIdShortAs<Property>("maxSamples")?.Value, out i))
                        maxSamples = i;

                    if (int.TryParse(smcts.FindFirstIdShortAs<Property>("maxSamplesInCollection")?.Value, out i))
                        maxSamplesInCollection = i;

                    actualSamples = smcts.FindFirstIdShortAs<Property>("actualSamples");
                    if (actualSamples != null)
                        actualSamples.Value = "0";

                    actualSamplesInCollection = smcts.FindFirstIdShortAs<Property>("actualSamplesInCollection");
                    if (actualSamplesInCollection != null)
                        actualSamplesInCollection.Value = "0";

                    actualCollections = smcts.FindFirstIdShortAs<Property>("actualCollections");
                    if (actualCollections != null)
                        actualCollections.Value = "0";

                    lowDataIndex = smcts.FindFirstIdShortAs<Property>("lowDataIndex");
                    highDataIndex = smcts.FindFirstIdShortAs<Property>("highDataIndex");

                    // challenge is to select SMes, which are NOT from a known semantic id!
                    var tsvAllowed = new[]
                    {
                        PrefTimeSeries10.CD_RecordId.Value,
                        PrefTimeSeries10.CD_UtcTime.Value,
                        PrefTimeSeries10.CD_ValueArray.Value
                    };

                    // find a Segment tagged as Template?
                    // create the time series tracking information                    

                    _trackSegment = new TrackInstanceTimeSeriesSegment();
                    var todel = new List<SubmodelElementCollection>();
                    var first = true;
                    foreach (var smcsegt in smcts.Value.FindAllSemanticIdAs<SubmodelElementCollection>(PrefTimeSeries10.CD_TimeSeriesSegment.Value))
                    {
                        if (smcsegt == null)
                            continue;

                        // relevant?
                        // TODO (jtikekar, 2023-09-04): check with Andreas
                        //if ((smcsegt.Kind.Value == ModellingKind.Template) && first)
                        {
                            first = false;

                            // find all elements with required qualifier FOR A SERIES ELEMENT
                            smcsegt.Value.RecurseOnSubmodelElements(null, null, (o, parents, sme) =>
                            {
                                var q = sme.FindQualifierOfType(PrefEnergyModel10.QualiIoTHubSeries);
                                if (q != null && q.Value != null && q.Value.Length > 0)
                                {
                                    // found the correct Qualifer, should indicate a variable in the
                                    // TEMPLATED time series
                                    if (!(sme is SubmodelElementCollection smcVar)
                                        || (true != sme.SemanticId?.Matches(PrefTimeSeries10.CD_TimeSeriesVariable.Value)))
                                        return;

                                    // ok, need to identify record id
                                    var pRecId = smcVar.Value?.FindFirstSemanticIdAs<Property>(PrefTimeSeries10.CD_RecordId.Value);
                                    var pDataPoint = smcVar.Value?.FindFirstAnySemanticId<Property>(tsvAllowed, invertAllowed: true);

                                    // proper?
                                    if (("" + pRecId?.Value).Length < 1 || pDataPoint == null)
                                        return;

                                    // ok, add
                                    _trackSegment.Variables.Add(new TrackInstanceTimeSeriesVariable()
                                    {
                                        SourceId = q.Value,
                                        TemplateRecordId = pRecId?.Value,
                                        TemplateDataPoint = pDataPoint,
                                        TemplateVariable = smcVar
                                    });
                                }
                            });
                        }

                        // remove all the stuff for a clean start
                        todel.Add(smcsegt);
                    }
                    foreach (var del in todel)
                        smcts.Value.Remove(del);
                }
            }

            public static void StartAllAsOneThread(IEnumerable<EnergyModelInstance> instances)
            {
                if (instances == null)
                    return;

                var t = new Thread(() =>
                {
                    var storedInstances = instances.ToArray();
                    while (true)
                    {
                        foreach (var emi in storedInstances)
                            emi?.CyclicCheck();

                        Thread.Sleep(100);
                    }
                });
                t.Start();
            }

            private int _testi = 0;

            public void CyclicCheck()
            {
                ;
                CyclicCheckDataPoints();
                CyclicCheckTimeSeries();
                ;
            }

            public void CyclicCheckDataPoints()
            {
                // access
                if (_sourceSystem == null || _dataPoint == null)
                    return;
                var timeStamp = DateTime.UtcNow;

                // simply iterate
                foreach (var dp in _dataPoint)
                {
                    // any action required?
                    if (dp?.Sme == null || !dp.IsTrigger(_sourceSystem))
                        continue;

                    // adopt new value & set
                    var val = dp.GetValue(_sourceSystem);
                    UpdateSME(
                        dp.Sme,
                        string.Format(CultureInfo.InvariantCulture, "{0}", val),
                        timeStamp);
                }
            }

            public void CyclicCheckTimeSeries()
            {
                // access
                if (_sourceSystem == null || _trackSegment == null)
                    return;
                var timeStamp = DateTime.UtcNow;

                // something to be done?
                if (!_trackSegment.IsTrigger(_sourceSystem))
                    return;

                // test
                if (actualSamples != null)
                {
                    _testi++;
                    UpdateSME(actualSamples, "" + _testi, timeStamp);
                }

                // OK, a new sample shall be added to the segment
                _trackSegment.TimeStamps.Add(DateTime.UtcNow);
                foreach (var tsv in _trackSegment.Variables)
                    tsv.Values.Add(tsv.GetValue(_sourceSystem));

                // now check, if the segement should be rendered intermediate or finally
                var cnt = _trackSegment.TimeStamps.Count;
                var doNextColl = (cnt >= maxSamplesInCollection);
                var doIntermediate = (cnt == 1) || (cnt == 3);

                // render on multiple times
                if (doIntermediate)
                {
                    if (_trackSegment.SegmentSmc == null)
                    {
                        Console.WriteLine("Create segement {0}", samplesCollectionsCount);

                        // create new segment
                        var newSeg = _trackSegment.CreateSegmentSmc(
                            _sourceSystem, _data, samplesCollectionsCount, totalSamples, timeStamp);

                        samplesCollectionsCount++;

                        // state initial creation as event .. updates need to follow
                        AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                        newSeg, "Add", _submodel, (ulong)timeStamp.Ticks);
                    }
                    else
                    {
                        // update
                        _trackSegment.UpdateSegmentSmc(_sourceSystem, totalSamples, timeStamp);
                    }
                }

                // final?
                if (doNextColl)
                {
                    // do a final update
                    _trackSegment.UpdateSegmentSmc(_sourceSystem, totalSamples, timeStamp);

                    // add to already existing segements .. delete an old one
                    _existingSegements.Add(_trackSegment.SegmentSmc);
                    if (_existingSegements.Count > 99)
                    {
                        // pop
                        var first = _existingSegements[0];
                        _existingSegements.RemoveAt(0);

                        // remove
                        _data.Value.Remove(first);
                        _data.SetTimeStamp(timeStamp);
                        AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                            first, "Remove", _submodel, (ulong)timeStamp.Ticks);
                    }

                    // commit und clear -> will make a new collection
                    Console.WriteLine("Clear segment");
                    totalSamples += _trackSegment.TimeStamps.Count;
                    _trackSegment.ClearRuntime();
                }
            }

        }
    }
}
