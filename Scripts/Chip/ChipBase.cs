using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChipBase : MonoBehaviour
{
    protected bool canMove = false;
    protected bool canSelect;
    protected float targetRotationX;

    [SerializeField] protected GameObject ButtonObject;
    [SerializeField] protected GameObject UnsellectButtonObject;

    [SerializeField] protected Player player;

    protected Vector3 target;

    protected PositionHandle currentPosition;

    protected Game game;


    protected void SelectedChipVisual() {
        if (canSelect) {
            transform.localScale *= 1.1f;
            canSelect = false;
        }
    }

    public void DeselectChipVisual() {
        if (!canSelect) {
            transform.localScale /= 1.1f;
            canSelect = true;
        }
    }

    public void ButtonTrue() {
        ButtonObject.SetActive(true);
    }

    public void ButtonFalse() {
        ButtonObject.SetActive(false);
    }

    public void MoveToTarget(Vector3 target) {
        transform.position = target;//simple way to move
    }

    public void MoveToTargetSmooth(Vector3 target) {
        canMove = true;//bool because smooth movement performed in Update that 
        this.target = target;
    }

    public void MoveToHomeSmooth(Vector3 target, float rotationX) {
        canMove = true;
        targetRotationX = rotationX;
        this.target = target;
    }

    public void SetGame(Game game) {
        this.game = game;//need because spaw above game, so impossible to GetComponentInParent
    }

    public void SetCurrentPosition(PositionHandle position) {
        currentPosition = position;
    }

    public PositionHandle GetCurrentPosition() {
        return currentPosition;
    }
    public Player GetPlayerState() {
        return player;
    }

    public void Restart() {
        targetRotationX = 0;
    }
}
