
using System.Collections.Generic;

namespace AasxServerStandardBib.Interfaces
{
    public interface IConceptDescriptionService
    {
        IConceptDescription CreateConceptDescription(IConceptDescription body);
        void DeleteConceptDescriptionById(string cdIdentifier);
        List<IConceptDescription> GetAllConceptDescriptions(string idShort, Reference isCaseOf, Reference dataSpecificationRef);
        IConceptDescription GetConceptDescriptionById(string cdIdentifier);
        void UpdateConceptDescriptionById(ConceptDescription body, string cdIdentifier);
    }
}
