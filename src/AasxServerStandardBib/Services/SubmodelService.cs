
using AasxServer;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using Extensions;
using System;
using System.Collections.Generic;

namespace AasxServerStandardBib.Services
{
    public class SubmodelService : ISubmodelService
    {
        private readonly IAppLogger<SubmodelService> _logger;
        private readonly IAdminShellPackageEnvironmentService _packageEnvService;

        public SubmodelService(IAppLogger<SubmodelService> logger, IAdminShellPackageEnvironmentService packageEnvService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _packageEnvService = packageEnvService;
        }

        #region PrivateMethods

        private bool IsSubmodelElementPresent(string submodelIdentifier, string idShortPath, out ISubmodelElement output, out IReferable smeParent)
        {
            output = null;
            smeParent = null;
            var submodel = _packageEnvService.GetSubmodelById(submodelIdentifier, out _);

            if (submodel != null)
            {
                output = GetSubmodelElementByPath(submodel, idShortPath, out IReferable parent);
                smeParent = parent;
                if (output != null)
                {
                    _logger.LogInformation($"Found SubmodelElement at {idShortPath} in submodel with Id {submodelIdentifier}");
                    return true;
                }

            }

            return false;
        }

        //TODO:jtikekar refactor
        private ISubmodelElement GetSubmodelElementByPath(IReferable parent, string idShortPath, out IReferable outParent)
        {
            outParent = parent;
            if (idShortPath.Contains('.'))
            {
                string[] idShorts = idShortPath.Split('.', 2);
                if (parent is Submodel submodel)
                {
                    var submodelElement = submodel.FindSubmodelElementByIdShort(idShorts[0]);
                    if (submodelElement != null)
                    {
                        return GetSubmodelElementByPath(submodelElement, idShorts[1], out outParent);
                    }
                }
                else if (parent is SubmodelElementCollection collection)
                {
                    var submodelElement = collection.FindFirstIdShortAs<ISubmodelElement>(idShorts[0]);
                    if (submodelElement != null)
                    {
                        return GetSubmodelElementByPath(submodelElement, idShorts[1], out outParent);
                    }
                }
                else if (parent is SubmodelElementList list)
                {
                    var submodelElement = list.FindFirstIdShortAs<ISubmodelElement>(idShorts[0]);
                    if (submodelElement != null)
                    {
                        return GetSubmodelElementByPath(submodelElement, idShorts[1], out outParent);
                    }
                }
                else if (parent is Entity entity)
                {
                    var submodelElement = entity.FindFirstIdShortAs<ISubmodelElement>(idShortPath);
                    if (submodelElement != null)
                    {
                        return GetSubmodelElementByPath(submodelElement, idShorts[1], out outParent);
                    }
                }
                else if (parent is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    var submodelElement = annotatedRelationshipElement.FindFirstIdShortAs<ISubmodelElement>(idShortPath);
                    if (submodelElement != null)
                    {
                        return GetSubmodelElementByPath(submodelElement, idShorts[1], out outParent);
                    }
                }
                else
                {
                    throw new Exception($"Parent of type {parent.GetType()} not supported.");
                }
            }
            else
            {
                if (parent is Submodel submodel)
                {
                    var submodelElement = submodel.FindSubmodelElementByIdShort(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement;
                    }
                }
                else if (parent is SubmodelElementCollection collection)
                {
                    var submodelElement = collection.FindFirstIdShortAs<ISubmodelElement>(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement;
                    }
                }
                else if (parent is SubmodelElementList list)
                {
                    var submodelElement = list.FindFirstIdShortAs<ISubmodelElement>(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement;
                    }
                }
                else if (parent is Entity entity)
                {
                    var submodelElement = entity.FindFirstIdShortAs<ISubmodelElement>(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement;
                    }
                }
                else if (parent is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    var submodelElement = annotatedRelationshipElement.FindFirstIdShortAs<ISubmodelElement>(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement;
                    }
                }
                else
                {
                    throw new Exception($"Parent of type {parent.GetType()} not supported.");
                }
            }
            return null;
        }



        #endregion

        public void DeleteSubmodelById(string submodelIdentifier)
        {
            _packageEnvService.DeleteSubmodelById(submodelIdentifier);
        }

        public void DeleteSubmodelElementByPath(string submodelIdentifier, string idShortPath)
        {
            var found = IsSubmodelElementPresent(submodelIdentifier, idShortPath, out ISubmodelElement submodelElement, out IReferable smeParent);
            if (found)
            {
                if (smeParent is SubmodelElementCollection parentCollection)
                {
                    parentCollection.Value.Remove(submodelElement);
                }
                else if (smeParent is SubmodelElementList parentList)
                {
                    parentList.Value.Remove(submodelElement);
                }
                else if (smeParent is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    annotatedRelationshipElement.Annotations.Remove((IDataElement)submodelElement);
                }
                else if (smeParent is Entity entity)
                {
                    entity.Statements.Remove(submodelElement);
                }
                else if (smeParent is Submodel parentSubmodel)
                {
                    parentSubmodel.SubmodelElements.Remove(submodelElement);
                }
                else
                {
                    _logger.LogDebug($"Could not delete SubmodelElement {submodelElement.IdShort}");
                    throw new Exception($"Unsupported data type of parent {smeParent.IdShort} for delete operation.");
                }
            }
            else
            {
                throw new NotFoundException($"Requested SubmodelElement NOT found in submodel with Id {submodelIdentifier}");
            }

            Program.signalNewData(1);
            _logger.LogDebug($"Deleted SubmodelElement at {idShortPath} from submodel with Id {submodelIdentifier}");
        }

        public List<ISubmodelElement> GetAllSubmodelElements(string submodelIdentifier)
        {
            var submodel = _packageEnvService.GetSubmodelById(submodelIdentifier, out _);
            return submodel.SubmodelElements;
        }


    }
}
