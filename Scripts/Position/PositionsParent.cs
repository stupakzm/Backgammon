using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public class PositionsParent : MonoBehaviour {

    [SerializeField] private BoxCollider[] positions;
    private List<PositionBase> reversePositionBlack;
    private List<PositionBase> reversePositionWhite;

    private void Awake() {
        SetPositionStates();
        SetPositionBlack();
        SetPositionWhite();
    }

    public void SetPositionStates() {
        for (int i = 0; i < positions.Length; i++) {
            if (i > 11 && i < 18) {
                positions[i].gameObject.GetComponent<PositionBase>().state = PositionState.WhiteLastPosition;
            }
            else if (i > 5 && i < 12) {
                positions[i].gameObject.GetComponent<PositionBase>().state = PositionState.BlackLastPosition;
            }
            else {
                positions[i].gameObject.GetComponent<PositionBase>().state = PositionState.RegularPosition;
            }
        }
    }

    public void PositionsEnable() {
        for (int i = 0; i < positions.Length - 2; i++) {//two last it`s home white and home black
            positions[i].enabled = true;
        }
    }

    public void PositionsDisable() {
        foreach (var p in positions) {
            p.enabled = false;
            var position = p.gameObject.GetComponent<PositionBase>();
            position.UnvisualizePosition();
        }
    }

    private void SetPositionWhite() {
        reversePositionWhite = new List<PositionBase>();
        var tempIndex = 12;
        reversePositionWhite.Add(positions[positions.Length - 2].gameObject.GetComponent<PositionBase>());
        for (var i = 0; i < 24; i++) {
            if (i <= 11) {
                reversePositionWhite.Add(positions[tempIndex + i].gameObject.GetComponent<PositionBase>());
                continue;
            }
            reversePositionWhite.Add(positions[--tempIndex].gameObject.GetComponent<PositionBase>());
        }
    }

    private void SetPositionBlack() {
        reversePositionBlack = new List<PositionBase>();
        var tempIndex = 11;
        //Debug.Log(positions.Length);
        reversePositionBlack.Add(positions[positions.Length - 1].gameObject.GetComponent<PositionBase>());
        for (var i = 0; i < 24; i++) {
            if (i <= 11) {//need to add with different index
                reversePositionBlack.Add(positions[tempIndex--].gameObject.GetComponent<PositionBase>());
                continue;
            }
            reversePositionBlack.Add(positions[i].gameObject.GetComponent<PositionBase>());
        }
    }

    public List<PositionBase> GetPositionWhite() {
        return reversePositionWhite;
    }

    public List<PositionBase> GetPositionBlack() {
        return reversePositionBlack;
    }
}