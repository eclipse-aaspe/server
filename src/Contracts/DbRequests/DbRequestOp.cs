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
    ReadPagedSubmodels,
    ReadAssetAdministrationShellById,
    ReadFileByPath,
    ReadAssetInformation,
    ReadThumbnail,
    ReadPackageEnv,
    ReadEventMessages,
    CreateSubmodel,
    CreateAssetAdministrationShell,
    CreateSubmodelElement,
    CreateSubmodelReference,
    UpdateSubmodelById,
    UpdateSubmodelElementByPath,
    UpdateAssetInformation,
    UpdateFileByPath,
    UpdateThumbnail,
    UpdateAssetAdministrationShellById,
    UpdateEventMessages,
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
