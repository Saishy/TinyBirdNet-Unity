using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyBirdNet;

class ExampleNetManager : TinyNetGameManager {

	public override void ClientConnectTo(string hostAddress, int hostPort) {
		base.ClientConnectTo(hostAddress, hostPort);

		ServerChangeScene("MainScene");
	}
}

