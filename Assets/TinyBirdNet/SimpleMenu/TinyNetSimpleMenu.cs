using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TinyBirdNet {

	public class TinyNetSimpleMenu : MonoBehaviour {

		public InputField ipToConnectField;
		public InputField portToConnectField;

		public void PressedConnectButton() {
			TinyNetGameManager.instance.StartClient();

			TinyNetGameManager.instance.ClientConnectTo(ipToConnectField.text.Length == 0 ? "localhost" : ipToConnectField.text, portToConnectField.text.Length == 0 ? 7777 : int.Parse(portToConnectField.text));
		}

		public void PressedHostButton() {
			TinyNetGameManager.instance.SetPort(portToConnectField.text.Length == 0 ? 7777 : int.Parse(portToConnectField.text));

			TinyNetGameManager.instance.StartServer();
		}

		public void ToggleNatPunching(bool bNewValue) {
			TinyNetGameManager.instance.ToggleNatPunching(bNewValue);
		}
	}
}