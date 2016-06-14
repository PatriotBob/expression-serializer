using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace ExpressionSerializer.Tests.Unit
{
    public class ExpressionSerializeTests
    {
        [Theory]
        [MemberData("SerializeData")]
        public async void Serialize(Expression<Func<Customer, bool>> lambda, string expectedResult)
        {
            var serializer = new ExpressionSerializer();
            var result = await serializer.Serialize(lambda);
            //Console.WriteLine(result);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [MemberData("DeserializeData")]
        public async void Deserialize(string json, Expression<Func<Customer, bool>> expectedLambda)
        {
            var serializer = new ExpressionSerializer();
            var result = await serializer.Deserialize<Customer, bool>(json);
            Assert.Equal(expectedLambda.ToString(), result.ToString());
        }

        public static IEnumerable<object[]> SerializeData
        {
            get
            {
                ValidateDataProperties();

                return Expressions.Select((e, i) => new object[] { e, Json[i] });
            }
        }


        public static IEnumerable<object[]> DeserializeData
        {
            get
            {
                ValidateDataProperties();

                return Expressions.Select((e, i) => new object[] { Json[i], e});
            }
        }
        private static void ValidateDataProperties()
        {
            if (Expressions.Count() != Json.Count())
                throw new InvalidOperationException("Expressions and Json collections should be the same length.");
        }

        private static IList<Expression<Func<Customer, bool>>> Expressions
        {
            get
            {
                return new Expression<Func<Customer, bool>>[] {
                    (a => false),
                    (a => a.FirstName == "Bob"),
                    (a => a.FirstName == "Bob" && a.LastName != "McUrist"),
                    (a => (a.FirstName == "Bob" && a.LastName != "McUrist") || a.LastName == "McUrist"),
                    (a => a.FirstName.Contains("Bob")),
                    (a => a.FirstName.Contains(a.LastName.Contains("Urist") ? "Bob" : "Jane")),
                    (a => a.Orders.Aggregate(0m, (t,o) => t+o.Total ) > 1000),
                    (a => a.Company.Name == "Place"),
                    (a => !(a.Orders.First().Total % 100 == 0))
                };
            }
        }

        private static IList<string> Json
        {
            get
            {
                return new [] {
                    "{\"=>\":[\"a\",false]}",
                    "{\"=>\":[\"a\",{\"==\":[{\".\":[\"a\",\"FirstName\"]},\"Bob\"]}]}",
                    "{\"=>\":[\"a\",{\"&&\":[{\"==\":[{\".\":[\"a\",\"FirstName\"]},\"Bob\"]},{\"!=\":[{\".\":[\"a\",\"LastName\"]},\"McUrist\"]}]}]}",
                    "{\"=>\":[\"a\",{\"||\":[{\"&&\":[{\"==\":[{\".\":[\"a\",\"FirstName\"]},\"Bob\"]},{\"!=\":[{\".\":[\"a\",\"LastName\"]},\"McUrist\"]}]},{\"==\":[{\".\":[\"a\",\"LastName\"]},\"McUrist\"]}]}]}",
                    "{\"=>\":[\"a\",{\"Contains\":[{\".\":[\"a\",\"FirstName\"]},\"Bob\"]}]}",
                    "{\"=>\":[\"a\",{\"Contains\":[{\".\":[\"a\",\"FirstName\"]},{\"?\":[{\"Contains\":[{\".\":[\"a\",\"LastName\"]},\"Urist\"]},\"Bob\",\"Jane\"]}]}]}",
                    "{\"=>\":[\"a\",{\">\":[{\"Aggregate\":[{\".\":[\"a\",\"Orders\"]},0.0,{\"=>\":[\"t\",\"o\",{\"+\":[\"t\",{\".\":[\"o\",\"Total\"]}]}]}]},1000.0]}]}",
                    "{\"=>\":[\"a\",{\"==\":[{\".\":[{\".\":[\"a\",\"Company\"]},\"Name\"]},\"Place\"]}]}",
                    "{\"=>\":[\"a\",{\"!\":[{\"==\":[{\"%\":[{\".\":[{\"First\":[{\".\":[\"a\",\"Orders\"]}]},\"Total\"]},100.0]},0.0]}]}]}"
                };
            }
        }
    }
}