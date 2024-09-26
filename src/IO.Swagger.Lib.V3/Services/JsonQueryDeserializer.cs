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


using AasxServerStandardBib.Logging;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.Interfaces;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IO.Swagger.Lib.V3.Services
{
    public class JsonQueryDeserializer : IJsonQueryDeserializer
    {
        private readonly IAppLogger<JsonQueryDeserializer> _logger;
        private readonly IBase64UrlDecoderService _decoderService;

        public JsonQueryDeserializer(IAppLogger<JsonQueryDeserializer> logger, IBase64UrlDecoderService decoderService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _decoderService = decoderService;
        }

        public Reference? DeserializeReference(string fieldName, string? referenceString)
        {
            Reference? output = null;
            try
            {
                if (!string.IsNullOrEmpty(referenceString))
                {
                    var decodedString = _decoderService.Decode(fieldName, referenceString);
                    var mStrm         = new MemoryStream(Encoding.UTF8.GetBytes(decodedString ?? string.Empty));
                    var node          = JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                    output = Jsonization.Deserialize.ReferenceFrom(node);
                }
            }
            catch (JsonException ex)
            {
                throw new JsonDeserializationException(fieldName, ex.Message);
            }

            return output;
        }
    }
}
