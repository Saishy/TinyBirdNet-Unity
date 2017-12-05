using LiteNetLib.Utils;
using System.Collections;
using System.Collections.Generic;
using TinyBirdNet;
using UnityEngine;

public class ExamplePawn : TinyNetBehaviour {

	string _playerName;
	[TinyNetSyncVar]
	string PlayerName { get { return _playerName; } set { _playerName = value; } }

	Vector3 _networkPosition;

	[TinyNetSyncVar]
	float xPos { get { return _networkPosition.x; } set { _networkPosition.x = value; } }
	[TinyNetSyncVar]
	float zPos { get { return _networkPosition.z; } set { _networkPosition.z = value; } }

	[TinyNetSyncVar]
	public short ownerPlayerControllerId { get; set; }

	public GameObject bulletPrefab;

	public Transform bulletSpawnPosition;

	public float movementSpeed;

	public float shootCooldown;

	[HideInInspector]
	public ExamplePlayerController controller;

	protected Rigidbody rbody;

	protected float timeForNextShoot = 0f;
	protected float movespeedPow; //Cache used for calculations

	protected byte currentDir;

	private void Awake() {
		xPos = transform.position.x;
		zPos = transform.position.z;

		rbody = GetComponent<Rigidbody>();

		movespeedPow = movementSpeed * movementSpeed;

		RegisterRPCDelegate(ServerShootReceive, "ServerShoot");
	}

	public override void OnStartAuthority() {
		base.OnStartAuthority();

		controller = TinyNetClient.instance.connToHost.GetPlayerController(ownerPlayerControllerId) as ExamplePlayerController;
	}

	public override void OnNetworkDestroy() {
		base.OnNetworkDestroy();

		controller.LosePawn();
		controller = null;
	}

	private void FixedUpdate() {
		if (!hasAuthority) {
			Vector3 pos = transform.position;
			Vector3 result = Vector3.MoveTowards(pos, _networkPosition, movementSpeed * Time.fixedDeltaTime);

			float dist = (result - _networkPosition).sqrMagnitude;
			if (dist <= 0.1f || dist >= movespeedPow) {
				result = _networkPosition;
			}

			transform.position = result;
		}
	}

	public void MoveToDir(byte direction) {
		switch (direction) {
			case 0:
				return;
			//Top
			case 1:
				currentDir = 1;
				rbody.MovePosition(rbody.position + Vector3.forward * movementSpeed * Time.fixedDeltaTime);
				return;
			//Right
			case 2:
				currentDir = 2;
				rbody.MovePosition(rbody.position +  Vector3.right * movementSpeed * Time.fixedDeltaTime);
				return;
			//Down
			case 3:
				currentDir = 3;
				rbody.MovePosition(rbody.position + Vector3.back * movementSpeed * Time.fixedDeltaTime);
				return;
			//Left
			case 4:
				currentDir = 4;
				rbody.MovePosition(rbody.position + Vector3.left * movementSpeed * Time.fixedDeltaTime);
				return;
		}
	}

	public void Shoot() {
		if (Time.time <= timeForNextShoot) {
			timeForNextShoot = Time.time + shootCooldown;

			ServerShoot(bulletSpawnPosition.position.x, bulletSpawnPosition.position.z);
		}
	}

	public void ServerSyncPosFromOwner(float nPosX, float nPosZ) {
		xPos = nPosX;
		zPos = nPosZ;
	}

	[TinyNetRPC(RPCTarget.Server, RPCCallers.ClientOwner)]
	void ServerShoot(float xPos, float zPos) {
		if (!isServer) {
			rpcRecycleWriter.Reset();
			rpcRecycleWriter.Put(xPos);
			rpcRecycleWriter.Put(zPos);

			SendRPC(rpcRecycleWriter, "ServerShoot");
			return;
		}

		ExampleBullet bullet = Instantiate(bulletPrefab, bulletSpawnPosition).GetComponent<ExampleBullet>();
		bullet.ownerNetworkId = NetworkID;
		TinyNetServer.instance.SpawnObject(bullet.gameObject);
	}

	void ServerShootReceive(NetDataReader reader) {
		ServerShoot(reader.GetFloat(), reader.GetFloat());
	}
}
