﻿using Decorator.Attributes;
using Decorator.Modules;

using FluentAssertions;

using System.Reflection;

using Xunit;

namespace Decorator.Tests
{
	public class DiscoverAttributeTests
	{
		// hey
		// is a test failing with PrivateStatic?
		// remove the 'readonly' text from it
		// do the same for the PrivateInstance while you're at it
		// thank you, come again

		public class DiscoverAttributeTestsBase
		{
			[Position(0), Required]
			public int PublicInstance;

			[Position(0), Required]
			private int PrivateInstance;

			[Position(0), Required]
			public static int PublicStatic;

			[Position(0), Required]
			private static int PrivateStatic;

			public int PrivateInstanceAccessor => PrivateInstance;
			public static int PrivateStaticAccessor => PrivateStatic;

			public void PreventReadonly()
			{
				PrivateInstance = 0;
				PrivateStatic = 0;
			}
		}

		private const int SUCCESS = 5;

		private static T GetInst<T>(bool il) where T : new()
		{
			if (!TestConverter<T>.TryDeserialize(il, new object[] { SUCCESS }, out var result))
			{
				throw new TestException(nameof(GetInst) + ", " + typeof(T));
			}

			return result;
		}

		[Discover(BindingFlags.Public | BindingFlags.Instance)]
		public class DiscoversPublicAndInstanceClass : DiscoverAttributeTestsBase { }

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void DiscoversPublicAndInstance(bool il)
			=> GetInst<DiscoversPublicAndInstanceClass>(il)
				.PublicInstance.Should().Be(SUCCESS);

		public class DiscoversDefaultClass : DiscoverAttributeTestsBase { }

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void DiscoversDefault(bool il)
			=> GetInst<DiscoversDefaultClass>(il)
				.PublicInstance.Should().Be(SUCCESS);

		[Discover(BindingFlags.NonPublic | BindingFlags.Instance)]
		public class DiscoversPrivateAndInstanceClass : DiscoverAttributeTestsBase { }

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void DiscoversPrivateAndInstance(bool il)
			=> GetInst<DiscoversPrivateAndInstanceClass>(il)
				.PrivateInstanceAccessor.Should().Be(SUCCESS);

		[Discover(BindingFlags.Public | BindingFlags.Static)]
		public class DiscoversPublicAndStaticClass : DiscoverAttributeTestsBase { }

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void DiscoversPublicAndStatic(bool il)
		{
			GetInst<DiscoversPublicAndStaticClass>(il);
			DiscoversPublicAndStaticClass.PublicStatic
				.Should().Be(SUCCESS);
		}

		[Discover(BindingFlags.NonPublic | BindingFlags.Static)]
		public class DiscoversPrivateAndStaticClass : DiscoverAttributeTestsBase { }

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void DiscoversPrivateAndStatic(bool il)
		{
			GetInst<DiscoversPrivateAndStaticClass>(il);
			DiscoversPrivateAndStaticClass.PrivateStaticAccessor
				.Should().Be(SUCCESS);
		}
	}
}