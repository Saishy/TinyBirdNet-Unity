using System.Collections;
using System.Collections.Generic;
using TinyBirdNet;
using UnityEngine;

public class Pawn : TinyNetBehaviour {

	[TinyNetSyncVar]
	int TestInt { get; set; }

	string _playerName;
	[TinyNetSyncVar]
	string PlayerName { get { return _playerName; } set { _playerName = value; } }
}
