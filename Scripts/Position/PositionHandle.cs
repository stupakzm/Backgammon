using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Device;

public class PositionHandle : PositionBase {

    private List<ChipBase> chipsList = new List<ChipBase>();
    private Vector3[] positions;
    private Vector3[] positionsForSimulation;
    private float defaultOffset = 0.35f;
    private float defaultOffsetSimulation = 28f;//was 36.5f
    private float offset;//depends on chips count
    private float offsetSimulation;//depends on chips count
    private float minOffset;//offseting chips at position from minOffset
    private float minOffsetSimulation;//offseting chips at position from minOffset
    private float maxOffset;//to maxOffset
    private float maxOffsetSimulation;//to maxOffset
    [SerializeField] private bool faceingPositive;
    private bool canSetStack;

    [SerializeField] private GameObject Visual;

    private void Awake() {
        minOffset = transform.position.z;
        minOffsetSimulation = transform.position.y;
        SetPositions();//setting default 5 positions for chips
        player = Player.Empty;

    }

    private void Start() {
        UnvisualizePosition();

        float offsetDependsOnScreen = (UnityEngine.Screen.currentResolution.height / defaultOffsetSimulation);
        if (faceingPositive) {
            maxOffset = defaultOffset * 5 + transform.position.z;
            maxOffsetSimulation = offsetDependsOnScreen * 5 + transform.position.y;
        }
        else {
            maxOffset = transform.position.z - defaultOffset * 5;
            maxOffsetSimulation = transform.position.y - offsetDependsOnScreen * 5;
        }

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
            if (!game.isSimulationOn) {
                chipToSetPosition.MoveToTargetSmooth(positions[chipsList.Count - 1]);
            }
            else {
                chipToSetPosition.MoveToTargetSmooth(positionsForSimulation[chipsList.Count - 1]);
            }
        }
        if (canSetStack) {
            SetPositionStackChip();
        }
    }

    private void HandleStackChipRemove() {
        canSetStack = true;
        if (chipsList.Count == 0) {
            player = Player.Empty;
            return;
        }

        if (chipsList.Count <= 5) {//if < that 5 set to default preset positions
            if (!game.isSimulationOn) {
                for (int i = 0; i < chipsList.Count; i++) {
                    chipsList[i].MoveToTargetSmooth(positions[i]);
                }
            }
            else {
                for (int i = 0; i < chipsList.Count; i++) {
                    chipsList[i].MoveToTargetSmooth(positionsForSimulation[i]);
                }
            }
            canSetStack = false;

        }
        if (canSetStack)
            SetPositionStackChip();
    }
    private void SetPositionStackChip() {//when more than 5 chips
        if (game.isSimulationOn) {
            float newLocalPosition = minOffsetSimulation;//need to reset variable
            if (faceingPositive) {
                offsetSimulation = (maxOffsetSimulation - minOffsetSimulation) / chipsList.Count;// calculating new offset based on chips count
                foreach (ChipBase chip in chipsList) {
                    chip.MoveToTargetSmooth(new Vector3(transform.position.x,newLocalPosition, 0));
                    //Debug.Log(newLocalPosition + " - newLocalPosition in faceingPositive - method [SetPositionStackChip]");
                    newLocalPosition += offsetSimulation;
                }
            }
            else {
                offsetSimulation = (minOffsetSimulation - maxOffsetSimulation) / chipsList.Count;
                foreach (ChipBase chip in chipsList) {
                    chip.MoveToTargetSmooth(new Vector3(transform.position.x,newLocalPosition, 0));
                    //Debug.Log(newLocalPosition + " - newLocalPosition in !faceingPositive - method [SetPositionStackChip]");
                    newLocalPosition -= offsetSimulation;
                }
            }
        }
        else {
            float newLocalPosition = minOffset;//need to reset variable
            if (faceingPositive) {
                offset = (maxOffset - minOffset) / chipsList.Count;// calculating new offset based on chips count
                foreach (ChipBase chip in chipsList) {
                    chip.MoveToTargetSmooth(new Vector3(transform.position.x, transform.position.y, newLocalPosition));
                    newLocalPosition += offset;
                }
            }
            else {
                offset = (minOffset - maxOffset) / chipsList.Count;
                foreach (ChipBase chip in chipsList) {
                    chip.MoveToTargetSmooth(new Vector3(transform.position.x, transform.position.y, newLocalPosition));
                    newLocalPosition -= offset;
                }
            }
        }
    }

    public void AddChip(ChipBase chipToAdd) {
        chipToAdd.GetCurrentPosition()?.RemoveChip(chipToAdd);
        if (chipsList.Count == 0) {
            player = chipToAdd.GetPlayerState();
        }
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
            for (int i = 0; i < positions.Length; i++) {
                positions[i] = new Vector3(transform.position.x, transform.position.y, setPos);
                //positions[i] = new Vector3(transform.position.x, 0, setPos);
                setPos += defaultOffset;
            }
        }
        else {
            for (int i = 0; i < positions.Length; i++) {
                positions[i] = new Vector3(transform.position.x, transform.position.y, setPos);
                //positions[i] = new Vector3(transform.position.x, 0, setPos);
                setPos -= defaultOffset;
            }
        }

        positionsForSimulation = new Vector3[5];
        float setPosSimulation = minOffsetSimulation;
        float offsetDependsOnScreen = UnityEngine.Screen.currentResolution.height / defaultOffsetSimulation;
        if (faceingPositive) {
            for (int i = 0; i < positionsForSimulation.Length; i++) {
                positionsForSimulation[i] = new Vector3(transform.position.x, setPosSimulation, 0);
                //positionsForSimulation[i] = new Vector3(transform.position.x, transform.position.y + setPosSimulation, 0);
                setPosSimulation += offsetDependsOnScreen;
            }
        }
        else {
            for (int i = 0; i < positionsForSimulation.Length; i++) {
                positionsForSimulation[i] = new Vector3(transform.position.x, setPosSimulation, 0);
                //positionsForSimulation[i] = new Vector3(transform.position.x, transform.position.y + setPosSimulation, 0);
                setPosSimulation -= offsetDependsOnScreen;
            }
        }

    }

    public override void ResetPosition() {
        chipsList.Clear();
        player = Player.Empty;
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

    public int GetChipListCount() {
        return chipsList.Count;
    }
}
