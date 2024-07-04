using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AasxServerDB.Entities
{
    public class OValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public int SMEId { get; set; }
        public virtual SMESet? SMESet { get; set; }

        public string Attribute { get; set; }
        public object Value { get; set; }
    }
}
