using LiteNetLib.Utils;
using System.Collections;
using System.Collections.Generic;
using TinyBirdNet;
using TinyBirdNet.Utils;
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

	protected Vector2 movementInput = Vector2.zero;
	protected bool bFireInput = false;

	public byte GetDir {
		get { return currentDir; }
	}

	protected Transform cameraTransform;

	protected override void Awake() {
		rbody = GetComponent<Rigidbody>();

		movespeedPow = movementSpeed * movementSpeed;

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

		//RegisterRPCDelegate(EmojiReceive, "Emoji");
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

	public static byte VectorToDirection(Vector2 axis) {
		int type = ((Mathf.RoundToInt(Mathf.Atan2(axis.y, axis.x) / (2f * Mathf.PI / 4f))) + 4) % 4;

		//0 = right, 3 = down
		switch (type) {
			case 0:
				return 2;
			case 1:
				return 1;
			case 2:
				return 4;
			case 3:
				return 3;
		}

		return 0;
	}

	private void FixedUpdate() {
		if (isServer) {
			byte dir = movementInput == Vector2.zero ? (byte)0 : VectorToDirection(movementInput);
			MoveToDir(dir);

			if (bFireInput) {
				Shoot();
			}
		}

		if (!isServer) {
			Vector3 pos = transform.position;
			Vector3 result = Vector3.MoveTowards(pos, _networkPosition, movementSpeed * Time.fixedDeltaTime);

			float dist = (result - _networkPosition).sqrMagnitude;
			if (dist <= 0.1f || dist >= movespeedPow) {
				result = _networkPosition;
			}

			FaceDir(netDir);

			transform.position = result;
		}
	}

	private void Update() {
		if (hasAuthority) {
			cameraTransform.position = new Vector3(transform.position.x, 10.0f, transform.position.z - 6f);
		}
	}

	private void MoveToDir(byte direction) {
		FaceDir(direction);

		switch (direction) {
			case 0:
				rbody.velocity = Vector3.zero;
				break;
			//Top
			case 1:
				rbody.velocity = Vector3.forward * movementSpeed;
				//rbody.AddForce(Vector3.forward * movementSpeed, ForceMode.Force);
				break;
			//Right
			case 2:
				rbody.velocity = Vector3.right * movementSpeed;
				//rbody.AddForce(Vector3.right * movementSpeed, ForceMode.Force);
				break;
			//Down
			case 3:
				rbody.velocity = Vector3.back * movementSpeed;
				//rbody.AddForce(Vector3.back * movementSpeed, ForceMode.Force);
				break;
			//Left
			case 4:
				rbody.velocity = Vector3.left * movementSpeed;
				//rbody.AddForce(Vector3.left * movementSpeed, ForceMode.Force);
				break;
		}

		xPos = transform.position.x;
		zPos = transform.position.z;
		if (direction == 0) {
			return;
		}
		netDir = direction;
	}

	public void GetMovementInput(Vector2 axis, bool bFire) {
		bFireInput = bFire;

		movementInput = Vector2.zero;

		if (axis.x > 0.5f) {
			movementInput.x = 1f;
		}
		if (axis.x < -0.5f) {
			movementInput.x = -1f;
		}
		if (axis.y > 0.5f) {
			movementInput.y = 1f;
		}
		if (axis.y < -0.5f) {
			movementInput.y = -1f;
		}
	}

	private Quaternion GetQuaternionForDir(byte direction) {
		switch (direction) {
			//Top
			case 1:
				return Quaternion.Euler(new Vector3(0f, 0f, 0f));
			//Right
			case 2:
				return Quaternion.Euler(new Vector3(0f, 90f, 0f));
			//Down
			case 3:
				return Quaternion.Euler(new Vector3(0f, 180f, 0f));
			//Left
			case 4:
				return Quaternion.Euler(new Vector3(0f, 270f, 0f));
		}

		return Quaternion.Euler(new Vector3(0f, 0f, 0f));
	}

	private void FaceDir(byte direction) {
		if (direction == 0) {
			return;
		}
		currentDir = direction;

		transform.rotation = GetQuaternionForDir(direction);

		playerText.transform.rotation = Quaternion.identity;
	}

	public void Shoot() {
		if (timeForNextShoot <= Time.time) {
			timeForNextShoot = Time.time + shootCooldown;

			if (!isServer) {
				return;
			}

			ExampleBullet bullet = Instantiate(bulletPrefab, bulletSpawnPosition.position, transform.rotation).GetComponent<ExampleBullet>();
			bullet.ownerNetworkId = NetIdentity.TinyInstanceID.NetworkID;
			bullet.direction = currentDir;

			bullet.transform.rotation = GetQuaternionForDir(currentDir);

			TinyNetServer.instance.SpawnObject(bullet.gameObject);
		}
	}

	public void Killed() {
		TinyNetServer.instance.DestroyObject(gameObject);
	}
}
