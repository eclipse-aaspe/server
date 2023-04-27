
using AasxServerStandardBib.Logging;
using Extensions;
using IO.Swagger.V1RC03.APIModels.Metadata;
using IO.Swagger.V1RC03.APIModels.ValueOnly;
using IO.Swagger.V1RC03.Exceptions;
using IO.Swagger.V1RC03.Extensions;
using System.Collections.Generic;
using Nodes = System.Text.Json.Nodes;

namespace IO.Swagger.V1RC03.Services
{
    public class OutputModifiersService : IOutputModifiersService
    {
        private readonly IAppLogger<OutputModifiersService> _logger;

        public OutputModifiersService(IAppLogger<OutputModifiersService> logger)
        {
            _logger = logger;
        }

        //TODO: jtikekar Remove
        //public object ApplyOutputModifiers(object obj, string level = null, string content = null, string extent = null)
        //{
        //    ValidateOutputModifiers(obj, level, content, extent);
        //    object output = ApplyLevelModifier(obj, level);
        //    output = ApplyContentModifier(output, content);

        //    return output;
        //}

        private object ApplyContentModifier(object obj, string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                switch (content.ToLower())
                {
                    case "path":
                        {
                            var idShortPathList = new List<string>();
                            GetIdShortPath(obj, ref idShortPathList, "");
                            return idShortPathList;
                        }
                    case "reference":
                        {
                            return GetObjectReference(obj);
                        }
                    case "value":
                        {
                            Nodes.JsonObject json = ValueOnlySerializer.ToJsonObject((IClass)obj, null);
                            return json;
                        }
                    case "metadata":
                        {
                            Nodes.JsonObject json = MetadataSerializer.ToJsonObject((IClass)obj);
                            return json;
                        }
                    case "normal":
                        {
                            Nodes.JsonObject json = Jsonization.Serialize.ToJsonObject((IClass)obj);
                            return json;
                        }
                }
            }

            return obj;
        }


        //TODO:remove
        private object GetObjectReference(object obj)
        {
            switch (obj)
            {
                case AssetAdministrationShell aas:
                    {
                        return aas.GetReference();
                    }
                case Submodel submodel:
                    {
                        return submodel.GetReference();
                    }
                case ISubmodelElement submodelElement:
                    {
                        return submodelElement.GetModelReference();
                    }
                case List<ISubmodelElement> submodelElements:
                    {
                        List<object> references = new List<object>();
                        foreach (var submodelElement in submodelElements)
                        {
                            object value = GetObjectReference(submodelElement);
                            references.Add(value);
                        }
                        return references;
                    }
                default:
                    {
                        throw new InvalidOutputModifierException("reference");
                    }
            }
        }

        //TODO:jtikekar remove
        private void GetIdShortPath(object obj, ref List<string> idShortPathList, string parentPath)
        {
            switch (obj)
            {
                case Submodel submodel:
                    {
                        idShortPathList.Add(submodel.IdShort);
                        parentPath = submodel.IdShort;
                        foreach (var submodelElement in submodel.SubmodelElements)
                        {
                            GetIdShortPath(submodelElement, ref idShortPathList, parentPath);
                        }
                        break;
                    }
                case SubmodelElementCollection collection:
                    {
                        if (idShortPathList.Count == 0)
                        {
                            idShortPathList.Add(collection.IdShort);
                        }
                        else
                        {
                            idShortPathList.Add(parentPath + "." + collection.IdShort);
                        }

                        parentPath = string.IsNullOrEmpty(parentPath) ? collection.IdShort : $"{parentPath}.{collection.IdShort}";
                        foreach (var submodelElement in collection.Value)
                        {
                            GetIdShortPath(submodelElement, ref idShortPathList, parentPath);
                        }
                        break;
                    }
                case SubmodelElementList list:
                    {
                        if (idShortPathList.Count == 0)
                        {
                            idShortPathList.Add(list.IdShort);
                        }
                        else
                        {
                            idShortPathList.Add(parentPath + "." + list.IdShort);
                        }
                        parentPath = string.IsNullOrEmpty(parentPath) ? list.IdShort : $"{parentPath}.{list.IdShort}";
                        foreach (var submodelElement in list.Value)
                        {
                            GetIdShortPath(submodelElement, ref idShortPathList, parentPath);
                        }
                        break;
                    }
                case Entity entitiy:
                    {
                        if (idShortPathList.Count == 0)
                        {
                            idShortPathList.Add(entitiy.IdShort);
                        }
                        else
                        {
                            idShortPathList.Add(parentPath + "." + entitiy.IdShort);
                        }
                        parentPath = string.IsNullOrEmpty(parentPath) ? entitiy.IdShort : $"{parentPath}.{entitiy.IdShort}";
                        foreach (var submodelElement in entitiy.Statements)
                        {
                            GetIdShortPath(submodelElement, ref idShortPathList, parentPath);
                        }
                        break;
                    }
                case ISubmodelElement submodelElement:
                    {
                        idShortPathList.Add(parentPath + "." + submodelElement.IdShort);
                        break;
                    }
                default:
                    {
                        throw new InvalidOutputModifierException("path");
                    }
            }
        }

        //TODO:jtikekar remove
        //public object ApplyLevelModifier(object obj, string level)
        //{
        //    if (!string.IsNullOrEmpty(level) && level.Equals("core", StringComparison.OrdinalIgnoreCase))
        //    {
        //        switch (obj)
        //        {
        //            case Submodel submodel:
        //                {
        //                    var submodelCore = new SubmodelCore(submodel);
        //                    return submodelCore;
        //                }
        //            case SubmodelElementCollection collection:
        //                {
        //                    var submodelCore = new SubmodelElementCollectionCore(collection);
        //                    return submodelCore;
        //                }
        //            case SubmodelElementList list:
        //                {
        //                    var submodelCore = new SubmodelElementListCore(list);
        //                    return submodelCore;
        //                }
        //            case Entity entity:
        //                {
        //                    var submodelCore = new EntityCore(entity);
        //                    return submodelCore;
        //                }
        //        }
        //    }

        //    return obj;
        //}

        public void ValidateOutputModifiers(object obj, string level = null, string content = null, string extent = null)
        {
            switch (obj)
            {
                case AssetAdministrationShell:
                    {
                        if (!string.IsNullOrEmpty(level))
                        {
                            throw new InvalidOutputModifierException(level);
                        }
                        if (!string.IsNullOrEmpty(content) && !content.EqualsAny("normal", "reference"))
                        {
                            throw new InvalidOutputModifierException(content);
                        }
                        if (!string.IsNullOrEmpty(extent))
                        {
                            throw new InvalidOutputModifierException(extent);
                        }
                        break;
                    }
                case SubmodelElementCollection:
                case SubmodelElementList:
                case Entity:
                case Submodel:
                    {
                        if (!string.IsNullOrEmpty(level) && !level.EqualsAny("deep", "core"))
                        {
                            throw new InvalidOutputModifierException(level);
                        }
                        if (!string.IsNullOrEmpty(content) && !content.EqualsAny("normal", "metadata", "value", "reference", "path"))
                        {
                            throw new InvalidOutputModifierException(content);
                        }
                        if (!string.IsNullOrEmpty(extent) && !extent.EqualsAny("withoutBlobValue", "withBlobValue"))
                        {
                            throw new InvalidOutputModifierException(extent);
                        }

                        break;
                    }
                case BasicEventElement:
                    {
                        if (!string.IsNullOrEmpty(level) && !level.EqualsAny("deep", "core"))
                        {
                            throw new InvalidOutputModifierException(level);
                        }
                        if (!string.IsNullOrEmpty(content) && !content.EqualsAny("normal", "metadata", "value", "reference"))
                        {
                            throw new InvalidOutputModifierException(content);
                        }
                        if (!string.IsNullOrEmpty(extent) && !extent.EqualsAny("withoutBlobValue", "withBlobValue"))
                        {
                            throw new InvalidOutputModifierException(extent);
                        }

                        break;
                    }
                case Capability:
                case Operation:
                    {
                        if (!string.IsNullOrEmpty(level) && !level.EqualsAny("deep", "core"))
                        {
                            throw new InvalidOutputModifierException(level);
                        }
                        if (!string.IsNullOrEmpty(content) && !content.EqualsAny("normal", "reference"))
                        {
                            throw new InvalidOutputModifierException(content);
                        }
                        if (!string.IsNullOrEmpty(extent) && !extent.EqualsAny("withoutBlobValue", "withBlobValue"))
                        {
                            throw new InvalidOutputModifierException(extent);
                        }

                        break;
                    }
                case IDataElement:
                    {
                        if (!string.IsNullOrEmpty(level) && !level.EqualsAny("deep", "core"))
                        {
                            throw new InvalidOutputModifierException(level);
                        }
                        if (!string.IsNullOrEmpty(content) && !content.EqualsAny("normal", "metadata", "value", "reference"))
                        {
                            throw new InvalidOutputModifierException(content);
                        }
                        if (!string.IsNullOrEmpty(extent) && !extent.EqualsAny("withoutBlobValue", "withBlobValue"))
                        {
                            throw new InvalidOutputModifierException(extent);
                        }

                        break;
                    }
            }
        }


    }
}
