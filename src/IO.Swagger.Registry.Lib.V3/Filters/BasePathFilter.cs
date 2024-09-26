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

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Text.RegularExpressions;

namespace IO.Swagger.Registry.Lib.V3.Filters
{
    /// <summary>
    /// BasePath Document Filter sets BasePath property of Swagger and removes it from the individual URL paths
    /// </summary>
    public class BasePathFilter : IDocumentFilter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="basePath">BasePath to remove from Operations</param>
        public BasePathFilter(string basePath)
        {
            BasePath = basePath;
        }

        /// <summary>
        /// Gets the BasePath of the Swagger Doc
        /// </summary>
        /// <returns>The BasePath of the Swagger Doc</returns>
        public string BasePath { get; }

        /// <summary>
        /// Apply the filter
        /// </summary>
        /// <param name="swaggerDoc">OpenApiDocument</param>
        /// <param name="context">FilterContext</param>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Servers.Add(new OpenApiServer() {Url = BasePath});

            var pathsToModify = swaggerDoc.Paths.Where(p => p.Key.StartsWith(BasePath)).ToList();

            foreach (var path in pathsToModify)
            {
                if (path.Key.StartsWith(BasePath))
                {
                    string newKey = Regex.Replace(path.Key, $"^{BasePath}", string.Empty);
                    swaggerDoc.Paths.Remove(path.Key);
                    swaggerDoc.Paths.Add(newKey, path.Value);
                }
            }
        }
    }
}