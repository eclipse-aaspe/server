/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
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

namespace AasxServerStandardBib
{
    //
    // Attributes
    //

    /// <summary>
    /// This attribute indicates, that it should e.g. serialized in JSON.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class CountForHash : System.Attribute
    {
    }

    /// <summary>
    /// This attribute indicates, that evaluation shall not count following field or not dive into references.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class SkipForHash : System.Attribute
    {
    }

    /// <summary>
    /// This attribute indicates, that the field / property is searchable
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class MetaModelName : System.Attribute
    {
        public string name;
        public MetaModelName(string name)
        {
            this.name = name;
        }
    }

    /// <summary>
    /// This attribute indicates, that the field / property shall be skipped for reflection
    /// in order to avoid cycles
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class SkipForReflection : System.Attribute
    {
    }

    /// <summary>
    /// This attribute indicates, that the field / property shall be skipped for searching, because it is not
    /// directly displayed in Package Explorer
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class SkipForSearch : System.Attribute
    {
    }

    /// <summary>
    /// This attribute indicates, that the field / property is searchable
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class TextSearchable : System.Attribute
    {
    }
}
