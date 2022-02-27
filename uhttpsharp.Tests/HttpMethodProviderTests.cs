using System;
using System.Collections.Generic;
using Shouldly;
using NUnit.Framework;

namespace uhttpsharp.Tests
{
    public class HttpMethodProviderTests
    {
        private static IHttpMethodProvider GetTarget()
        {
            return new HttpMethodProvider();
        }

        public static IEnumerable<object> Methods => Enum.GetNames(typeof(HttpMethods));

        [Test]
        [TestCase(HttpMethods.Connect)]
        [TestCase(HttpMethods.Delete)]
        [TestCase(HttpMethods.Get)]
        [TestCase(HttpMethods.Head)]
        [TestCase(HttpMethods.Options)]
        [TestCase(HttpMethods.Patch)]
        [TestCase(HttpMethods.Post)]
        [TestCase(HttpMethods.Put)]
        [TestCase(HttpMethods.Trace)]
        public void Should_Get_Right_Method(HttpMethods method)
        {
            // Arrange
            string methodName = Enum.GetName(typeof(HttpMethods), method);
            IHttpMethodProvider target = GetTarget();

            // Act
            HttpMethods actual = target.Provide(methodName);

            // Assert
            actual.ToString().ShouldBe(methodName);
        }
    }
}