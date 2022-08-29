using AasCore.Aas3_0_RC02;
using AasxServerStandardBib.Exceptions;
using IO.Swagger.V1RC03.Extensions;
using IO.Swagger.V1RC03.Logging;
using Org.BouncyCastle.Asn1.X509.Qualified;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Services
{
    public class InputModifierService : IInputModifierService
    {
        private readonly IAppLogger<InputModifierService> _logger;

        public InputModifierService(IAppLogger<InputModifierService> logger)
        {
            _logger = logger;
        }

        public void ValidateInputModifiers(Type objectType, string level = null, string content = null, string extent = null)
        {
            if (objectType == typeof(AssetAdministrationShell))
            {
                if (!string.IsNullOrEmpty(level))
                {
                    throw new InvalidOutputModifierException(level);
                }
                if (!string.IsNullOrEmpty(content) && !content.EqualsAny("normal"))
                {
                    throw new InvalidOutputModifierException(content);
                }
                if (!string.IsNullOrEmpty(extent))
                {
                    throw new InvalidOutputModifierException(extent);
                }
            }
            else if (objectType == typeof(Submodel))
            {
                if (!string.IsNullOrEmpty(level) && !level.EqualsAny("deep", "core"))
                {
                    throw new InvalidOutputModifierException(level);
                }
                if (!string.IsNullOrEmpty(content) && !content.EqualsAny("normal", "metadata", "value"))
                {
                    throw new InvalidOutputModifierException(content);
                }
                if (!string.IsNullOrEmpty(extent) && !extent.EqualsAny("withBlobValue", "withoutBlobValue"))
                {
                    throw new InvalidOutputModifierException(extent);
                }
            }
            else if (objectType == typeof(ISubmodelElement))
            {
                if (!string.IsNullOrEmpty(level) && !level.EqualsAny("deep", "core"))
                {
                    throw new InvalidOutputModifierException(level);
                }
                if (!string.IsNullOrEmpty(content) && !content.EqualsAny("normal", "metadata", "value"))
                {
                    throw new InvalidOutputModifierException(content);
                }
                if (!string.IsNullOrEmpty(extent) && !extent.EqualsAny("withBlobValue", "withoutBlobValue"))
                {
                    throw new InvalidOutputModifierException(extent);
                }
            }
        }
    }

}
