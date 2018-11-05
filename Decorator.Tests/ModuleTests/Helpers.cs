﻿using FluentAssertions;

namespace Decorator.Tests.ModuleTests
{
	public static class Helpers
	{
		public static object[] GenerateAndCorrupt<T>(int pos)
			where T : IDecorable, new()
		{
			var item = new T();
			var result = Converter<T>.Serialize(item);

			if (result[pos].GetType() == typeof(int))
			{
				result[pos] = "__corrupt__";
			}
			else
			{
				result[pos] = 1030307;
			}

			return result;
		}

		public static int EndsOn<T>(object[] deserialize)
			where T : IDecorable, new()
		{
			int position = 0;

			Converter<T>.TryDeserialize(deserialize, ref position, out _)
				.Should().Be(false, "Ensure the data being modified is corrupt.\r\nIf this happens to sometimes pass, please revise the data corruptor.");

			return position;
		}

		public static int LengthOfDefault<T>()
			where T : IDecorable, new()
			=> Converter<T>.Serialize(new T()).Length;
	}
}