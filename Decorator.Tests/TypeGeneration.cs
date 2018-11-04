﻿using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Decorator.Tests
{
	public class TypeGeneration
	{
		public class FlattensNonDecorable : IDecorable
		{
			public FlattensNonDecorable() => NonDecorableValue = new NonDecorable();

			[Position(0), Flatten]
			public NonDecorable NonDecorableValue { get; set; }

			public class NonDecorable
			{
				[Position(0), Required]
				public int NoDecorableAttribute;
			}
		}

		[Fact]
		public void Throws_InvalidDeclarationException_When_AttemptingToFlattenAClassWithoutDecorableInterface()
		{
			((Action)(() =>
			{
				EnsureCompiled<FlattensNonDecorable>.Ensure();
			})).Should().ThrowExactly<InvalidDeclarationException>();
		}



		public class NoSpecificationOnPositionAttribute : IDecorable
		{
			[Position(0)]
			public string ReferenceType { get; set; }
		}

		[Fact]
		public void Throws_BrokenAttributePairingException_When_PositionAttributeLacksAnAttributeModifier()
		{
			((Action)(() =>
			{
				EnsureCompiled<NoSpecificationOnPositionAttribute>.Ensure();
			})).Should().ThrowExactly<BrokenAttributePairingException>();
		}



		public class DuplicatePositionAttributes : IDecorable
		{
			[Position(0), Required]
			public string ReferenceType { get; set; }

			[Position(0), Required]
			public int ValueType { get; set; }
		}

		[Fact]
		public void Throws_IrrationalAttributeValueException_When_DuplicatePositionsWithSameValue()
		{
			((Action)(() =>
			{
				EnsureCompiled<DuplicatePositionAttributes>.Ensure();
			})).Should().ThrowExactly<IrrationalAttributeValueException>();
		}



		public class NoDefaultConstructor : IDecorable
		{
			public NoDefaultConstructor(string s)
			{
				ReferenceType = s;
			}

			[Position(0), Required]
			public string ReferenceType { get; set; }
		}

		[Fact]
		public void Throws_NoDefaultConstructorException()
		{
			((Action)(() =>
			{
				EnsureCompiled<NoDefaultConstructor>.Ensure();
			})).Should().ThrowExactly<NoDefaultConstructorException>();
		}



		public class IrrationalPositionAttribute : IDecorable
		{
			public IrrationalPositionAttribute()
			{
			}

			[Position(int.MinValue), Required]
			public string ReferenceType { get; set; }
		}

		[Fact]
		public void Throws_IrrationalAttributeValueException_When_PositionIsLessThanOne()
		{
			((Action)(() =>
			{
				EnsureCompiled<IrrationalPositionAttribute>.Ensure();
			})).Should().ThrowExactly<IrrationalAttributeValueException>();
		}



		public class TooManyPairings : IDecorable
		{
			public TooManyPairings()
			{
			}

			[Position(0), Required, Optional, Flatten, Array, FlattenArray]
			public string ReferenceType { get; set; }
		}

		[Fact]
		public void Throws_IrrationalAttributeException_When_ThereAreMoreThenOneModifiersOnAMember()
		{
			((Action)(() =>
			{
				EnsureCompiled<TooManyPairings>.Ensure();
			})).Should().ThrowExactly<IrrationalAttributeException>();
		}

		

		public class PerfectlyFineClass : IDecorable
		{
			[Position(0), Required]
			public string ReferenceType { get; set; }

			[Position(1), Array]
			public int[] MyArray;

			[Position(3), Optional]
			public int Optional;
		}

		[Fact]
		public void Throws_Nothing_When_DefinedNormally()
		{
			((Action)(() =>
			{
				EnsureCompiled<PerfectlyFineClass>.Ensure();
			})).Should().NotThrow();
		}



		public class HasNoMembers : IDecorable
		{
		}

		public class AlsoHasNoMembers : HasNoMembers
		{
		}

		[Fact]
		public void Throws_Nothing_When_DefinedWithNothing()
		{
			((Action)(() =>
			{
				EnsureCompiled<AlsoHasNoMembers>.Ensure();
			})).Should().NotThrow();
		}
	}
}
