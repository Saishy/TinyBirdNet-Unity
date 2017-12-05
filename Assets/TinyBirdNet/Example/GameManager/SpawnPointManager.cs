using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour {

	public Collider[] spawnAreas;

	public LayerMask playerCollisionMask;

	public static SpawnPointManager instance;

	private void Awake() {
		instance = this;
	}

	public static Vector3 GetSpawnPoint() {
		Vector3 pos = new Vector3();

		Collider[] touching = new Collider[1];
		int x = 0;
		int randomStart = Random.Range(0, instance.spawnAreas.Length);
		int count = instance.spawnAreas.Length;

		for (int i = randomStart; count > 0; i++, count--) {
			if (i > instance.spawnAreas.Length) {
				i = 0;
			}

			x = Physics.OverlapBoxNonAlloc(instance.spawnAreas[i].transform.position, instance.spawnAreas[i].bounds.extents, touching, instance.spawnAreas[i].transform.rotation, instance.playerCollisionMask);

			if (x == 0) {
				pos = instance.spawnAreas[i].transform.position;

				break;
			}
		}

		return pos;
	}
}
