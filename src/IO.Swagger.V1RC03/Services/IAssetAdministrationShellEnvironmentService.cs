
using IO.Swagger.V1RC03.ApiModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using static IO.Swagger.V1RC03.Controllers.AssetAdministrationShellEnvironmentAPIController;

namespace IO.Swagger.V1RC03.Services
{
    public interface IAssetAdministrationShellEnvironmentService
    {
        AssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier, out int packageIndex);
        Submodel GetSubmodelById(string submodelIdentifier, out int packageIndex);
        List<AssetAdministrationShell> GetAllAssetAdministrationShells(List<SpecificAssetId> assetIds = null, string idShort = null);
        List<ConceptDescription> GetAllConceptDescriptions(string idShort = null, Reference reqIsCaseOf = null, Reference reqDataSpecificationRef = null);
        object GetAllSubmodelElements(string aasIdentifier, string submodelIdentifier, APIModels.Core.OutputModifierContext outputModifierContext = null);
        object GetAllSubmodelElementsFromSubmodel(string submodelIdentifier, APIModels.Core.OutputModifierContext outputModifierContext = null);
        List<Reference> GetAllSubmodelReferences(string aasIdentifier);
        List<Submodel> GetAllSubmodels(Reference reqSemanticId = null, string idShort = null);
        AssetInformation GetAssetInformationFromAas(string aasIdentifier);
        ConceptDescription GetConceptDescriptionById(string cdIdentifier, out int packageIndex);
        void DeleteAssetAdministrationShellById(string aasIdentifier);
        void DeleteConceptDescriptionById(string cdIdentifier);
        void DeleteSubmodelById(string submodelIdentifier);
        void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier);
        Submodel GetSubmodel(string aasIdentifier, string submodelIdentifier);
        ISubmodelElement GetSubmodelElementByPathSubmodelRepo(string submodelIdentifier, string idShortPath, out object smeParent);
        ISubmodelElement GetSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);
        void DeleteSubmodelElementByPathSubmodelRepo(string submodelIdentifier, string idShortPath);
        void DeleteSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);
        string GetFileByPathSubmodelRepo(string submodelIdentifier, string idShortPath, out byte[] byteArray, out long fileSize);
        string GetFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, out byte[] content, out long fileSize);
        OperationResult GetOperationAsyncResultSubmodelRepo(string submodelIdentifier, string idShortPath, string handleId);
        OperationResult GetOperationAsyncResult(string aasIdentifier, string submodelIdentifier, string idShortPath, string handleId);
        OperationResult InvokeOperationSubmodelRepo(string submodelIdentifier, string idShortPath, OperationRequest operationRequest);
        OperationHandle InvokeOperationAsyncSubmodelRepo(string submodelIdentifier, string idShortPath, OperationRequest operationRequest);
        AssetAdministrationShell CreateAssetAdministrationShell(AssetAdministrationShell body);
        ConceptDescription CreateConceptDescription(ConceptDescription body);
        Submodel CreateSubmodel(Submodel body, string aasIdentifier);
        ISubmodelElement CreateSubmodelElementSubmodelRepo(ISubmodelElement body, string submodelIdentifier, int first);
        ISubmodelElement CreateSubmodelElement(ISubmodelElement body, string aasIdentifier, string submodelIdentifier, int first);
        ISubmodelElement CreateSubmodelElementByPathSubmodelRepo(ISubmodelElement body, string submodelIdentifier, string idShortPath, int first);
        ISubmodelElement CreateSubmodelElementByPath(ISubmodelElement body, string aasIdentifier, string submodelIdentifier, string idShortPath, int first);
        Reference CreateSubmodelReference(Reference body, string aasIdentifier);
        void UpdateAssetAdministrationShellById(AssetAdministrationShell body, string aasIdentifier);
        void UpdateAssetInformation(AssetInformation body, string aasIdentifier);
        void UpdateConceptDescriptionById(ConceptDescription body, string cdIdentifier);
        void UpdateSubmodelById(Submodel body, string submodelIdentifier, APIModels.Core.OutputModifierContext outputModifierContext = null);
        void UpdateSubmodel(Submodel body, string aasIdentifier, string submodelIdentifier, APIModels.Core.OutputModifierContext outputModifierContext);
        void UpdateSubmodelElementByPathSubmodelRepo(ISubmodelElement body, string submodelIdentifier, string idShortPath, APIModels.Core.OutputModifierContext outputModifierContext);
        void UpdateSubmodelElementByPath(ISubmodelElement body, string aasIdentifier, string submodelIdentifier, string idShortPath, APIModels.Core.OutputModifierContext outputModifierContext);
        void UpdateFileByPathSubmodelRepo(string decodedSubmodelId, string idShortPath, string fileName, string contentType, Stream fileContent);
        void UpdateFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, Stream stream);
        void SecurityCheckInit(HttpContext _context, string _route, string _httpOperation);
        void SecurityCheck(string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null);
    }
}