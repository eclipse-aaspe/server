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

using AasSecurity.Models;
using AdminShellNS;
using Extensions;

namespace AasSecurity
{
    // TODO (jtikekar, 2023-09-04): whether to make it static
    internal class SecurityMetamodelParser
    {
        //private static ILogger _logger = ApplicationLogging.CreateLogger("SecurityMetamodelParser");
        internal static void ParserSecurityMetamodel(AdminShellPackageEnv? env, ISubmodel submodel)
        {
            var accessControlPolicyPoints = ParseAccessControlPolicyPoint(env, submodel);
        }

        private static AccessControlPolicyPoints ParseAccessControlPolicyPoint(AdminShellPackageEnv? env, ISubmodel submodel)
        {
            AccessControlPolicyPoints output = new AccessControlPolicyPoints();

            var acppCollection = submodel.FindFirstIdShortAs<SubmodelElementCollection>("accessControlPolicyPoints");
            if (acppCollection != null && acppCollection.Value != null)
            {
                foreach (var submodelElement in acppCollection.Value)
                {
                    if (submodelElement.IdShort != null)
                    {
                        switch (submodelElement.IdShort.ToLower())
                        {
                            case "policyadministrationpoint":
                                {
                                    if (submodelElement is SubmodelElementCollection policyAdminPointColl)
                                    {
                                        output.PolicyAdministrationPoint = ParsePolicyAdminPoint(env, policyAdminPointColl);
                                    }
                                    break;
                                }
                            default:
                                {
                                    //_logger.LogError($"Unhandled submodel element {submodelElement.IdShort} while parsing AccessControlPolicyPoint.");
                                    break;
                                }
                        }
                    }

                }
            }

            return output;
        }

        private static PolicyAdministrationPoint? ParsePolicyAdminPoint(AdminShellPackageEnv? env, SubmodelElementCollection policyAdminPointColl)
        {
            PolicyAdministrationPoint? output = new PolicyAdministrationPoint();
            foreach (var submodelElement in policyAdminPointColl.Value!)
            {
                if (submodelElement.IdShort != null)
                {
                    switch (submodelElement.IdShort.ToLower())
                    {
                        case "externalaccesscontrol":
                            {
                                if (submodelElement is Property property)
                                {
                                    output.ExternalAccessControl = bool.Parse(property.Value!);
                                }
                                break;
                            }
                        case "localaccesscontrol":
                            {
                                if (submodelElement is SubmodelElementCollection localAccessControl && localAccessControl.Value != null)
                                {
                                    var accessPermissionRules = localAccessControl.Value[0] as SubmodelElementCollection;
                                    if (accessPermissionRules != null && accessPermissionRules.Value != null)
                                    {
                                        output.LocalAccessControl = new AccessControl();
                                        output.LocalAccessControl.AccessPermissionRules = new List<AccessPermissionRule>();
                                        foreach (var rule in accessPermissionRules.Value)
                                        {
                                            output.LocalAccessControl.AccessPermissionRules.Add(ParseAccessPermissionRule(env, rule));
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                //_logger.LogError($"Unhandled submodel element {submodelElement.IdShort} while parsing PolicyAdministrationPoint.");
                                break;
                            }
                    }
                }
            }
            return output;
        }

        private static AccessPermissionRule? ParseAccessPermissionRule(AdminShellPackageEnv? env, ISubmodelElement ruleElement)
        {
            AccessPermissionRule? output = null;
            if (ruleElement is SubmodelElementCollection rule && rule.Value != null)
            {
                output = new AccessPermissionRule();
                foreach (var submodelElement in rule.Value)
                {
                    switch (submodelElement.IdShort!.ToLower())
                    {
                        case "targetsubjectattributes":
                            {
                                if (submodelElement is SubmodelElementCollection targetSubjectAttributes)
                                {
                                    output.TargetSubjectAttributes = new List<string>();
                                    foreach (var subjectAttributeElement in targetSubjectAttributes.Value!)
                                    {
                                        if (subjectAttributeElement is Property property)
                                        {
                                            output.TargetSubjectAttributes.Add(property.IdShort!);
                                        }
                                    }
                                }
                                break;
                            }
                        case "permissionsperobject":
                            {
                                if (submodelElement is SubmodelElementCollection permissionsPerObjects)
                                {
                                    output.PermissionsPerObject = new List<PermissionsPerObject>();
                                    foreach (var permPerObjColl in permissionsPerObjects.Value!)
                                    {
                                        output.PermissionsPerObject.Add(ParsePermissionPerObject(env, permPerObjColl));
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                //_logger.LogError($"Unhandled submodel element {submodelElement.IdShort} while parsing AccessPermissionRule.");
                                break;
                            }
                    }
                }
            }
            if (output != null)
            {
                CreateSecurityRule(env, output);
            }
            return output;
        }

        private static void CreateSecurityRule(AdminShellPackageEnv? env, AccessPermissionRule? accPermRule)
        {
            if (accPermRule.TargetSubjectAttributes != null && accPermRule.PermissionsPerObject != null)
            {
                foreach (var subjectAttribute in accPermRule.TargetSubjectAttributes)
                {
                    foreach (var permPerObject in accPermRule.PermissionsPerObject)
                    {
                        var permissions = permPerObject.Permission.Permissions;
                        foreach (var permission in permissions)
                        {
                            var securityRole = new SecurityRole();
                            if (subjectAttribute.Contains(':'))
                            {
                                string[] split = subjectAttribute.Split(':');
                                securityRole.Name = split[1];
                                securityRole.Condition = split[0].ToLower();
                            }
                            else
                            {
                                securityRole.Name = subjectAttribute;
                                securityRole.Condition = ""; // TODO (jtikekar, 2023-09-04):jtikekar handle by defaults
                            }

                            if (permPerObject._Object is Property objectProperty && objectProperty != null)
                            {
                                ReadPermissionPerObjectProperty(securityRole, objectProperty);
                            }
                            else if (permPerObject._Object is IClass _object)
                            {
                                ReadPermissionPerObject(securityRole, _object);
                            }
                            Enum.TryParse(permission, true, out AccessRights accessRight);
                            securityRole.Permission = accessRight;
                            securityRole.Kind = permPerObject.Permission.KindOfPermission;
                            securityRole.Usage = permPerObject.Usage;
                            securityRole.UsageEnv = env;
                            GlobalSecurityVariables.SecurityRoles.Add(securityRole);
                        }
                    }
                }
            }
        }

        private static void ReadPermissionPerObject(SecurityRole securityRole, IClass _object)
        {
            securityRole.ObjectReference = _object;
            if (_object is IReferable referable)
            {
                securityRole.ObjectType = referable.GetSelfDescription().ElementAbbreviation.ToLower();
                if (_object is Submodel submodel)
                {
                    securityRole.Submodel = submodel;
                    securityRole.ObjectPath = submodel.IdShort!;
                }
                else if (_object is ISubmodelElement submodelElement)
                {
                    securityRole.ObjectType = "submodelElement";
                    IReferable parent = submodelElement;
                    string path = parent.IdShort!;
                    while (parent.Parent != null)
                    {
                        parent = (IReferable)parent.Parent;
                        path = parent.IdShort! + "." + path;
                    }
                    securityRole.Submodel = parent as Submodel;
                    securityRole.ObjectPath = path;
                }
            }
        }

        private static void ReadPermissionPerObjectProperty(SecurityRole securityRole, Property objectProperty)
        {
            var propValue = objectProperty.Value!.ToLower();
            if (propValue != null)
            {
                securityRole.ObjectType = propValue;
                if (propValue.Contains("api"))
                {
                    string[] split = propValue.Split(":");
                    if (split[0] == "api")
                    {
                        securityRole.ObjectType = split[0];
                        securityRole.ApiOperation = split[1];
                    }
                }
                else if (propValue.Contains("semanticid"))
                {
                    string[] split = propValue.Split(':');
                    if (split[0] == "semanticid")
                    {
                        securityRole.ObjectType = split[0];
                        securityRole.SemanticId = split[1];
                        for (int j = 2; j < split.Length; j++)
                            securityRole.SemanticId += ":" + split[j];
                    }
                }
                else if (propValue.Contains("aas"))
                {
                    string[] split = propValue.Split(':');
                    if (split[0] == "aas")
                    {
                        securityRole.ObjectType = split[0];
                        securityRole.AAS = split[1];
                        for (int j = 2; j < split.Length; j++)
                            securityRole.AAS += ":" + split[j];
                    }
                }
                //The permission is on AAS Property
                else
                {
                    securityRole.ObjectType = "submodelElement";
                    IReferable parent = objectProperty;
                    string path = parent.IdShort!;
                    while (parent.Parent != null)
                    {
                        parent = (IReferable)parent.Parent;
                        path = parent.IdShort! + "." + path;
                    }
                    securityRole.Submodel = parent as Submodel;
                    securityRole.ObjectPath = path;
                }
            }
        }

        private static PermissionsPerObject? ParsePermissionPerObject(AdminShellPackageEnv? env, ISubmodelElement permPerObjColl)
        {
            if (permPerObjColl == null) return null;

            var output = new PermissionsPerObject();
            if (permPerObjColl is SubmodelElementCollection permPerObj)
            {
                foreach (var submodelElement in permPerObj.Value!)
                {
                    switch (submodelElement.IdShort!.ToLower())
                    {
                        case "object":
                            {
                                if (submodelElement is IReferenceElement objectRef)
                                {
                                    output._Object = env.AasEnv.FindReferableByReference(objectRef.Value!);
                                }
                                else if (submodelElement is Property objectProp)
                                {
                                    output._Object = objectProp;
                                }
                                break;
                            }
                        case "permission":
                            {
                                if (submodelElement is SubmodelElementCollection permission && permission.Value != null)
                                {
                                    output.Permission = new Permission();
                                    output.Permission.Permissions = new List<string>();
                                    foreach (var permissionElement in permission.Value!)
                                    {
                                        if (permissionElement is Property permKindProp)
                                        {
                                            Enum.TryParse(permKindProp.Value, out KindOfPermissionEnum permissionKind);
                                            output.Permission.KindOfPermission = permissionKind;
                                        }
                                        else if (permissionElement is ReferenceElement permissionRef)
                                        {
                                            var perm = env.AasEnv.FindReferableByReference(permissionRef.Value);
                                            if (perm != null && perm is Property permProp)
                                            {
                                                output.Permission.Permissions.Add(permProp.IdShort!);
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        case "usage":
                            {
                                if (submodelElement is ISubmodelElementCollection usageCollection)
                                {
                                    output.Usage = Copying.Deep(usageCollection);
                                }
                                break;
                            }
                        default:
                            {
                                //_logger.LogError($"Unhandled submodel element {submodelElement.IdShort} while parsing PermissionPerObject.");
                                break;
                            }
                    }
                }
            }

            return output;
        }
    }
}