using System.ComponentModel.DataAnnotations;
using AasCore.Aas3_0;
using AasxServerDB.Entities;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace AasxServerDB
{
    public class PageRetriever
    {
        public static List<EnvSet> GetPageEnvData(int size = 1000, string searchLower = "", long envid = 0)
        {
            return new AasContext().EnvSets
                .Where(a =>
                    (envid == 0 || a.Id == envid) &&
                    (searchLower.IsNullOrEmpty() || a.Path.ToLower().Contains(searchLower)))
                .Take(size)
                .ToList();
        }

        public static List<CDSet> GetPageCDData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long cdid = 0)
        {
            var withDateTime = !dateTime.Equals(DateTime.MinValue);
            return new AasContext().CDSets
                .Where(a =>
                    (envid == 0 || a.EnvId == envid) &&
                    (cdid == 0 || a.Id == cdid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (a.IdShort != null && a.IdShort.ToLower().Contains(searchLower)) ||
                        (a.Category != null && a.Category.ToLower().Contains(searchLower)) ||
                        (a.Identifier != null && a.Identifier.ToLower().Contains(searchLower)) ||
                        (withDateTime && a.TimeStampTree.CompareTo(dateTime) > 0))
                    )
                .Take(size)
                .ToList();
        }

        public static List<AASSet> GetPageAASData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long aasid = 0)
        {
            var withDateTime = !dateTime.Equals(DateTime.MinValue);
            return new AasContext().AASSets
                .Where(a =>
                    (envid == 0 || a.EnvId == envid) &&
                    (aasid == 0 || a.Id == aasid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (a.IdShort != null && a.IdShort.ToLower().Contains(searchLower)) ||
                        (a.Category != null && a.IdShort.ToLower().Contains(searchLower)) ||
                        (a.Identifier != null && a.Identifier.ToLower().Contains(searchLower)) ||
                        (a.GlobalAssetId != null && a.GlobalAssetId.ToLower().Contains(searchLower)) ||
                        (a.AssetType != null && a.AssetType.ToLower().Contains(searchLower)) ||
                        (withDateTime && a.TimeStampTree.CompareTo(dateTime) > 0))
                    )
                .Take(size)
                .ToList();
        }

        public static List<SMSet> GetPageSMData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long aasid = 0, long smid = 0)
        {
            var withDateTime = !dateTime.Equals(DateTime.MinValue);
            return new AasContext().SMSets
                .Where(s => (envid == 0 || s.EnvId == envid) && (aasid == 0 || s.AASId == aasid) && (smid == 0 || s.Id == smid) &&
                    (searchLower.IsNullOrEmpty() ||
                    (s.IdShort != null && s.IdShort.ToLower().Contains(searchLower)) ||
                    (s.Category != null && s.Category.ToLower().Contains(searchLower)) ||
                    (s.Identifier != null && s.Identifier.ToLower().Contains(searchLower)) ||
                    (s.SemanticId != null && s.SemanticId.ToLower().Contains(searchLower)) ||
                    (withDateTime && s.TimeStampTree.CompareTo(dateTime) > 0)))
                .Take(size)
                .ToList();
        }

        public static List<SMESet> GetPageSMEData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long smid = 0, long smeid = 0, long parid = 0)
        {
            var withDateTime = !dateTime.Equals(DateTime.MinValue);
            var data = new List<SMESet>();
            using (var db = new AasContext())
            {
                data = db.SMESets
                    .Where(sme => (smid == 0 || sme.SMId == smid) && (smeid == 0 || sme.Id == smeid) && (parid == 0 || sme.ParentSMEId == parid) &&
                        (searchLower.IsNullOrEmpty() ||
                        (sme.IdShort != null && sme.IdShort.ToLower().Contains(searchLower)) ||
                        (sme.Category != null && sme.Category.ToLower().Contains(searchLower)) ||
                        (sme.SemanticId != null && sme.SemanticId.ToLower().Contains(searchLower)) ||
                        (sme.SMEType != null && sme.SMEType.ToLower().Contains(searchLower)) ||
                        (sme.TValue != null && sme.TValue.ToLower().Contains(searchLower)) ||
                        (withDateTime && sme.TimeStampTree.CompareTo(dateTime) > 0) ||
                        db.OValueSets.Any(sv => sv.SMEId == sme.Id && (sv.Attribute.ToLower().Contains(searchLower) || ((string) sv.Value).ToLower().Contains(searchLower))) ||
                        (sme.TValue != null && (
                            (sme.TValue.Equals("S") && db.SValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToLower().Contains(searchLower))))) ||
                            (sme.TValue.Equals("I") && db.IValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower))))) ||
                            (sme.TValue.Equals("D") && db.DValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower)))))))))
                    .Take(size)
                    .ToList();
            }
            return data;
        }

        public static List<SValueSet> GetPageSValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            return new AasContext().SValueSets
                .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                    (v.Value != null && v.Value.ToLower().Contains(searchLower)) ||
                    (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower))))
                .Take(size)
                .ToList();
        }

        public static List<IValueSet> GetPageIValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            if (!Int64.TryParse(searchLower, out var iEqual))
                iEqual = 0;

            return new AasContext().IValueSets
                .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                            (searchLower.IsNullOrEmpty() ||
                            (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower)) ||
                            (iEqual == 0 || v.Value == iEqual)))
                .Take(size)
                .ToList();
        }

        public static List<DValueSet> GetPageDValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            if (!double.TryParse(searchLower, out var dEqual))
                dEqual = 0;

            return new AasContext().DValueSets
                .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                            (searchLower.IsNullOrEmpty() ||
                            (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower)) ||
                            (dEqual == 0 || v.Value == dEqual)))
                .Take(size)
                .ToList();
        }

        public static List<OValueSet> GetPageOValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            return new AasContext().OValueSets
                .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                    (v.Attribute != null && v.Attribute.ToLower().Contains(searchLower)) ||
                    (v.Value != null && ((string) v.Value).ToLower().Contains(searchLower))))
                .Take(size)
                .ToList();
        }
    }
}