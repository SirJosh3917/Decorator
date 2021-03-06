﻿using Decorator.Tests.Decorations;

using FluentAssertions;

using System.Reflection;

using Xunit;

namespace Decorator.Tests.TestRequired
{
	public class RequireAttributeTests
	{
		public const string TestString = "Test_QRAF(E*H_";
		public const int TestInt = 8172643;
		public const int ExpectedSize = 1;

		public class SampleClass
		{
			[Required] public string PropertyString { get; set; }

			public static PropertyInfo GetPropertyStringInfo() => typeof(SampleClass).GetProperty(nameof(PropertyString));

			[Required] public int PropertyInt { get; set; }

			public static PropertyInfo GetPropertyIntInfo() => typeof(SampleClass).GetProperty(nameof(PropertyInt));

			[Required] public string FieldString;

			public static FieldInfo GetFieldStringInfo() => typeof(SampleClass).GetField(nameof(FieldString));

			[Required] public int FieldInt;

			public static FieldInfo GetFieldIntInfo() => typeof(SampleClass).GetField(nameof(FieldInt));
		}

		public static IMemberTest<PropertyInfo, RequiredAttribute> GetPropertyTests() => new DecorationFactoryPropertyTests<RequiredAttribute>();

		public static IMemberTest<FieldInfo, RequiredAttribute> GetFieldTests() => new DecorationFactoryFieldTests<RequiredAttribute>();

		public class PropertyTests
		{
			public class GetsType
			{
				[Fact]
				public void GetTypeString()
				{
					new RequiredAttribute()
						.GetType(SampleClass.GetPropertyStringInfo())
						.Should()
						.Be(typeof(string));
				}

				[Fact]
				public void GetTypeInt()
				{
					new RequiredAttribute()
						.GetType(SampleClass.GetPropertyIntInfo())
						.Should()
						.Be(typeof(int));
				}
			}

			public class String
			{
				private readonly BaseTest<SampleClass, PropertyInfo, RequiredAttribute, string> _tester;

				public String()
				{
					_tester = new BaseTest<SampleClass, PropertyInfo, RequiredAttribute, string>
					(
						value: TestString,
						tester: GetPropertyTests(),
						memberInfo: SampleClass.GetPropertyStringInfo(),
						getValue: (instance) => instance.PropertyString,
						setValue: (instance, value) => instance.PropertyString = value,
						expectedEstimateSize: ExpectedSize
					);
				}

				[Fact] public void Deserialize() => _tester.Deserialize();

				[Fact] public void Serialize() => _tester.Serialize();

				[Fact] public void EstimateSize() => _tester.EstimateSize();
			}

			public class Int
			{
				private readonly BaseTest<SampleClass, PropertyInfo, RequiredAttribute, int> _tester;

				public Int()
				{
					_tester = new BaseTest<SampleClass, PropertyInfo, RequiredAttribute, int>
					(
						value: TestInt,
						tester: GetPropertyTests(),
						memberInfo: SampleClass.GetPropertyIntInfo(),
						getValue: (instance) => instance.PropertyInt,
						setValue: (instance, value) => instance.PropertyInt = value,
						expectedEstimateSize: ExpectedSize
					);
				}

				[Fact] public void Deserialize() => _tester.Deserialize();

				[Fact] public void Serialize() => _tester.Serialize();

				[Fact] public void EstimateSize() => _tester.EstimateSize();
			}
		}

		public class FieldTests
		{
			public class GetsType
			{
				[Fact]
				public void GetTypeString()
				{
					new RequiredAttribute()
						.GetType(SampleClass.GetFieldStringInfo())
						.Should()
						.Be(typeof(string));
				}

				[Fact]
				public void GetTypeInt()
				{
					new RequiredAttribute()
						.GetType(SampleClass.GetFieldIntInfo())
						.Should()
						.Be(typeof(int));
				}
			}

			public class String
			{
				private readonly BaseTest<SampleClass, FieldInfo, RequiredAttribute, string> _tester;

				public String()
				{
					_tester = new BaseTest<SampleClass, FieldInfo, RequiredAttribute, string>
					(
						value: TestString,
						tester: GetFieldTests(),
						memberInfo: SampleClass.GetFieldStringInfo(),
						getValue: (instance) => instance.FieldString,
						setValue: (instance, value) => instance.FieldString = value,
						expectedEstimateSize: ExpectedSize
					);
				}

				[Fact] public void Deserialize() => _tester.Deserialize();

				[Fact] public void Serialize() => _tester.Serialize();

				[Fact] public void EstimateSize() => _tester.EstimateSize();
			}

			public class Int
			{
				private readonly BaseTest<SampleClass, FieldInfo, RequiredAttribute, int> _tester;

				public Int()
				{
					_tester = new BaseTest<SampleClass, FieldInfo, RequiredAttribute, int>
					(
						value: TestInt,
						tester: GetFieldTests(),
						memberInfo: SampleClass.GetFieldIntInfo(),
						getValue: (instance) => instance.FieldInt,
						setValue: (instance, value) => instance.FieldInt = value,
						expectedEstimateSize: ExpectedSize
					);
				}

				[Fact] public void Deserialize() => _tester.Deserialize();

				[Fact] public void Serialize() => _tester.Serialize();

				[Fact] public void EstimateSize() => _tester.EstimateSize();
			}
		}

		public static bool TryDeserializeProperty<T>(PropertyInfo propertyInfo, ref object[] array, object instance, ref int index)
			=> DeserializationTesting.Deserialize<T>(new RequiredAttribute(), propertyInfo, ref array, instance, ref index);

		public class MoreTests
		{
			public const int StartIndex = 1;

			public class String
			{
				[Theory]
				[InlineData(new object[] { new object[] { } })]
				[InlineData(new object[] { new object[] { 0, 1, 2, 3, 4 } })]
				public void DoesntDeserialize(object[] data)
				{
					var instance = new SampleClass();
					var i = StartIndex;
					TryDeserializeProperty<string>(SampleClass.GetPropertyStringInfo(), ref data, instance, ref i)
						.Should()
						.BeFalse();

					instance.PropertyString
						.Should()
						.Be(default);
				}

				[Theory]
				[InlineData(new object[] { new object[] { null, null, null, null } })]
				[InlineData(new object[] { new object[] { null, "ha", null, null } })]
				public void DoesDeserialize(object[] data)
				{
					var instance = new SampleClass();
					var i = StartIndex;
					TryDeserializeProperty<string>(SampleClass.GetPropertyStringInfo(), ref data, instance, ref i)
						.Should()
						.BeTrue();

					instance.PropertyString
						.Should()
						.Be((string)data[1]);
				}
			}

			public class Int
			{
				[Theory]
				[InlineData(new object[] { new object[] { } })]
				[InlineData(new object[] { new object[] { null, null, null, null } })]
				[InlineData(new object[] { new object[] { "0", "1", "2", "3" } })]
				public void DoesntDeserialize(object[] data)
				{
					var instance = new SampleClass();
					var i = StartIndex;
					TryDeserializeProperty<int>(SampleClass.GetPropertyIntInfo(), ref data, instance, ref i)
						.Should()
						.BeFalse();

					instance.PropertyInt
						.Should()
						.Be(default);
				}

				[Theory]
				[InlineData(new object[] { new object[] { null, 5211, null, null } })]
				public void DoesDeserialize(object[] data)
				{
					var instance = new SampleClass();
					var i = StartIndex;
					TryDeserializeProperty<int>(SampleClass.GetPropertyIntInfo(), ref data, instance, ref i)
						.Should()
						.BeTrue();

					instance.PropertyInt
						.Should()
						.Be((int)data[1]);
				}
			}
		}
	}
}