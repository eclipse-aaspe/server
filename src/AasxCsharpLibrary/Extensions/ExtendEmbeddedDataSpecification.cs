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

using System.Collections.Generic;
// using DataSpecificationContent = HasDataSpecification.DataSpecificationContent;

namespace Extensions
{
    // TODO (Jui, 2022-12-21): I do not know, if to put the List<> extension here or in a separate file
    public static class ExtendListOfEmbeddedDataSpecification
    {


        public static EmbeddedDataSpecification FindFirstIEC61360Spec(this List<EmbeddedDataSpecification> list)
        {
            foreach (var eds in list)
                if (eds?.DataSpecificationContent is DataSpecificationIec61360
                    || eds?.DataSpecification?.MatchesExactlyOneKey(
                        ExtendIDataSpecificationContent.GetKeyForIec61360()) == true)
                    return eds;
            return null;
        }

        public static DataSpecificationIec61360 GetIEC61360Content(this List<IEmbeddedDataSpecification> list)
        {
            foreach (var eds in list)
                if (eds?.DataSpecificationContent is DataSpecificationIec61360 dsiec)
                    return dsiec;
            return null;
        }

        // TODO (jtikekar, 2023-09-04): DataSpecificationPhysicalUnit
        //public static DataSpecificationPhysicalUnit GetPhysicalUnitContent(this List<EmbeddedDataSpecification> list)
        //{
        //    foreach (var eds in list)
        //        if (eds?.DataSpecificationContent is DataSpecificationPhysicalUnit dspu)
        //            return dspu;
        //    return null;
        //}
    }

    public static class ExtendEmbeddedDataSpecification
    {
        public static EmbeddedDataSpecification ConvertFromV20(this EmbeddedDataSpecification embeddedDataSpecification, AasxCompatibilityModels.AdminShellV20.EmbeddedDataSpecification sourceEmbeddedSpec)
        {
            if (sourceEmbeddedSpec != null)
            {
                if (sourceEmbeddedSpec.dataSpecification != null && !sourceEmbeddedSpec.dataSpecification.IsEmpty)
                {
                    embeddedDataSpecification.DataSpecification = ExtensionsUtil.ConvertReferenceFromV20(sourceEmbeddedSpec.dataSpecification, ReferenceTypes.ExternalReference);

                    // TODO (MIHO, 2022-19-12): check again, see questions
                    var o2id  = "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360";
                    var oldid = "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/2/0";
                    var newid = "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0";

                    if (sourceEmbeddedSpec.dataSpecification?.Matches("", false, "IRI", oldid,
                        AasxCompatibilityModels.AdminShellV20.Key.MatchMode.Identification) == true)
                    {
                        embeddedDataSpecification.DataSpecification.Keys[0].Value = newid;
                    }

                    if (sourceEmbeddedSpec.dataSpecification?.Matches("", false, "IRI", o2id,
                        AasxCompatibilityModels.AdminShellV20.Key.MatchMode.Identification) == true)
                    {
                        embeddedDataSpecification.DataSpecification.Keys[0].Value = newid;
                    }
                }
            }

            if (sourceEmbeddedSpec.dataSpecificationContent?.dataSpecificationIEC61360 != null)
            {
                embeddedDataSpecification.DataSpecificationContent =
                    new DataSpecificationIec61360(null).ConvertFromV20(
                        sourceEmbeddedSpec.dataSpecificationContent.dataSpecificationIEC61360);
            }
            return embeddedDataSpecification;
        }

        public static EmbeddedDataSpecification CreateIec61360WithContent(DataSpecificationIec61360 content = null)
        {
            if (content == null)
                content = new DataSpecificationIec61360(null);

            var res = new EmbeddedDataSpecification(
                new Reference(ReferenceTypes.ExternalReference,
                    new List<IKey>(new[] { ExtendIDataSpecificationContent.GetKeyForIec61360() })),
                content);
            return res;
        }

        public static bool FixReferenceWrtContent(this EmbeddedDataSpecification eds)
        {
            // does content tell something?
            var ctc = ExtendIDataSpecificationContent.GuessContentTypeFor(eds?.DataSpecificationContent);
            var ctr = ExtendIDataSpecificationContent.GuessContentTypeFor(eds?.DataSpecification);

            if (ctc == ExtendIDataSpecificationContent.ContentTypes.NoInfo)
                return false;

            if (ctr == ctc)
                return false;

            // ok, fix
            eds.DataSpecification = new Reference(ReferenceTypes.ExternalReference,
                new List<IKey> { ExtendIDataSpecificationContent.GetKeyFor(ctc) });
            return true;
        }
    }
}
