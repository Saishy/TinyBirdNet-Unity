using System.Collections;
using System.Collections.Generic;
using TinyBirdNet;
using UnityEngine;

public class ExamplePawn : TinyNetBehaviour {

	string _playerName;
	[TinyNetSyncVar]
	string PlayerName { get { return _playerName; } set { _playerName = value; } }

	Vector2 _networkPosition;

	[TinyNetSyncVar]
	float xPos { get; set; }
	[TinyNetSyncVar]
	float yPos { get; set; }

	public float movementSpeed;

	[HideInInspector]
	public ExamplePlayerController controller;

	protected Rigidbody rbody;

	private void Awake() {
		rbody = GetComponent<Rigidbody>();
	}

	[TinyNetRPC(RPCTarget.Server, RPCCallers.ClientOwner)]
	void ClientToServerCallRPC() {
		if (!TinyNetGameManager.instance.isServer) {

		}
	}

	public void MoveToDir(byte direction) {
		switch (direction) {
			case 0:
				return;
			//Top
			case 1:
				rbody.MovePosition(Vector3.forward * movementSpeed * Time.fixedDeltaTime);
				return;
			//Right
			case 2:
				rbody.MovePosition(Vector3.right * movementSpeed * Time.fixedDeltaTime);
				return;
			//Down
			case 3:
				rbody.MovePosition(Vector3.back * movementSpeed * Time.fixedDeltaTime);
				return;
			//Left
			case 4:
				rbody.MovePosition(Vector3.left * movementSpeed * Time.fixedDeltaTime);
				return;
		}
	}

	public void Shoot() {

	}
}
