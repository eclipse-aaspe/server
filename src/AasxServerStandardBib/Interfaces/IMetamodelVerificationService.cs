using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Interfaces
{
    public interface IMetamodelVerificationService
    {
        void VerifyRequestBody(IClass body);
    }
}
