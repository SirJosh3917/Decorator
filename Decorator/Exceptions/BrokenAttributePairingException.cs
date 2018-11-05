﻿using System;

namespace Decorator
{
	public class BrokenAttributePairingException : IrrationalAttributeException
	{
		public BrokenAttributePairingException() : this(ExceptionManager.BrokenAttributePairingDefault)
		{
		}

		public BrokenAttributePairingException(string message) : this(message, null)
		{
		}

		public BrokenAttributePairingException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}