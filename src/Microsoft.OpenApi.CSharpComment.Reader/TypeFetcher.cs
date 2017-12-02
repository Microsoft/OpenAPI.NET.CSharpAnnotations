﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.CSharpComment.Reader.Exceptions;
using Microsoft.OpenApi.CSharpComment.Reader.Extensions;

namespace Microsoft.OpenApi.CSharpComment.Reader
{
    /// <summary>
    /// Proxy class for fetching type information by loading assemblies into private AppDomain.
    /// </summary>
    public class TypeFetcher
    {
        private readonly List<string> _contractAssemblyPaths = new List<string>();

        /// <summary>
        /// Creates new instance of <see cref="TypeFetcher"/>.
        /// </summary>
        /// <param name="contractAssemblyPaths">The list of contract assembly paths.</param>
        public TypeFetcher(IEnumerable<string> contractAssemblyPaths)
        {
            foreach (var contractAssemblyPath in contractAssemblyPaths)
            {
                if (string.IsNullOrEmpty(contractAssemblyPath) || !File.Exists(contractAssemblyPath.Trim()))
                {
                    throw new ArgumentException(
                        FormattableString.Invariant($"Path \"{contractAssemblyPath}\" does not exist"));
                }

                var newFileName = CopyFileToPrivateBin(contractAssemblyPath.Trim());
                _contractAssemblyPaths.Add(newFileName);
            }
        }

        /// <summary>
        /// Copies the given assembly to the private bin directory.
        /// </summary>
        /// <param name="assemblyPath">The assembly to copy.</param>
        /// <returns>The new assembly path.</returns>
        private static string CopyFileToPrivateBin(string assemblyPath)
        {
            var domain = AppDomain.CurrentDomain;

            assemblyPath = assemblyPath.Trim();
            var fullPath = Path.GetFullPath(assemblyPath);

            var binPath = Path.Combine(domain.BaseDirectory, domain.SetupInformation.PrivateBinPath);
            Directory.CreateDirectory(binPath);

            var newFilePath = Path.Combine(binPath, Path.GetFileName(assemblyPath));

            // Manually copy file to location where CLR will be able to locate it.
            if (!File.Exists(newFilePath))
            {
                File.Copy(fullPath, newFilePath, true);
            }

            return newFilePath;
        }

        private Type CreateListType(string typeName)
        {
            var listType = typeof(IList<>);

            // Creates a list type, setting the provided type as the generic type.
            return listType.MakeGenericType(LoadType(typeName));
        }

        /// <summary>
        /// Handle generic types. These appear in xml annotations as follows:
        /// <![CDATA[
        ///    <see cref="T:Contracts.Generic`1"/>
        ///    <see cref="T:Contracts.CustomClass"/>
        ///    Equivalent to Generic<CustomClass>
        /// ]]>
        /// </summary>
        /// <param name="allTypeNames">The list of type names specified in the documentation.</param>
        /// <returns>The generic type.</returns>
        private Type ExtractGenericType(IList<string> allTypeNames)
        {
            var start = 0;
            return ExtractGenericTypeRecurse(allTypeNames, ref start);
        }

        /// <summary>
        /// This method should only be called by ExtractGenericType.
        /// </summary>
        /// <param name="allTypeNames">The list of type names specified in the documentation.</param>
        /// <param name="index">
        /// Reference to current index in allTypes. It is incremented in different layers of the recursive procedure.
        /// </param>
        /// <returns>The generic type.</returns>
        private Type ExtractGenericTypeRecurse(IList<string> allTypeNames, ref int index)
        {
            if (index >= allTypeNames.Count)
            {
                throw new UndocumentedGenericTypeException();
            }

            var currentTypeName = allTypeNames[index];
            var numberOfGenerics = ExtractNumberOfGenerics(currentTypeName);

            // A generic type was expected, but the documented type is a non-generic.
            // Generic types must be documented in order.
            if (numberOfGenerics == 0)
            {
                throw new UnorderedGenericTypeException();
            }

            var genericTypeArray = new Type[numberOfGenerics];

            // Iterate over documented generics, retrieving their respective types.
            for (var j = 0; j < numberOfGenerics; j++)
            {
                if (++index >= allTypeNames.Count)
                {
                    throw new UndocumentedGenericTypeException();
                }

                var currentGenericTypeName = allTypeNames[index];

                // If another generic is encountered, recurse. Otherwise, load the type.
                genericTypeArray[j] = IsGenericType(currentGenericTypeName)
                    ? ExtractGenericTypeRecurse(allTypeNames, ref index)
                    : LoadType(currentGenericTypeName);
            }

            var type = LoadType(currentTypeName);

            // Load type and set generic types
            return type.MakeGenericType(genericTypeArray);
        }

        /// <summary>
        /// Extracts the number of generics specified in the type name.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>The number of generics.</returns>
        private static int ExtractNumberOfGenerics(string typeName)
        {
            return IsGenericType(typeName)? int.Parse(typeName.Split('`')[1], CultureInfo.CurrentCulture): 0;
        }

        /// <summary>
        /// Gets the type from the cref value.
        /// </summary>
        /// <param name="crefValues">The list of cref values.</param>
        /// <returns>The type.</returns>
        public Type LoadTypeFromCrefValues(IList<string> crefValues)
        {
            if (!crefValues.Any())
            {
                return null;
            }

            string typeName;

            if (crefValues.First().Contains("[]"))
            {
                var crefValue = crefValues.First().Split('[')[0];

                typeName = crefValue.ExtractTypeNameFromCref();
                return CreateListType(typeName);
            }
            
            if (crefValues.Any(IsGenericType))
            {
                return ExtractGenericType(crefValues.Select(i => i.ExtractTypeNameFromCref()).ToList());
            }

            typeName = crefValues.First().ExtractTypeNameFromCref();
            return LoadType(typeName);
        }

        /// <summary>
        /// Loads the given type name from assemblies located in the given assembly path.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <exception cref="TypeLoadException">Thrown when type was not found.</exception>
        /// <returns>The type.</returns>
        public Type LoadType(string typeName)
        {
            Type contractType = null;

            // Load custom type from the given list of assemblies.
            foreach (var file in _contractAssemblyPaths)
            {
                var assembly = Assembly.LoadFrom(file);

                if (contractType == null)
                {
                    contractType = assembly.GetType(typeName);
                }
            }

            // Attempt to load type from friendly name, search through all loaded assemblies.
            if (contractType == null)
            {
                var potentialTypes = new List<Type>();

                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic);

                foreach (var loadedAssembly in loadedAssemblies)
                {
                    try
                    {
                        var potentialTypesInAssembly = loadedAssembly.GetTypes()
                            .Where(t => t.Name.Equals(typeName) || t.FullName.Equals(typeName));
                        potentialTypes.AddRange(potentialTypesInAssembly);
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        // HACK - Unable to load types from assembly due to missing dependencies.
                        // The types that are available, however, are included in the exception.
                        // Check to see if a potential type is specified
                        foreach (var loadedType in e.Types)
                        {
                            if (loadedType != null && (loadedType.Name.Equals(typeName)
                                || loadedType.FullName.Equals(typeName)))
                            {
                                potentialTypes.Add(loadedType);
                            }
                        }
                    }
                }

                if (potentialTypes.Count > 1)
                {
                    var errorMessage = FormattableString.Invariant($"Could not uniquely identify type: \"{typeName}\"") +
                        ", please use fully qualified namespace. The following types exist: ";

                    errorMessage += string.Join(" ", potentialTypes.Select(type => type.FullName));

                    throw new TypeLoadException(errorMessage);
                }

                contractType = potentialTypes.FirstOrDefault();
            }

            // Could not find the type.
            if (contractType == null)
            {
                var errorMessage = FormattableString.Invariant($"Type \"{typeName}\" could not be found. ") +
                    "Ensure that it exists in one of the following assemblies: ";

                errorMessage += string.Join(" ", _contractAssemblyPaths.Select(Path.GetFileName));

                throw new TypeLoadException(errorMessage);
            }

            return contractType;
        }

        private static bool IsGenericType(string typeName)
        {
            return typeName.Contains("`");
        }
    }
}