using LiteNetLib.Utils;
using System;
using System.Text;

namespace TinyBirdNet.Utils {

	public class TinyNetStateData {

		public byte[] data = new byte[64];
		public int position;
		public int dataSize;

		public int frameTick;

		public bool EndOfData {
			get { return position == dataSize; }
		}

		public int AvailableBytes {
			get { return dataSize - position; }
		}

		#region GetMethods

		public byte GetByte() {
			byte res = data[position];
			position += 1;
			return res;
		}

		public sbyte GetSByte() {
			var b = (sbyte)data[position];
			position++;
			return b;
		}

		public bool[] GetBoolArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new bool[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetBool();
			}
			return arr;
		}

		public ushort[] GetUShortArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new ushort[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetUShort();
			}
			return arr;
		}

		public short[] GetShortArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new short[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetShort();
			}
			return arr;
		}

		public long[] GetLongArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new long[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetLong();
			}
			return arr;
		}

		public ulong[] GetULongArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new ulong[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetULong();
			}
			return arr;
		}

		public int[] GetIntArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new int[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetInt();
			}
			return arr;
		}

		public uint[] GetUIntArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new uint[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetUInt();
			}
			return arr;
		}

		public float[] GetFloatArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new float[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetFloat();
			}
			return arr;
		}

		public double[] GetDoubleArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new double[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetDouble();
			}
			return arr;
		}

		public string[] GetStringArray() {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new string[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetString();
			}
			return arr;
		}

		public string[] GetStringArray(int maxStringLength) {
			ushort size = BitConverter.ToUInt16(data, position);
			position += 2;
			var arr = new string[size];
			for (int i = 0; i < size; i++) {
				arr[i] = GetString(maxStringLength);
			}
			return arr;
		}

		public bool GetBool() {
			bool res = data[position] > 0;
			position += 1;
			return res;
		}

		public char GetChar() {
			char result = BitConverter.ToChar(data, position);
			position += 2;
			return result;
		}

		public ushort GetUShort() {
			ushort result = BitConverter.ToUInt16(data, position);
			position += 2;
			return result;
		}

		public short GetShort() {
			short result = BitConverter.ToInt16(data, position);
			position += 2;
			return result;
		}

		public long GetLong() {
			long result = BitConverter.ToInt64(data, position);
			position += 8;
			return result;
		}

		public ulong GetULong() {
			ulong result = BitConverter.ToUInt64(data, position);
			position += 8;
			return result;
		}

		public int GetInt() {
			int result = BitConverter.ToInt32(data, position);
			position += 4;
			return result;
		}

		public uint GetUInt() {
			uint result = BitConverter.ToUInt32(data, position);
			position += 4;
			return result;
		}

		public float GetFloat() {
			float result = BitConverter.ToSingle(data, position);
			position += 4;
			return result;
		}

		public double GetDouble() {
			double result = BitConverter.ToDouble(data, position);
			position += 8;
			return result;
		}

		public string GetString(int maxLength) {
			int bytesCount = GetInt();
			if (bytesCount <= 0 || bytesCount > maxLength * 2) {
				return string.Empty;
			}

			int charCount = Encoding.UTF8.GetCharCount(data, position, bytesCount);
			if (charCount > maxLength) {
				return string.Empty;
			}

			string result = Encoding.UTF8.GetString(data, position, bytesCount);
			position += bytesCount;
			return result;
		}

		public string GetString() {
			int bytesCount = GetInt();
			if (bytesCount <= 0) {
				return string.Empty;
			}

			string result = Encoding.UTF8.GetString(data, position, bytesCount);
			position += bytesCount;
			return result;
		}

		public byte[] GetRemainingBytes() {
			byte[] outgoingData = new byte[AvailableBytes];
			Buffer.BlockCopy(data, position, outgoingData, 0, AvailableBytes);
			position = data.Length;
			return outgoingData;
		}

		public void GetRemainingBytes(byte[] destination) {
			Buffer.BlockCopy(data, position, destination, 0, AvailableBytes);
			position = data.Length;
		}

		public void GetBytes(byte[] destination, int lenght) {
			Buffer.BlockCopy(data, position, destination, 0, lenght);
			position += lenght;
		}

		public byte[] GetBytesWithLength() {
			int length = GetInt();
			byte[] outgoingData = new byte[length];
			Buffer.BlockCopy(data, position, outgoingData, 0, length);
			position += length;
			return outgoingData;
		}
		#endregion

		#region PeekMethods

		public byte PeekByte() {
			return data[position];
		}

		public sbyte PeekSByte() {
			return (sbyte)data[position];
		}

		public bool PeekBool() {
			return data[position] > 0;
		}

		public char PeekChar() {
			return BitConverter.ToChar(data, position);
		}

		public ushort PeekUShort() {
			return BitConverter.ToUInt16(data, position);
		}

		public short PeekShort() {
			return BitConverter.ToInt16(data, position);
		}

		public long PeekLong() {
			return BitConverter.ToInt64(data, position);
		}

		public ulong PeekULong() {
			return BitConverter.ToUInt64(data, position);
		}

		public int PeekInt() {
			return BitConverter.ToInt32(data, position);
		}

		public uint PeekUInt() {
			return BitConverter.ToUInt32(data, position);
		}

		public float PeekFloat() {
			return BitConverter.ToSingle(data, position);
		}

		public double PeekDouble() {
			return BitConverter.ToDouble(data, position);
		}

		public string PeekString(int maxLength) {
			int bytesCount = BitConverter.ToInt32(data, position);
			if (bytesCount <= 0 || bytesCount > maxLength * 2) {
				return string.Empty;
			}

			int charCount = Encoding.UTF8.GetCharCount(data, position + 4, bytesCount);
			if (charCount > maxLength) {
				return string.Empty;
			}

			string result = Encoding.UTF8.GetString(data, position + 4, bytesCount);
			return result;
		}

		public string PeekString() {
			int bytesCount = BitConverter.ToInt32(data, position);
			if (bytesCount <= 0) {
				return string.Empty;
			}

			string result = Encoding.UTF8.GetString(data, position + 4, bytesCount);
			return result;
		}
		#endregion

		public void Clear() {
			position = 0;
			dataSize = 0;
		}
	}

	public class TinyNetStateSetter {

		protected TinyNetStateData _tinyNetState = new TinyNetStateData();

		public TinyNetStateData GetStateData() {
			return _tinyNetState;
		}

		/// <summary>
		/// Copies the byte array from start[inclusive] to end[inclusive].
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="start">The start.</param>
		/// <param name="end">The end.</param>
		protected void CopyByteArray(byte[] source, int start, int end) {
			int size = Math.Abs(end - start);
			if (_tinyNetState.data.Length < size) {
				_tinyNetState.data = new byte[size];
			}

			int count = 0;
			for (int i = start; i <= end; i++, count++) {
				_tinyNetState.data[count] = source[i];
			}

			_tinyNetState.position = 0;
			_tinyNetState.dataSize = count;
		}

		public void SetSource(NetDataWriter dataWriter) {
			CopyByteArray(dataWriter.Data, 0, dataWriter.Length - 1);
		}

		public void SetSource(byte[] source) {
			CopyByteArray(source, 0, source.Length - 1);
		}

		public void SetSource(byte[] source, int offset) {
			CopyByteArray(source, offset, source.Length - 1);
		}

		public void SetSource(byte[] source, int offset, int maxSize) {
			CopyByteArray(source, offset, maxSize - 1);
		}

		public void SetFrameTick(int newTick) {
			_tinyNetState.frameTick = newTick;
		}

		/// <summary>
		/// Clone NetDataReader without data copy (usable for OnReceive)
		/// </summary>
		/// <returns>new NetDataReader instance</returns>
		public TinyNetStateSetter Clone() {
			return new TinyNetStateSetter(_tinyNetState.data, _tinyNetState.position, _tinyNetState.dataSize);
		}

		public TinyNetStateSetter() {
		}

		public TinyNetStateSetter(byte[] source) {
			SetSource(source);
		}

		public TinyNetStateSetter(byte[] source, int offset) {
			SetSource(source, offset);
		}

		public TinyNetStateSetter(byte[] source, int offset, int maxSize) {
			SetSource(source, offset, maxSize);
		}
	}
}

