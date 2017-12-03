using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyBirdNet;

class ExampleNetManager : TinyNetGameManager {

	protected override void AwakeVirtual() {
		base.AwakeVirtual();

		TinyNetScene.createPlayerAction = CreatePlayerAndAdd;
	}

	void CreatePlayerAndAdd(TinyNetConnection conn, int playerId) {
		conn.SetPlayerController<ExamplePlayerController>(new ExamplePlayerController((short)playerId));
	}

	public override void ClientConnectTo(string hostAddress, int hostPort) {
		base.ClientConnectTo(hostAddress, hostPort);

		ServerChangeScene("MainScene");
	}
}

