namespace AasRegistryDiscovery.WebApi.Services.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasRegistryDiscovery.WebApi.Exceptions;
using AasRegistryDiscovery.WebApi.Exceptions;
using AasRegistryDiscovery.WebApi.Interfaces;
using AasRegistryDiscovery.WebApi.Models;
using AasRegistryDiscovery.WebApi.Persistence.InMemory;
using Microsoft.Extensions.Logging;

public class InMemoryPersistenceService : IPersistenceService
{
    private readonly ILogger<InMemoryPersistenceService> _logger;

    public InMemoryPersistenceService(ILogger<InMemoryPersistenceService> logger)
    {
        _logger = logger;
    }

    public AssetAdministrationShellDescriptor CreateAssetAdminitrationShellDescriptor(AssetAdministrationShellDescriptor newAasDescriptor)
    {
        PersistenceInMemory.AddAasDescriptor(newAasDescriptor);
        _logger.LogInformation($"Added new AasDescriptor with Id {newAasDescriptor.Id}");
        return newAasDescriptor;
    }

    public SubmodelDescriptor CreateSubmodelDescriptorWithinAasDescriptor(string? aasIdentifier, SubmodelDescriptor smDescriptor)
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

    public SubmodelDescriptor GetSubmodelDescriptorWithinAasDescriptor(string? aasIdentifier, string? smIdentifier)
    {
        SubmodelDescriptor output = null;
        var aasDescriptor = GetAssetAdministrationShellDescriptorById(aasIdentifier);

        if(aasDescriptor.SubmodelDescriptors !=null)
        {
            output = aasDescriptor.SubmodelDescriptors.Find(s => s.Id.Equals(smIdentifier));
        }

        if(output == null)
        {
            throw new NotFoundException($"SubmodelDescriptor with id {smIdentifier} NOT found within AasDescriptor with id {aasIdentifier}.");
        }

        return output;
    }
}
