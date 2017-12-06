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

	private void Awake() {
		cMeshRender = GetComponent<MeshRenderer>();
		rbody = GetComponent<Rigidbody>();
	}

	private void OnTriggerEnter(Collider other) {
		if (!gameObject.activeSelf) {
			return;
		}

		ExamplePawn pawn = other.gameObject.GetComponent<ExamplePawn>();

		if (other.CompareTag("Player")) {
			if (pawn.NetIdentity.NetworkID == ownerNetworkId) {
				return;
			}

			if (isServer) {
				TinyNetServer.instance.DestroyObject(gameObject);
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

	public override void OnStartClient() {
		base.OnStartClient();

		if (TinyNetScene.GetTinyNetIdentityByNetworkID(ownerNetworkId).hasAuthority) {
			cMeshRender.material = friendlyMat;
		}
	}
}
