using UnityEngine;
using System.Collections;
using FastMember;
using System;

public class TinyNetPropertyAccessor<T> {

	TypeAccessor accessor;
	string propName;

	public TinyNetPropertyAccessor(Type type, string newPropName) {
		accessor = TypeAccessor.Create(type, true);
		propName = newPropName;
	}

	public T Get(object obj) {
		return (T)accessor[obj, propName];
	}

	public void Set(object obj, T value) {
		accessor[obj, propName] = value;
	}
}
