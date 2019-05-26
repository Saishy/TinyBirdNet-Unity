using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TinyBirdNet.Utils {

	public static class TinyBitArrayUtil {

		/*public static byte DirtyFlagToByte(BitArray bitArray) {
			if (bitArray.Length > 8) {
				throw new ArgumentException("Argument length shall be at most 8 bits.");
			}

			byte output = 0;

			for (int i = 0; i < 8; i++) {
				if (bitArray[i]) { //check the first bit 
					output |= (byte)(1 << i);
				} else {
					output |= (byte)(0 << i);
				}
			}

			return output;
		}

		public static void ByteToDirtyFlag(byte input, BitArray bitArray) {
			for (int i = 0; i < 8; i++) {
				if ((input & 1) == 1) { //check the first bit 
					bitArray[i] = true;
				} else {
					bitArray[i] = false;
				}
				input >>= 1; // move all bits right 1 so the next first bit from 110 becomes 11
			}
		}

		public static short DirtyFlagToShort(BitArray bitArray) {
			if (bitArray.Length > 16) {
				throw new ArgumentException("Argument length shall be at most 16 bits.");
			}

			short output = 0;

			for (int i = 0; i < 16; i++) {
				if (bitArray[i]) { //check the first bit 
					output |= (short)(1 << i);
				} else {
					output |= (short)(0 << i);
				}
			}

			return output;
		}

		public static void ShortToDirtyFlag(short input, BitArray bitArray) {
			for (int i = 0; i < 16; i++) {
				if ((input & 1) == 1) { //check the first bit 
					bitArray[i] = true;
				} else {
					bitArray[i] = false;
				}
				input >>= 1; // move all bits right 1 so the next first bit from 110 becomes 11
			}
		}

		public static int DirtyFlagToInt(BitArray bitArray) {
			if (bitArray.Length > 32) {
				throw new ArgumentException("Argument length shall be at most 32 bits.");
			}

			int output = 0;

			for (int i = 0; i < 32; i++) {
				if (bitArray[i]) { //check the first bit 
					output |= 1 << i;
				} else {
					output |= 0 << i;
				}
			}

			return output;
		}

		public static void IntToDirtyFlag(int input, BitArray bitArray) {
			for (int i = 0; i < 32; i++) {
				if ((input & 1) == 1) { //check the first bit 
					bitArray[i] = true;
				} else {
					bitArray[i] = false;
				}
				input >>= 1; // move all bits right 1 so the next first bit from 110 becomes 11
			}
		}

		public static ulong DirtyFlagToULong(BitArray bitArray) {
			if (bitArray.Length > 64) {
				throw new ArgumentException("Argument length shall be at most 64 bits.");
			}

			ulong output = 0;

			for (int i = 0; i < 64; i++) {
				if (bitArray[i]) { //check the first bit 
					output |= (uint)(1 << i);
				} else {
					output |= (uint)(0 << i);
				}
			}

			return output;
		}

		public static void LongToDirtyFlag(long input, BitArray bitArray) {
			for (int i = 0; i < 64; i++) {
				if ((input & 1) == 1) { //check the first bit 
					bitArray[i] = true;
				} else {
					bitArray[i] = false;
				}
				input >>= 1; // move all bits right 1 so the next first bit from 110 becomes 11
			}
		}*/

		/// <summary>
		/// Converts a bitArray to an integral type of size ulong or less.
		/// </summary>
		/// <param name="bitArray">The bitarray.</param>
		/// <returns>An integral that represents the bit array.</returns>
		/// <exception cref="ArgumentException">BitArray length shall be at most 64.</exception>
		public static ulong BitArrayToU64(BitArray bitArray) {
			if (bitArray.Count > 64) {
				throw new ArgumentException("BitArray length shall be at most 64.");
			}

			var len = Math.Min(64, bitArray.Count);
			ulong n = 0;

			for (int i = 0; i < len; i++) {
				if (bitArray.Get(i))
					n |= 1UL << i;
			}

			return n;
		}

		/// <summary>
		/// Converts an integral type of size ulong or less to a BitArray.
		/// </summary>
		/// <param name="input">The integral.</param>
		/// <param name="bitArray">The bitarray.</param>
		/// <exception cref="ArgumentException">BitArray length shall be at most 64.</exception>
		public static void U64ToBitArray(ulong input, BitArray bitArray) {
			if (bitArray.Count > 64) {
				throw new ArgumentException("BitArray length shall be at most 64.");
			}

			for (int i = 0; i < bitArray.Count; i++) {
				if ((input & 1) == 1) { //check the first bit 
					bitArray[i] = true;
				} else {
					bitArray[i] = false;
				}
				input >>= 1; // move all bits right 1 so the next first bit from 110 becomes 11
			}
		}

		public static string Display(ulong value, bool cull = false) {
			int i = Convert.ToString((long)value, 2).Length;

			var m = i % 8;
			int padding = 0;
			if (m != 0) {
				padding = 8 - m;
			}
			if (cull) {
				return Regex.Replace(Convert.ToString((long)value, 2), ".{8}", "$0 ");
			}

			string s = new String('0', padding) + Convert.ToString((long)value, 2);
			s = Regex.Replace(s, ".{8}", "$0 ");

			//Reverse
			char[] charArray = s.ToCharArray();
			Array.Reverse(charArray);
			s = new string(charArray).Trim();

			return s;
		}

		public static string Display(BitArray bitarray) {
			string stest = "";

			for (int i = 0; i < bitarray.Count; i++) {
				if (i != 0 && i % 8 == 0) {
					stest += " ";
				}
				if (bitarray[i]) {
					stest += "1";
				} else {
					stest += "0";
				}
			}

			return stest;
		}
	}
}
