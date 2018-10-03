﻿using Xunit;

namespace Decorator.Tests
{
    public sealed class OptionalDeserializationTests
    {
        [Fact, Trait("Project", "Decorator.Tests")]
        [Trait("Category", nameof(OptionalDeserializationTests))]
        public void OptionalBehavior()
        {
            var bm = new BasicMessage("opt", "required", "should default to int value 0");
            Decorator.Deserializer.TryDeserializeItem<OptionalMsg>(bm, out var res);

            Assert.Equal("required", res.RequiredString);
            Assert.Equal(default, res.OptionalValue);
        }

        /*
         * THIS WAS DECIDED AGAINST
         * due to the fact that there are repeatable messages, sometimes omitting an optional at the end and sometimes not omitting it
         * could lead to heavy confusion.
         *
         * as such, it is decided against.
         *
        [Fact, Trait("Project", "Decorator.Tests")]
        [Trait("Category", nameof(OptionalDeserializationTests))]
        public void MessageCountDoesntMatterAtTheEnd() {
            var bm = new BasicMessage("opt", "required");
            var res = Deserializer.Deserialize<OptionalMsg>(bm);

            Assert.Equal("required", res.RequiredString);
            Assert.Equal(default, res.OptionalValue);
        }
        */
    }
}