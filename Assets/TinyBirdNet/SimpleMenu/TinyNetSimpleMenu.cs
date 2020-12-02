using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TinyBirdNet {

	public class TinyNetSimpleMenu : MonoBehaviour {

		public InputField ipToConnectField;
		public InputField portToConnectField;
		public InputField hostPortField;

		public void PressedConnectButton() {
			TinyNetGameManager.Instance.StartClient();

			TinyNetGameManager.Instance.ClientConnectTo(ipToConnectField.text.Length == 0 ? "localhost" : ipToConnectField.text, portToConnectField.text.Length == 0 ? 7777 : int.Parse(portToConnectField.text));
		}

		public void PressedHostButton() {
			TinyNetGameManager.Instance.SetPort(hostPortField.text.Length == 0 ? 7777 : int.Parse(hostPortField.text));

			TinyNetGameManager.Instance.StartServer();

			TinyNetGameManager.Instance.StartClient();

			TinyNetGameManager.Instance.ClientConnectTo("localhost", hostPortField.text.Length == 0 ? 7777 : int.Parse(hostPortField.text));
		}

		public void ToggleNatPunching(bool bNewValue) {
			TinyNetGameManager.Instance.ToggleNatPunching(bNewValue);
		}
	}
}
