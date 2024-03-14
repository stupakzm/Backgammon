using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CubeSelectedHandler : MonoBehaviour {//Handels in banner - Select cubes

    [SerializeField] private CubeSelected firstCube;
    [SerializeField] private CubeSelected secondCube;
    [SerializeField] private GameObject SelectButton;
    [SerializeField] private Game game;
    [SerializeField] private TextMeshProUGUI cubesSelectedText;
    private bool canChangeFirstCube = true;

    private void Start() {
        DisableButton();
    }

    public void SetCubeSelectedState(Cube state) {
        if (canChangeFirstCube) {
            firstCube.SetState(state);
        }
        else {
            secondCube.SetState(state);
        }
        if (secondCube.GetState() != 7 && firstCube.GetState() != 7) {
            SelectButton.SetActive(true);
            cubesSelectedText.text = "Click on green to save";
        }
        canChangeFirstCube = !canChangeFirstCube;
    }

    public void DisableButton() {
        SelectButton.SetActive(false);
    }

    public void GetSelectedCubes() {//on button click accept selected cubes
        game.DisableMainBanner();
        game.SetSelectedCubes(firstCube.GetState(), secondCube.GetState());
    }
}