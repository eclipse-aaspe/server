/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

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

