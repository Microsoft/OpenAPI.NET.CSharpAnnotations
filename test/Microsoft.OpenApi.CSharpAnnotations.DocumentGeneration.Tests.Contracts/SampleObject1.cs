// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts
{
    /// <summary>
    /// Sample contract for object 1
    /// </summary>
    public sealed class SampleObject1
    {
        /// <summary>
        /// Gets or sets the sample property bool
        /// </summary>
        [JsonProperty(PropertyName = "samplePropertyBool", Required = Required.Default)]
        public bool SamplePropertyBool { get; set; }

        /// <summary>
        /// Gets or sets the sample property int
        /// </summary>
        [JsonProperty(PropertyName = "samplePropertyStringInt", Required = Required.Default)]
        public int SamplePropertyInt { get; set; }

        /// <summary>
        /// Gets or sets the sample property string 1
        /// </summary>
        [JsonProperty(PropertyName = "samplePropertyString1", Required = Required.Default)]
        public string SamplePropertyString1 { get; set; }

        /// <summary>
        /// Gets or sets the sample property string 2
        /// </summary>
        [JsonProperty(PropertyName = "samplePropertyString2", Required = Required.Default)]
        public string SamplePropertyString2 { get; set; }

        /// <summary>
        /// Gets or sets the sample property string 3
        /// </summary>
        [JsonProperty(PropertyName = "samplePropertyString3", Required = Required.Always)]
        public string SamplePropertyString3 { get; set; }

        /// <summary>
        /// Gets or sets the sample property string 4
        /// </summary>
        [JsonProperty(PropertyName = "samplePropertyString4", Required = Required.Always)]
        public string SamplePropertyString4 { get; set; }

        /// <summary>
        /// Gets or sets the sample property enum
        /// </summary>
        [JsonProperty(PropertyName = "samplePropertyEnum", Required = Required.Always)]
        public SampleEnum1 SamplePropertyEnum { get; set; }

        /// <summary>
        /// Gets or sets the sample timespan
        /// </summary>
        [JsonProperty(PropertyName = "samplePropertyTimespan", Required = Required.Always)]
        public TimeSpan SamplePropertyTimespan { get; set; }
    }
}