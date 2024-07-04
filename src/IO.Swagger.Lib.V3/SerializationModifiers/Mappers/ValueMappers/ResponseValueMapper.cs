/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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

using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

public class ResponseValueMapper
{
    private static ResponseValueTransformer Transformer = new ResponseValueTransformer();

    public static IValueDTO? Map(IClass source)
    {
        var transformed = Transformer.Transform(source);
        return transformed as IValueDTO;
    }
}