using System;
using UnityEngine;

/// <summary>
/// When used on a compatible property type, it will send it's value to all clients if they are changed.
/// <para>byte, sbyte, short, ushort, int, uint, long, ulong, float, double, bool, string.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TinyNetSyncVar : System.Attribute { }
