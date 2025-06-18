namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum DbRequestOp
{
    ReadPackageEnv,
    ReadPagedAssetAdministrationShells,
    ReadAssetAdministrationShellById,
    CreateAssetAdministrationShell,
    ReplaceAssetAdministrationShellById,
    DeleteAssetAdministrationShellById,

    CreateSubmodelReference,
    DeleteSubmodelReferenceById,

    ReadPagedSubmodels,
    ReadSubmodelById,
    CreateSubmodel,
    UpdateSubmodelById,
    ReplaceSubmodelById,
    DeleteSubmodelById,

    ReadPagedSubmodelElements,
    ReadSubmodelElementByPath,
    CreateSubmodelElement,
    UpdateSubmodelElementByPath,
    ReplaceSubmodelElementByPath,
    DeleteSubmodelElementByPath,

    ReadAssetInformation,
    ReplaceAssetInformation,

    ReadFileByPath,
    ReplaceFileByPath,
    DeleteFileByPath,

    ReadThumbnail,
    ReplaceThumbnail,
    DeleteThumbnail,

    ReadEventMessages,
    UpdateEventMessages,

    ReadPagedConceptDescriptions,
    ReadConceptDescriptionById,
    CreateConceptDescription,
    ReplaceConceptDescriptionById,
    DeleteConceptDescriptionById,

    GenerateSerializationByIds,

    QuerySearchSMs,
    QueryCountSMs,
    QuerySearchSMEs,
    QueryCountSMEs,

    QueryGetSMs,

    DeleteAASXByPackageId,
    ReadAASXByPackageId,
    ReadPagedAASXPackageIds,
    CreateAASXPackage,
    ReplaceAASXPackageById
}
