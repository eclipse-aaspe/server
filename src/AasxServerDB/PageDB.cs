namespace AasxServerDB
{
    public class PageDB
    {
        static public List<AASXSet> GetPageAASXData(int size = 1000, string searchLower = "", long aasxid = 0)
        {
            List<AASXSet> data = null;
            using (AasContext db = new AasContext())
            {
                data = db.AASXSets
                    .OrderBy(a => a.Id)
                    .Where(a => (aasxid == 0 || a.Id == aasxid) &&
                    (searchLower == "" || a.AASX.ToLower().Contains(searchLower)))
                    .Take(size)
                    .ToList();
            }
            return data;
        }

        static public List<AASSet> GetPageAASData(int size = 1000, string searchLower = "", long aasxid = 0, long aasid = 0)
        {
            List<AASSet> data = null;
            using (AasContext db = new AasContext())
            {
                data = db.AASSets
                    .OrderBy(a => a.Id)
                    .Where(a => (aasxid == 0 || a.AASXId == aasxid) && (aasid == 0 || a.Id == aasid) &&
                        (searchLower == "" || a.IdShort.ToLower().Contains(searchLower) || (a.Identifier == null || a.Identifier.ToLower().Contains(searchLower)) || a.AssetKind.ToLower().Contains(searchLower) || a.GlobalAssetId.ToLower().Contains(searchLower)))
                    .Take(size)
                    .ToList();
            }
            return data;
        }

        static public List<SMSet> GetPageSMData(int size = 1000, string searchLower = "", long aasxid = 0, long aasid = 0, long smid = 0)
        {
            List<SMSet> data = null;
            using (AasContext db = new AasContext())
            {
                data = db.SMSets
                    .OrderBy(s => s.Id)
                    .Where(s => (aasxid == 0 || s.AASXId == aasxid) && (aasid == 0 || s.AASId == aasid) && (smid == 0 || s.Id == smid) &&
                        (searchLower == "" || s.Identifier.ToLower().Contains(searchLower) || s.IdShort.ToLower().Contains(searchLower) || s.SemanticId.ToLower().Contains(searchLower)))
                    .Take(size)
                    .ToList();
            }
            return data;
        }

        static public List<SMESet> GetPageSMEData(int size = 1000, string searchLower = "", long smid = 0, long smeid = 0)
        {
            List<SMESet> data = null;
            using (AasContext db = new AasContext())
            {
                data = db.SMESets
                    .OrderBy(sme => sme.Id)
                    .Where(sme => (smid == 0 || sme.SMId == smid) && (smeid == 0 || sme.Id == smeid) &&
                        (searchLower == "" || sme.IdShort.ToLower().Contains(searchLower) || sme.SemanticId.ToLower().Contains(searchLower) || sme.SMEType.ToLower().Contains(searchLower) || sme.ValueType.ToLower().Contains(searchLower)
                        || (sme.ValueType == "S" && db.SValueSets.Any(sv => sv.SMEId == sme.Id && sv.Annotation.ToLower().Contains(searchLower) && sv.Value.ToLower().Contains(searchLower)))
                        || (sme.ValueType == "I" && db.IValueSets.Any(sv => sv.SMEId == sme.Id && sv.Annotation.ToLower().Contains(searchLower) && sv.Value.ToString().ToLower().Contains(searchLower)))
                        || (sme.ValueType == "F" && db.DValueSets.Any(sv => sv.SMEId == sme.Id && sv.Annotation.ToLower().Contains(searchLower) && sv.Value.ToString().ToLower().Contains(searchLower)))
                        ))
                    .Take(size)
                    .ToList();
            }
            return data;
        }

        static public List<SValueSet> GetPageSValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            List<SValueSet> data = null;
            using (AasContext db = new AasContext())
            {
                data = db.SValueSets
                    .OrderBy(v => v.SMEId)
                    .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                        (searchLower == "" || v.Value.ToLower().Contains(searchLower) || v.Annotation.ToLower().Contains(searchLower)))
                    .Take(size)
                    .ToList();
            }
            return data;
        }

        static public List<IValueSet> GetPageIValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            long iEqual = 0;
            try
            {
                iEqual = Convert.ToInt64(searchLower);
            }
            catch { }

            List<IValueSet> data = null;
            using (AasContext db = new AasContext())
            {
                data = db.IValueSets
                    .OrderBy(v => v.SMEId)
                    .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                       (searchLower == "" || v.Annotation.ToLower().Contains(searchLower)) &&
                       (iEqual == 0 || v.Value == iEqual))
                    .Take(size)
                    .ToList();
            }
            return data;
        }

        static public List<DValueSet> GetPageDValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            double fEqual = 0;
            try
            {
                fEqual = Convert.ToDouble(searchLower);
            }
            catch { }

            List<DValueSet> data = null;
            using (AasContext db = new AasContext())
            {
                data = db.DValueSets
                    .OrderBy(v => v.SMEId)
                    .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                       (searchLower == "" || v.Annotation.ToLower().Contains(searchLower)) &&
                       (fEqual == 0 || v.Value == fEqual))
                    .Take(size)
                    .ToList();
            }
            return data;
        }
    }
}
