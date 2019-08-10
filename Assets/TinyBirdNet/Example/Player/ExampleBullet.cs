using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TinyBirdNet;

public class ExampleBullet : TinyNetBehaviour {

	public Material friendlyMat;
	public Material enemyMat;

	protected MeshRenderer cMeshRender;

	protected Rigidbody rbody;

	public float movementSpeed;

	[TinyNetSyncVar]
	public int ownerNetworkId { get; set; }

	[TinyNetSyncVar]
	public byte direction { get; set; }

	protected override void Awake() {
		base.Awake();

		cMeshRender = GetComponent<MeshRenderer>();
		rbody = GetComponent<Rigidbody>();

		NetIdentity.RegisterEventHandler(TinyNetIdentity.TinyNetComponentEvents.OnStartClient, OnStartClient);
	}

	private void OnTriggerEnter(Collider other) {
		if (!gameObject.activeSelf) {
			return;
		}

		ExamplePawn pawn = other.gameObject.GetComponent<ExamplePawn>();

		if (other.CompareTag("Player")) {
			if (pawn.NetIdentity.TinyInstanceID.NetworkID == ownerNetworkId) {
				return;
			}

			if (isServer) {
				TinyNetServer.instance.DestroyObject(gameObject);
				pawn.Killed();
			} else {
				gameObject.SetActive(false);
			}
		} else {
			if (other.gameObject.layer == 8) {
				if (isServer) {
					TinyNetServer.instance.DestroyObject(gameObject);
				} else {
					gameObject.SetActive(false);
				}
			}
		}
	}

	private void FixedUpdate() {
		rbody.MovePosition(rbody.position + transform.forward * movementSpeed * Time.fixedDeltaTime);
	}

	/*public override void OnNetworkCreate() {
		base.OnNetworkCreate();

		for (int i = 0; i < 32; i++) {
			DirtyFlag.Set(i, Random.Range(0, 2) == 0);
			if (i == 0) {
				DirtyFlag.Set(0, true);
			}
		}

		int test = TinyNetStateSyncer.DirtyFlagToInt(DirtyFlag);
		Debug.Log(TinyNetStateSyncer.Display(test));

		TinyNetStateSyncer.IntToDirtyFlag(test, DirtyFlag);

		string stest = "";
		for (int i = 0; i < 32; i++) {
			if (i != 0 && i % 8 == 0) {
				stest += " ";
			}
			if (DirtyFlag[i]) {
				stest += "1";
			} else {
				stest += "0";
			}
		}
		Debug.Log(stest);
		Debug.Log("==============================");

		//DirtyFlag.SetAll(false);
		DirtyFlag.Set(0, true);
		test = TinyNetStateSyncer.DirtyFlagToInt(DirtyFlag);
		Debug.Log(TinyNetStateSyncer.Display(test));
		TinyNetStateSyncer.IntToDirtyFlag(test, DirtyFlag);
		test = TinyNetStateSyncer.DirtyFlagToInt(DirtyFlag);
		Debug.Log(TinyNetStateSyncer.Display(test));
	}*/

	public override void OnStartClient() {
		base.OnStartClient();

		switch (direction) {
			case 0:
				Debug.LogWarning("ExampleBullet direction is zero.");
				break;
			case 1:
				transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
				break;
			//Right
			case 2:
				transform.rotation = Quaternion.Euler(new Vector3(0f, 90f, 0f));
				break;
			//Down
			case 3:
				transform.rotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
				break;
			//Left
			case 4:
				transform.rotation = Quaternion.Euler(new Vector3(0f, 270f, 0f));
				break;
		}

		if (TinyNetScene.GetTinyNetIdentityByNetworkID(ownerNetworkId).HasAuthority) {
			cMeshRender.material = friendlyMat;
		}
	}
}
