/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;

namespace AasxIntegrationBase
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    
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
                              // Consider alternatives based on specific requirements

                              // Set other options as needed
                              WriteIndented = false // Example of setting indentation
                          };

            return options;
        }
    }
}
