using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static AdminShellNS.AdminShellV20;

namespace IO.Swagger.Helpers
{
    /// <summary>
    /// This class is responsible for the ValueOnly-Serialization
    /// </summary>
    public class ValueOnlyJsonConverter : JsonConverter
    {
        private readonly bool IsValueOnly;
        private object m_object;

        public ValueOnlyJsonConverter()
        {
        }

        public ValueOnlyJsonConverter(bool isValueOnly, object obj)
        {
            this.IsValueOnly = isValueOnly;
            m_object = obj;
        }



        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var exists = serializer.Converters.Any(e => e.GetType() == typeof(ValueOnlyJsonConverter) && ((ValueOnlyJsonConverter)e).IsValueOnly == true);
            if (exists)
            {
                if (m_object is Property property)
                {
                    Console.WriteLine($"############### {property.ToValueOnlySerialization()} ########################");
                    writer.WriteValue(property.ToValueOnlySerialization());
                }
            }
        }


    }
}
