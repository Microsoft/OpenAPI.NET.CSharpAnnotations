﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Microsoft.OpenApi.CSharpComment.Reader.Models
{
    /// <summary>
    /// Model representing the generation error for the operation.
    /// </summary>
    public class GenerationError
    {
        /// <summary>
        /// Default constructor. Required for deserialization.
        /// </summary>
        public GenerationError()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GenerationError"/> based on the other instance.
        /// </summary>
        public GenerationError(GenerationError other)
        {
            if (other == null)
            {
                return;
            }

            Message = other.Message;
            ExceptionType = other.ExceptionType;
        }

        /// <summary>
        /// The type name of the exception.
        /// </summary>
        public Type ExceptionType { get; set; }

        /// <summary>
        /// The message providing details on the generation.
        /// </summary>
        [JsonProperty]
        public string Message { get; set; }
    }
}