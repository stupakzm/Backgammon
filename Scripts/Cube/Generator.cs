using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    public CubeSelected[] Cubes; //change to gameObjcet
    [SerializeField] private Game game;
    [SerializeField] private MainBannerController MainBanner;
    private Button Button;
    private Image Image;
    private TextMeshProUGUI Text;
    private GameRules gameRules; 

    private void Awake() {
        Button = GetComponent<Button>();
        Image = GetComponent<Image>();
        Text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnButtonClick() {
        gameRules = game.GetGameRules();
        game.ChipButtonsDisableBoth();
        StartCoroutine("RollTheDice");
        MainBanner.DisbleBanner();
        Cubes[0].SetState((Cube)7);
        Cubes[1].SetState((Cube)7);


        Cubes[0].gameObject.SetActive(true);
        Cubes[1].gameObject.SetActive(true);
        if (gameRules == GameRules.SetCubes) {
            DisableGameObject();//one generation at a time
        } 
        else DisableButton();
    }

    // Coroutine that rolls the dice
    private IEnumerator RollTheDice() {
        int randomDiceSide = 1;

        // Final side or value that dice reads in the end of coroutine
        int finalSide1 = 0;
        int finalSide2 = 0;

        // Loop to switch dice sides ramdomly
        for (int i = 0; i <= 10; i++) {
            randomDiceSide = Random.Range(1, 7);

            // Set state according to random value
            Cubes[0].SetState((Cube)randomDiceSide);

            // Pause before next itteration
            yield return new WaitForSeconds(0.1f);
        }

        finalSide1 = randomDiceSide;

        for (int i = 0; i <= 10; i++) {
            randomDiceSide = Random.Range(1, 7);

            Cubes[1].SetState((Cube)randomDiceSide);

            // Pause before next itteration
            yield return new WaitForSeconds(0.1f);
        }

        finalSide2 = randomDiceSide;

        if (gameRules == GameRules.SetCubes) {
            game.SetSelectedCubes(finalSide1, finalSide2);
        }
        else {
            game.SetSelectedCubes(finalSide1, finalSide2);
            EnableButton();
        }
    }

    public void SetCubesPosition() {
        gameObject.transform.localPosition = new Vector3(150, -25, gameObject.transform.localPosition.z);
    }

    public void NotSetCubesPosition() {
        gameObject.transform.localPosition = new Vector3(100, 6, gameObject.transform.localPosition.z);
    }

    private void DisableButton() {
        Button.enabled = false;
    }

    private void EnableButton() {
        Button.enabled = true;
    }

    public void DisableGameObject() {
        Button.enabled = false;
        Image.enabled = false;
        Text.enabled = false;
    }

    public void EnableGameObject() {
        Button.enabled = true;
        Image.enabled = true;
        Text.enabled = true;
    }
}
