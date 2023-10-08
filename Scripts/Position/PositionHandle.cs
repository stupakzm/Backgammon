using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionHandle : PositionBase {

    private List<ChipBase> chipsList = new List<ChipBase>();
    private Vector3[] positions;
    private float defaultOffset = 0.35f;
    private float offset;//depends on chips count
    private float minOffset;//offseting chips at position from minOffset
    private float maxOffset;//to maxOffset
    [SerializeField] private bool faceingPositive;
    private bool canSetStack;

    private Game game;
    [SerializeField] private GameObject Visual;

    private void Awake() {
        minOffset = transform.position.z;
        SetPositions();//setting default 5 positions for chips
        player = Player.Empty;

    }
    private void Start() {
        game = GetComponentInParent<Game>();
        UnvisualizePosition();

        if (faceingPositive)
            maxOffset = defaultOffset*5 + transform.position.z;
        else 
            maxOffset = transform.position.z - defaultOffset * 5;

    }

    public void SubscribeToOnChipButon() {
        game.OnPositionButon += Game_OnPositionButon;
    }

    public void UnsubscribeToOnChipButon() {
        game.OnPositionButon -= Game_OnPositionButon;
    }

    private void Game_OnPositionButon(object sender, EventArgs e) {
        List<PositionBase> posiblePositionsToMove = game.GetPosiblePositionsToMoveChipLast();//getting possible position from game
        bool canMove = true;
        if (posiblePositionsToMove != null) {
            canMove = posiblePositionsToMove.Contains(this);
        }

        if (game.currentChip != null && canMove) {
            if (chipsList.Count == 0) {
                AddChip(game.currentChip);
                player = game.currentChip.GetPlayerState();
            }
            else if (player == game.currentChip.GetPlayerState()) {
                AddChip(game.currentChip);
            }
            else {
                game.currentChip.DeselectChipVisual();
            }
        }
        else {
            game.currentChip?.DeselectChipVisual();
        }
        
    }

    private void HandleStackChipAdd(ChipBase chipToSetPosition) {
        canSetStack = true;
        //if less than 5, set on fixed position
        if (chipsList.Count <= 5) {
            canSetStack = false;

            chipToSetPosition.MoveToTargetSmooth(positions[chipsList.Count-1]);
        }
        if(canSetStack) 
            SetPositionStackChip();
    }

    private void HandleStackChipRemove() {
        canSetStack = true;
        if (chipsList.Count == 0) {
            player = Player.Empty;
            return;
        }

        if (chipsList.Count <= 5) {//if < that 5 set to default preset positions
            for (int i = 0; i < chipsList.Count; i++) {
                chipsList[i].MoveToTargetSmooth(positions[i]);
            }
            canSetStack = false;
           
        }
        if(canSetStack)
            SetPositionStackChip();
    }
    private void SetPositionStackChip() {//when more than 5 chips
        float newLocalPosition = minOffset;//need to reset variable??
        if (faceingPositive) {
            offset = (maxOffset - minOffset) / chipsList.Count;// calculating new offset based on chips count
            foreach (ChipBase chip in chipsList) {
                chip.MoveToTargetSmooth(new Vector3(transform.position.x, 0, newLocalPosition));
                newLocalPosition += offset;
            }
        }
        else {
            offset = (minOffset - maxOffset) / chipsList.Count;
            foreach (ChipBase chip in chipsList) {
                chip.MoveToTargetSmooth(new Vector3(transform.position.x, 0, newLocalPosition));
                newLocalPosition -= offset;
            }
        }
    }

    public void AddChip(ChipBase chipToAdd){
        chipToAdd.GetCurrentPosition()?.RemoveChip(chipToAdd);
        chipsList.Add(chipToAdd);
        chipToAdd.SetCurrentPosition(this);
        chipToAdd.DeselectChipVisual();
        HandleStackChipAdd(chipToAdd);
    }

    public void RemoveChip(ChipBase chipToRemove) {
        chipsList.Remove(chipToRemove);
        HandleStackChipRemove();
    }

    private void SetPositions() {
        positions = new Vector3[5];
        float setPos = minOffset;
        if (faceingPositive) {
            for (int i = 0; i < 5; i++) {
                positions[i] = new Vector3(transform.position.x, 0, setPos);
                setPos += defaultOffset;
            }
        }
        else {
            for (int i = 0; i < 5; i++) {
                positions[i] = new Vector3(transform.position.x, 0, setPos);
                setPos -= defaultOffset;
            }
        }
    }

    public override void ResetPosition() {
        chipsList.Clear();
    }

    public override void VisualizePosition() {
        Visual.SetActive(true);
    }

    public override void UnvisualizePosition() {
        Visual.SetActive(false);
    }

    public List<ChipBase> GetChipList() {
        return chipsList;
    }
}
