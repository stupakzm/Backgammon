using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainBannerController : MonoBehaviour {

    [Header("SetGamesMode")]
    [SerializeField] private GameObject SetGameMode;
    [SerializeField] private TextMeshProUGUI massageInSelectPlayMode;//aka won player
    [Header("ChooseCubes")]
    [SerializeField] private GameObject ChooseCubes;
    [SerializeField] private TextMeshProUGUI cubesToSelectText;
    [SerializeField] private TextMeshProUGUI cubesSelectedText;
    [Header("Settings")]
    [SerializeField] private TextMeshProUGUI GeneratorState;
    [SerializeField] private TextMeshProUGUI PointsToWinWhite;
    [SerializeField] private TextMeshProUGUI PointsToWinBlack;
    [SerializeField] private GameObject GeneratorStateGameObject;
    [SerializeField] private GameObject SettingsMode;
    [SerializeField] private GameObject GeneratorOnlyToggle;
    [Header("Other")]
    [SerializeField] private Game game;
    [SerializeField] private Generator Generator;
    [SerializeField] private GameObject ShowRulesButton;


    public void DisbleBanner() {
        gameObject.SetActive(false);
    }

    public void ActivateSellectGameMode() {
        gameObject.SetActive(true);
        SetGameMode.SetActive(true);
        ChooseCubes.SetActive(false);
        Generator.DisableGameObject();
        SettingsMode.SetActive(false);
        ShowRulesButton.SetActive(true);
    }

    public void ActivateChooseCubesMode(Player playerState) {
        ShowRulesButton.SetActive(false);
        gameObject.SetActive(true);
        if(GameSettings.Generator == true) Generator.EnableGameObject();
        else Generator.DisableGameObject();
        SetGameMode.SetActive(false);
        ChooseCubes.SetActive(true);
        SettingsMode.SetActive(false);
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
        ShowRulesButton.SetActive(true);
    }

    public void ActivateSettingsMode() {
        ShowRulesButton.SetActive(true);
        gameObject.SetActive(true);
        SetGameMode.SetActive(false);
        ChooseCubes.SetActive(false);
        Generator.DisableGameObject();
        SettingsMode.SetActive(true);
        if (game.GetGameRules() == GameRules.SetCubes) {
            GeneratorStateGameObject.SetActive(false);
            GeneratorOnlyToggle.SetActive(true);
        }
        else {
            GeneratorOnlyToggle.SetActive(false);
            GeneratorStateGameObject.SetActive(true);
        }
        UpdateText();
    }

    public void UpdateText() {
        string state = GameSettings.Generator == true ? "enabled" : "disabled";
        GeneratorState.text = "Current state - " + state;

        PointsToWinWhite.text = "Points to win for White - " + game.pointsToWinWhite;
        PointsToWinBlack.text = "Points to win for Black - " + game.pointsToWinBlack;
    }

    public void ButtonCloseSettings() {
        if (GameSettings.Generator == true && game.GetGameRules() != GameRules.SetCubes) {
        Generator.EnableGameObject();
        }
        SettingsMode.SetActive(false);
        gameObject.SetActive(false);
    }

    public void OnValueChangedGeneratorOnlyToggle() {
        game.SetGeneratorOnlyMode(GeneratorOnlyToggle.GetComponent<Toggle>().isOn);
    }
}
    
