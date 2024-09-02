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


namespace Extensions
{
    public class AasElementSelfDescription
    {
        public string AasElementName { get; set; }

        public string ElementAbbreviation { get; set; }

        public KeyTypes? KeyType { get; set; }

        public AasSubmodelElements? SmeType { get; set; }

        public AasElementSelfDescription(string aasElementName, string elementAbbreviation,
            KeyTypes? keyType, AasSubmodelElements? smeType)
        {
            AasElementName = aasElementName;
            ElementAbbreviation = elementAbbreviation;
            KeyType = keyType;
            SmeType = smeType;
        }
    }
}
