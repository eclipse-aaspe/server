/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using Microsoft.EntityFrameworkCore;
using Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace AasxServerDB.Entities
{
    public class SMESet
    {
        public int Id { get; set; }

        [ForeignKey("SMSet")]
        public int SMId { get;             set; }
        public virtual SMSet? SMSet { get; set; }

        public int? ParentSMEId { get; set; }
        public virtual SMESet? ParentSME { get; set; }

        public string? SMEType { get; set; }
        public string? ValueType { get; set; }
        public string? SemanticId { get; set; }
        public string IdShort { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }

        public virtual ICollection<IValueSet> IValueSets { get; } = new List<IValueSet>();
        public virtual ICollection<DValueSet> DValueSets { get; } = new List<DValueSet>();
        public virtual ICollection<SValueSet> SValueSets { get; } = new List<SValueSet>();

        public string getValue()
        {
            using (AasContext db = new AasContext())
            {
                switch (ValueType)
                {
                    case "S":
                        var ls = db.SValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
                        if (ls.Count != 0)
                            return ls.First().ToString();
                        break;
                    case "I":
                        var li = db.IValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
                        if (li.Count != 0)
                            return li.First().ToString();
                        break;
                    case "F":
                        var ld = db.DValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
                        if (ld.Count != 0)
                            return ld.First().ToString();
                        break;
                }
            }
            return string.Empty;
        }

        public List<string?> getMLPValue()
        {
            var list = new List<string?>();
            if (SMEType == "MLP")
            {
                using (AasContext db = new AasContext())
                {
                    var mlpValueSetList = db.SValueSets.Where(s => s.SMEId == Id).ToList();
                    foreach (var mlpValue in mlpValueSetList)
                    {
                        list.Add(mlpValue.Annotation);
                        list.Add(mlpValue.Value);
                    }
                    return list;
                }
            }
            return new List<string?>();
        }

        public static List<SValueSet>? getValueList(List<SMESet> smesets)
        {
            var              smeIds    = smesets.OrderBy(s => s.Id).Select(s => s.Id).ToList();
            long             first     = smeIds.First();
            long             last      = smeIds.Last();
            List<SValueSet>? valueList = null;
            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                valueList = db.SValueSets.FromSqlRaw("SELECT * FROM SValueSets WHERE ParentSMEId >= " + first + " AND ParentSMEId <=" + last + " UNION SELECT * FROM IValueSets WHERE ParentSMEId >= " + first + " AND ParentSMEId <=" + last + " UNION SELECT * FROM DValueSets WHERE ParentSMEId >= " + first + " AND ParentSMEId <=" + last)
                    .Where(v => smeIds.Contains(v.SMEId))
                    .OrderBy(v => v.SMEId)
                    .ToList();
                watch.Stop();
                Console.WriteLine("Getting the value list took this time: " + watch.ElapsedMilliseconds);
            }
            return valueList;
        }
    }
}