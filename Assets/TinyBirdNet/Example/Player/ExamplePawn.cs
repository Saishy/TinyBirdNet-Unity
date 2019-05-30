using LiteNetLib.Utils;
using System.Collections;
using System.Collections.Generic;
using TinyBirdNet;
using UnityEngine;

public class ExamplePawn : TinyNetBehaviour {

	string _playerName;
	[TinyNetSyncVar]
	public string PlayerName { get { return _playerName; } set { _playerName = value; } }

	Vector3 _networkPosition;

	[TinyNetSyncVar]
	float xPos { get { return _networkPosition.x; } set { _networkPosition.x = value; } }
	[TinyNetSyncVar]
	float zPos { get { return _networkPosition.z; } set { _networkPosition.z = value; } }
	[TinyNetSyncVar]
	byte netDir { get; set; }

	[TinyNetSyncVar]
	public short ownerPlayerControllerId { get; set; }

	public GameObject bulletPrefab;

	public Transform bulletSpawnPosition;

	public TextMesh playerText;

	public float movementSpeed;

	public float shootCooldown;

	[HideInInspector]
	public ExamplePlayerController controller;

	protected Rigidbody rbody;

	protected float timeForNextShoot = 0f;
	protected float movespeedPow; //Cache used for calculations

	protected byte currentDir = 1;

	protected Transform cameraTransform;

	protected override void Awake() {
		rbody = GetComponent<Rigidbody>();

		movespeedPow = movementSpeed * movementSpeed;

		base.Awake();

		NetIdentity.RegisterEventHandler(TinyNetIdentity.TinyNetComponentEvents.OnNetworkCreate, OnNetworkCreate);
		NetIdentity.RegisterEventHandler(TinyNetIdentity.TinyNetComponentEvents.OnStartServer, OnStartServer);
		NetIdentity.RegisterEventHandler(TinyNetIdentity.TinyNetComponentEvents.OnStartAuthority, OnStartAuthority);
		NetIdentity.RegisterEventHandler(TinyNetIdentity.TinyNetComponentEvents.OnGiveAuthority, OnGiveAuthority);
		NetIdentity.RegisterEventHandler(TinyNetIdentity.TinyNetComponentEvents.OnStartClient, OnStartClient);
		NetIdentity.RegisterEventHandler(TinyNetIdentity.TinyNetComponentEvents.OnNetworkDestroy, OnNetworkDestroy);
	}

	private void Start() {
		xPos = transform.position.x;
		zPos = transform.position.z;
	}

	public override void OnNetworkCreate() {
		base.OnNetworkCreate();

		RegisterRPCDelegate(ServerShootReceive, "ServerShoot");
	}

	public override void OnStartServer() {
		base.OnStartServer();

		timeForNextShoot = Time.time + 0.3f;
	}

	public override void OnStartAuthority() {
		base.OnStartAuthority();

		controller = TinyNetClient.instance.connToHost.GetPlayerController<ExamplePlayerController>(ownerPlayerControllerId);
		controller.GetPawn(this);

		cameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;
	}

	public override void OnGiveAuthority() {
		base.OnGiveAuthority();

		controller = NetIdentity.ConnectionToOwnerClient.GetPlayerController<ExamplePlayerController>(ownerPlayerControllerId);
		controller.GetPawn(this);
	}

	public override void OnStartClient() {
		base.OnStartClient();

		playerText.text = PlayerName;
	}

	public override void OnNetworkDestroy() {
		base.OnNetworkDestroy();

		if (hasAuthority) {
			controller.LosePawn();
			controller = null;
		}
	}

	private void FixedUpdate() {
		if (!hasAuthority) {
			Vector3 pos = transform.position;
			Vector3 result = Vector3.MoveTowards(pos, _networkPosition, movementSpeed * Time.fixedDeltaTime);

			float dist = (result - _networkPosition).sqrMagnitude;
			if (dist <= 0.1f || dist >= movespeedPow) {
				result = _networkPosition;
			}

			FaceDir(netDir);

			transform.position = result;
		} else {
			cameraTransform.position = new Vector3(transform.position.x, 10.0f, transform.position.z - 6f);
		}
	}

	public void MoveToDir(byte direction) {
		FaceDir(direction);

		switch (direction) {
			case 0:
				return;
			//Top
			case 1:
				rbody.MovePosition(rbody.position + Vector3.forward * movementSpeed * Time.fixedDeltaTime);
				break;
			//Right
			case 2:
				rbody.MovePosition(rbody.position +  Vector3.right * movementSpeed * Time.fixedDeltaTime);
				break;
			//Down
			case 3:
				rbody.MovePosition(rbody.position + Vector3.back * movementSpeed * Time.fixedDeltaTime);
				break;
			//Left
			case 4:
				rbody.MovePosition(rbody.position + Vector3.left * movementSpeed * Time.fixedDeltaTime);
				break;
		}

		xPos = transform.position.x;
		zPos = transform.position.z;
		netDir = direction;
	}

	private void FaceDir(byte direction) {
		switch (direction) {
			case 0:
				return;
			//Top
			case 1:
				currentDir = 1;
				transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
				break;
			//Right
			case 2:
				currentDir = 2;
				transform.rotation = Quaternion.Euler(new Vector3(0f, 90f, 0f));
				break;
			//Down
			case 3:
				currentDir = 3;
				transform.rotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
				break;
			//Left
			case 4:
				currentDir = 4;
				transform.rotation = Quaternion.Euler(new Vector3(0f, 270f, 0f));
				break;
		}

		playerText.transform.rotation = Quaternion.identity;
	}

	public void Shoot() {
		if (timeForNextShoot <= Time.time) {
			timeForNextShoot = Time.time + shootCooldown;

			ServerShoot(bulletSpawnPosition.position.x, bulletSpawnPosition.position.z, currentDir);
		}
	}

	public void ServerSyncPosFromOwner(float nPosX, float nPosZ, byte dir) {
		//TinyBirdUtils.TinyLogger.Log("ExamplePawn::ServerSyncPosFromOwner called with: " + nPosX + "/" + nPosZ + " dir: " + dir);
		xPos = nPosX;
		zPos = nPosZ;
		netDir = dir;
	}

	[TinyNetRPC(RPCTarget.Server, RPCCallers.ClientOwner)]
	void ServerShoot(float xPos, float zPos, byte dir) {
		if (!isServer) {
			rpcRecycleWriter.Reset();
			rpcRecycleWriter.Put(xPos);
			rpcRecycleWriter.Put(zPos);
			rpcRecycleWriter.Put(dir);

			SendRPC(rpcRecycleWriter, "ServerShoot");
			return;
		}

		ExampleBullet bullet = Instantiate(bulletPrefab, bulletSpawnPosition.position, transform.rotation).GetComponent<ExampleBullet>();
		bullet.ownerNetworkId = NetIdentity.TinyInstanceID.NetworkID;
		bullet.direction = dir;
		switch (dir) {
			case 1:
				bullet.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
				break;
			//Right
			case 2:
				bullet.transform.rotation = Quaternion.Euler(new Vector3(0f, 90f, 0f));
				break;
			//Down
			case 3:
				bullet.transform.rotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
				break;
			//Left
			case 4:
				bullet.transform.rotation = Quaternion.Euler(new Vector3(0f, 270f, 0f));
				break;
		}

		TinyNetServer.instance.SpawnObject(bullet.gameObject);
	}

	void ServerShootReceive(NetDataReader reader) {
		ServerShoot(reader.GetFloat(), reader.GetFloat(), reader.GetByte());
	}

	public void Killed() {
		TinyNetServer.instance.DestroyObject(gameObject);
	}
}
