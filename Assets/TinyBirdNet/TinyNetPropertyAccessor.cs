using FastMember;
using System;
using System.Collections.Generic;

namespace TinyBirdNet {

	/// <summary>
	/// Creates an acessor for a property, used for <see cref="TinyNetSyncVar"/>.
	/// </summary>
	/// <typeparam name="T">The <see cref="System.Type"/> of this property.</typeparam>
	public class TinyNetPropertyAccessor<T> {

		/// <summary>
		/// A dictionary of accessors for each object type.
		/// </summary>
		static Dictionary<Type, TypeAccessor> accessor = new Dictionary<Type, TypeAccessor>();

		/// <summary>
		/// The property name.
		/// </summary>
		string propName;
		/// <summary>
		/// The previous value of that property.
		/// </summary>
		T previousValue;
		/// <summary>
		/// The object type that owns this property.
		/// </summary>
		Type objType;

		/// <summary>
		/// Initializes a new instance of the <see cref="TinyNetPropertyAccessor{T}"/> class.
		/// </summary>
		/// <param name="obj">The object that owns the property.</param>
		/// <param name="newPropName">New name of the property.</param>
		public TinyNetPropertyAccessor(object obj, string newPropName) {
			objType = obj.GetType();

			if (!accessor.ContainsKey(objType)) {
				accessor[objType] = TypeAccessor.Create(objType, true);
			}
			
			propName = newPropName;
		}

		/// <summary>
		/// Gets the property value.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns>The property value.</returns>
		public T Get(object obj) {
			return (T)accessor[objType][obj, propName];
		}

		/// <summary>
		/// Sets the property value.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="value">The value.</param>
		public void Set(object obj, T value) {
			accessor[objType][obj, propName] = value;
		}

		/// <summary>
		/// Checks if the value has changed and then updates the <see cref="previousValue"/>.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns>
		///   <c>true</c> if this property value has changed since the last time it was checked; otherwise, <c>false</c>.
		/// </returns>
		public bool CheckIfChangedAndUpdate(object obj) {
			T current = (T)accessor[objType][obj, propName];

			if ((current == null && previousValue == null) || current.Equals(previousValue)) {
				return false;
			}

			previousValue = current;
			return true;
		}

		/// <summary>
		/// Checks if the property value has changed.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns>
		///   <c>true</c> if this property value has changed since the last time it was checked; otherwise, <c>false</c>.
		/// </returns>
		public bool WasChanged(object obj) {
			return accessor[objType][obj, propName].Equals(previousValue);
		}

		/// <summary>
		/// Updates the <see cref="previousValue"/>.
		/// </summary>
		/// <param name="obj">The object.</param>
		public void UpdateValue(object obj) {
			previousValue = (T)accessor[objType][obj, propName];
		}
	}
}
