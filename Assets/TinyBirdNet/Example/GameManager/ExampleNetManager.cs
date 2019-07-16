using TinyBirdNet;
using TinyBirdNet.Messaging;
using UnityEngine;

class ExampleNetManager : TinyNetGameManager {

	protected TinyNetShortMessage shortMessage = new TinyNetShortMessage();
	protected TinyNetStringMessage stringMsg = new TinyNetStringMessage();

	protected override void AwakeVirtual() {
		base.AwakeVirtual();

		TinyNetScene.createPlayerAction = CreatePlayerAndAdd;
		TinyNetClient.OnClientReadyEvent = OnClientReady;
	}

	void CreatePlayerAndAdd(TinyNetConnection conn, int playerId) {
		conn.SetPlayerController<ExamplePlayerController>(new ExamplePlayerController((short)playerId, conn));
	}

	public override void StartServer() {
		base.StartServer();

		if (serverManager != null && serverManager.isRunning) {
			ServerChangeScene("MainScene");
		}
	}

	public void StartSinglePlayer() {
		
	}

	public override void ClientConnectTo(string hostAddress, int hostPort) {
		base.ClientConnectTo(hostAddress, hostPort);
	}

	public override void RegisterMessageHandlersServer() {
		base.RegisterMessageHandlersServer();

		serverManager.RegisterHandlerSafe(TinyNetMsgType.Highest + 1, OnPlayerNameReceive);
	}

	protected void OnClientReady() {
		clientManager.RequestAddPlayerControllerToServer();

		//Remember this only works cos we only have 1 player per connection in this demo.
		stringMsg.msgType = TinyNetMsgType.Highest + 1;
		stringMsg.value = System.Environment.UserName;

		TinyNetClient.instance.connToHost.Send(stringMsg, LiteNetLib.DeliveryMethod.ReliableOrdered);
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

