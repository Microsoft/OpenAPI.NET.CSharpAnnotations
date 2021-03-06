﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System;
using System.Runtime.Serialization;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions
{
    /// <summary>
    /// The exception that is recorded when the documentation contains operations with the same path and operation method.
    /// </summary>
    [Serializable]
    internal class DuplicateOperationException : DocumentationException
    {
        /// <summary>
        /// The default <see cref="DuplicateOperationException"/>.
        /// </summary>
        public DuplicateOperationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateOperationException"/>.
        /// </summary>
        public DuplicateOperationException(string path, string operationMethod, string documentVariantTitle = null)
            : base(string.IsNullOrWhiteSpace(documentVariantTitle)
                ? string.Format(SpecificationGenerationMessages.DuplicateOperation, operationMethod, path)
                : string.Format(SpecificationGenerationMessages.DuplicateOperationWithVariantInfo, operationMethod,
                    path, documentVariantTitle))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateOperationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected DuplicateOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}