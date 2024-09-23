namespace AasxServerDB
{
    using System.Linq;
    using AasxServerDB.Entities;
    using Microsoft.IdentityModel.Tokens;

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

            // Create a query to filter by envid and cdid
            var db = new AasContext();
            var dbId = db.CDSets
                .Where(cd =>
                    (envid == 0 || cd.EnvId == envid) &&
                    (cdid == 0  || cd.Id == cdid));

            // If no search string or no items matching the previous condition
            if (searchLower.IsNullOrEmpty() || dbId.Count() == 0)
                return dbId.Take(size).ToList();

            // Filter inside db 
            var dbSide = dbId
                .Where(cd =>
                    (cd.IdShort != null && cd.IdShort.ToLower().Contains(searchLower)) ||
                    (cd.Category != null && cd.Category.ToLower().Contains(searchLower)) ||
                    (cd.Identifier != null && cd.Identifier.ToLower().Contains(searchLower)) ||
                    (withDateTime && cd.TimeStampTree.CompareTo(dateTime) > 0))
                .Take(size)
                .AsEnumerable();

            // Returns the results if the list is full
            if (dbSide.Count() == size)
                return dbSide.ToList();

            // Filter on the client side
            /*var clientSide = dbId.AsEnumerable()
                .Where(cd =>
                    (cd.DisplayName != null && AasContext.SerializeList(cd.DisplayName).ToLower().Contains(searchLower)) ||
                    (cd.Description != null && AasContext.SerializeList(cd.Description).ToLower().Contains(searchLower)) ||
                    (cd.Extensions != null && AasContext.SerializeList(cd.Extensions).ToLower().Contains(searchLower)) ||
                    (cd.Administration != null && AasContext.SerializeElement(cd.Administration).ToLower().Contains(searchLower)) ||
                    (cd.IsCaseOf != null && AasContext.SerializeList(cd.IsCaseOf).ToLower().Contains(searchLower)) ||
                    (cd.EmbeddedDataSpecifications != null && AasContext.SerializeList(cd.EmbeddedDataSpecifications).ToLower().Contains(searchLower)))
                .Take(size);*/

            // Combine and return the results
            var combine = new HashSet<CDSet>(dbSide);//.Union(clientSide);
            var shortCom = combine.Take(size).ToList();
            return shortCom;
        }

        public static List<AASSet> GetPageAASData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long aasid = 0)
        {
            // Check if a valid dateTime is provided
            var withDateTime = !dateTime.Equals(DateTime.MinValue);

            // Create a query to filter by envid and cdid
            var db = new AasContext();
            var dbId = db.AASSets
                .Where(aas =>
                    (envid == 0 || aas.EnvId == envid) &&
                    (aasid == 0 || aas.Id == aasid));

            // If no search string or no items matching the previous condition
            if (searchLower.IsNullOrEmpty() || dbId.Count() == 0)
                return dbId.Take(size).ToList();

            // Filter inside db 
            var dbSide = dbId
                .Where(aas =>
                    (aas.IdShort != null && aas.IdShort.ToLower().Contains(searchLower)) ||
                    (aas.Category != null && aas.Category.ToLower().Contains(searchLower)) ||
                    (aas.Identifier != null && aas.Identifier.ToLower().Contains(searchLower)) ||
                    (aas.GlobalAssetId != null && aas.GlobalAssetId.ToLower().Contains(searchLower)) ||
                    (aas.AssetType != null && aas.AssetType.ToLower().Contains(searchLower)) ||
                    (withDateTime && aas.TimeStampTree.CompareTo(dateTime) > 0))
                .Take(size)
                .AsEnumerable();

            // Returns the results if the list is full
            if (dbSide.Count() == size)
                return dbSide.ToList();

            // Filter on the client side
            /*var clientSide = dbId.AsEnumerable()
                .Where(aas =>
                    (aas.DisplayName != null && AasContext.SerializeList(aas.DisplayName).ToLower().Contains(searchLower)) ||
                    (aas.Description != null && AasContext.SerializeList(aas.Description).ToLower().Contains(searchLower)) ||
                    (aas.Extensions != null && AasContext.SerializeList(aas.Extensions).ToLower().Contains(searchLower)) ||
                    (aas.Administration != null && AasContext.SerializeElement(aas.Administration).ToLower().Contains(searchLower)) ||
                    (aas.EmbeddedDataSpecifications != null && AasContext.SerializeList(aas.EmbeddedDataSpecifications).ToLower().Contains(searchLower)) ||
                    (aas.DerivedFrom != null && AasContext.SerializeElement(aas.DerivedFrom).ToLower().Contains(searchLower)) ||
                    (aas.AssetKind != null && AasContext.SerializeElement(aas.AssetKind).ToLower().Contains(searchLower)) ||
                    (aas.SpecificAssetIds != null && AasContext.SerializeList(aas.SpecificAssetIds).ToLower().Contains(searchLower)) ||
                    (aas.DefaultThumbnail != null && AasContext.SerializeElement(aas.DefaultThumbnail).ToLower().Contains(searchLower)))
                .Take(size);*/

            // Combine and return the results
            var combine = new HashSet<AASSet>(dbSide);//.Union(clientSide);
            var shortCom = combine.Take(size).ToList();
            return shortCom;
        }

        public static List<SMSet> GetPageSMData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long aasid = 0, long smid = 0)
        {
            // Check if a valid dateTime is provided
            var withDateTime = !dateTime.Equals(DateTime.MinValue);

            // Create a query to filter by envid and cdid
            var db = new AasContext();
            var dbId = db.SMSets
                .Where(sm =>
                    (envid == 0 || sm.EnvId == envid) &&
                    (aasid == 0 || sm.AASId == aasid) &&
                    (smid == 0 || sm.Id == smid));

            // If no search string or no items matching the previous condition
            if (searchLower.IsNullOrEmpty() || dbId.Count() == 0)
                return dbId.Take(size).ToList();

            // Filter inside db 
            var dbSide = dbId
                .Where(sm =>
                    (sm.IdShort != null && sm.IdShort.ToLower().Contains(searchLower)) ||
                    (sm.Category != null && sm.Category.ToLower().Contains(searchLower)) ||
                    (sm.Identifier != null && sm.Identifier.ToLower().Contains(searchLower)) ||
                    (sm.SemanticId != null && sm.SemanticId.ToLower().Contains(searchLower)) ||
                    (withDateTime && sm.TimeStampTree.CompareTo(dateTime) > 0))
                .Take(size)
                .AsEnumerable();

            // Returns the results if the list is full
            if (dbSide.Count() == size)
                return dbSide.ToList();

            // Filter on the client side
            /*var clientSide = dbId.AsEnumerable()
                .Where(sm =>
                    (sm.DisplayName != null && AasContext.SerializeList(sm.DisplayName).ToLower().Contains(searchLower)) ||
                    (sm.Description != null && AasContext.SerializeList(sm.Description).ToLower().Contains(searchLower)) ||
                    (sm.Extensions != null && AasContext.SerializeList(sm.Extensions).ToLower().Contains(searchLower)) ||
                    (sm.Administration != null && AasContext.SerializeElement(sm.Administration).ToLower().Contains(searchLower)) ||
                    (sm.Kind != null && AasContext.SerializeElement(sm.Kind).ToLower().Contains(searchLower)) ||
                    (sm.SupplementalSemanticIds != null && AasContext.SerializeList(sm.SupplementalSemanticIds).ToLower().Contains(searchLower)) ||
                    (sm.Qualifiers != null && AasContext.SerializeList(sm.Qualifiers).ToLower().Contains(searchLower)) ||
                    (sm.EmbeddedDataSpecifications != null && AasContext.SerializeList(sm.EmbeddedDataSpecifications).ToLower().Contains(searchLower)))
                .Take(size);*/

            // Combine and return the results
            var combine = new HashSet<SMSet>(dbSide);//.Union(clientSide);
            var shortCom = combine.Take(size).ToList();
            return shortCom;
        }

        public static List<SMESet> GetPageSMEData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long smid = 0, long smeid = 0, long parid = 0)
        {
            // Check if a valid dateTime is provided
            var withDateTime = !dateTime.Equals(DateTime.MinValue);

            // Create a query to filter by envid and cdid
            var db = new AasContext();
            var dbId = db.SMESets
                .Where(sme =>
                    (smid == 0 || sme.SMId == smid) &&
                    (smeid == 0 || sme.Id == smeid) &&
                    (parid == 0 || sme.ParentSMEId == parid));

            // If no search string or no items matching the previous condition
            if (searchLower.IsNullOrEmpty() || dbId.Count() == 0)
                return dbId.Take(size).ToList();

            // Filter inside db 
            var dbSide = dbId
                .Where(sme =>
                    (sme.SMEType != null && sme.SMEType.ToLower().Contains(searchLower)) ||
                    (sme.IdShort != null && sme.IdShort.ToLower().Contains(searchLower)) ||
                    (sme.Category != null && sme.Category.ToLower().Contains(searchLower)) ||
                    (sme.SemanticId != null && sme.SemanticId.ToLower().Contains(searchLower)) ||
                    (sme.TValue != null && sme.TValue.ToLower().Contains(searchLower)) ||
                    (withDateTime && sme.TimeStampTree.CompareTo(dateTime) > 0) ||
                    db.OValueSets.Any(sv => sv.SMEId == sme.Id && (sv.Attribute.ToLower().Contains(searchLower) || sv.Value.ToLower().Contains(searchLower))) ||
                    (sme.TValue != null && (
                        (sme.TValue.Equals("S") && db.SValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToLower().Contains(searchLower))))) ||
                        (sme.TValue.Equals("I") && db.IValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower))))) ||
                        (sme.TValue.Equals("D") && db.DValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower)))))
                    )))
                .Take(size)
                .AsEnumerable();

            // Returns the results if the list is full
            if (dbSide.Count() == size)
                return dbSide.ToList();

            // Filter on the client side
            /*var clientSide = dbId.AsEnumerable()
                .Where(sme =>
                    (sme.DisplayName != null && AasContext.SerializeList(sme.DisplayName).ToLower().Contains(searchLower)) ||
                    (sme.Description != null && AasContext.SerializeList(sme.Description).ToLower().Contains(searchLower)) ||
                    (sme.Extensions != null && AasContext.SerializeList(sme.Extensions).ToLower().Contains(searchLower)) ||
                    (sme.SupplementalSemanticIds != null && AasContext.SerializeList(sme.SupplementalSemanticIds).ToLower().Contains(searchLower)) ||
                    (sme.Qualifiers != null && AasContext.SerializeList(sme.Qualifiers).ToLower().Contains(searchLower)) ||
                    (sme.EmbeddedDataSpecifications != null && AasContext.SerializeList(sme.EmbeddedDataSpecifications).ToLower().Contains(searchLower)))
                .Take(size);*/

            // Combine and return the results
            var combine = new HashSet<SMESet>(dbSide);//.Union(clientSide);
            var shortCom = combine.Take(size).ToList();
            return shortCom;
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