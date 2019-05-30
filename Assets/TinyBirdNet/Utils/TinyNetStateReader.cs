using LiteNetLib.Utils;
using System.Linq;
using System.Text;

namespace TinyBirdNet.Utils {

	public class TinyNetStateReader : NetDataReader {

		public int FrameTick {
			get; protected set;
		}
	}
}
