namespace AasxServerDB
{
    using System.Linq;
    using AasxServerDB.Entities;
    using Microsoft.IdentityModel.Tokens;
    using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

    public class PageRetriever
    {
        public static List<EnvSet> GetPageEnvData(int size = 1000, string searchLower = "", long envid = 0) =>
            new AasContext().EnvSets
                .Where(a =>
                    (envid == 0 || a.Id == envid) &&
                    (searchLower.IsNullOrEmpty() || a.Path.ToLower().Contains(searchLower)))
                .Take(size)
                .ToList();

        public static List<CDSet> GetPageCDData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long cdid = 0)
        {
            // Check if a valid dateTime is provided
            var withDateTime = !dateTime.Equals(DateTime.MinValue);

            // Create a query to filter by ids
            var db = new AasContext();
            var query = db.CDSets
                .Where(cd =>
                    (envid == 0 || cd.EnvId == envid) &&
                    (cdid == 0  || cd.Id == cdid));

            // If the search string is specified and elements match the previous condition
            if (!searchLower.IsNullOrEmpty() && query.Any())
                query = query.Where(cd =>
                    (withDateTime && cd.TimeStampTree.CompareTo(dateTime) > 0) ||
                    (cd.IdShort != null && cd.IdShort.ToLower().Contains(searchLower)) ||
                    (cd.DisplayName != null && cd.DisplayName.ToLower().Contains(searchLower)) ||
                    (cd.Category != null && cd.Category.ToLower().Contains(searchLower)) ||
                    (cd.Description != null && cd.Description.ToLower().Contains(searchLower)) ||
                    (cd.Extensions != null && cd.Extensions.ToLower().Contains(searchLower)) ||
                    (cd.Identifier != null && cd.Identifier.ToLower().Contains(searchLower)) ||
                    (cd.Administration != null && cd.Administration.ToLower().Contains(searchLower)) ||
                    (cd.IsCaseOf != null && cd.IsCaseOf.ToLower().Contains(searchLower)) ||
                    (cd.EmbeddedDataSpecifications != null && cd.EmbeddedDataSpecifications.ToLower().Contains(searchLower)));

            // Return the results
            var result = query.Take(size).ToList();
            return result;
        }

        public static List<AASSet> GetPageAASData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long aasid = 0)
        {
            // Check if a valid dateTime is provided
            var withDateTime = !dateTime.Equals(DateTime.MinValue);

            // Create a query to filter by ids
            var db = new AasContext();
            var query = db.AASSets
                .Where(aas =>
                    (envid == 0 || aas.EnvId == envid) &&
                    (aasid == 0 || aas.Id == aasid));

            // If the search string is specified and elements match the previous condition
            if (!searchLower.IsNullOrEmpty() && query.Any())
                query = query.Where(aas =>
                    (withDateTime && aas.TimeStampTree.CompareTo(dateTime) > 0) ||
                    (aas.IdShort != null && aas.IdShort.ToLower().Contains(searchLower)) ||
                    (aas.DisplayName != null && aas.DisplayName.ToLower().Contains(searchLower)) ||
                    (aas.Category != null && aas.Category.ToLower().Contains(searchLower)) ||
                    (aas.Description != null && aas.Description.ToLower().Contains(searchLower)) ||
                    (aas.Extensions != null && aas.Extensions.ToLower().Contains(searchLower)) ||
                    (aas.Identifier != null && aas.Identifier.ToLower().Contains(searchLower)) ||
                    (aas.Administration != null && aas.Administration.ToLower().Contains(searchLower)) ||
                    (aas.EmbeddedDataSpecifications != null && aas.EmbeddedDataSpecifications.ToLower().Contains(searchLower)) ||
                    (aas.DerivedFrom != null && aas.DerivedFrom.ToLower().Contains(searchLower)) ||
                    (aas.AssetKind != null && aas.AssetKind.ToLower().Contains(searchLower)) ||
                    (aas.GlobalAssetId != null && aas.GlobalAssetId.ToLower().Contains(searchLower)) ||
                    (aas.AssetType != null && aas.AssetType.ToLower().Contains(searchLower)) ||
                    (aas.SpecificAssetIds != null && aas.SpecificAssetIds.ToLower().Contains(searchLower)) ||
                    (aas.DefaultThumbnail != null && aas.DefaultThumbnail.ToLower().Contains(searchLower)));

            // Return the results
            var result = query.Take(size).ToList();
            return result;
        }

        public static List<SMSet> GetPageSMData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long aasid = 0, long smid = 0)
        {
            // Check if a valid dateTime is provided
            var withDateTime = !dateTime.Equals(DateTime.MinValue);

            // Create a query to filter by ids
            var db = new AasContext();
            var query = db.SMSets
                .Where(sm =>
                    (envid == 0 || sm.EnvId == envid) &&
                    (aasid == 0 || sm.AASId == aasid) &&
                    (smid == 0 || sm.Id == smid));

            // If the search string is specified and elements match the previous condition
            if (!searchLower.IsNullOrEmpty() && query.Any())
                query = query.Where(sm =>
                    (withDateTime && sm.TimeStampTree.CompareTo(dateTime) > 0) ||
                    (sm.IdShort != null && sm.IdShort.ToLower().Contains(searchLower)) ||
                    (sm.DisplayName != null && sm.DisplayName.ToLower().Contains(searchLower)) ||
                    (sm.Category != null && sm.Category.ToLower().Contains(searchLower)) ||
                    (sm.Description != null && sm.Description.ToLower().Contains(searchLower)) ||
                    (sm.Extensions != null && sm.Extensions.ToLower().Contains(searchLower)) ||
                    (sm.Identifier != null && sm.Identifier.ToLower().Contains(searchLower)) ||
                    (sm.Administration != null && sm.Administration.ToLower().Contains(searchLower)) ||
                    (sm.Kind != null && sm.Kind.ToLower().Contains(searchLower)) ||
                    (sm.SemanticId != null && sm.SemanticId.ToLower().Contains(searchLower)) ||
                    (sm.SupplementalSemanticIds != null && sm.SupplementalSemanticIds.ToLower().Contains(searchLower)) ||
                    (sm.Qualifiers != null && sm.Qualifiers.ToLower().Contains(searchLower)) ||
                    (sm.EmbeddedDataSpecifications != null && sm.EmbeddedDataSpecifications.ToLower().Contains(searchLower)));

            // Return the results
            var result = query.Take(size).ToList();
            return result;
        }

        public static List<SMESet> GetPageSMEData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long smid = 0, long smeid = 0, long parid = 0)
        {
            // Check if a valid dateTime is provided
            var withDateTime = !dateTime.Equals(DateTime.MinValue);

            // Create a query to filter by ids
            var db = new AasContext();
            var query = db.SMESets
                .Where(sme =>
                    (smid == 0 || sme.SMId == smid) &&
                    (smeid == 0 || sme.Id == smeid) &&
                    (parid == 0 || sme.ParentSMEId == parid));

            // If the search string is specified and elements match the previous condition
            if (!searchLower.IsNullOrEmpty() && query.Any())
                query = query.Where(sme =>
                    (withDateTime && sme.TimeStampTree.CompareTo(dateTime) > 0) ||
                    (sme.SMEType != null && sme.SMEType.ToLower().Contains(searchLower)) ||
                    (sme.IdShort != null && sme.IdShort.ToLower().Contains(searchLower)) ||
                    (sme.DisplayName != null && sme.DisplayName.ToLower().Contains(searchLower)) ||
                    (sme.Category != null && sme.Category.ToLower().Contains(searchLower)) ||
                    (sme.Description != null && sme.Description.ToLower().Contains(searchLower)) ||
                    (sme.Extensions != null && sme.Extensions.ToLower().Contains(searchLower)) ||
                    (sme.SemanticId != null && sme.SemanticId.ToLower().Contains(searchLower)) ||
                    (sme.SupplementalSemanticIds != null && sme.SupplementalSemanticIds.ToLower().Contains(searchLower)) ||
                    (sme.Qualifiers != null && sme.Qualifiers.ToLower().Contains(searchLower)) ||
                    (sme.EmbeddedDataSpecifications != null && sme.EmbeddedDataSpecifications.ToLower().Contains(searchLower)) ||
                    (sme.TValue != null && sme.TValue.ToLower().Contains(searchLower)) ||
                    db.OValueSets.Any(sv => sv.SMEId == sme.Id && (sv.Attribute.ToLower().Contains(searchLower) || sv.Value.ToLower().Contains(searchLower))) ||
                    (sme.TValue != null && (
                        (sme.TValue.Equals("S") && db.SValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToLower().Contains(searchLower))))) ||
                        (sme.TValue.Equals("I") && db.IValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower))))) ||
                        (sme.TValue.Equals("D") && db.DValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower)))))
                    )));

            // Return the results
            var result = query.Take(size).ToList();
            return result;
        }

        public static List<SValueSet> GetPageSValueData(int size = 1000, string searchLower = "", long smeid = 0) =>
            new AasContext().SValueSets
                .Where(v =>
                    (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (v.Value != null && v.Value.ToLower().Contains(searchLower)) ||
                        (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower))))
                .Take(size)
                .ToList();

        public static List<IValueSet> GetPageIValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            if (!Int64.TryParse(searchLower, out var iEqual))
                iEqual = 0;

            return new AasContext().IValueSets
                .Where(v =>
                    (smeid == 0 || v.SMEId == smeid) &&
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
                .Where(v =>
                    (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower)) ||
                        (dEqual == 0 || v.Value == dEqual)))
                .Take(size)
                .ToList();
        }

        public static List<OValueSet> GetPageOValueData(int size = 1000, string searchLower = "", long smeid = 0) =>
            new AasContext().OValueSets
                .Where(v =>
                    (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (v.Attribute != null && v.Attribute.ToLower().Contains(searchLower)) ||
                        (v.Value != null && v.Value.ToLower().Contains(searchLower))))
                .Take(size)
                .ToList();
    }
}