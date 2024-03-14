using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HomePositionHandler : PositionBase {

    private List<HomePosition> positions = new List<HomePosition>();
    [SerializeField] private float offset;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private float rotation;
    private int winCountChips;
    [SerializeField] private GameObject Visual;
    private BoxCollider boxCollider;

    private void Awake() {
        boxCollider = GetComponent<BoxCollider>();
    }

    public override bool AddChipToHomeWin(ChipBase chipToAdd) {//true - if in list of HomePosition called positions last one position.chip != null; if not null - false
        foreach (var position in positions) {
            if (position.chip == null) {//if null -> set to pos in HomePosition
                chipToAdd.ButtonFalse();
                chipToAdd.DeselectChipVisual();
                chipToAdd.GetCurrentPosition().RemoveChip(chipToAdd);
                position.chip = chipToAdd;
                position.chip.MoveToHomeSmooth(position.position, rotation);
                if (positions[winCountChips-1].chip != null) {
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    public bool AddChipToHomeWinSetCubes(ChipBase chipToAdd, int indexCurentChipPos) {//true - if last chip & playr won; false if not last chip
        foreach (HomePosition position in positions) {
            if (position.chip == null) {//if null -> set to pos in HomePosition
                chipToAdd.ButtonFalse();
                chipToAdd.DeselectChipVisual();
                chipToAdd.GetCurrentPosition().RemoveChip(chipToAdd);
                
                position.chip = chipToAdd;
                position.chip.MoveToHomeSmooth(position.position, rotation);
                if (positions[winCountChips - 1].chip != null) {
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    public void SetCountOfChips(int count) {
        winCountChips = count;
    }


    public override void ResetPosition() {
        positions?.Clear();
        SetPositions();
    }

    private void SetPositions() {
        Vector3 positionToSet = startPosition;
        for (int i = 0; i < winCountChips; i++) {
            positions.Add(new HomePosition(positionToSet));
            positions[i].position = positionToSet;
            positionToSet += new Vector3(0, 0, offset);
        }
    }

    public override void VisualizePosition() {
        Visual.SetActive(true);
        boxCollider.enabled = true;
    }

    public override void UnvisualizePosition() {
        Visual.SetActive(false);
        boxCollider.enabled = false;
    }
}

public class HomePosition {
    public Vector3 position;
    public ChipBase chip;

    public HomePosition(Vector3 position)
    {
        this.position = position;
    }
}