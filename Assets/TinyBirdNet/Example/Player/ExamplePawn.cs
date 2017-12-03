using System.Collections;
using System.Collections.Generic;
using TinyBirdNet;
using UnityEngine;

public class ExamplePawn : TinyNetBehaviour {

	string _playerName;
	[TinyNetSyncVar]
	string PlayerName { get { return _playerName; } set { _playerName = value; } }

	[TinyNetRPC(RPCTarget.Server, RPCCallers.ClientOwner)]
	void ClientToServerCallRPC() {
		if (TinyNetGameManager.instance.isClient) {

		}
	}


}
