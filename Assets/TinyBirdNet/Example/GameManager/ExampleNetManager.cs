using TinyBirdNet;
using TinyBirdNet.Messaging;
using UnityEngine;

class ExampleNetManager : TinyNetGameManager {

	protected TinyNetShortMessage shortMessage = new TinyNetShortMessage();
	protected TinyNetStringMessage stringMsg = new TinyNetStringMessage();

	protected override void AwakeVirtual() {
		base.AwakeVirtual();
	}

	public override bool StartServer() {
		bool bResult = base.StartServer();

#if DEBUG
		//serverManager.NetManager.SimulateLatency = true;
		//serverManager.NetManager.SimulationMaxLatency = 800;
		//serverManager.NetManager.SimulationMinLatency = 600;
#endif

		if (bResult && serverManager != null && serverManager.isRunning) {
			ServerChangeScene("MainScene");
		}

		return bResult;
	}

	public void StartSinglePlayer() {
		
	}

	public override void RegisterMessageHandlersServer() {
		base.RegisterMessageHandlersServer();

		serverManager.RegisterHandlerSafe(TinyNetMsgType.Highest + 1, OnPlayerNameReceive);
	}

	public override TinyNetPlayerController CreatePlayerController(TinyNetConnection conn, byte playerId, byte[] msgData) {
		return new ExamplePlayerController(playerId, conn);
	}

	public override void OnClientReady() {
		base.OnClientReady();

		if (clientManager.connToHost.playerControllers.Count > 0) {
			return;
		}

		clientManager.RequestAddPlayerControllerToServer(null);

		//Remember this only works cos we only have 1 player per connection in this demo.
		stringMsg.msgType = TinyNetMsgType.Highest + 1;
		stringMsg.value = System.Environment.UserName;

		clientManager.connToHost.Send(stringMsg, LiteNetLib.DeliveryMethod.ReliableOrdered);
	}

	public void PawnRequest(ExamplePlayerController controller) {
		ExamplePawn newPawn = Instantiate(GameManager.instance.pawnPrefab, SpawnPointManager.GetSpawnPoint(), Quaternion.identity);
		newPawn.ownerPlayerControllerId = controller.playerControllerId;
		newPawn.PlayerName = (controller).userName;

		serverManager.SpawnWithClientAuthority(newPawn.gameObject, controller.Conn);
	}

	protected void OnPlayerNameReceive(TinyNetMessageReader netMsg) {
		netMsg.ReadMessage(stringMsg);

		//This only works because this game uses only one controller per connection!
		((ExamplePlayerController)netMsg.tinyNetConn.GetFirstPlayerController()).userName = stringMsg.value;
	}
}

