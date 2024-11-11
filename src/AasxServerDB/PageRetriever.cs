/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
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

namespace AasxServerDB
{
    using System;
    using System.Linq;
    using AasxServerDB.Entities;
    using Microsoft.IdentityModel.Tokens;

    public class PageRetriever
    {
        public static List<EnvSet> GetPageEnvData(int size = 1000, string searchLower = "", long envid = 0, long cdid = 0)
        {
            // Create a query to filter by ids
            var db = new AasContext();
            IEnumerable<EnvSet> query;
            if (cdid != 0)
                query = db.EnvCDSets
                    .Where(envcd =>
                        envcd.CDId == cdid &&
                        (envid == 0 || envcd.EnvId == envid))
                    .Join(db.EnvSets, envcd => envcd.EnvId, env => env.Id, (envcd, env) => env);
            else
                query = db.EnvSets
                    .Where(env => envid == 0 || env.Id == envid);

            // If the search string is specified and elements match the previous condition
            if (!searchLower.IsNullOrEmpty() && query.Any())
                query = query.Where(env =>
                    env.Path != null && env.Path.ToLower().Contains(searchLower));

            // Return the results
            var result = query.Take(size).ToList();
            return result;
        }

        public static List<CDSet> GetPageCDData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long envid = 0, long cdid = 0)
        {
            // Check if a valid dateTime is provided
            var withDateTime = !dateTime.Equals(DateTime.MinValue);

            // Create a query to filter by ids
            var db = new AasContext();
            IEnumerable<CDSet> query;
            if (envid != 0)
                query = db.EnvCDSets
                    .Where(envcd =>
                        envcd.EnvId == envid &&
                        (cdid == 0  || envcd.CDId == cdid))
                    .Join(db.CDSets, envcd => envcd.CDId, cd => cd.Id, (envcd, cd) => cd);
            else
                query = db.CDSets
                    .Where(cd => cdid == 0 || cd.Id == cdid);

            // If the search string is specified and elements match the previous condition
            if (!searchLower.IsNullOrEmpty() && query.Any())
                query = query.Where(cd =>
                    (withDateTime                           && cd.TimeStampTree.CompareTo(dateTime) > 0) ||
                    (cd.IdShort != null                     && cd.IdShort.ToLower().Contains(searchLower)) ||
                    (cd.DisplayName != null                 && cd.DisplayName.ToLower().Contains(searchLower)) ||
                    (cd.Category != null                    && cd.Category.ToLower().Contains(searchLower)) ||
                    (cd.Description != null                 && cd.Description.ToLower().Contains(searchLower)) ||
                    (cd.Extensions != null                  && cd.Extensions.ToLower().Contains(searchLower)) ||
                    (cd.Identifier != null                  && cd.Identifier.ToLower().Contains(searchLower)) ||
                    (cd.IsCaseOf != null                    && cd.IsCaseOf.ToLower().Contains(searchLower)) ||
                    (cd.EmbeddedDataSpecifications != null  && cd.EmbeddedDataSpecifications.ToLower().Contains(searchLower)) ||
                    (cd.Version != null                     && cd.Version.ToLower().Contains(searchLower)) ||
                    (cd.Revision != null                    && cd.Revision.ToLower().Contains(searchLower)) ||
                    (cd.Creator != null                     && cd.Creator.ToLower().Contains(searchLower)) ||
                    (cd.TemplateId != null                  && cd.TemplateId.ToLower().Contains(searchLower)) ||
                    (cd.AEmbeddedDataSpecifications != null && cd.AEmbeddedDataSpecifications.ToLower().Contains(searchLower)));

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
                    (withDateTime                            && aas.TimeStampTree.CompareTo(dateTime) > 0) ||
                    (aas.IdShort != null                     && aas.IdShort.ToLower().Contains(searchLower)) ||
                    (aas.DisplayName != null                 && aas.DisplayName.ToLower().Contains(searchLower)) ||
                    (aas.Category != null                    && aas.Category.ToLower().Contains(searchLower)) ||
                    (aas.Description != null                 && aas.Description.ToLower().Contains(searchLower)) ||
                    (aas.Extensions != null                  && aas.Extensions.ToLower().Contains(searchLower)) ||
                    (aas.Identifier != null                  && aas.Identifier.ToLower().Contains(searchLower)) ||
                    (aas.EmbeddedDataSpecifications != null  && aas.EmbeddedDataSpecifications.ToLower().Contains(searchLower)) ||
                    (aas.DerivedFrom != null                 && aas.DerivedFrom.ToLower().Contains(searchLower)) ||
                    (aas.Version != null                     && aas.Version.ToLower().Contains(searchLower)) ||
                    (aas.Revision != null                    && aas.Revision.ToLower().Contains(searchLower)) ||
                    (aas.Creator != null                     && aas.Creator.ToLower().Contains(searchLower)) ||
                    (aas.TemplateId != null                  && aas.TemplateId.ToLower().Contains(searchLower)) ||
                    (aas.AEmbeddedDataSpecifications != null && aas.AEmbeddedDataSpecifications.ToLower().Contains(searchLower)) ||
                    (aas.AssetKind != null                   && aas.AssetKind.ToLower().Contains(searchLower)) ||
                    (aas.GlobalAssetId != null               && aas.GlobalAssetId.ToLower().Contains(searchLower)) ||
                    (aas.AssetType != null                   && aas.AssetType.ToLower().Contains(searchLower)) ||
                    (aas.SpecificAssetIds != null            && aas.SpecificAssetIds.ToLower().Contains(searchLower)) ||
                    (aas.DefaultThumbnailPath != null        && aas.DefaultThumbnailPath.ToLower().Contains(searchLower)) ||
                    (aas.DefaultThumbnailContentType != null && aas.DefaultThumbnailContentType.ToLower().Contains(searchLower)));

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
                    (withDateTime                           && sm.TimeStampTree.CompareTo(dateTime) > 0) ||
                    (sm.IdShort != null                     && sm.IdShort.ToLower().Contains(searchLower)) ||
                    (sm.DisplayName != null                 && sm.DisplayName.ToLower().Contains(searchLower)) ||
                    (sm.Category != null                    && sm.Category.ToLower().Contains(searchLower)) ||
                    (sm.Description != null                 && sm.Description.ToLower().Contains(searchLower)) ||
                    (sm.Extensions != null                  && sm.Extensions.ToLower().Contains(searchLower)) ||
                    (sm.Identifier != null                  && sm.Identifier.ToLower().Contains(searchLower)) ||
                    (sm.Kind != null                        && sm.Kind.ToLower().Contains(searchLower)) ||
                    (sm.SemanticId != null                  && sm.SemanticId.ToLower().Contains(searchLower)) ||
                    (sm.SupplementalSemanticIds != null     && sm.SupplementalSemanticIds.ToLower().Contains(searchLower)) ||
                    (sm.Qualifiers != null                  && sm.Qualifiers.ToLower().Contains(searchLower)) ||
                    (sm.EmbeddedDataSpecifications != null  && sm.EmbeddedDataSpecifications.ToLower().Contains(searchLower)) ||
                    (sm.Version != null                     && sm.Version.ToLower().Contains(searchLower)) ||
                    (sm.Revision != null                    && sm.Revision.ToLower().Contains(searchLower)) ||
                    (sm.Creator != null                     && sm.Creator.ToLower().Contains(searchLower)) ||
                    (sm.TemplateId != null                  && sm.TemplateId.ToLower().Contains(searchLower)) ||
                    (sm.AEmbeddedDataSpecifications != null && sm.AEmbeddedDataSpecifications.ToLower().Contains(searchLower)));

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
                    (withDateTime                           && sme.TimeStampTree.CompareTo(dateTime) > 0) ||
                    (sme.SMEType != null                    && sme.SMEType.ToLower().Contains(searchLower)) ||
                    (sme.IdShort != null                    && sme.IdShort.ToLower().Contains(searchLower)) ||
                    (sme.DisplayName != null                && sme.DisplayName.ToLower().Contains(searchLower)) ||
                    (sme.Category != null                   && sme.Category.ToLower().Contains(searchLower)) ||
                    (sme.Description != null                && sme.Description.ToLower().Contains(searchLower)) ||
                    (sme.Extensions != null                 && sme.Extensions.ToLower().Contains(searchLower)) ||
                    (sme.SemanticId != null                 && sme.SemanticId.ToLower().Contains(searchLower)) ||
                    (sme.SupplementalSemanticIds != null    && sme.SupplementalSemanticIds.ToLower().Contains(searchLower)) ||
                    (sme.Qualifiers != null                 && sme.Qualifiers.ToLower().Contains(searchLower)) ||
                    (sme.EmbeddedDataSpecifications != null && sme.EmbeddedDataSpecifications.ToLower().Contains(searchLower)) ||
                    (sme.TValue != null                     && sme.TValue.ToLower().Contains(searchLower)) ||
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

        public static List<OValueSet> GetPageOValueData(int size = 1000, string searchLower = "", long smeid = 0) =>
            new AasContext().OValueSets
                .Where(v =>
                    (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (v.Attribute != null && v.Attribute.ToLower().Contains(searchLower)) ||
                        (v.Value != null     && v.Value.ToLower().Contains(searchLower))))
                .Take(size)
                .ToList();

        public static List<SValueSet> GetPageSValueData(int size = 1000, string searchLower = "", long smeid = 0) =>
            new AasContext().SValueSets
                .Where(v =>
                    (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower)) ||
                        (v.Value != null      && v.Value.ToLower().Contains(searchLower))))
                .Take(size)
                .ToList();

        public static List<IValueSet> GetPageIValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            var withNum = Int64.TryParse(searchLower, out var iEqual);

            return new AasContext().IValueSets
                .Where(v =>
                    (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower)) ||
                        (withNum              && v.Value == iEqual)))
                .Take(size)
                .ToList();
        }

        public static List<DValueSet> GetPageDValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            var withNum = double.TryParse(searchLower, out var dEqual);

            return new AasContext().DValueSets
                .Where(v =>
                    (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() ||
                        (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower)) ||
                        (withNum              && v.Value == dEqual)))
                .Take(size)
                .ToList();
        }
    }
}