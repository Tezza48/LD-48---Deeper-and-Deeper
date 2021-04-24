using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyState : GridEntity
{
    public bool isLayerLocked;

    public enum State
    {
        Idle,
        Wonder,
        Follow,
    }

    public State state;
}
