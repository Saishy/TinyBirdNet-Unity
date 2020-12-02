using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TinyBirdNet;
using TinyBirdNet.Messaging;
using LiteNetLib.Utils;
using System;

public class ExamplePlayerController : TinyNetPlayerController {

	[Flags]
	public enum MovementKeys : byte {
		Left = 1 << 1,
		Right = 1 << 2,
		Up = 1 << 3,
		Down = 1 << 4,
		Fire = 1 << 5
	}

	protected ExampleInputMessage inputMessageReader = new ExampleInputMessage();
	protected ExampleInputMessage inputMessageBuffer = new ExampleInputMessage();

	protected ExamplePawn pawn;

	public ExamplePawn Pawn { get { return pawn; } }

	public string userName;

	protected float timeForSpawn;

	protected bool bAskedForPawn = false;

	public ExamplePlayerController() : base() {
	}

	public ExamplePlayerController(byte playerControllerId, TinyNetConnection nConn) : base(playerControllerId, nConn) {
		inputMessageBuffer.playerControllerId = playerControllerId;
	}

	//Finalizer
	/*~ExamplePlayerController() {
		if (fixedUpdateCoroutine != null) {
			TinyNetGameManager.instance.StopCoroutine(fixedUpdateCoroutine);
		}
	}*/

	public override void OnDisconnect() {
		if (TinyNetGameManager.Instance.isServer && pawn != null) {
			TinyNetServer.instance.DestroyObject(pawn.gameObject);
		}
	}

	public override void Update() {
		if (!HasAuthority) {
			return;
		}
		Vector2 axis = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;

		bool bFire = false;
		if (Input.GetButton("Fire1")) {
			bFire = true;
		}

		/*if (!IsServerVersion()) {
			InsertInput(axis, bFire);
		}*/

		inputMessageBuffer.keys = 0;

		inputMessageBuffer.serverTick = TinyNetGameManager.Instance.GetFrameTick();

		if (bFire) {
			inputMessageBuffer.keys |= MovementKeys.Fire;
		}
		if (axis.x > 0.5f) {
			inputMessageBuffer.keys |= MovementKeys.Right;
		}
		if (axis.x < -0.5f) {
			inputMessageBuffer.keys |= MovementKeys.Left;
		}
		if (axis.y > 0.5f) {
			inputMessageBuffer.keys |= MovementKeys.Up;
		}
		if (axis.y < -0.5f) {
			inputMessageBuffer.keys |= MovementKeys.Down;
		}

		TinyNetClient.instance.connToHost.Send(inputMessageBuffer, LiteNetLib.DeliveryMethod.Sequenced);
	}

	public override void GetInputMessage(TinyNetMessageReader netMsg) {
		//if (TinyNetLogLevel.logDev) { TinyBirdUtils.TinyLogger.Log("ExamplePlayerController::GetInputMessage called"); }

		netMsg.ReadMessage(inputMessageReader);

		Vector2 axis = new Vector2();

		if ((inputMessageReader.keys & MovementKeys.Up) != 0) {
			axis.y = 1f;
		}
		if ((inputMessageReader.keys & MovementKeys.Down) != 0) {
			axis.y = -1f;
		}

		if ((inputMessageReader.keys & MovementKeys.Left) != 0) {
			axis.x = -1f;
		}
		if ((inputMessageReader.keys & MovementKeys.Right) != 0) {
			axis.x = 1f;
		}

		bool bFire = (inputMessageReader.keys & MovementKeys.Fire) != 0;
		InsertInput(axis, bFire);

		if (bFire && pawn == null) {
			AskForPawn();
		}

		/*if (pawn != null) {

			//pawn.ServerSyncPosFromOwner(inputMessageReader.xPos, inputMessageReader.zPos, inputMessageReader.dir);
			return;
		}*/

		//if (TinyNetLogLevel.logDev) { TinyBirdUtils.TinyLogger.Log("ExamplePlayerController::GetInputMessage no pawn?"); }
	}

	protected void InsertInput(Vector2 axis, bool bFire) {
		if (pawn != null) {
			pawn.GetMovementInput(axis, bFire);
		}
	}

	protected void AskForPawn() {
		if (!bAskedForPawn && timeForSpawn <= Time.time) {
			((ExampleNetManager)TinyNetGameManager.Instance).PawnRequest(this);

			bAskedForPawn = true;
		}
	}

	public void GetPawn(ExamplePawn nPawn) {
		pawn = nPawn;
	}

	public void LosePawn() {
		pawn = null;
		bAskedForPawn = false;

		timeForSpawn = Time.time + 3f;
	}
}

public class ExampleInputMessage : TinyNetInputMessage {
	public ExamplePlayerController.MovementKeys keys;
	public ushort serverTick;

	public override void Deserialize(NetDataReader reader) {
		base.Deserialize(reader);

		keys = (ExamplePlayerController.MovementKeys)reader.GetByte();
		serverTick = reader.GetUShort();
	}

	public override void Serialize(NetDataWriter writer) {
		base.Serialize(writer);

		writer.Put((byte)keys);
		writer.Put(serverTick);
	}
}
