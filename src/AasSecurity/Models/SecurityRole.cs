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

using AdminShellNS;

namespace AasSecurity.Models
{
    internal class SecurityRole
    {
        internal string?                     RulePath        { get; set; }
        internal string?                     Condition       { get; set; }
        internal string?                     Name            { get; set; }
        internal string?                     ObjectType      { get; set; }
        internal string?                     ApiOperation    { get; set; }
        internal IClass?                     ObjectReference { get; set; }
        internal string                     ObjectPath      { get; set; }
        internal AccessRights?               Permission      { get; set; }
        internal KindOfPermissionEnum?       Kind            { get; set; }
        internal ISubmodel?                  Submodel        { get; set; }
        internal string?                     SemanticId      { get; set; }
        internal ISubmodelElementCollection? Usage           { get; set; }
        internal string?                     AAS             { get; set; }
        internal AdminShellPackageEnv?       UsageEnv        { get; set; }
    }
}