using System;
using System.Collections;
using System.Collections.Generic;

namespace TinyBirdNet.Utils {

	public class TinyNetCycleBuffer<T> : IEnumerable<T> {

		private readonly T[] _data;
		private int _start;
		private int _end;
		private int _count;
		private readonly int _capacity;

		// Since the start can be moved, we take it into account
		public T this[int frame] {
			get {
				return _data[(_start + frame) % _capacity];
			}
		}

		public int Count {
			get {
				return _count;
			}
		}

		public T First {
			get {
				return _data[_start];
			}
		}

		public T Last {
			get {
				return _data[(_start + _count - 1) % _capacity];
			}
		}

		public bool IsFull {
			get {
				return _count == _capacity;
			}
		}

		public TinyNetCycleBuffer(int count) {
			_data = new T[count];
			_capacity = count;

			Reset();
		}

		public void Add(T element) {
			if (_count == _capacity) {
				throw new IndexOutOfRangeException("TinyNetFrameBuffer:Add is already full.");
			}
			_data[_end] = element;
			_end = (_end + 1) % _capacity;
			_count++;
		}

		public void Reset() {
			_start = 0;
			_end = 0;
			_count = 0;
		}

		public void RemoveFromStart(int count) {
			if (count > _capacity || count > _count) {
				throw new OverflowException("TinyNetFrameBuffer::RemoveFromStart count is bigger than current array.");
			}
			// Instead of moving the whole collection, we simply alter the starting frame.
			_start = (_start + count) % _capacity;
			_count -= count;
		}

		public IEnumerator<T> GetEnumerator() {
			int counter = _start;

			while (counter != _end) {
				yield return _data[counter];
				counter = (counter + 1) % _capacity;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
