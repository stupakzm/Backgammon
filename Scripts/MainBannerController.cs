using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainBannerController : MonoBehaviour {

    [SerializeField] private GameObject SetGameMode;
    [SerializeField] private GameObject ChooseCubes;
    [SerializeField] private TextMeshProUGUI cubesToSelectText;
    [SerializeField] private TextMeshProUGUI cubesSelectedText;
    [SerializeField] private TextMeshProUGUI massageInSelectPlayMode;//aka won player
    [SerializeField] private Generator Generator;


    public void DisbleBanner() {
        gameObject.SetActive(false);
    }

    public void ActivateSellectGameMode() {
        gameObject.SetActive(true);
        SetGameMode.SetActive(true);
        ChooseCubes.SetActive(false);
        Generator.DisableGameObject();
    }

    public void ActivateChooseCubesMode(Player playerState) {
        this.gameObject.SetActive(true);
        Generator.EnableGameObject();
        SetGameMode.SetActive(false);
        ChooseCubes.SetActive(true);
        var CubeSelectedHandler = ChooseCubes.GetComponentInChildren<CubeSelectedHandler>();
        CubeSelectedHandler.SetCubeSelectedState(Cube.Null);
        CubeSelectedHandler.SetCubeSelectedState(Cube.Null);
        CubeSelectedHandler.DisableButton();
        var playerColorText = (int)playerState == 0 ? "First Player" : "Second Player";
        cubesToSelectText.text = playerColorText + " to choose the dice";
        cubesSelectedText.text = "Selected dice";
        //change text - choose for player [playerState]
    }

    public void ActivatePlayerWonMode(string wonText) {
        ActivateSellectGameMode();
        massageInSelectPlayMode.text = wonText;
    }

   
}
    
