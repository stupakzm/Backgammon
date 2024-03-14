using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GameSequence
{
    public Player PlayerMadeMove;
    public int FromPositionIndex;
    public int ToPositionIndex;
    public int Cube1;
    public int Cube2;
    public int ChipIndex;
    public int PareIndex;
    public bool TookFromHead;

    public GameSequence(Player PlayerMadeMove, int FromPositionIndex, int ToPositionIndex, int Cube1, int Cube2, int ChipIndex, int PareIndex, bool TookFromHead)
    {
        this.PlayerMadeMove = PlayerMadeMove;
        this.FromPositionIndex = FromPositionIndex;
        this.ToPositionIndex = ToPositionIndex;
        this.Cube1 = Cube1;
        this.Cube2 = Cube2;
        this.ChipIndex = ChipIndex;
        this.PareIndex = PareIndex;
        this.TookFromHead = TookFromHead;
    }

    public override string ToString() {
        return String.Format("{0} - playerMadeMove, {1} - fromPos, {2} - toPos, {3} - cube1, {4} - cube2, {5} - chipIndex, {6} - pareIndex, {7} - tookFromHead", PlayerMadeMove, FromPositionIndex, ToPositionIndex, Cube1, Cube2, ChipIndex, PareIndex, TookFromHead);
    }
}
