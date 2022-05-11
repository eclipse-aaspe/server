using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IO.Swagger.Extensions
{
    public static class JsonSerializerExtension
    {
        private static readonly ConditionalWeakTable<object, object> CwtUseSensitive = new ConditionalWeakTable<object, object>();

        public static JsonSerializer UseSensitive(this JsonSerializer options)
        {
            CwtUseSensitive.AddOrUpdate(options, null);
            return options;
        }

        public static bool HasSensitive(this JsonSerializer options) =>
            CwtUseSensitive.TryGetValue(options, out _);
    }
}
