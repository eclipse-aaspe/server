using Microsoft.EntityFrameworkCore;
using Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using AasCore.Aas3_0;
using System.Linq;
using Newtonsoft.Json.Linq;

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
        public virtual ICollection<OValueSet> OValueSets { get; } = new List<OValueSet>();

        public List<string[]> GetValue()
        {
            var list = new List<string[]>();
            var dataType = ConverterDataType.StringToDataType(ValueType);
            if (dataType == null)
                return [[string.Empty, string.Empty]];

            var tableDataType = ConverterDataType.DataTypeToTable[(DataTypeDefXsd) dataType];
            switch (tableDataType)
            {
                case DataTypeDefXsd.String:
                    list = new AasContext().SValueSets.Where(s => s.SMEId == Id).ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value ?? string.Empty, valueDB.Annotation ?? string.Empty]);
                    break;
                case DataTypeDefXsd.Integer:
                    list = new AasContext().IValueSets.Where(s => s.SMEId == Id).ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value == null ? string.Empty : valueDB.Value.ToString(), valueDB.Annotation ?? string.Empty]);
                    break;
                case DataTypeDefXsd.Double:
                    list = new AasContext().DValueSets.Where(s => s.SMEId == Id).ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value == null ? string.Empty : valueDB.Value.ToString(), valueDB.Annotation ?? string.Empty]);
                    break;
            }

            if (list.Count > 0 || (SMEType != null && SMEType.Equals("MLP")))
                return list;
            return [[string.Empty, string.Empty]];
        }

        public List<string[]> GetValueDictionary()
        {
            var db = new AasContext();
            var list = db.SValueSets.Where(s => s.SMEId == Id).ToList()
                .ConvertAll<string[]>(value => [value.Value ?? string.Empty, value.Annotation ?? string.Empty]);
            list.AddRange(db.IValueSets.Where(s => s.SMEId == Id).ToList()
                .ConvertAll<string[]>(value => [value.Value == null ? string.Empty : value.Value.ToString(), value.Annotation ?? string.Empty]));
            list.AddRange(db.DValueSets.Where(s => s.SMEId == Id).ToList()
                .ConvertAll<string[]>(value => [value.Value == null ? string.Empty : value.Value.ToString(), value.Annotation ?? string.Empty]));
            if (list.Count > 0 || (SMEType != null && SMEType.Equals("MLP")))
                return list;
            return [[string.Empty, string.Empty]];
        }

        public Dictionary<string, object> GetOValue()
        {
            var dic = new AasContext().OValueSets.Where(s => s.SMEId == Id).ToList().ToDictionary(valueDB => valueDB.Attribute, valueDB => valueDB.Value);
            if (dic != null)
                return dic;
            return new Dictionary<string, object>();
        }
    }
}