using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TinyBirdNet {

	public class TinyNetSimpleMenu : MonoBehaviour {

		public InputField ipToConnectField;
		public InputField portToConnectField;

		public void PressedConnectButton() {
			TinyNetManager.instance.StartClient();

			TinyNetManager.instance.ClientConnectTo(ipToConnectField.text.Length == 0 ? "localhost" : ipToConnectField.text, portToConnectField.text.Length == 0 ? 7777 : int.Parse(portToConnectField.text));
		}

		public void PressedHostButton() {
			TinyNetManager.instance.SetPort(portToConnectField.text.Length == 0 ? 7777 : int.Parse(portToConnectField.text));

			TinyNetManager.instance.StartServer();
		}
	}
}