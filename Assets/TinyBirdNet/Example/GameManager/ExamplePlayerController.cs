using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TinyBirdNet;
using TinyBirdNet.Messaging;
using LiteNetLib.Utils;

public class ExamplePlayerController : TinyNetPlayerController {

	//<summary>Will send the input buffer every X fixed frames</summary>*/
	//public int inputBufferSize = 5;

	//protected List<ExampleInputMessage> inputBuffer;
	protected ExampleInputMessage inputMessageReader = new ExampleInputMessage();
	protected ExampleInputMessage inputMessageBuffer = new ExampleInputMessage();

	protected Coroutine fixedUpdateCoroutine;

	public ExamplePlayerController() : base() {
		/*inputBuffer = new List<ExampleInputMessage>(inputBufferSize);
		inputMessages = new ExampleInputMessage[inputBufferSize];

		for (int i = 0; i < inputMessages.Length; i++) {
			inputMessages[i] = new ExampleInputMessage();
		}*/
	}

	public ExamplePlayerController(short playerControllerId) : base(playerControllerId) {
		//Hacky way, but I want a coroutine...
		fixedUpdateCoroutine = TinyNetGameManager.instance.StartCoroutine(FixedUpdateLoop());
	}

	//Finalizer
	~ExamplePlayerController() {
		if (fixedUpdateCoroutine != null) {
			TinyNetGameManager.instance.StopCoroutine(fixedUpdateCoroutine);
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

	IEnumerator FixedUpdateLoop() {
		while (true) {
			Vector2 axis = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;

			MoveToDir(axis == Vector2.zero ? (byte)0 : VectorToDirection(axis));

			if (Input.GetButton("Fire1")) {
				Shoot();
			}

			yield return new WaitForFixedUpdate();
		}
	}

	public override void GetInputMessage(TinyNetMessageReader netMsg) {
		netMsg.ReadMessage(inputMessageReader);

		MoveToDir(inputMessageReader.Direction);

		if (inputMessageReader.bShoot) {
			Shoot();
		}
	}

	protected void MoveToDir(byte direction) {
		switch (direction) {
			case 0:
				return;
			//Top
			case 1:
				return;
			//Right
			case 2:
				return;
			//Down
			case 3:
				return;
			//Left
			case 4:
				return;
		}
	}

	protected void Shoot() {

	}
}

public class ExampleInputMessage : TinyNetInputMessage {
	public byte Direction;
	public bool bShoot;

	public override void Deserialize(NetDataReader reader) {
		base.Deserialize(reader);

		Direction = reader.GetByte();
		bShoot = reader.GetBool();
	}

	public override void Serialize(NetDataWriter writer) {
		base.Serialize(writer);

		writer.Put(Direction);
		writer.Put(bShoot);
	}
}