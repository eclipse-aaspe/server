/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace AasxIntegrationBase
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Newtonsoft.Json.Serialization;

    // see: https://stackoverflow.com/questions/11099466/
    // using-a-custom-type-discriminator-to-tell-json-net-which-type-of-a-class-hierarc

    /// <summary>
    /// This attribute indicates, that it should e.g. serialized in JSON.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class DisplayNameAttribute : System.Attribute
    {
        /// <summary>
        /// Name to show up in JSON.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Setting this parameter prevents the look-up direction from name to type.
        /// By this, two different types can feature the same DisplayName.
        /// </summary>
        public bool NoTypeLookup { get; set; }

        public DisplayNameAttribute(string displayName, bool noTypeLookup = false)
        {
            this.DisplayName = displayName;
            this.NoTypeLookup = noTypeLookup;
        }
    }

    /// <summary>
    /// Serialization Binder for AASX Options. Allows modified $typename via [DisplayNameAttribute]
    /// </summary>
    public class DisplayNameSerializationBinder : DefaultSerializationBinder
    {
        private Dictionary<string, Type> _nameToType;
        private Dictionary<Type, string> _typeToName;

        public DisplayNameSerializationBinder(Type[] startingTypes)
        {
            if (startingTypes == null)
                return;

            _nameToType = new Dictionary<string, Type>();
            _typeToName = new Dictionary<Type, string>();

            foreach (var startingType in startingTypes)
            {
                var customDisplayNameTypes =
                    // this.GetType()
                    startingType
                        .Assembly
                        //concat with references if desired
                        .GetTypes()
                        .Where(x => x
                            .GetCustomAttributes(false)
                            .Any(y => y is DisplayNameAttribute));

                foreach (var t in customDisplayNameTypes)
                {
                    var dn = t.GetCustomAttributes(false).OfType<DisplayNameAttribute>().First().DisplayName;
                    var ntu = t.GetCustomAttributes(false).OfType<DisplayNameAttribute>().First().NoTypeLookup;
                    // MIHO, 21-05-24: added existence check to make more buller proof
                    if (!ntu && !_nameToType.ContainsKey(dn))
                        _nameToType.Add(dn, t);
                    if (!_typeToName.ContainsKey(t))
                        _typeToName.Add(t, dn);
                }
            }
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (false == _typeToName.ContainsKey(serializedType))
            {
                base.BindToName(serializedType, out assemblyName, out typeName);
                return;
            }

            var name = _typeToName[serializedType];

            assemblyName = null;
            typeName = name;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            if (_nameToType.ContainsKey(typeName))
                return _nameToType[typeName];

            return base.BindToType(assemblyName, typeName);
        }
    }

    public static class AasxPluginOptionSerialization
    {
        public static JsonSerializerOptions GetDefaultJsonOptions(Type[] startingTypes)
        {
            var options = new JsonSerializerOptions
                          {
                              // Custom serialization binder if needed (not directly supported in System.Text.Json)
                              // SerializationBinder = new DisplayNameSerializationBinder(startingTypes),

                              // Ignore null values during serialization
                              DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

                              // Serialize reference loops (not directly supported in System.Text.Json)
                              // ReferenceHandler = ReferenceHandler.Serialize,

                              // Include type information for objects (similar to TypeNameHandling.Objects)
                              // Note: System.Text.Json does not directly support TypeNameHandling like Newtonsoft.Json
                              // Consider alternatives based on specific requirements

                              // Set other options as needed
                              WriteIndented = false // Example of setting indentation
                          };

            return options;
        }
    }
}
