using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ExpressionSerializer
{
    public class ExpressionSerializer
    {
        public async Task<string> Serialize<TIn, TOut>(Expression<Func<TIn, TOut>> expression)
        {
            return await Task.Factory.StartNew(() => {
                var visitor = new Visitor();
                var resultExpression = (Expression<Func<object>>)visitor.Visit(expression);
                var result = resultExpression.Compile()();
                return JsonConvert.SerializeObject(result);
            });
        }

        public async Task<Expression<Func<TIn, TOut>>> Deserialize<TIn, TOut>(string json)
        {
            return await Task.Factory.StartNew(() => {
                var data = JsonConvert.DeserializeObject<Dictionary<string, IEnumerable<object>>>(json, new [] {new Converter()});
                var builder = new Builder<TIn, TOut>();
                return (Expression<Func<TIn, TOut>>)builder.Build(data);
            });
        }

        protected class Converter : CustomCreationConverter<Dictionary<string, IEnumerable<object>>>
        {
            public override Dictionary<string, IEnumerable<object>> Create(Type objectType)
            {
                return new Dictionary<string, IEnumerable<object>>();
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(object) || base.CanConvert(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.StartObject
                    || reader.TokenType == JsonToken.Null)
                    return base.ReadJson(reader, objectType, existingValue, serializer);

                return serializer.Deserialize(reader);
            }
        }
    }
}
