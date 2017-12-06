using FastMember;
using System;
using System.Collections.Generic;

namespace TinyBirdNet {

	public class TinyNetPropertyAccessor<T> {

		static Dictionary<Type, TypeAccessor> accessor = new Dictionary<Type, TypeAccessor>();

		string propName;
		T previousValue;
		Type objType;

		public TinyNetPropertyAccessor(object obj, string newPropName) {
			objType = obj.GetType();

			if (!accessor.ContainsKey(objType)) {
				accessor[objType] = TypeAccessor.Create(objType, true);
			}
			
			propName = newPropName;
		}

		public T Get(object obj) {
			return (T)accessor[objType][obj, propName];
		}

		public void Set(object obj, T value) {
			accessor[objType][obj, propName] = value;
		}

		public bool CheckIfChangedAndUpdate(object obj) {
			T current = (T)accessor[objType][obj, propName];

			if ((current == null && previousValue == null) || current.Equals(previousValue)) {
				previousValue = current;
				return true;
			}

			return false;
		}

		public bool WasChanged(object obj) {
			return accessor[objType][obj, propName].Equals(previousValue);
		}

		public void UpdateValue(object obj) {
			previousValue = (T)accessor[objType][obj, propName];
		}
	}
}
