using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal interface IValue
    {
        void ToJsonObject(Stream body);
    }
}
