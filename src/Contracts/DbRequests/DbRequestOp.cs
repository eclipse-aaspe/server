namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum DbRequestOp
{
    ReadAllAssetAdministrationShells,
    ReadSubmodelById,
    ReadPagedSubmodelElements,
    ReadSubmodelElementByPath,
    ReadAllSubmodels,
    ReadAssetAdministrationShellById,
    ReadFileByPath,
    ReadAssetInformation,
    ReadThumbnail,
    CreateSubmodel,
    CreateAssetAdministrationShell,
    CreateSubmodelElement,
    CreateSubmodelElementByPath,
    CreateSubmodelReference,
    UpdateSubmodelById,
    UpdateSubmodelElementByPath,
    UpdateAssetInformation,
    UpdateFileByPath,
    UpdateThumbnail,
    UpdateAssetAdministrationShellById,
    ReplaceSubmodelById,
    ReplaceSubmodelElementByPath,
    ReplaceFileByPath,
    DeleteAssetAdministrationShellById,
    DeleteFileByPath,
    DeleteSubmodelById,
    DeleteSubmodelElementByPath,
    DeleteSubmodelReferenceById,
    DeleteThumbnail
}
