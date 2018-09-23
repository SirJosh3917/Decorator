﻿using Decorator.Attributes;
using Decorator.Exceptions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Decorator {

	/// <summary>
	/// Deserializes any message to a type.
	/// </summary>
	/// <typeparam name="TClass">The type of the class.</typeparam>
	/// <autogeneratedoc />
	public static class Deserializer {
		private static HashcodeDictionary<Type, MessageDefinition> Definitions;
		private static FunctionWrapper _tryDeserialize;
		private static FunctionWrapper _tryDeserializeRepeatable;

		static Deserializer() {
			Definitions = new HashcodeDictionary<Type, MessageDefinition>();

			var methods = typeof(Deserializer)
							.GetMethods();

			var genericTType = typeof(object).MakeByRefType();
			var genericIEnumerableTType = typeof(IEnumerable<object>).MakeByRefType();

			// TODO: put the 'foreach' precursor stuff in a function, not the HandleParam stuff

			foreach (var method in methods)
				if (method.IsGenericMethodDefinition)
					foreach (var parameter in method.MakeGenericMethod(typeof(object))
										.GetParameters())
						HandleParam(genericTType, genericIEnumerableTType, method, parameter);

#if THIS_SHOULDNT_HAPPEN

			// exceptions should never *actually* throw

			if (_tryDeserialize == default)
				throw new ConstructorException("static ctor issue: can't find method correlating to TryDeserialize<T>(out T)");

			if (_tryDeserializeRepeatable == default)
				throw new ConstructorException("static ctor issue: can't find method correlating to TryDeserialize<T>(out IEnumerable<T>)");

#endif
		}

		private static void HandleParam(Type genericTType, Type genericIEnumerableTType, MethodInfo method, ParameterInfo parameter) {
			if (parameter.IsOut)
				if (parameter.ParameterType == genericTType)
					_tryDeserialize = new FunctionWrapper(method);
				else if (parameter.ParameterType == genericIEnumerableTType)
					_tryDeserializeRepeatable = new FunctionWrapper(method);
		}


		/// <summary>
		/// Attempts to deserialize a <paramref name="m"/> to a <typeparamref name="TItem"/>, and returns whether or not it can.
		/// </summary>
		/// <typeparam name="TItem">The type of the item.</typeparam>
		/// <param name="m">The message.</param>
		/// <param name="result">The result after deserialization</param>
		/// <returns><c>true</c> if it can deserialize it, <c>false</c> if it can't</returns>
		public static bool TryDeserializeItem<TItem>(BaseMessage m, out TItem result) {
			if (m is null) throw new ArgumentNullException(nameof(m));

			var def = GetDefinitionFor<TItem>();

			if (def is null) {
				result = default;
				return false;
			}

			// attrib checking
			if (!EnsureAttributesOn<TItem>(m, def)) {
				result = default;
				return false;
			}

			return TryDeserializeValues<TItem>(m, def, out result);
		}

		/// <summary>
		/// Attempts to deserialize the <paramref name="m"/> to a <typeparamref name="IEnumerable{TItem}"/>, and returns whether or not it can.
		/// </summary>
		/// <typeparam name="TItem">The type of the item.</typeparam>
		/// <param name="m">The message.</param>
		/// <param name="result">The result after deserialization</param>
		/// <returns><c>true</c> if it can deserialize it, <c>false</c> if it can't</returns>
		public static bool TryDeserializeItems<TItem>(BaseMessage m, out IEnumerable<TItem> result) {
			if (m is null) throw new ArgumentNullException(nameof(m));

			var def = GetDefinitionFor<TItem>();

			if (def is null) {
				result = default;
				return false;
			}

			// attrib checking
			if (!EnsureAttributesOn<TItem>(m, def) ||
				!def.Repeatable) {
				result = default;
				return false;
			}

			return TryDeserializeValues<TItem>(m, def, out result);
		}

#region reflectionified

		public static bool TryDeserializeItem(Type t, BaseMessage m, out object result) {
			if (t is null) throw new ArgumentNullException(nameof(t));
			if (m is null) throw new ArgumentNullException(nameof(m));

			var args = new object[] { m, null };

			var method = _tryDeserialize.GetMethodFor(t);

			if (!(bool)(method(null, args))) {
				result = default;
				return false;
			}

			result = args[1];

			return true;
		}

		public static bool TryDeserializeItems(Type t, BaseMessage m, out IEnumerable<object> result) {
			if (t is null) throw new ArgumentNullException(nameof(t));
			if (m is null) throw new ArgumentNullException(nameof(m));

			var args = new object[] { m, null };

			if (!(bool)(_tryDeserializeRepeatable.GetMethodFor(t)(null, args))) {
				result = default;
				return false;
			}

			result = (IEnumerable<object>)args[1];

			return true;
		}

#endregion reflectionified

		// TODO: message definitions go in their own class

		internal static MessageDefinition GetDefinitionFor<T>()
			=> GetDefinitionForType(typeof(T));

		internal static MessageDefinition GetDefinitionForType(Type type) {
			// cache
			if (Definitions.TryGetValue(type, out var res)) return res;

			// if it is a message
			if (!AttributeCache<MessageAttribute>.TryHasAttribute(type, out var msgAttrib)) {
				res = default;
				Definitions.TryAdd(type, res);
				return res;
			}

			var repeatable = AttributeCache<RepeatableAttribute>.TryHasAttribute(type, out var _);

			// store properties
			var props = type.GetProperties();

			var max = 0;
			var msgProps = new MessageProperty[props.Length];

			for (var j = 0; j < props.Length; j++) {
				var i = props[j];

				if (HandleItem(i, out var prop))
					msgProps[max++] = prop;
			}

			// resize the array if needed
			if (msgProps.Length != max) {
				var newMsgProps = new MessageProperty[max];
				Array.Copy(msgProps, 0, newMsgProps, 0, max);
				msgProps = newMsgProps;
			}

			var msgDef = new MessageDefinition(
					msgAttrib[0].Type,
					msgProps,
					repeatable
				);

			Definitions.TryAdd(type, msgDef);
			return msgDef;
		}


		private static bool TryDeserializeValues<T>(BaseMessage m, MessageDefinition def, out T result) {
			var max = 0;

			// prevent boxing calls
			var instance = InstanceOf<T>.Create();

			// array exists so we can set values at the end of the function, in order to gain more speed when handling
			// messages that don't deserialize properly.
			//var props = new MessageProperty[def.Properties.Length];

			foreach (var i in def.Properties) {
				if (PropertyQualifies(i, m))
					i.Set(instance, m.Arguments[i.IntPos]);
					//props[max++] = i;
				else if (i.State == TypeRequiredness.Required) {
					result = default;
					return false;
				}
			}
			/*
			for (var i = 0; i < max; i++) {
				// don't want to make the call to the array twice
				var j = props[i];

				j.Set(instance, m.Arguments[j.IntPos]);
			}
			*/
			result = instance;
			return true;
		}

		private static bool TryDeserializeValues<T>(BaseMessage m, MessageDefinition def, out IEnumerable<T> result) {
			var max = m.Count / def.IntMaxCount;

			var itms = new T[max];

			for (var i = 0; i < max; i++) {
				var messageItems = new object[def.IntMaxCount];

				Array.Copy(m.Arguments, i * def.IntMaxCount, messageItems, 0, def.IntMaxCount);

				if (!TryDeserializeValues<T>(new BasicMessage(null, messageItems), def, out T item)) {
					result = default;
					return false;
				}

				itms[i] = item;
			}

			result = itms;
			return true;
		}

		private static bool HandleItem(PropertyInfo i, out MessageProperty prop) {
			if (AttributeCache<PositionAttribute>.TryHasAttribute(i, out var posAttrib)) {
				var required = AttributeCache<RequiredAttribute>.TryHasAttribute(i, out var _);
				var optional = AttributeCache<OptionalAttribute>.TryHasAttribute(i, out var _);

				if (!required && !optional)
					required = true;
				else if (optional)
					required = false;

				prop = new MessageProperty(
						posAttrib[0].Position,
						required,
						i
					);
				return true;
			}

			prop = default;
			return false;
		}

		private static bool PropertyQualifies(MessageProperty prop, BaseMessage m) {
			if (m.Arguments.Length > prop.IntPos)
				return (prop.PropertyInfo.PropertyType == m.Arguments[prop.Position]?.GetType());

			return false;
		}

		private static bool EnsureAttributesOn<T>(BaseMessage m, MessageDefinition def)
			=> m.Type == def.Type;
	}
}