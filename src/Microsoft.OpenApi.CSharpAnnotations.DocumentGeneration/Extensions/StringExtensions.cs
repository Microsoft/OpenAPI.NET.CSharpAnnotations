// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly Regex AllNonCompliantCharactersRegex = new Regex(@"[^a-zA-Z0-9\.\-_]");
        private static readonly Regex GenericMarkersRegex = new Regex(@"`[0-9]+");

        /// <summary>
        /// Determines whether the string contains the given substring using the specified StringComparison.
        /// </summary>
        /// <param name="value">The full string.</param>
        /// <param name="substring">The substring to check.</param>
        /// <param name="stringComparison">Stirng comparison.</param>
        /// <returns>Whether the string contains the given substring using the specified StringComparison.</returns>
        public static bool Contains(this string value, string substring, StringComparison stringComparison)
        {
            return value.IndexOf(substring, stringComparison) >= 0;
        }

        /// <summary>
        /// Gets the field name from the "cref" value.
        /// e.g. if the value is
        /// F:Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.Examples.SampleObject1Example,
        /// this will return SampleObject1Example.
        /// </summary>
        /// <param name="value">The cref value.</param>
        /// <returns>The type name.</returns>
        public static string ExtractFieldNameFromCref(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var field = Regex.IsMatch(value, "^F:") ? value.Split(':')[1] : value;

            return field.Split('.').Last();
        }

        /// <summary>
        /// Gets the type name from the "cref" value.
        /// </summary>
        /// <param name="value">The cref value.</param>
        /// <returns>The type name.</returns>
        public static string ExtractTypeNameFromCref(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return Regex.IsMatch(value, "^T:") ? value.Split(':')[1] : value;
        }

        /// <summary>
        /// Gets the type name name from the field "cref" value.
        /// e.g. if the value is
        /// F:Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.Examples.SampleObject1Example,
        /// this will return Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.Examples.
        /// </summary>
        /// <param name="value">The cref value.</param>
        /// <returns>The type name.</returns>
        public static string ExtractTypeNameFromFieldCref(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var field = Regex.IsMatch(value, "^F:") ? value.Split(':')[1] : value;

            return field.Substring(0, field.LastIndexOf('.'));
        }

        /// <summary>
        /// Removes blank lines.
        /// </summary>
        /// <param name="value">The original string.</param>
        /// <returns>The updated string.</returns>
        public static string RemoveBlankLines(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return Regex.Replace(
                    value,
                    @"^\s*$(\r\n|\r|\n)",
                    string.Empty,
                    RegexOptions.Multiline)
                .Trim();
        }

        /// <summary>
        /// Removes the duplicate string from the parameter name to work around the roslyn issue.
        /// https://github.com/dotnet/roslyn/issues/26292.
        /// </summary>
        /// <remarks>
        /// If a character is not allowed in an identifier name in .NET, roslyn duplicates the string following the
        /// first occurrence of that special character in the complied xml.
        /// e.g if the param name is service-Catalog-Id-1, roslyn will convert it to
        /// service-Catalog-Id-1-Catalog-Id-1 and this method will remove the duplicate string -Catalog-Id-1 and
        /// return service-Catalog-Id-1
        /// Also @ is allowed at the start of the identifier name, so roslyn duplicates the string following the
        /// second occurrence of that special character in the complied xml.
        /// e.g if the param name is @skip@skip, roslyn will convert it to
        /// @skip@skip@skip and this method will remove the duplicate string @skip and
        /// return @skip@skip
        /// </remarks>
        /// <param name="value">The param name to update.</param>
        /// <returns>The updated value.</returns>
        public static string RemoveRoslynDuplicateString(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string specialCharRegex = "[^a-zA-Z0-9_]";

            // Only alphanumeric or underscore characters are allowed in an identifier name.
            // So look for special character other than these.
            Match match = Regex.Match(value, specialCharRegex);

            if (!match.Success)
            {
                return value;
            }

            string firstOccurredSpecialCharacter = match.Value;

            var specialCharIndex = value.IndexOf(firstOccurredSpecialCharacter[0]);

            // @ is allowed at the start of the identifier name, so look for special character again in the substring
            // not including first char.
            // e.g. if a param name is @skip@skip, roslyn will compile it as @skip@skip@skip.
            if (firstOccurredSpecialCharacter == "@")
            {
                match = Regex.Match(value.Substring(1), specialCharRegex);

                if (!match.Success)
                {
                    return value;
                }

                specialCharIndex = value.IndexOf(match.Value[0], 1);
            }

            // Divide the string after special character into two halves and check if they are equal.
            // If equal take only first half of string. if not equal return value as is.
            var valueLengthStartingWithSpecialCharToEnd = value.Length - specialCharIndex;

            // If the length is odd then its a mismatch, return value as is.
            if (valueLengthStartingWithSpecialCharToEnd % 2 != 0)
            {
                return value;
            }

            var firstHalf = value.Substring(specialCharIndex, valueLengthStartingWithSpecialCharToEnd / 2);
            var secondHalf = value.Substring(specialCharIndex + valueLengthStartingWithSpecialCharToEnd / 2,
                valueLengthStartingWithSpecialCharToEnd / 2);

            return firstHalf == secondHalf ? value.Substring(0, specialCharIndex + firstHalf.Length)
                : value;
        }

        /// <summary>
        /// Sanitizes class name to satisfy the OpenAPI V3 restriction, i.e.
        /// match the regular expression ^[a-zA-Z0-9\.\-_]+$.
        /// </summary>
        /// <param name="value">The original class name string.</param>
        /// <returns>The sanitized class name.</returns>
        public static string SanitizeClassName(this string value)
        {
            // Replace + (used when this type has a parent class name) by .
            value = value.Replace(oldChar: '+', newChar: '.');

            // Remove `n from a generic type. It's clear that this is a generic type
            // since it will be followed by other types name(s).
            value = GenericMarkersRegex.Replace(value, string.Empty);

            // Replace , (used to separate multiple types used in a generic) by - 
            value = value.Replace(oldChar: ',', newChar: '-');

            // Replace all other non-compliant strings, including [ ] used in generics by _
            value = AllNonCompliantCharactersRegex.Replace(value, "_");

            return value;
        }

        /// <summary>
        /// Converts the first letter of the string to lowercase.
        /// </summary>
        /// <param name="value">The original string.</param>
        /// <returns>The updated string.</returns>
        public static string ToCamelCase(this string value)
        {
            if (value == null)
            {
                return null;
            }

            value = value.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Substring(startIndex: 0, length: 1).ToLowerInvariant() + value.Substring(1);
        }

        /// <summary>
        /// Converts the first letter of the string to uppercase.
        /// </summary>
        /// <param name="value">The original string.</param>
        /// <returns>The updated string.</returns>
        public static string ToTitleCase(this string value)
        {
            if (value == null)
            {
                return null;
            }

            value = value.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Substring(startIndex: 0, length: 1).ToUpperInvariant() + value.Substring(startIndex: 1);
        }
    }
}