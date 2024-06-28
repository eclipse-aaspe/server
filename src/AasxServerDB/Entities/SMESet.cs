using Microsoft.EntityFrameworkCore;
using Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

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

        public List<string[]> getValue()
        {
            var list = new List<string[]>();
            using (AasContext db = new AasContext())
            {
                switch (ValueType)
                {
                    case "S":
                        list = db.SValueSets.Where(s => s.SMEId == Id).ToList()
                            .ConvertAll<string[]>(valueDB => [valueDB.Value ?? string.Empty, valueDB.Annotation ?? string.Empty]);
                        break;
                    case "I":
                        list = db.IValueSets.Where(s => s.SMEId == Id).ToList()
                            .ConvertAll<string[]>(valueDB => [valueDB.Value == null ? string.Empty : valueDB.Value.ToString(), valueDB.Annotation ?? string.Empty]);
                        break;
                    case "D":
                        list = db.DValueSets.Where(s => s.SMEId == Id).ToList()
                            .ConvertAll<string[]>(valueDB => [valueDB.Value == null ? string.Empty : valueDB.Value.ToString(), valueDB.Annotation ?? string.Empty]);
                        break;
                }
            }
            if (list.Count > 0 || SMEType.Equals("MLP"))
                return list;
            return [[string.Empty, string.Empty]];
        }
    }
}