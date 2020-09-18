// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions
{
    /// <summary>
    /// The exception that is recorded when it is expected to have operationId XML tag 
    /// but it is missing or there are more than one tags.
    /// </summary>
    [Serializable]
    internal class InvalidOperationIdException : DocumentationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidOperationIdException"/>.
        /// </summary>
        /// <param name="message">Error message.</param>
        public InvalidOperationIdException(string message = "")
            : base(message)
        {
        }
    }
}