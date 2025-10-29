/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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

namespace Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using AdminShellNS.Models;
using Contracts.DbRequests;
using Contracts.LevelExtent;
using Contracts.Pagination;
using Contracts.QueryResult;
using Contracts.Security;

public interface IDbRequestHandlerService
{
    Task<DbRequestPackageEnvResult> ReadPackageEnv(string aasIdentifier, string submodelIdentifier);

    Task<List<IClass>> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort);
    Task<IAssetAdministrationShell> ReadAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier);
    Task<string> ReadAssetAdministrationShellByIdSigned(ISecurityConfig securityConfig, string aasIdentifier);
    Task<IAssetAdministrationShell> CreateAssetAdministrationShell(ISecurityConfig securityConfig, IAssetAdministrationShell body);
    Task<DbRequestResult> ReplaceAssetAdministrationShellById(ISecurityConfig security, string aasIdentifier, AssetAdministrationShell body);
    Task<DbRequestResult> ReplaceAssetAdministrationShellByIdSigned(ISecurityConfig security, string aasIdentifier, AssetAdministrationShell body, string jws);
    Task<DbRequestResult> DeleteAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier);
    Task<DbRequestResult> DeleteAssetAdministrationShellByIdSigned(ISecurityConfig securityConfig, string aasIdentifier);

    Task<IClass> CreateSubmodelReferenceInAAS(ISecurityConfig securityConfig, Reference body, string aasIdentifier);
    Task<DbRequestResult> DeleteSubmodelReferenceById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    Task<List<IClass>> ReadPagedSubmodels(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, Reference reqSemanticId, string idShort, LevelEnum? level, ExtentEnum? extent);
    Task<IClass> ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, LevelEnum? level, ExtentEnum? extent);
    Task<string> ReadSubmodelByIdSigned(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, LevelEnum? level, ExtentEnum? extent, bool isSkipPayload);
    Task<IClass> CreateSubmodel(ISecurityConfig securityConfig, ISubmodel newSubmodel, string aasIdentifier);
    Task<DbRequestResult> UpdateSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body);
    Task<DbRequestResult> ReplaceSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body);
    Task<DbRequestResult> ReplaceSubmodelByIdSigned(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body, string jws);
    Task<DbRequestResult> DeleteSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);
    Task<DbRequestResult> DeleteSubmodelByIdSigned(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    Task<List<IClass>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, LevelEnum? level, ExtentEnum? extent);
    Task<IClass> ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, LevelEnum? level, ExtentEnum? extent);
    Task<IClass> CreateSubmodelElement(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodelElement body, string idShortPath, bool first = true);
    Task<DbRequestResult> UpdateSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    Task<DbRequestResult> ReplaceSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    Task<DbRequestResult> DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);

    Task<IAssetInformation> ReadAssetInformation(ISecurityConfig securityConfig, string aasIdentifier);
    Task<DbRequestResult> ReplaceAssetInformation(ISecurityConfig securityConfig, string aasIdentifier, AssetInformation body);

    Task<DbFileRequestResult> ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
    Task<DbRequestResult> ReplaceFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream);
    Task<DbRequestResult> DeleteFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);

    Task<DbFileRequestResult> ReadThumbnail(ISecurityConfig securityConfig, string aasIdentifier);
    Task<DbRequestResult> ReplaceThumbnail(ISecurityConfig securityConfig, string aasIdentifier, string fileName, string contentType, MemoryStream stream);
    Task<DbRequestResult> DeleteThumbnail(ISecurityConfig securityConfig, string aasIdentifier);

    Task<List<Events.EventPayload>> ReadEventMessages(ISecurityConfig securityConfig, DbEventRequest dbEventRequest);
    Task<DbRequestResult> UpdateEventMessages(ISecurityConfig securityConfig, DbEventRequest dbEventRequest);

    Task<List<IClass>> ReadPagedConceptDescriptions(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string idShort = null, IReference isCaseOf = null, IReference dataSpecificationRef = null);
    Task<IClass> ReadConceptDescriptionById(ISecurityConfig securityConfig, string cdIdentifier);
    Task<string> ReadConceptDescriptionByIdSigned(ISecurityConfig securityConfig, string cdIdentifier);
    Task<IClass> CreateConceptDescription(ISecurityConfig securityConfig, IConceptDescription body);
    Task<DbRequestResult> ReplaceConceptDescriptionById(ISecurityConfig securityConfig, IConceptDescription body, string cdIdentifier);
    Task<DbRequestResult> ReplaceConceptDescriptionByIdSigned(ISecurityConfig securityConfig, string cdIdentifier, IConceptDescription body, string jws);
    Task<DbRequestResult> DeleteConceptDescriptionById(ISecurityConfig securityConfig, string cdIdentifier);
    Task<DbRequestResult> DeleteConceptDescriptionByIdSigned(ISecurityConfig securityConfig, string cdIdentifier);

    Task<DbRequestResult> GenerateSerializationByIds(ISecurityConfig securityConfig, List<string> aasIds = null, List<string> submodelIds = null, bool includeCD = false, bool createAASXPackage = false);

    Task<QResult> QuerySearchSMs(ISecurityConfig securityConfig, bool withTotalCount, bool withLastId, string semanticId, string identifier, string diff, IPaginationParameters paginationParameters, string expression);
    Task<int> QueryCountSMs(ISecurityConfig securityConfig, string semanticId, string identifier, string diff, IPaginationParameters paginationParameters, string expression);
    Task<QResult> QuerySearchSMEs(ISecurityConfig securityConfig, string requested, bool withTotalCount, bool withLastId, string smSemanticId, string smIdentifier, string semanticId, string diff,
        string contains, string equal, string lower, string upper, IPaginationParameters paginationParameters, string expression);
    Task<int> QueryCountSMEs(ISecurityConfig securityConfig, string smSemanticId, string smIdentifier, string semanticId, string diff, string contains, string equal, string lower, string upper,
        IPaginationParameters paginationParameters, string expression);
    Task<List<object>> QueryGetSMs(ISecurityConfig securityConfig, IPaginationParameters paginationParameters, string resultType, string expression);

    Task<DbRequestResult> DeleteAASXByPackageId(ISecurityConfig securityConfig, string packageId);
    Task<DbFileRequestResult> ReadAASXByPackageId(ISecurityConfig securityConfig, string packageId);
    Task<List<PackageDescription>> ReadPagedAASXPackageIds(ISecurityConfig securityConfig, IPaginationParameters paginationParameters, string aadId);
    Task<PackageDescription> CreateAASXPackage(ISecurityConfig securityConfig, MemoryStream stream, string fileName);
    Task<DbRequestResult> UpdateAASXPackageById(ISecurityConfig securityConfig, string packageId, MemoryStream stream, string fileName);
}

