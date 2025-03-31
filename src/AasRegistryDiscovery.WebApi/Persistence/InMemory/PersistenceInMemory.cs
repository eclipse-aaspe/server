namespace AasRegistryDiscovery.WebApi.Persistence.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasRegistryDiscovery.WebApi.Exceptions;
using AasRegistryDiscovery.WebApi.Models;

internal static class PersistenceInMemory
{
    internal static List<AssetAdministrationShellDescriptor> AssetAdministrationShellDescriptors { get; private set; }

    internal static List<SubmodelDescriptor> SubmodelDescriptors { get; private set; }

    internal static void AddAasDescriptor(AssetAdministrationShellDescriptor aasDescriptor)
    {
        AssetAdministrationShellDescriptors ??= new();
        if(IsAasDescriptorPresent(aasDescriptor))
        {
            throw new DuplicateResourceException($"AasDescriptor with {aasDescriptor.Id} already exists.");
        }
        AssetAdministrationShellDescriptors.Add(aasDescriptor);
    }

    private static bool IsAasDescriptorPresent(AssetAdministrationShellDescriptor aasDescriptor)
    {
        return AssetAdministrationShellDescriptors.Exists(a => a.Id.Equals(aasDescriptor.Id));
    }

    internal static void RemoveAasDescriptor(AssetAdministrationShellDescriptor aasDescriptor)
    {
        if (AssetAdministrationShellDescriptors != null)
        {
            AssetAdministrationShellDescriptors.Remove(aasDescriptor);
        }
    }

    internal static void AddSubmodelDescriptor(SubmodelDescriptor smDescriptor)
    {
        SubmodelDescriptors ??= new();
        SubmodelDescriptors.Add(smDescriptor);
    }

    internal static void RemoveSubmodelDescriptor(SubmodelDescriptor smDescriptor)
    {
        if (SubmodelDescriptors != null)
        {
            SubmodelDescriptors.Remove(smDescriptor);
        }
    }

}

