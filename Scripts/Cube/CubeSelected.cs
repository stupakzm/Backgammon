using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeSelected : MonoBehaviour {

    private Cube cubeState;
    [SerializeField] private GameObject[] dots;

    public void SetState(Cube cubeState) {
        this.cubeState = cubeState;
        VisualizeState();
    }

    public int GetState() {
        return (int)cubeState;
    }

    public void VisualizeState() {
        GetDotIndexes index = new GetDotIndexes();
        List<int> indexes = index.GetIndexes(cubeState);
        for (int i = 0; i < dots.Length; i++) {
            if (indexes.Contains(i)) {
                dots[i].SetActive(true);
            }
            else {
                dots[i].SetActive(false);
            }
        }
    }

    public void ChangeTransparency(float transparentFloat) {
        Color transparent;
        var image = GetComponent<Image>();
        transparent = image.color;
        transparent.a = transparentFloat;
        image.color = transparent;
        transparent = dots[0].GetComponentInChildren<Image>().color;
        transparent.a = transparentFloat;
        foreach (GameObject dot in dots) {
            dot.GetComponentInChildren<Image>().color = transparent;
        }
    }
}
