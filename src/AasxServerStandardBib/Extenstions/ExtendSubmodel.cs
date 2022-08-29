using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendSubmodel
    {
        public static Submodel ConvertFromV10(this Submodel submodel, AasxCompatibilityModels.AdminShellV10.Submodel sourceSubmodel, bool shallowCopy = false)
        {
            if (sourceSubmodel == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceSubmodel.idShort))
            {
                submodel.IdShort = "";
            }
            else
            {
                submodel.IdShort = sourceSubmodel.idShort;
            }

            if (sourceSubmodel.description != null)
            {
                submodel.Description = ExtensionsUtil.ConvertDescriptionFromV10(sourceSubmodel.description);
            }

            if (sourceSubmodel.administration != null)
            {
                submodel.Administration = new AdministrativeInformation(version: sourceSubmodel.administration.version, revision: sourceSubmodel.administration.revision);
            }

            if (sourceSubmodel.semanticId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceSubmodel.semanticId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }
                }
                submodel.SemanticId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            if (sourceSubmodel.kind != null)
            {
                if (sourceSubmodel.kind.IsInstance)
                {
                    submodel.Kind = ModelingKind.Instance;
                }
                else
                {
                    submodel.Kind = ModelingKind.Template;
                }
            }

            if (sourceSubmodel.qualifiers != null && sourceSubmodel.qualifiers.Count != 0)
            {
                if (submodel.Qualifiers == null && submodel.Qualifiers.Count != 0)
                {
                    submodel.Qualifiers = new List<Qualifier>();
                }

                foreach (var sourceQualifier in sourceSubmodel.qualifiers)
                {
                    var newQualifier = new Qualifier("", DataTypeDefXsd.String);
                    newQualifier = newQualifier.ConvertFromV10(sourceQualifier);
                    submodel.Qualifiers.Add(newQualifier);
                }
            }

            if (!shallowCopy && sourceSubmodel.submodelElements != null)
            {
                if (submodel.SubmodelElements == null)
                {
                    submodel.SubmodelElements = new List<ISubmodelElement>();
                }

                foreach (var submodelElementWrapper in sourceSubmodel.submodelElements)
                {
                    var sourceSubmodelELement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelELement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV10(sourceSubmodelELement, shallowCopy);
                        submodel.SubmodelElements.Add(outputSubmodelElement);
                    }

                }
            }

            return submodel;

        }

        public static Submodel ConvertFromV20(this Submodel submodel, AasxCompatibilityModels.AdminShellV20.Submodel sourceSubmodel, bool shallowCopy = false)
        {
            if (sourceSubmodel == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceSubmodel.idShort))
            {
                submodel.IdShort = "";
            }
            else
            {
                submodel.IdShort = sourceSubmodel.idShort;
            }

            if (sourceSubmodel.description != null)
            {
                submodel.Description = ExtensionsUtil.ConvertDescriptionFromV20(sourceSubmodel.description);
            }

            if (sourceSubmodel.administration != null)
            {
                submodel.Administration = new AdministrativeInformation(version: sourceSubmodel.administration.version, revision: sourceSubmodel.administration.revision);
            }

            if (sourceSubmodel.semanticId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceSubmodel.semanticId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }
                }
                submodel.SemanticId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            if (sourceSubmodel.kind != null)
            {
                if (sourceSubmodel.kind.IsInstance)
                {
                    submodel.Kind = ModelingKind.Instance;
                }
                else
                {
                    submodel.Kind = ModelingKind.Template;
                }
            }

            if (sourceSubmodel.qualifiers != null && sourceSubmodel.qualifiers.Count != 0)
            {
                if (submodel.Qualifiers == null)
                {
                    submodel.Qualifiers = new List<Qualifier>();
                }

                foreach (var sourceQualifier in sourceSubmodel.qualifiers)
                {
                    var newQualifier = new Qualifier("", DataTypeDefXsd.String);
                    newQualifier = newQualifier.ConvertFromV20(sourceQualifier);
                    submodel.Qualifiers.Add(newQualifier);
                }
            }

            if (!shallowCopy && sourceSubmodel.submodelElements != null)
            {
                if (submodel.SubmodelElements == null)
                {
                    submodel.SubmodelElements = new List<ISubmodelElement>();
                }

                foreach (var submodelElementWrapper in sourceSubmodel.submodelElements)
                {
                    var sourceSubmodelELement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelELement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV20(sourceSubmodelELement, shallowCopy);
                        submodel.SubmodelElements.Add(outputSubmodelElement);
                    }

                }
            }

            return submodel;

        }

        public static T FindFirstIdShortAs<T>(this Submodel submodel, string idShort) where T : ISubmodelElement
        {

            var submodelElement = submodel.SubmodelElements.Where(sme => (sme != null) && (sme is T) && sme.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            return (T)submodelElement;
        }

        public static IEnumerable<T> FindDeep<T>(this Submodel submodel)
        {
            if (submodel.SubmodelElements == null || submodel.SubmodelElements.Count == 0)
            {
                yield break;
            }

            foreach (var submodelElement in submodel.SubmodelElements)
            {
                submodelElement.FindDeep<T>();
            }
        }

        public static Reference GetModelReference(this Submodel submodel)
        {
            var key = new Key(KeyTypes.Submodel, submodel.Id);
            var outputReference = new Reference(ReferenceTypes.ModelReference, new List<Key>() { key })
            {
                ReferredSemanticId = submodel.SemanticId
            };

            return outputReference;
        }

        public static void RecurseOnSubmodelElements(this Submodel submodel, object state, Func<object, List<IReferable>, ISubmodelElement, bool> lambda)
        {
            submodel.SubmodelElements?.RecurseOnReferables(state, null, (o, par, rf) =>
            {
                if (rf is ISubmodelElement sme)
                    return lambda(o, par, sme);
                else
                    return true;
            });
        }

        public static ISubmodelElement FindSubmodelElementByIdShort(this Submodel submodel, string smeIdShort)
        {
            if (submodel.SubmodelElements == null || submodel.SubmodelElements.Count == 0)
            {
                return null;
            }

            var submodelElements = submodel.SubmodelElements.Where(sme => (sme != null) && sme.IdShort.Equals(smeIdShort, StringComparison.OrdinalIgnoreCase));
            if (submodelElements.Any())
            {
                return submodelElements.First();
            }
            else
            {
                return null;
            }
        }

        public static void SetAllParents(this Submodel submodel, DateTime timestamp)
        {
            if (submodel.SubmodelElements != null)
                foreach (var sme in submodel.SubmodelElements)
                    SetParentsForSME(submodel, sme, timestamp);
        }

        private static void SetParentsForSME(IReferable parent, ISubmodelElement submodelElement, DateTime timestamp)
        {
            if (submodelElement == null)
                return;

            submodelElement.Parent = parent;
            submodelElement.TimeStamp = timestamp;
            submodelElement.TimeStampCreate = timestamp;

            foreach (var childElement in submodelElement.EnumerateChildren())
            {
                SetParentsForSME(submodelElement, childElement, timestamp);
            }
        }

        private static void SetParentsForSME(IReferable parent, ISubmodelElement submodelElement)
        {
            if (submodelElement == null)
                return;

            submodelElement.Parent = parent;

            foreach (var childElement in submodelElement.EnumerateChildren())
            {
                SetParentsForSME(submodelElement, childElement);
            }
        }

        public static void SetAllParents(this Submodel submodel)
        {
            if (submodel.SubmodelElements != null)
                foreach (var sme in submodel.SubmodelElements)
                    SetParentsForSME(submodel, sme);
        }

        public static void Add(this Submodel submodel, ISubmodelElement submodelElement)
        {
            if (submodel.SubmodelElements == null)
            {
                submodel.SubmodelElements = new List<ISubmodelElement>();
            }

            submodelElement.Parent = submodel;
            submodel.SubmodelElements.Add(submodelElement);
        }

        public static void Insert(this Submodel submodel, int index, ISubmodelElement submodelElement)
        {
            if (submodel.SubmodelElements == null)
            {
                submodel.SubmodelElements = new List<ISubmodelElement>();
            }

            submodelElement.Parent = submodel;
            submodel.SubmodelElements.Insert(index, submodelElement);
        }

    }
}
