﻿using Decorator.Attributes;
using Decorator.Exceptions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Decorator {

	public class FunctionWrapper {
		public FunctionWrapper(MethodInfo method) {
			this.Method = method;
			this._versions = new ConcurrentDictionary<Type, ILFunc>();
		}

		public MethodInfo Method { get; }

		private ConcurrentDictionary<Type, ILFunc> _versions { get; }

		public ILFunc GetMethodFor(Type type) {
			if (this._versions.TryGetValue(type, out var res)) return res;

			var genMethod = this.Method
								.MakeGenericMethod(type);

			var method = IL.ILWrapRefSupport(
							genMethod
						);

			_versions.TryAdd(type, method);

			return method;
		}
	}

	/// <summary>Manages message types and also a good chunk of the internal deserialization code.</summary>
	public class DeserializationGrunt {
		public ConcurrentDictionary<Type, MessageDefinition> Definitions { get; } = new ConcurrentDictionary<Type, MessageDefinition>();

		public bool TryDeserializeRepeatable<T>(BaseMessage m, out IEnumerable<T> result) {
			if (m == default) throw new ArgumentNullException(nameof(m));
			var args = m.Arguments ?? this._emptyObjArr;

			var def = this.GetDefinitionFor<T>();

			if(def == default) {
				result = default;
				return false;
			}

			// attrib checking
			if (!EnsureAttributesOn<T>(m, def) ||
				!def.Repeatable) {
				result = default;
				return false;
			}

			return TryDeserializeRepeatableValues<T>(m, def, out result);
		}

		public bool TryDeserialize<T>(BaseMessage m, out T result) {
			// checking
			if (m == default) throw new ArgumentNullException(nameof(m));
			var args = m.Arguments ?? this._emptyObjArr;
			
			var def = this.GetDefinitionFor<T>();

			if(def == default) {
				result = default;
				return false;
			}

			// attrib checking
			if (!EnsureAttributesOn<T>(m, def)) {
				result = default;
				return false;
			}

			return TryDeserializeValues<T>(m, def, out result);
		}

		/// <summary>Gets the definition for <typeparamref name="T"/>.</summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <autogeneratedoc />
		public MessageDefinition GetDefinitionFor<T>() {
			var type = typeof(T);

			// cache
			if (this.Definitions.TryGetValue(type, out var res)) return res;

			// if it is a message
			if(!AttributeCache<MessageAttribute>.HasAttribute(type, out var msgAttrib)) {
				res = default;
				this.Definitions.TryAdd(type, res);
				return res;
			}
			
			var repeatable = AttributeCache<RepeatableAttribute>.HasAttribute(type, out var _);

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

			this.Definitions.TryAdd(type, msgDef);
			return msgDef;
		}

		#region reflectionified
		private static FunctionWrapper _tryDeserialize = new FunctionWrapper(
				typeof(DeserializationGrunt)
					.GetMethod(nameof(TryDeserialize))
			);

		public bool TryDeserializeType(Type t, BaseMessage m, out object result) {
			var args = new object[] { m, null };

			var method = _tryDeserialize.GetMethodFor(t);

			var pass = (bool)(method(this, args));

			if(!pass) {
				result = default;
				return false;
			}

			result = args[1];

			return true;
		}

		private static FunctionWrapper _tryDeserializeRepeatable = new FunctionWrapper(
				typeof(DeserializationGrunt)
					.GetMethod(nameof(TryDeserializeRepeatable))
			);
		
		public bool TryDeserializeRepeatableType(Type t, BaseMessage m, out IEnumerable<object> result) {
			var args = new object[] { m, null };

			var pass = (bool)(_tryDeserializeRepeatable.GetMethodFor(t)(this, args));

			if(!pass) {
				result = default;
				return false;
			}

			result = (IEnumerable<object>)args[1];

			return true;
		}

		private static FunctionWrapper _getDefinitionFor = new FunctionWrapper(
				typeof(DeserializationGrunt)
					.GetMethod(nameof(GetDefinitionFor))
			);

		public MessageDefinition GetDefinitionForType(Type type) {
			return (MessageDefinition)(_getDefinitionFor.GetMethodFor(type)(this, default));
		}
		#endregion

		private bool TryDeserializeValues<T>(BaseMessage m, MessageDefinition def, out T result) {
			var max = 0;

			// array exists so we can set values at the end of the function, in order to gain more speed when handling
			// messages that don't deserialize properly.
			var props = new MessageProperty[def.Properties.Length];

			foreach (var i in def.Properties) {
				if (PropertyQualifies(i, m))
					props[max++] = i;
				else if (i.State == TypeRequiredness.Required) {
					result = default;
					return false;
				}
			}

			// prevent boxing calls
			var instance = (object)InstanceOf<T>.Create();

			for (var i = 0; i < max; i++) {
				// don't want to make the call to the array twice
				var j = props[i];

				j.Set(instance, m.Arguments[j.IntPos]);
			}

			result = (T)instance;
			return true;
		}

		private bool TryDeserializeRepeatableValues<T>(BaseMessage m, MessageDefinition def, out IEnumerable<T> result) {
			var max = m.Count / def.IntMaxCount;

			var itms = new T[max];

			for (var i = 0; i < max; i++) {
				
				var messageItems = new object[def.IntMaxCount];

				Array.Copy(m.Arguments, i * def.IntMaxCount, messageItems, 0, def.IntMaxCount);

				if(!TryDeserializeValues<T>(new BasicMessage(null, messageItems), def, out var item)) {
					result = default;
					return false;
				}

				itms[i] = item;
			}

			result = itms;
			return true;
		}

		private static bool HandleItem(PropertyInfo i, out MessageProperty prop) {
			if (AttributeCache<PositionAttribute>.HasAttribute(i, out var posAttrib)) {
				var required = AttributeCache<RequiredAttribute>.HasAttribute(i, out var _);
				var optional = AttributeCache<OptionalAttribute>.HasAttribute(i, out var _);

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
		
		private object[] _emptyObjArr = new object[] { };
		private bool EnsureAttributesOn<T>(BaseMessage m, MessageDefinition def)
			=> m.Type == def.Type;

		private static bool PropertyQualifies(MessageProperty prop, BaseMessage m) {
			if (m.Arguments.Length > prop.IntPos)
				return (prop.PropertyType == m.Arguments[prop.Position]?.GetType());

			return false;
		}
	}
}