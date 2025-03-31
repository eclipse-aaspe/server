namespace AasRegistryDiscovery.WebApi.Services.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AasRegistryDiscovery.WebApi.Exceptions;
using AasRegistryDiscovery.WebApi.Exceptions;
using AasRegistryDiscovery.WebApi.Interfaces;
using AasRegistryDiscovery.WebApi.Models;
using AasRegistryDiscovery.WebApi.Persistence.InMemory;
using Microsoft.Extensions.Logging;

public class InMemoryPersistenceService : IPersistenceService
{
    private readonly ILogger<InMemoryPersistenceService> _logger;
    private readonly IDescriptorPaginationService _paginationService;

    public InMemoryPersistenceService(
        ILogger<InMemoryPersistenceService> logger,
        IDescriptorPaginationService paginationService)
    {
        _logger = logger;
        _paginationService = paginationService;
    }

    public AssetAdministrationShellDescriptor CreateAssetAdminitrationShellDescriptor(AssetAdministrationShellDescriptor newAasDescriptor)
    {
        PersistenceInMemory.AddAasDescriptor(newAasDescriptor);
        _logger.LogInformation($"Added new AasDescriptor with Id {newAasDescriptor.Id}");
        return newAasDescriptor;
    }

    public SubmodelDescriptor CreateSubmodelDescriptorWithinAasDescriptor(string aasIdentifier, SubmodelDescriptor smDescriptor)
    {
        var aasDescriptor = GetAssetAdministrationShellDescriptorById(aasIdentifier);

        bool isSmDescPresent = false;
        if (aasDescriptor.SubmodelDescriptors != null)
        {
            isSmDescPresent = aasDescriptor.SubmodelDescriptors.Exists(s => s.Id.Equals(smDescriptor.Id)); 
        }

        if(isSmDescPresent)
        {
            throw new DuplicateResourceException($"SubmodelDescriptor with {smDescriptor.Id} already present in AasDescriptor with id {aasIdentifier}.");
        }

        aasDescriptor.SubmodelDescriptors ??= [];
        aasDescriptor.SubmodelDescriptors.Add(smDescriptor);

        return smDescriptor;
    }

    public void DeleteAssetAdministrationShellDescriptorById(string aasIdentifier)
    {
        var aasDescriptor = GetAssetAdministrationShellDescriptorById(aasIdentifier);
        PersistenceInMemory.RemoveAasDescriptor(aasDescriptor);
    }

    public void DeleteSmDescriptorWithinAasDescriptorById(string aasIdentifier, string smIdentifier)
    {
        var submodelDescriptor = GetSubmodelDescriptorWithinAasDescriptor(aasIdentifier, smIdentifier, out AssetAdministrationShellDescriptor aasDescriptor);
        if(submodelDescriptor != null)
        {
            aasDescriptor.SubmodelDescriptors?.Remove(submodelDescriptor);
        }
    }

    public AssetAdministrationShellDescriptor GetAssetAdministrationShellDescriptorById(string aasIdentifier)
    {
        AssetAdministrationShellDescriptor output = null;
        if (PersistenceInMemory.AssetAdministrationShellDescriptors != null)
        {
            output = PersistenceInMemory.AssetAdministrationShellDescriptors.Find(a => a.Id.Equals(aasIdentifier)); 
        }
        if (output == null)
        {
            throw new NotFoundException($"AasDescriptor with aasIdentifier {aasIdentifier} NOT found.");
        }

        return output;
    }

    public SubmodelDescriptor GetSubmodelDescriptorWithinAasDescriptor(string aasIdentifier, string smIdentifier)
    {
        SubmodelDescriptor output = GetSubmodelDescriptorWithinAasDescriptor(aasIdentifier, smIdentifier, out _);

        return output;
    }

    private SubmodelDescriptor GetSubmodelDescriptorWithinAasDescriptor(string aasIdentifier, string smIdentifier, out AssetAdministrationShellDescriptor aasDescriptor)
    {
        SubmodelDescriptor output = null;
        aasDescriptor = GetAssetAdministrationShellDescriptorById(aasIdentifier);

        if(aasDescriptor.SubmodelDescriptors !=null)
        {
            output = aasDescriptor.SubmodelDescriptors.Find(s => s.Id.Equals(smIdentifier));
        }

        if(output == null)
        {
            aasDescriptor = null;
            throw new NotFoundException($"SubmodelDescriptor with id {smIdentifier} NOT found within AasDescriptor with id {aasIdentifier}.");
        }

        return output;
    }

    public void UpdateAssetAdministrationShellDescriptor(string aasIdentifier, AssetAdministrationShellDescriptor newAasDescriptor)
    {
        var aasDescriptor = GetAssetAdministrationShellDescriptorById(aasIdentifier);
        if(aasDescriptor != null)
        {
            PersistenceInMemory.RemoveAasDescriptor(aasDescriptor);
            PersistenceInMemory.AddAasDescriptor(newAasDescriptor);
        }
    }

    public void UpdateSmDescriptorWithinAasDescriptor(string aasIdentifier, string smIdentifier, SubmodelDescriptor newSmDescriptor)
    {
        var submodelDescriptor = GetSubmodelDescriptorWithinAasDescriptor(aasIdentifier, smIdentifier, out AssetAdministrationShellDescriptor aasDescriptor);
        if (submodelDescriptor != null)
        {
            aasDescriptor.SubmodelDescriptors?.Remove(submodelDescriptor);
            aasDescriptor.SubmodelDescriptors.Add(newSmDescriptor);
        }
    }

    public AasDescriptorPagedResult GetAllAsssetAdministrationShellDescriptors(int? limit, string? cursor, AssetKind? assetKind, string? assetType)
    {
        var output = PersistenceInMemory.AssetAdministrationShellDescriptors;
        if(output != null && output.Count != 0)
        {
            if(assetKind != null && assetKind.HasValue)
            {
                output = output.Where(a => a.AssetKind == assetKind).ToList();
            }

            if(!string.IsNullOrEmpty(assetType))
            {
                output = output.Where(a => !string.IsNullOrEmpty(a.AssetType) && a.AssetType.Equals(assetType)).ToList();
            }
        }

        if(output == null)
        {
            output = new List<AssetAdministrationShellDescriptor>();
        }

        var finalOutput = _paginationService.GetPaginatedList(output, new PaginationParameters(cursor, limit));

        return finalOutput;
    }

    public SubmodelDescriptorPagedResult GetAllSmDescriptorsWithinAasDescriptor(string aasIdentifier, int? limit, string? cursor)
    {
        List<SubmodelDescriptor> output = new();
        var aasDescriptor = GetAssetAdministrationShellDescriptorById(aasIdentifier);
        if(aasDescriptor != null && aasDescriptor.SubmodelDescriptors != null)
        {
            output = aasDescriptor.SubmodelDescriptors;
        }

        var finalOutput = _paginationService.GetPaginatedList(output, new PaginationParameters(cursor, limit));
        return finalOutput;
    }
}
