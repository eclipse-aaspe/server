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

using AdminShellNS;

namespace AasSecurity.Models
{
    public class SecurityRole
    {
        public string?                     RulePath        { get; set; }
        public string?                     Condition       { get; set; }
        public string?                     Name            { get; set; }
        public string?                     ObjectType      { get; set; }
        public string?                     ApiOperation    { get; set; }
        public IClass?                     ObjectReference { get; set; }
        public string                     ObjectPath      { get; set; }
        public AccessRights?               Permission      { get; set; }
        public KindOfPermissionEnum?       Kind            { get; set; }
        public ISubmodel?                  Submodel        { get; set; }
        public string?                     SemanticId      { get; set; }
        public ISubmodelElementCollection? Usage           { get; set; }
        public string?                     AAS             { get; set; }
        public AdminShellPackageEnv?       UsageEnv        { get; set; }
    }
}