using LiteNetLib.Utils;
using System.Linq;
using System.Text;

namespace TinyBirdNet.Utils {

	public class TinyNetStateReader : NetDataReader {

		public ushort FrameTick {
			get; protected set;
		}

		public void SetFrameTick(ushort nFrame) {
			FrameTick = nFrame;
		}
	}
}
