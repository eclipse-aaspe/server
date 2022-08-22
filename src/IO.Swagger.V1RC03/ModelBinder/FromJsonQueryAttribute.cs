using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.ModelBinder
{
    public class FromJsonQueryAttribute : ModelBinderAttribute
    {
        public FromJsonQueryAttribute()
        {
            BinderType = typeof(JsonQueryBinder);
        }
    }
}
