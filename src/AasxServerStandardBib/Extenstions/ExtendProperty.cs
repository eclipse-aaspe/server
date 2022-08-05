using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendProperty
    {
        public static string ValueAsText(this Property property)
        {
            return "" + property.Value;
        }

        public static Property ConvertFromV10(this Property property, AasxCompatibilityModels.AdminShellV10.Property sourceProperty)
        {
            if (sourceProperty == null)
            {
                return null;
            }
            property.ValueType = (DataTypeDefXsd)Stringification.DataTypeDefXsdFromString("xs:" + sourceProperty.valueType);
            property.Value = sourceProperty.value;
            if (sourceProperty.valueId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceProperty.valueId.Keys)
                {
                    //keyList.Add(new Key(ExtensionsUtil.GetKeyTypeFromString(refKey.type), refKey.value));
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {sourceProperty.valueType} not found for property {property.IdShort}");
                    }
                }
                property.ValueId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            return property;
        }

        public static Property ConvertFromV20(this Property property, AasxCompatibilityModels.AdminShellV20.Property sourceProperty)
        {
            if (sourceProperty == null)
            {
                return null;
            }

            var propertyType = Stringification.DataTypeDefXsdFromString("xs:" + sourceProperty.valueType);
            if (propertyType != null)
            {
                property.ValueType = (DataTypeDefXsd)propertyType;
            }
            else
            {
                Console.WriteLine($"ValueType {sourceProperty.valueType} not found for property {property.IdShort}");
            }
            property.Value = sourceProperty.value;
            if (sourceProperty.valueId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceProperty.valueId.Keys)
                {
                    //keyList.Add(new Key(ExtensionsUtil.GetKeyTypeFromString(refKey.type), refKey.value));
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {sourceProperty.valueType} not found for property {property.IdShort}");
                    }
                }
                property.ValueId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            return property;
        }
    }
}
