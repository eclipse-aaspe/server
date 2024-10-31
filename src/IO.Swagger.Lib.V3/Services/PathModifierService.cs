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

using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.SerializationModifiers.PathModifier;
using System.Collections.Generic;
using System.Linq;

namespace IO.Swagger.Lib.V3.Services
{
    public class PathModifierService : IPathModifierService
    {
        private static readonly PathTransformer _pathTransformer = new PathTransformer();

        /// <summary>
        /// Serialize an instance of the meta-model into a JSON object.
        /// </summary>
        public List<string> ToIdShortPath(IClass? that)
        {
            var context = new PathModifierContext();
            return _pathTransformer.Transform(that, context);
        }

        public List<string> ToIdShortPath(List<ISubmodel?> submodelList)
        {
            var output = new List<string>();

            foreach (var submodel in submodelList)
            {
                var context = new PathModifierContext();
                var path = _pathTransformer.Transform(submodel, context);
                output.AddRange(path);
            }

            return output;
        }

        public List<string> ToIdShortPath(List<ISubmodelElement?> submodelElementList)
        {
            //var output = new List<List<string>>();
            var output = new List<string>();
            foreach (var submodelElement in submodelElementList)
            {
                var context = new PathModifierContext(isGetAllSmes: true);
                var path = _pathTransformer.Transform(submodelElement, context);
                output.AddRange(path);
            }

            return output;
        }
    }
}
