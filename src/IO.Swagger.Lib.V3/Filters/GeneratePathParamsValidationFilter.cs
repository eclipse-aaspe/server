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

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IO.Swagger.Filters
{
    /// <summary>
    /// Path Parameter Validation Rules Filter
    /// </summary>
    public class GeneratePathParamsValidationFilter : IOperationFilter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="operation">Operation</param>
        /// <param name="context">OperationFilterContext</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var pars = context.ApiDescription.ParameterDescriptions;

            foreach (var par in pars)
            {
                var swaggerParam = operation.Parameters.SingleOrDefault(p => p.Name == par.Name);

                var attributes = ((ControllerParameterDescriptor)par.ParameterDescriptor).ParameterInfo.CustomAttributes;

                if (attributes != null && attributes.Count() > 0 && swaggerParam != null)
                {
                    // Required - [Required]
                    var requiredAttr = attributes.FirstOrDefault(p => p.AttributeType == typeof(RequiredAttribute));
                    if (requiredAttr != null)
                    {
                        swaggerParam.Required = true;
                    }

                    // Regex Pattern [RegularExpression]
                    var regexAttr = attributes.FirstOrDefault(p => p.AttributeType == typeof(RegularExpressionAttribute));
                    if (regexAttr != null)
                    {
                        var regex = (string)regexAttr.ConstructorArguments[0].Value;
                        swaggerParam.Schema.Pattern = regex;
                    }

                    // String Length [StringLength]
                    int? minLenght = null, maxLength = null;
                    var stringLengthAttr = attributes.FirstOrDefault(p => p.AttributeType == typeof(StringLengthAttribute));
                    if (stringLengthAttr != null)
                    {
                        if (stringLengthAttr.NamedArguments.Count == 1)
                        {
                            minLenght = (int)(stringLengthAttr.NamedArguments.Single(p => p.MemberName == "MinimumLength").TypedValue.Value ?? 0);
                        }
                        maxLength = (int)(stringLengthAttr.ConstructorArguments[0].Value ?? 1);
                    }

                    var minLengthAttr = attributes.FirstOrDefault(p => p.AttributeType == typeof(MinLengthAttribute));
                    if (minLengthAttr != null)
                    {
                        minLenght = (int)(minLengthAttr.ConstructorArguments[0].Value ?? 0);
                    }

                    var maxLengthAttr = attributes.FirstOrDefault(p => p.AttributeType == typeof(MaxLengthAttribute));
                    if (maxLengthAttr != null)
                    {
                        maxLength = (int)(maxLengthAttr.ConstructorArguments[0].Value ?? 1);
                    }

                    if (swaggerParam is OpenApiParameter)
                    {
                        ((OpenApiParameter)swaggerParam).Schema.MinLength = minLenght;
                        ((OpenApiParameter)swaggerParam).Schema.MaxLength = maxLength;
                    }

                    // Range [Range]
                    var rangeAttr = attributes.FirstOrDefault(p => p.AttributeType == typeof(RangeAttribute));
                    if (rangeAttr != null)
                    {
                        var rangeMin = (int)(rangeAttr.ConstructorArguments[0].Value ?? 0);
                        var rangeMax = (int)(rangeAttr.ConstructorArguments[1].Value ?? 1);

                        swaggerParam.Schema.Minimum = rangeMin;
                        swaggerParam.Schema.Maximum = rangeMax;
                    }
                }
            }
        }
    }
}
