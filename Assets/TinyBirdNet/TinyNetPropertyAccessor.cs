using FastMember;
using System;

namespace TinyBirdNet {

	public class TinyNetPropertyAccessor<T> {

		TypeAccessor accessor;
		string propName;
		T previousValue;

		public TinyNetPropertyAccessor(string newPropName) {
			accessor = TypeAccessor.Create(typeof(T), true);
			propName = newPropName;
		}

		public T Get(object obj) {
			return (T)accessor[obj, propName];
		}

		public void Set(object obj, T value) {
			accessor[obj, propName] = value;
		}

		public bool CheckIfChangedAndUpdate(object obj) {
			T current = (T)accessor[obj, propName];

			if (current.Equals(previousValue)) {
				previousValue = current;
				return true;
			}

			return false;
		}

		public bool WasChanged(object obj) {
			return accessor[obj, propName].Equals(previousValue);
		}

		public void UpdateValue(object obj) {
			previousValue = (T)accessor[obj, propName];
		}
	}
}
