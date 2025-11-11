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

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.Controller
{
    [ApiController]
    public class ImageController : ControllerBase
    {
        [HttpGet]
        [Route("/blazor/image/{id}")]
        public async Task<IActionResult> GetImage(string id)
        {
            id = Base64UrlEncoder.Decode(id);

            if (!id.StartsWith("$$"))
            {
                return NotFound();
            }

            var split = id.Split("$$");
            id = split[1];

            var bearer = "";
            var basicAuth = "";
            if (split.Length == 4)
            {
                switch (split[2])
                {
                    case "bearer":
                        bearer = split[3];
                        break;
                    case "basicauth":
                        basicAuth = split[3];
                        break;
                    default:
                        break;
                }
            }

            if (id.Contains("?bearerHead="))
            {
                split = id.Split("?bearerHead=");
                id = split[0];
                bearer = split[1];
            }

            var handler = new HttpClientHandler
            {
                DefaultProxyCredentials = CredentialCache.DefaultCredentials
            };

            var client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Get, id);

            if (bearer != "")
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }
            if (basicAuth != "")
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            var stream = await response.Content.ReadAsStreamAsync();

            return new FileStreamResult(stream, contentType);
        }
    }
}
