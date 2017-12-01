using System;
using System.Collections.Generic;

namespace TinyBirdNet {
	/// <summary>
	/// When used on a compatible property type, it will send it's value to all clients if they are changed.
	/// <para>byte, sbyte, short, ushort, int, uint, long, ulong, float, double, bool, string.</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class TinyNetSyncVar : Attribute {

		public static HashSet<Type> allowedTypes = new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(bool), typeof(string) };
	}
}
