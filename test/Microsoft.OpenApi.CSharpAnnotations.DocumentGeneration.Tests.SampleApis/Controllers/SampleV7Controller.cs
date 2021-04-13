// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.Controllers
{
    /// <summary>
    /// Sample v7 controller.
    /// </summary>
    public class SampleV7Controller : ApiController
    {
        /// <summary>
        /// Sample get 1
        /// </summary>
        /// <group>Sample V7</group>
        /// <verb>GET</verb>
        /// <url>https://myapi.sample.com/V7/samples/{id}?queryString={queryString}</url>
        /// <param name="queryString" cref="string" in="query">Query param 1 with no media type</param>
        /// <param name="sampleObjectInQuery2" cref="SampleObject1" in="query" type="application/json">Query param as application/json content</param>
        /// <param name="id" cref="string" in="path">The object id</param>
        /// <response code="200"><see cref="SampleObject1"/>Sample object retrieved</response>
        /// <response code="400"><see cref="string"/>Bad request</response>
        [HttpGet]
        [Route("V7/samples/{id}")]
        public async Task<SampleObject1> SampleGet1(string id, string queryString = null) {
            throw new NotImplementedException();
        }
    }
}