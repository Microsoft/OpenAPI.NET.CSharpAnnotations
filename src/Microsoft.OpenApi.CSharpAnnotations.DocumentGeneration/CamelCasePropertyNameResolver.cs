// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Reflection;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    /// <summary>
    /// Resolves property name by camel casing.
    /// </summary>
    [Serializable]
    public class CamelCasePropertyNameResolver : DefaultPropertyNameResolver, IPropertyNameResolver
    {
        /// <summary>
        /// Resolves the property name for the given property info.
        /// </summary>
        /// <param name="propertyInfo">The property info to resolve property name for.</param>
        /// <returns>The property info.</returns>
        public new string ResolvePropertyName(PropertyInfo propertyInfo)
        {
            return base.ResolvePropertyName(propertyInfo).ToCamelCase();
        }
    }
}