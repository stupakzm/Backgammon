using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionBase : MonoBehaviour {

    public Player player { get; set; }
    public PositionState state { get; set; }

    public virtual void ResetPosition() { }

    public virtual void VisualizePosition() { }

    public virtual void UnvisualizePosition() { }

    public virtual bool AddChipToHomeWin(ChipBase chipToAdd) { return false; }
}