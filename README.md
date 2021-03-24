# TinyBirdNet
A high level API for making networking games in Unity, utilizes [RevenantX/LiteNetLib](https://github.com/RevenantX/LiteNetLib)

The goal of TinyBirdNet is to create an environment where you can easily add networking to unity games, while using the LiteNetLib behind the scenes.

## Features
- Deals with lots of common problems like readying, scene changes, player owned objects.
- All code available, no obfuscation, no dlls.
- Free for everyone!
- No additional charges for CCU, bandwidth, nothing, it's yours to use.

## Latest Versions:
- 2.0.0.a1
	- Added server tick.
	- Changed component system to use Interfaces, no need to extend from a specific class anymore.
	- Dirty flags are dynamically sized up to 64 different automatic synced variables.
	- State updates are now packed into a single message.
	- ToDo:
		- Client-Side prediction.
		- Lag compensation.
		- Add emoji communication.
		- Update wiki and documentation
- 1.1.3
	- Added documentation to most parts of the code.
	- Fixed minor typos.

**Supports Unity 2017 and above**

Current version has been tested across Windows and Linux, please open an issue if you find any bugs.

![Two players connected](https://i.imgur.com/pQJhZEd.png)


## How To Use

Adding the TinyBirdNet folder under Assets to your project will give you everything needed to start networking on your game.

Inside the Examples folder you will find a simple working demo of a networked game.

You will need a `TinyNetGameManager` or derived instance always enabled on your game, so please add one to your first scene and mark it as [Don't Destroy on Load](https://docs.unity3d.com/ScriptReference/Object.DontDestroyOnLoad.html).
An unique "ConnectKey" is required for your game to be able to connect, preferably include the current version of your application on the key string.

The recommended workflow is to create a class derived from `TinyNetPlayerController` and implement your player control logic there, for each new player on the game a new `TinyNetPlayerController` is spawned. You are able to send inputs between the client Player Controllers and the server one by using an `TinyNetInputMessage`.

Each `GameObject` that you wish to include network code must contain a single `TinyNetIdentity`, after that, classes that wish to include networking code must either implement `ITinyNetComponent` or you can make it a child of `TinyNetBehaviour` to gain access to many features like `[TinyNetSyncVar]` and RPC methods.

By creating classes derived from `TinyNetBehaviour` you are able to enjoy the automatic serialization and deserialization, though manual serialization/deserialization and use of `ITinyNetMessage` are still possible.

You are able to sync up to 64 Properties per `TinyNetBehaviour` by using the `[TinyNetSyncVar]` Attribute.
You may at any time bypass this limit by implementing your own `TinySerialize` and `TinyDeserialize` Methods.

You are able to spawn and destroy objects in the network by merely calling `TinyNetServer.instance.SpawnObject` and `TinyNetServer.instance.DestroyObject` on a server, given a valid GameObject that contains a `TinyNetIdentity`.

Remember to register all prefabs that have `TinyNetIdentity` on your `TinyNetGameManager`, manually or by clicking the __Register all TinyNetIdentity prefabs__ button on your `TinyNetGameManager` inspector.


#### Please see the Wiki and Documentation for more information.
