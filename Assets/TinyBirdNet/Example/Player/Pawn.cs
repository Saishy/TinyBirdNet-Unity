using System.Collections;
using System.Collections.Generic;
using TinyBirdNet;
using UnityEngine;

public class Pawn : TinyNetBehaviour {

	[TinyNetSyncVar]
	int TestInt { get; set; }

	private int _testInt2 = 2;
	[TinyNetSyncVar]
	int TestIntWithoutSet { get { return _testInt2; } }

	private int _testInt3 = 3;
	[TinyNetSyncVar]
	int TestIntWithoutGet { set { _testInt3 = value; } }
}
