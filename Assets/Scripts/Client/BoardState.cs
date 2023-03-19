using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BoardState
{
    WaitingForPlayer,
    OurTurn,
    EnemyTurn,
    WeWon,
    EnemyWon,
    NotResolved,
    Draw
}
