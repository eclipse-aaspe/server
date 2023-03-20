using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Interfaces
{
    public interface ISubmodelService
    {
        void DeleteSubmodelById(string submodelIdentifier);
        void DeleteSubmodelElementByPath(string submodelIdentifier, string idShortPath);
        List<ISubmodelElement> GetAllSubmodelElements(string submodelIdentifier);
    }
}
