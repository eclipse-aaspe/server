using AasSecurity.Models;
using AdminShellNS;
using Extensions;

namespace AasSecurity;

internal static class SecurityMetamodelParser
{
    private const char Separator = ':';

    internal static void ParserSecurityMetamodel(AdminShellPackageEnv env, ISubmodel submodel)
    {
        ParseAccessControlPolicyPoint(env, submodel);
    }

    private static void ParseAccessControlPolicyPoint(AdminShellPackageEnv env, ISubmodel submodel)
    {
        var output = new AccessControlPolicyPoints();

        var accessControlPolicyCollection = submodel.FindFirstIdShortAs<SubmodelElementCollection>("accessControlPolicyPoints");
        if (accessControlPolicyCollection?.Value == null) return;
        foreach (var submodelElement in accessControlPolicyCollection.Value)
        {
            if (submodelElement.IdShort == null || submodelElement.IdShort.ToLower() != "policyadministrationpoint")
            {
                continue;
            }

            if (submodelElement is SubmodelElementCollection policyAdminPointColl)
            {
                output.PolicyAdministrationPoint = ParsePolicyAdminPoint(env, policyAdminPointColl);
            }
        }
    }

    private static PolicyAdministrationPoint ParsePolicyAdminPoint(AdminShellPackageEnv env, ISubmodelElementCollection policyAdminPointColl)
    {
        var output = new PolicyAdministrationPoint();
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
                                output.LocalAccessControl.AccessPermissionRules = new List<AccessPermissionRule?>();
                                foreach (var rule in accessPermissionRules.Value)
                                {
                                    output.LocalAccessControl.AccessPermissionRules.Add(ParseAccessPermissionRule(env, rule));
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        return output;
    }

    private static AccessPermissionRule? ParseAccessPermissionRule(AdminShellPackageEnv env, IClass ruleElement)
    {
        AccessPermissionRule? output = null;
        if (ruleElement is SubmodelElementCollection {Value: not null} rule)
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
                }
            }
        }

        if (output != null)
        {
            CreateSecurityRule(env, output);
        }

        return output;
    }

    private static void CreateSecurityRule(AdminShellPackageEnv env, AccessPermissionRule? accPermRule)
    {
        if (accPermRule == null)
        {
            return;
        }

        foreach (var subjectAttribute in accPermRule.TargetSubjectAttributes)
        {
            foreach (var permPerObject in accPermRule.PermissionsPerObject)
            {
                var permissions = permPerObject.Permission.Permissions;
                foreach (var permission in permissions)
                {
                    var securityRole = new SecurityRole();
                    if (subjectAttribute.Contains(Separator))
                    {
                        var split = subjectAttribute.Split(Separator);
                        securityRole.Name = split[1];
                        securityRole.Condition = split[0].ToLower();
                    }
                    else
                    {
                        securityRole.Name = subjectAttribute;
                        securityRole.Condition = string.Empty;
                    }

                    switch (permPerObject._Object)
                    {
                        case Property objectProperty:
                            ReadPermissionPerObjectProperty(securityRole, objectProperty);
                            break;
                        case { } _object:
                            ReadPermissionPerObject(securityRole, _object);
                            break;
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

    private static void ReadPermissionPerObject(SecurityRole securityRole, IClass _object)
    {
        securityRole.ObjectReference = _object;
        if (_object is not IReferable referable)
        {
            return;
        }

        securityRole.ObjectType = referable.GetSelfDescription().ElementAbbreviation.ToLower();
        switch (_object)
        {
            case Submodel submodel:
                securityRole.Submodel = submodel;
                securityRole.ObjectPath = submodel.IdShort!;
                break;
            case ISubmodelElement submodelElement:
            {
                securityRole.ObjectType = "submodelElement";
                IReferable parent = submodelElement;
                var path = parent.IdShort!;
                while (parent.Parent != null)
                {
                    parent = (IReferable) parent.Parent;
                    path = $"{parent.IdShort!}.{path}";
                }

                securityRole.Submodel = parent as Submodel;
                securityRole.ObjectPath = path;
                break;
            }
        }
    }

    private static void ReadPermissionPerObjectProperty(SecurityRole securityRole, IProperty objectProperty)
    {
        var propValue = objectProperty.Value!.ToLower();
        securityRole.ObjectType = propValue;
        if (propValue.Contains("api"))
        {
            var split = propValue.Split(Separator);
            if (!split[0].Equals("api"))
            {
                return;
            }

            securityRole.ObjectType = split[0];
            securityRole.ApiOperation = split[1];
        }
        else if (propValue.Contains("semanticid"))
        {
            var split = propValue.Split(Separator);
            if (!split[0].Equals("semanticid"))
            {
                return;
            }

            securityRole.ObjectType = split[0];
            securityRole.SemanticId = split[1];
            for (var j = 2; j < split.Length; j++)
            {
                securityRole.SemanticId += $"{Separator}{split[j]}";
            }
        }
        else if (propValue.Contains("aas"))
        {
            var split = propValue.Split(Separator);
            if (!split[0].Equals("aas"))
            {
                return;
            }

            securityRole.ObjectType = split[0];
            securityRole.AAS = split[1];
            for (var j = 2; j < split.Length; j++)
            {
                securityRole.AAS += $"{Separator}{split[j]}";
            }
        }
        //The permission is on AAS Property
        else
        {
            securityRole.ObjectType = "submodelElement";
            IReferable parent = objectProperty;
            var path = parent.IdShort!;
            while (parent.Parent != null)
            {
                parent = (IReferable) parent.Parent;
                path = $"{parent.IdShort!}.{path}";
            }

            securityRole.Submodel = parent as Submodel;
            securityRole.ObjectPath = path;
        }
    }

    private static PermissionsPerObject ParsePermissionPerObject(AdminShellPackageEnv env, IClass permPerObjColl)
    {
        var output = new PermissionsPerObject();
        if (permPerObjColl is not SubmodelElementCollection permPerObj)
        {
            return output;
        }
        foreach (var submodelElement in permPerObj.Value!)
        {
            switch (submodelElement.IdShort!.ToLower())
            {
                case "object":
                {
                    output._Object = submodelElement switch
                    {
                        IReferenceElement objectRef => env.AasEnv.FindReferableByReference(objectRef.Value!),
                        Property objectProp => objectProp,
                        _ => output._Object
                    };

                    break;
                }
                case "permission":
                {
                    if (submodelElement is SubmodelElementCollection {Value: not null} permission)
                    {
                        output.Permission = new Permission();
                        output.Permission.Permissions = new List<string>();
                        foreach (var permissionElement in permission.Value!)
                        {
                            switch (permissionElement)
                            {
                                case Property permKindProp:
                                    Enum.TryParse(permKindProp.Value, out KindOfPermissionEnum permissionKind);
                                    output.Permission.KindOfPermission = permissionKind;
                                    break;
                                case ReferenceElement permissionRef:
                                {
                                    var perm = env.AasEnv.FindReferableByReference(permissionRef.Value);
                                    if (perm is Property permProp)
                                    {
                                        output.Permission.Permissions.Add(permProp.IdShort!);
                                    }

                                    break;
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
            }
        }

        return output;
    }
}