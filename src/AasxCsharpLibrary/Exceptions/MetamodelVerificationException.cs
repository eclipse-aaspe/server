using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AasCore.Aas3_0_RC02.Reporting;

namespace AdminShellNS.Exceptions
{
    public class MetamodelVerificationException : Exception
    {
        public List<Error> ErrorList { get; }

        public MetamodelVerificationException(List<Error> errorList) : base($"The request body not conformant with the metamodel. Found {errorList.Count} errors !!")
        {
            ErrorList = errorList;
        }

        
    }
}
