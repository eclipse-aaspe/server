using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IO.Swagger.Extensions
{
#pragma warning disable CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
    public static class JsonSerializerExtension
#pragma warning restore CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
    {
        private static readonly ConditionalWeakTable<object, object> CwtUseSensitive = new ConditionalWeakTable<object, object>();

#pragma warning disable CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
        public static JsonSerializer UseSensitive(this JsonSerializer options)
#pragma warning restore CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
        {
            CwtUseSensitive.AddOrUpdate(options, null);
            return options;
        }

#pragma warning disable CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
        public static bool HasSensitive(this JsonSerializer options) =>
#pragma warning restore CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
            CwtUseSensitive.TryGetValue(options, out _);
    }
}
