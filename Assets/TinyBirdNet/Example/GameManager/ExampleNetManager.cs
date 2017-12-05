using TinyBirdNet;
using TinyBirdNet.Messaging;
using UnityEngine;

class ExampleNetManager : TinyNetGameManager {

	protected TinyNetShortMessage shortMessage = new TinyNetShortMessage();

	protected override void AwakeVirtual() {
		base.AwakeVirtual();

		TinyNetScene.createPlayerAction = CreatePlayerAndAdd;
		TinyNetClient.OnClientReadyEvent = OnClientReady;
	}

	void CreatePlayerAndAdd(TinyNetConnection conn, int playerId) {
		conn.SetPlayerController<ExamplePlayerController>(new ExamplePlayerController((short)playerId));
	}

	public override void ClientConnectTo(string hostAddress, int hostPort) {
		base.ClientConnectTo(hostAddress, hostPort);

		ServerChangeScene("MainScene");
	}

	public override void RegisterMessageHandlersServer() {
		base.RegisterMessageHandlersServer();

		serverManager.RegisterHandlerSafe(TinyNetMsgType.SpawnPlayer, OnPawnRequestMessage);
	}

	protected void OnClientReady() {
		clientManager.RequestAddPlayerControllerToServer();
	}

	protected void OnPawnRequestMessage(TinyNetMessageReader netMsg) {
		netMsg.ReadMessage(shortMessage);

		ExamplePawn newPawn = Instantiate(GameManager.instance.pawnPrefab, SpawnPointManager.GetSpawnPoint(), Quaternion.identity);
		newPawn.ownerPlayerControllerId = shortMessage.value;

		serverManager.SpawnWithClientAuthority(newPawn.gameObject, netMsg.tinyNetConn);
	}
}

