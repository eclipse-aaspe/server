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

using System.Collections.Generic;

namespace IO.Swagger.Models;

/// <summary>
/// The Description object enables servers to present their capabilities to the clients, in particular which profiles they implement. At least one defined profile is required. Additional, proprietary attributes might be included. Nevertheless, the server must not expect that a regular client understands them.
/// </summary>
public interface IServiceDescription
{
    /// <summary>
    /// Gets or Sets Profiles
    /// </summary>
    List<ServiceDescription.ServiceProfiles>? profiles { get; set; }

    /// <summary>
    /// Returns the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    string ToString();

    /// <summary>
    /// Returns the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    string ToJson();

    /// <summary>
    /// Returns true if objects are equal
    /// </summary>
    /// <param name="obj">Object to be compared</param>
    /// <returns>Boolean</returns>
    bool Equals(object? obj);

    /// <summary>
    /// Returns true if ServiceDescription instances are equal
    /// </summary>
    /// <param name="other">Instance of ServiceDescription to be compared</param>
    /// <returns>Boolean</returns>
    bool Equals(ServiceDescription? other);

    /// <summary>
    /// Gets the hash code
    /// </summary>
    /// <returns>Hash code</returns>
    int GetHashCode();
}