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

namespace IO.Swagger.Lib.V3.Converters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ServiceProfileEnumValueConverter : JsonConverterFactory
{
    private readonly JsonStringEnumConverter baseConverter;
    private readonly JsonNamingPolicy? namingPolicy;
    public ServiceProfileEnumValueConverter()
    {
        this.baseConverter = new JsonStringEnumConverter();
    }

    public override bool CanConvert(Type typeToConvert) => baseConverter.CanConvert(typeToConvert);
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var query = from field in typeToConvert.GetFields(BindingFlags.Public | BindingFlags.Static)
                    let attr = field.GetCustomAttribute<EnumMemberAttribute>()
                    where attr != null && attr.Value != null
                    select (field.Name, attr.Value);
        var dictionary = query.ToDictionary(p => p.Item1, p => p.Item2);
        if (dictionary.Count > 0)
            return new JsonStringEnumConverter(new DictionaryLookupNamingPolicy(dictionary, namingPolicy)).CreateConverter(typeToConvert, options);
        else
            return baseConverter.CreateConverter(typeToConvert, options);
    }


}

public class JsonNamingPolicyDecorator : JsonNamingPolicy
{
    readonly JsonNamingPolicy? underlyingNamingPolicy;

    public JsonNamingPolicyDecorator(JsonNamingPolicy? underlyingNamingPolicy) => this.underlyingNamingPolicy = underlyingNamingPolicy;
    public override string ConvertName(string name) => underlyingNamingPolicy?.ConvertName(name) ?? name;
}

internal class DictionaryLookupNamingPolicy : JsonNamingPolicyDecorator
{
    readonly Dictionary<string, string> dictionary;

    public DictionaryLookupNamingPolicy(Dictionary<string, string> dictionary, JsonNamingPolicy? underlyingNamingPolicy) : base(underlyingNamingPolicy) => this.dictionary = dictionary ?? throw new ArgumentNullException();
    public override string ConvertName(string name) => dictionary.TryGetValue(name, out var value) ? value : base.ConvertName(name);
}
