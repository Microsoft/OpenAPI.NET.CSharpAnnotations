// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.FilterTests
{
    [Collection("DefaultSettings")]
    public class OperationHandlerGetUrlTests
    {
        private readonly ITestOutputHelper _output;

        public OperationHandlerGetUrlTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForGetUrlShouldReturnTheAbsolutePathUsingUrlXmlTag))]
        public void GetUrlShouldReturnTheAbsolutePathUsingUrlXmlTag(
            string testName,
            XElement element,
            string expectedUrl)
        {
            _output.WriteLine(testName);

            // Action
            var url = OperationHandler.GetUrl(element);

            // Assert
            url.Should().BeEquivalentTo(expectedUrl);
        }

        [Fact]
        public void GetUrlShouldThrowInvalidUrlExceptionByMissingUrlTag()
        {
            var element = XElement.Parse($@"
                <member>
                    <!--url>Missing tag!</url-->
                </member>
            ");

            // Action
            Action action = () => { OperationHandler.GetUrl(element); };

            // Assert
            action.Should().ThrowExactly<InvalidUrlException>();
        }


        [Fact]
        public void GetUrlShouldThrowInvalidUrlExceptionByWrongUrl()
        {
            var element = XElement.Parse($@"
                <member>
                    <url>This is not a valid url.</url>
                </member>
            ");

            // Action
            Action action = () => { OperationHandler.GetUrl(element); };

            // Assert
            action.Should().ThrowExactly<InvalidUrlException>();
        }

        public static IEnumerable<object[]> GetTestCasesForGetUrlShouldReturnTheAbsolutePathUsingUrlXmlTag()
        {
            yield return new object[]
            {
                "Simple url works",
                XElement.Parse(@"
                    <member>
                        <url>https://localhost/v2/role/{role}/assign</url>
                    </member>
                "),
                "/v2/role/{role}/assign"
            };

            yield return new object[]
            {
                "Complex encoded url works",
                XElement.Parse($@"
                    <member>
                        <url>{WebUtility.UrlEncode("https://localhost/v2/role/{role}/assign?p1=1&p2=2")}</url>
                    </member>
                "),
                "/v2/role/{role}/assign"
            };

            yield return new object[]
            {
                "The first url is considerd",
                XElement.Parse(@"
                    <member>
                        <url>https://localhost/v2/role/{role}/assign</url>
                        <url>https://localhost/this/url/should/not/be/considered</url>
                    </member>
                "),
                "/v2/role/{role}/assign"
            };
        }
    }
}