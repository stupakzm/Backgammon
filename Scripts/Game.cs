using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Playables;
using System.ComponentModel.Design;
using System.Reflection;
using System.Linq;

public class Game : MonoBehaviour {

    public event EventHandler OnPositionButon; //an event that executes code from PositionHandle.cs, but is called by Game.cs

    public event EventHandler OnChipButon;//an event that executes code from ChipBlack.cs or ChipWhite.cs, but is called by Game.cs

    private List<ChipBase> chipsWhite = new List<ChipBase>();
    private List<ChipBase> chipsBlack = new List<ChipBase>();

    [SerializeField] private GameObject chipWhite;//prefab
    [SerializeField] private GameObject chipBlack;
    [SerializeField] private GameObject DeselectButton;
    [SerializeField] private CubeSelected CubeVisual1;
    [SerializeField] private CubeSelected CubeVisual2;
    [SerializeField] private Generator Generator;
    private bool isEnableCubeVisuals = true;//using in method DisableCubeVisuals to make cubes transparent
    private int previousPositionIndex;
    //private int currentPositionIndex;
    private int pareIndex;//shows how much moves left if pare cubes
    private bool tookFromHead;//to control the number of chips taken from the head

    private PositionBase startPosWhite;
    private PositionBase startPosBlack;
    private List<PositionBase> positionWhiteReverse;
    private List<PositionBase> positionBlackReverse;
    private PositionsParent positionParent;//assigns and settles indexes for positions and gives them to Game.cs
    private List<PositionBase> listToReturn;//will be transferred to processing when you click on the position, returning in method GetPosiblePositionsToMoveChipLast()

    [SerializeField] private MainBannerController MainBanner;//is used to select the game mode, select dice and show who won


    public ChipBase currentChip { get; set; }//is saved when you click on a chip for further actions on it.(deselection and moving it to another position)
    private ChipBase previousChip;
    public PositionHandle currentPosition { get; set; }
    private PositionHandle previousPosition;//added for future modifications like undo

    private int chipsCount = 15;
    private GameRules gameRules;
    private Player PlayerToMove = (Player)1;
    private int cubeFirst;
    private int cubeLast;
    private int movesMade;
    private float freezeTimeBeforeChangePlayerToMove = 1f;

    private void Awake() {
        positionParent = GetComponentInChildren<PositionsParent>();

    }

    private void Start() {
        //banner start 
        MainBanner.ActivateSellectGameMode();
        positionParent.PositionsDisable();
        DisableButtonDeseletChip();
        InstantiateChips(chipsCount);
        positionWhiteReverse = positionParent.GetPositionWhite();
        positionBlackReverse = positionParent.GetPositionBlack();
        positionBlackReverse[0].GetComponent<HomePositionHandler>().SetCountOfChips(chipsCount);//transfer information about the number of created chips to the "home" position Black
        positionWhiteReverse[0].GetComponent<HomePositionHandler>().SetCountOfChips(chipsCount);//transfer information about the number of created chips to the "home" position White
        DisableHomeBoth();
        startPosWhite = positionWhiteReverse[positionWhiteReverse.Count - 1];//positions are counted from the end, so the starting position will be the last one
        startPosBlack = positionBlackReverse[positionBlackReverse.Count - 1];

        tookFromHead = false;//it is needed to limit the taking of chips from the "head" according to the rules
        movesMade = 0;//this variable is needed in order to move 2 chips to the sixth position on the first move when the dice roll 6 6, according to the rules. also for further updates of the game and statistics
    }

    private void Update() {
        RaycastTouch();
        RaycastMousePosition();
    }

    private void RaycastTouch() {
        for (var i = 0; i < Input.touchCount; ++i) {
            RaycastHit hitInfo;
            Ray rayOrigin;
            if (Input.GetTouch(i).phase == UnityEngine.TouchPhase.Began) {

                // Construct a ray from the current touch coordinates
                rayOrigin = Camera.main.ScreenPointToRay(Input.GetTouch(i).position);
                // Create a particle if hit
                if (Physics.Raycast(rayOrigin, out hitInfo)) {
                    // Debug.Log("Raycast hit object " + hitInfo.transform.name + " at the position of " + hitInfo.transform.position);

                    if (hitInfo.transform.gameObject.TryGetComponent(out PositionHandle position)) {
                        //Debug.Log("hiting position" + position);
                    }
                }
            }
        }
    }

    private void RaycastMousePosition() {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hitInfo;

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Ray rayOrigin = Camera.main.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(rayOrigin, out hitInfo)) {
                //Debug.Log("Raycast hit object " + hitInfo.transform.name + " at the position of " + hitInfo.transform.position);

                if (hitInfo.transform.gameObject.TryGetComponent(out PositionHandle position)) {
                    position.SubscribeToOnChipButon();
                    ButtonPosition();
                    position.UnsubscribeToOnChipButon();
                }
                if (hitInfo.transform.gameObject.TryGetComponent(out HomePositionHandler homePosition)) {
                    Debug.Log("TryGetComponent(out HomePositionHandler homePosition) - true");
                    ButtonHome();
                }
            }
        }
    }

    public void ButtonChip() {//a method that is called when you click on a chip
        previousChip?.DeselectChipVisual();
        bool gotPosiblePosition = false;

        OnChipButon?.Invoke(this, EventArgs.Empty);//currentChip is assigned here

        if (currentChip.GetPlayerState() == Player.FirstPlayer) {
            if (currentChip.GetCurrentPosition().state == PositionState.WhiteLastPosition) {
                if (IsAllChipsInLastPositionWhite()) {
                    if (gameRules == GameRules.SetCubes) {
                        GetPosiblePositionsToMoveChipSetCubesInLastPosWhite(cubeFirst, cubeLast);
                        //methods for obtaining possible positions when all the chips in the last positions are different from the usual ones, so they are separated
                    }
                    else {
                        GetPosiblePositionsToMoveChip();
                    }
                    gotPosiblePosition = true;
                }
            }
        }
        else if (currentChip.GetPlayerState() == Player.SecondPlayer) {
            if (currentChip.GetCurrentPosition().state == PositionState.BlackLastPosition) {
                if (IsAllChipsInLastPositionBlack()) {
                    if (gameRules == GameRules.SetCubes) {
                        GetPosiblePositionsToMoveChipSetCubesInLastPosBlack(cubeFirst, cubeLast);
                    }
                    else {
                        GetPosiblePositionsToMoveChip();
                    }
                    gotPosiblePosition = true;
                }
            }
        }

        //for now
        if (!gotPosiblePosition) {
            if (gameRules == GameRules.SetCubes)
                GetPosiblePositionsToMoveChipSetCubes(cubeFirst, cubeLast);
            else
                GetPosiblePositionsToMoveChip();
        }
        positionParent.PositionsEnable();//enable colliders

        //for now
        EnableButtonDeseletChip();
        currentPosition = currentChip.GetCurrentPosition();
        previousChip = currentChip;
    }

    public void ButtonPosition() {
        previousPositionIndex = PlayerToMove == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentPosition) : positionBlackReverse.IndexOf(currentPosition);
        OnPositionButon?.Invoke(this, EventArgs.Empty);
        positionParent.PositionsDisable();
        DisableButtonDeseletChip();
        //DisableHomeBoth();
        currentPosition = currentChip?.GetCurrentPosition();
        if (gameRules == GameRules.SetCubes) {
            var index = PlayerToMove == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentPosition) : positionBlackReverse.IndexOf(currentPosition);
            if (cubeFirst == cubeLast) MadeMoveInSetCubesPare(index, false);
            else MadeMoveInSetCubes(index);
        }

        previousChip = currentChip;
        currentChip = null;
    }

    private void MadeMoveInSetCubes(int currentPositionIndex) {

        int variationBetweenIndexes = previousPositionIndex - currentPositionIndex;

        if (previousPositionIndex == 24 && variationBetweenIndexes != 0) {//a rule that does not allow you to take more than one chip from the "head"
            DisableChipButtonsAtSatartPos(PlayerToMove);
            tookFromHead = true;
        }

        if (variationBetweenIndexes == (cubeFirst + cubeLast)) {
            CubeVisual1.gameObject.SetActive(false);
            CubeVisual2.gameObject.SetActive(false);
            ChipButtonsDisableBoth();
            Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
            return;
        }
        else if (variationBetweenIndexes == cubeFirst) {
            cubeFirst = 0;
            CubeVisual1.gameObject.SetActive(false);
        }
        else if (variationBetweenIndexes == cubeLast) {
            cubeLast = 0;
            CubeVisual2.gameObject.SetActive(false);
        }

        CheckingForChangePlayerToMove(cubeFirst == cubeLast);
    }

    private void MadeMoveInSetCubesHome(int currentPositionIndex) {//method when chip moved to home. called in ButtonHome()
        if (currentPositionIndex == cubeFirst) {
            cubeFirst = 0;
            CubeVisual1.gameObject.SetActive(false);
            CheckingForChangePlayerToMove(cubeFirst == cubeLast);
            return;
        }
        else if (currentPositionIndex == cubeLast) {
            cubeLast = 0;
            CubeVisual2.gameObject.SetActive(false);
            CheckingForChangePlayerToMove(cubeFirst == cubeLast);
            return;
        }
        //To deal with the fact that the dice can be larger than the index I used in the last position, I added three additional bools
        bool firstLessThanSecond = cubeFirst < cubeLast;
        bool firstLessThanIndex = currentPositionIndex > cubeFirst;
        bool lastLessThanIndex = currentPositionIndex > cubeLast;
        if (firstLessThanIndex && lastLessThanIndex) {
            Debug.LogError("firstLessThanIndex && lastLessThanIndex - true");
            return;
        }
        else if (!firstLessThanIndex && !lastLessThanIndex) {
            if (firstLessThanSecond) {
                cubeFirst = 0;
                CubeVisual1.gameObject.SetActive(false);
                CheckingForChangePlayerToMove(cubeFirst == cubeLast);
                return;
            }
            else if (!firstLessThanSecond) {
                cubeLast = 0;
                CubeVisual2.gameObject.SetActive(false);
                CheckingForChangePlayerToMove(cubeFirst == cubeLast);
                return;
            }
        }
        else if (firstLessThanIndex) {
            cubeLast = 0;
            CubeVisual2.gameObject.SetActive(false);
            CheckingForChangePlayerToMove(cubeFirst == cubeLast);
            return;
        }
        else {
            cubeFirst = 0;
            CubeVisual1.gameObject.SetActive(false);
            CheckingForChangePlayerToMove(cubeFirst == cubeLast);
            return;
        }

    }

    private void MadeMoveInSetCubesPare(int currentPositionIndex, bool isLastPos) {
        int variationBetweenIndexes;

        if (isLastPos) variationBetweenIndexes = currentPositionIndex > cubeFirst ? previousPositionIndex - currentPositionIndex : currentPositionIndex;
        else variationBetweenIndexes = previousPositionIndex - currentPositionIndex;

        if (previousPositionIndex == 24 && variationBetweenIndexes != 0) {
            DisableChipButtonsAtSatartPos(PlayerToMove);
            tookFromHead = true;
        }

        if (variationBetweenIndexes == (cubeFirst)) {
            pareIndex -= 1;
        }
        else if (variationBetweenIndexes == (cubeFirst * 2)) {
            pareIndex -= 2;
        }
        else if (variationBetweenIndexes == (cubeFirst * 3)) {
            pareIndex -= 3;
        }
        else if (variationBetweenIndexes == (cubeFirst * 4)) {
            pareIndex -= 4;
        }
        CheckingForChangePlayerToMove(cubeFirst == cubeLast);
    }

    private void MadeMoveInSetCubesPareHome(int index) {
        if (index == cubeFirst) {
            pareIndex -= 1;
            CheckingForChangePlayerToMove(cubeFirst == cubeLast);
            return;
        }
        bool cubeLessThanIndex = cubeFirst < index;
        if (!cubeLessThanIndex) {
            pareIndex -= 1;
            CheckingForChangePlayerToMove(cubeFirst == cubeLast);
        }
    }

    private void CheckingForChangePlayerToMove(bool isPare) {
        if (IsAllChipsCanNotMakeMoveInSetCubes(isPare)) {
            ChipButtonsDisableBoth();
            Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
            return;
        }
        if (isPare) {
            if (pareIndex == 0) {
                ChipButtonsDisableBoth();
                Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
            }
        }
        else {
            if (cubeFirst == 0 && cubeLast == 0) {
                ChipButtonsDisableBoth();
                Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
            }
        }
    }

    private void ChangePlayerToMove() {
        PlayerToMove = PlayerToMove == 0 ? (Player)1 : (Player)0;
        MainBanner.ActivateChooseCubesMode(PlayerToMove);
        tookFromHead = false;
        movesMade++;
        Generator.EnableGameObject();
    }

    public void ButtonHome() {
        if (currentChip == null) {
            Debug.LogError("currentChip can`t be null in ButtonHome method!");
        }
        if (currentChip.GetPlayerState() == Player.FirstPlayer) {
            var indexOfCurPos = positionWhiteReverse.IndexOf(currentPosition);
            bool isWon = positionWhiteReverse[0].AddChipToHomeWin(currentChip);
            if (isWon) {
                ChipButtonsDisableBoth();
                positionParent.PositionsDisable();
                DisableButtonDeseletChip();
                Invoke(nameof(PlayerWonGame), 1f);
                return;
            }
            if (gameRules == GameRules.SetCubes) {
                if (cubeFirst == cubeLast) MadeMoveInSetCubesPareHome(indexOfCurPos);
                else MadeMoveInSetCubesHome(indexOfCurPos);
            }
            positionWhiteReverse[0].UnvisualizePosition();
        }
        else if (currentChip.GetPlayerState() == Player.SecondPlayer) {
            var indexOfCurPos = positionBlackReverse.IndexOf(currentPosition);
            bool isWon = positionBlackReverse[0].AddChipToHomeWin(currentChip);
            if (isWon) {
                ChipButtonsDisableBoth();
                positionParent.PositionsDisable();
                DisableButtonDeseletChip();
                Invoke(nameof(PlayerWonGame), 1f);
                return;
            }
            if (gameRules == GameRules.SetCubes) {
                if (cubeFirst == cubeLast) MadeMoveInSetCubesPareHome(indexOfCurPos);
                else MadeMoveInSetCubesHome(indexOfCurPos);
            }
            positionBlackReverse[0].UnvisualizePosition();
        }
        positionParent.PositionsDisable();
        DisableButtonDeseletChip();
        currentChip = null;
    }

    private void PlayerWonGame() {
        Player winner = currentChip.GetPlayerState();
        currentChip = null;
        SetStartPositions();
        ResetHomePositions();
        MainBanner.ActivatePlayerWonMode("Winner player " + ((int)winner + 1));//this banner will have the same game selection as at the start 
    }

    public void RestartGame() {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void StartLongGameFreeAspect() {//Button
        gameRules = GameRules.FreeAspect;
        RestartGameHelper();
        ChipButtonsEnableBoth();
        CubeVisual1.gameObject.SetActive(false);
        CubeVisual2.gameObject.SetActive(false);
        MainBanner.DisbleBanner();
        Generator.NotSetCubesPosition();
        Generator.EnableGameObject();
    }

    public void StartGameFixedSix() {//Button
        gameRules = GameRules.FixedSix;
        RestartGameHelper();
        ChipButtonsEnableBoth();
        CubeVisual1.gameObject.SetActive(false);
        CubeVisual2.gameObject.SetActive(false);
        MainBanner.DisbleBanner();
        Generator.NotSetCubesPosition();
        Generator.EnableGameObject();
    }

    public void StartGameSetCubes() {//Button
        RestartGameHelper();
        gameRules = GameRules.SetCubes;
        PlayerToMove = Player.SecondPlayer;
        ChangePlayerToMove();//banner is activated here
        Generator.SetCubesPosition();
    }

    private void RestartGameHelper() {
        positionParent.PositionsDisable();
        DisableButtonDeseletChip();
        SetStartPositions();
        ResetHomePositions();
    }

    private void ResetHomePositions() {
        positionBlackReverse[0].ResetPosition();
        positionWhiteReverse[0].ResetPosition();
    }

    private void InstantiateChips(int countOfChips) {
        for (int i = 0; i < countOfChips; i++) {
            ChipBase chipFirst = Instantiate(chipWhite).GetComponent<ChipBase>();
            chipFirst.SetGame(this);
            chipsWhite.Add(chipFirst);
            ChipBase chipSecond = Instantiate(chipBlack).GetComponent<ChipBase>();
            chipSecond.SetGame(this);
            chipsBlack.Add(chipSecond);
        }
    }

    private void SetStartPositions() {
        MainBanner.DisbleBanner();
        startPosWhite.ResetPosition();
        startPosBlack.ResetPosition();
        movesMade = 0;
        for (int i = 0; i < chipsCount; i++) {
            startPosWhite.GetComponent<PositionHandle>().AddChip(chipsWhite[i]);
            startPosWhite.player = Player.FirstPlayer;
            chipsWhite[i].transform.rotation = Quaternion.Euler(0, 0, 0);
            chipsWhite[i].Restart();
            startPosBlack.GetComponent<PositionHandle>().AddChip(chipsBlack[i]);
            startPosBlack.player = Player.SecondPlayer;
            chipsBlack[i].transform.rotation = Quaternion.Euler(0, 0, 0);
            chipsBlack[i].Restart();
        }
    }

    private void DisableButtonDeseletChip() {
        DeselectButton.SetActive(false);
    }

    private void EnableButtonDeseletChip() {
        DeselectButton.SetActive(true);
    }

    private void DisableHomeBoth() {
        positionBlackReverse[0].UnvisualizePosition();
        positionWhiteReverse[0].UnvisualizePosition();
    }

    private bool IsAllChipsInLastPositionWhite() {
        foreach (var chip in chipsWhite)
            if (chip.GetCurrentPosition().state != PositionState.WhiteLastPosition)
                return false;
        return true;
    }

    private bool IsAllChipsInLastPositionBlack() {
        foreach (var chip in chipsBlack)
            if (chip.GetCurrentPosition().state != PositionState.BlackLastPosition)
                return false;
        return true;
    }

    private void ChipButtonsEnable(Player playerState) {
        if (playerState == Player.FirstPlayer) {
            foreach (var i in chipsWhite) i.ButtonTrue();
            foreach (var i in chipsBlack) i.ButtonFalse();
        }
        else if (playerState == Player.SecondPlayer) {
            foreach (var i in chipsBlack) i.ButtonTrue();
            foreach (var i in chipsWhite) i.ButtonFalse();
        }
    }
    private void ChipButtonsEnableBoth() {
        foreach (var i in chipsWhite) i.ButtonTrue();
        foreach (var i in chipsBlack) i.ButtonTrue();
    }

    public void ChipButtonsDisableBoth() {
        foreach (var i in chipsWhite) i.ButtonFalse();
        foreach (var i in chipsBlack) i.ButtonFalse();
    }

    public void ButtonDeseletChip() {//Button
        DisableHomeBoth();
        previousChip = currentChip;
        currentChip?.DeselectChipVisual();
        currentChip = null;
        DisableButtonDeseletChip();
        positionParent.PositionsDisable();//disable colliders
    }

    public List<PositionBase> GetPosiblePositionsToMoveChipLast() {
        return listToReturn;

    }

    private void DisableChipButtonsAtSatartPos(Player playerState) {//control of taking chips from the "head"
        if (playerState == Player.FirstPlayer) {
            foreach (var i in startPosWhite.GetComponent<PositionHandle>().GetChipList()) i.ButtonFalse();
        }
        else if (playerState == Player.SecondPlayer) {
            foreach (var i in startPosBlack.GetComponent<PositionHandle>().GetChipList()) i.ButtonFalse();
        }
    }


    private void GetPosiblePositionsToMoveChipSetCubes(int cube1, int cube2) {
        listToReturn = new List<PositionBase>();
        bool isPareCubes = cube1 == cube2;
        List<int> posibleIndexes = new List<int>();
        int currentPositionIndex = PlayerToMove == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition()) : positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());

        if (isPareCubes) {
            if (movesMade < 3 && cube1 == 6) {
                if (PlayerToMove == Player.FirstPlayer) {
                    var tempPositionWhiteReverse = positionWhiteReverse[18].GetComponent<PositionHandle>();
                    tempPositionWhiteReverse.AddChip(chipsWhite[0]);
                    tempPositionWhiteReverse.AddChip(chipsWhite[1]);
                    tempPositionWhiteReverse.player = Player.FirstPlayer;
                }
                else {
                    var tempPositionBlackReverse = positionBlackReverse[18].GetComponent<PositionHandle>();
                    tempPositionBlackReverse.AddChip(chipsBlack[0]);
                    tempPositionBlackReverse.AddChip(chipsBlack[1]);
                    tempPositionBlackReverse.player = Player.SecondPlayer;
                }
                Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
                return;
            }
            for (int i = 0; i < pareIndex; i++) {
                if (PlayerToMove == Player.FirstPlayer) {
                    if ((currentPositionIndex - (cube1 * (i + 1))) > 0) {
                        if (positionWhiteReverse[currentPositionIndex - (cube1 * (i + 1))].player != Player.SecondPlayer) {
                            posibleIndexes.Add(currentPositionIndex - (cube1 * (i + 1)));
                        }
                        else break;
                    }
                }
                else {
                    if ((currentPositionIndex - (cube1 * (i + 1))) > 0) {
                        if (positionBlackReverse[currentPositionIndex - (cube1 * (i + 1))].player != Player.FirstPlayer) {
                            posibleIndexes.Add(currentPositionIndex - (cube1 * (i + 1)));
                        }
                        else break;
                    }
                }
            }
        }
        else {
            int variationIndexCube1 = (currentPositionIndex - cube1) < 0 ? 0 : currentPositionIndex - cube1;
            int variationIndexCube2 = (currentPositionIndex - cube2) < 0 ? 0 : currentPositionIndex - cube2;

            if (cube1 == 0) {
                if (cube2 == 0) {
                    //cant be like this, must be changePlayerToMove
                    Debug.LogError("cube1 and cube2 are = 0");
                }
                else {//cube1 = 0 & cube2 != 0
                    if (PlayerToMove == Player.FirstPlayer) {
                        if (positionWhiteReverse[variationIndexCube2].player != Player.SecondPlayer && variationIndexCube2 > 0)
                            posibleIndexes.Add(variationIndexCube2);
                    }
                    else {
                        if (positionBlackReverse[variationIndexCube2].player != Player.FirstPlayer && variationIndexCube2 > 0)
                            posibleIndexes.Add(variationIndexCube2);
                    }
                }
            }
            else {
                if (cube2 == 0) {// cube1 != 0 & cube2 = 0
                     if (PlayerToMove == Player.FirstPlayer) {
                        if (positionWhiteReverse[variationIndexCube1].player != Player.SecondPlayer && variationIndexCube1 > 0)
                            posibleIndexes.Add(variationIndexCube1);
                    }
                    else {
                        if (positionBlackReverse[variationIndexCube1].player != Player.FirstPlayer && variationIndexCube1 > 0)
                            posibleIndexes.Add(variationIndexCube1);
                    }
                }
                else {// cube1 != 0 & cube2 != 0
                    if(variationIndexCube1 >0)
                    posibleIndexes.Add(variationIndexCube1);
                    if(variationIndexCube2 >0)
                    posibleIndexes.Add(variationIndexCube2);
                    if (PlayerToMove == Player.FirstPlayer) {
                        if (positionWhiteReverse[variationIndexCube1].player != Player.SecondPlayer)
                            if (positionWhiteReverse[variationIndexCube2].player != Player.SecondPlayer)
                                if ((currentPositionIndex - cube1 - cube2) > 0)
                                    posibleIndexes.Add(currentPositionIndex - cube1 - cube2);
                    }
                    else {
                        if (positionBlackReverse[variationIndexCube1].player != Player.FirstPlayer)
                            if (positionBlackReverse[variationIndexCube2].player != Player.FirstPlayer)
                                if ((currentPositionIndex - cube1 - cube2) > 0)
                                    posibleIndexes.Add(currentPositionIndex - cube1 - cube2);
                    }
                }
            }
        }



        if (PlayerToMove == Player.FirstPlayer) {
            int positionToRemove = CheckingOpenLastPosWhiteChips();//rule - that u cannot block all 6 last positions for oponent
            if (positionToRemove != -1) {
                if (posibleIndexes.Contains(positionToRemove)) {
                    posibleIndexes.Remove(positionToRemove);
                }
            }
            for (int i = 0; i < posibleIndexes.Count; i++) {
                listToReturn.Add(positionWhiteReverse[posibleIndexes[i]]);
                positionWhiteReverse[posibleIndexes[i]].VisualizePosition();
            }
        }
        else {
            int positionToRemove = CheckingOpenLastPosBlackChips();
            if (positionToRemove != -1) {
                if (posibleIndexes.Contains(positionToRemove)) {
                    posibleIndexes.Remove(positionToRemove);
                }
            }
            for (int i = 0; i < posibleIndexes.Count; i++) {
                listToReturn.Add(positionBlackReverse[posibleIndexes[i]]);
                positionBlackReverse[posibleIndexes[i]].VisualizePosition();
            }
        }

    }

    private int CheckingOpenLastPosWhiteChips() {//a rule that does not allow closing all positions from which you can enter the "home"
        int lastPosBlackOpen = 0;
        int tempPos = -1;
        for (int i = 13; i <= 18; i++) {
            if (positionWhiteReverse[i].player == Player.SecondPlayer) {
                return -1;
            }
            else if (positionWhiteReverse[i].player == Player.FirstPlayer) {
                continue;
            }
            else {
                lastPosBlackOpen++;
                tempPos = i;
            }
        }
        if (lastPosBlackOpen > 1) {
            return -1;
        }
        else if (lastPosBlackOpen == 1) {
            return tempPos;
        }
        else {
            Debug.LogError("all end positions for black are occupied");
        }
        return -1;
    }

    private int CheckingOpenLastPosBlackChips() {//a rule that does not allow closing all positions from which you can enter the "home"
        int lastPosWhiteOpen = 0;
        int tempPos = -1;
        for (int i = 13; i <= 18; i++) {
            if (positionBlackReverse[i].player == Player.FirstPlayer) {
                return -1;
            }
            else if (positionBlackReverse[i].player == Player.SecondPlayer) {
                continue;
            }
            else {
                lastPosWhiteOpen++;
                tempPos = i;
            }
        }
        if (lastPosWhiteOpen > 1) {
            return -1;
        }
        else if (lastPosWhiteOpen == 1) {
            return tempPos;
        }
        else {
            Debug.LogError("all end positions for black are occupied");
        }
        return -1;
    }

    private bool IsAllChipsCanNotMakeMoveInSetCubesHelper(int indexToCheck, bool allInLastWhite, bool allInLastBlack) {//true if a player can make a move
        if (PlayerToMove == Player.FirstPlayer)
            for (int i = 0; i < chipsCount; i++) {
                if (chipsWhite[i].GetCurrentPosition() == startPosWhite && tookFromHead) continue;
                int currentPositionIndex = positionWhiteReverse.IndexOf(chipsWhite[i].GetCurrentPosition());
                if ((currentPositionIndex - indexToCheck) <= 0 && allInLastWhite) return true;
                if ((currentPositionIndex - indexToCheck) < 0) continue;
                if (positionWhiteReverse[currentPositionIndex - indexToCheck].player != Player.SecondPlayer)
                    return true;
            }
        else
            for (int i = 0; i < chipsCount; i++) {
                if (chipsBlack[i].GetCurrentPosition() == startPosBlack && tookFromHead) continue;
                int currentPositionIndex = positionBlackReverse.IndexOf(chipsBlack[i].GetCurrentPosition());
                if ((currentPositionIndex - indexToCheck) <= 0 && allInLastBlack) return true;
                if ((currentPositionIndex - indexToCheck) < 0) continue;
                if (positionBlackReverse[currentPositionIndex - indexToCheck].player != Player.FirstPlayer)
                    return true;
            }
        return false;
    }

    private bool IsAllChipsCanNotMakeMoveInSetCubes(bool isPare) {//false if a player can make a move
        bool allInLastWhite = IsAllChipsInLastPositionWhite();
        bool allInLastBlack = IsAllChipsInLastPositionBlack();
        if (isPare) {
            if (PlayerToMove == Player.FirstPlayer)
                for (int i = 0; i < chipsCount; i++) {
                    if (chipsWhite[i].GetCurrentPosition() == startPosWhite && tookFromHead) continue;
                    int tempIndex = positionWhiteReverse.IndexOf(chipsWhite[i].GetCurrentPosition());
                    if ((tempIndex - cubeFirst) <= 0 && allInLastWhite) return false;
                    if ((tempIndex - cubeFirst) < 0) continue;
                    if (positionWhiteReverse[tempIndex - cubeFirst].player != Player.SecondPlayer)
                        return false;
                }
            else
                for (int i = 0; i < chipsCount; i++) {
                    if (chipsBlack[i].GetCurrentPosition() == startPosBlack && tookFromHead) continue;
                    int tempIndex = positionBlackReverse.IndexOf(chipsBlack[i].GetCurrentPosition());
                    if ((tempIndex - cubeFirst) < 0) continue;
                    if ((tempIndex - cubeFirst) <= 0 && allInLastBlack) return false;
                    if (positionBlackReverse[tempIndex - cubeFirst].player != Player.FirstPlayer)
                        return false;
                }
        }
        else {
            if (cubeFirst != 0)
                if (IsAllChipsCanNotMakeMoveInSetCubesHelper(cubeFirst, allInLastWhite, allInLastBlack))
                    return false;

            if (cubeLast != 0)
                if (IsAllChipsCanNotMakeMoveInSetCubesHelper(cubeLast, allInLastWhite, allInLastBlack))
                    return false;

        }
        return true;
    }

    private void GetPosiblePositionsToMoveChip() {//for FreeAspect and FixedSix game modes
        //tempIndex == currentPositionIndex
        listToReturn = new List<PositionBase>();
        bool isAllChipsInLastPos = false;
        if (gameRules == GameRules.FreeAspect) {
            int supplIndex = 0;//is needed to prevent jumping through 6 positions occupied by the opponent
            if (currentChip?.GetPlayerState() == Player.FirstPlayer) {
                var currentPositionIndex = positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition());
                for (int i = currentPositionIndex; i >= 0; i--) {
                    if (positionWhiteReverse[i].player == Player.SecondPlayer) supplIndex++;
                    else supplIndex = 0;
                    if (supplIndex == 6) {
                        break;
                    };
                }
                isAllChipsInLastPos = IsAllChipsInLastPositionWhite();
                for (int i = currentPositionIndex; i >= supplIndex; i--) {
                    if (!isAllChipsInLastPos && i == 0) continue;
                    listToReturn.Add(positionWhiteReverse[i]);
                    positionWhiteReverse[i].VisualizePosition();
                }
            }
            else if (currentChip?.GetPlayerState() == Player.SecondPlayer) {
                var currentPositionIndex = positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());
                for (int i = currentPositionIndex; i >= 0; i--) {
                    if (positionBlackReverse[i].player == Player.FirstPlayer) supplIndex++;
                    else supplIndex = 0;
                    if (supplIndex == 6) {
                        break;
                    };
                }
                isAllChipsInLastPos = IsAllChipsInLastPositionBlack();
                for (int i = currentPositionIndex; i >= supplIndex; i--) {
                    if (!isAllChipsInLastPos && i == 0) continue;
                    listToReturn.Add(positionBlackReverse[i]);
                    positionBlackReverse[i].VisualizePosition();
                }
            }
        }
        else if (gameRules == GameRules.FixedSix) {
            if (currentChip?.GetPlayerState() == Player.FirstPlayer) {
                var currentPositionIndex = positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition());
                int maxIndex = (currentPositionIndex - 6) <= 0 ? 0 : currentPositionIndex - 6;
                isAllChipsInLastPos = IsAllChipsInLastPositionWhite();
                for (int i = currentPositionIndex; i >= maxIndex; i--) {
                    if (!isAllChipsInLastPos && i == 0) continue;
                    listToReturn.Add(positionWhiteReverse[i]);
                    positionWhiteReverse[i].VisualizePosition();
                }
            }
            else if (currentChip?.GetPlayerState() == Player.SecondPlayer) {
                var currentPositionIndex = positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());
                int maxIndex = (currentPositionIndex - 6) <= 0 ? 0 : currentPositionIndex - 6;
                isAllChipsInLastPos = IsAllChipsInLastPositionBlack();
                for (int i = currentPositionIndex; i >= maxIndex; i--) {
                    if (!isAllChipsInLastPos && i == 0) continue;
                    listToReturn.Add(positionBlackReverse[i]);
                    positionBlackReverse[i].VisualizePosition();
                }
            }
        }
    }

    private void CheckingChipsAtBiggerPositionInLastPosWhite(int currentPositionIndex) {//this method is used only when [currentPositionIndex - cube < 0]
        for (int i = 1; i < 6; i++) {
            if (positionWhiteReverse[currentPositionIndex + i].state == PositionState.WhiteLastPosition) {
                if (positionWhiteReverse[currentPositionIndex + i].player == Player.FirstPlayer) {
                    positionWhiteReverse[0].UnvisualizePosition();
                    break;//if so cannot enable home
                }
                else {
                    positionWhiteReverse[0].VisualizePosition();
                }
            }
            if (positionWhiteReverse[currentPositionIndex + i].state == PositionState.RegularPosition)
                break;//out of lastPosWhite
        }
    }

    private void CheckingChipsAtBiggerPositionInLastPosBlack(int currentPositionIndex) {//this method is used only when [currentPositionIndex - cube < 0]
        for (int i = 1; i < 6; i++) {
            if (positionBlackReverse[currentPositionIndex + i].state == PositionState.BlackLastPosition) {
                if (positionBlackReverse[currentPositionIndex + i].player == Player.SecondPlayer) {
                    positionBlackReverse[0].UnvisualizePosition();
                    break;//if so cannot enable home
                }
                else {
                    positionBlackReverse[0].VisualizePosition();
                }
            }
            if (positionBlackReverse[currentPositionIndex + i].state == PositionState.RegularPosition)
                break;//out of lastPosBlack
        }
    }

    private void GetPosiblePositionsToMoveChipSetCubesInLastPosWhite(int cube1, int cube2) {//IsAllChipsInLastPositionWhite is already checking
        listToReturn = new List<PositionBase>();
        bool visualizeHome = false;
        bool isPareCubes = cube1 == cube2;
        int currentPositionIndex = positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition());
        if (isPareCubes) {
            if ((currentPositionIndex - cube1) > 0) {
                if (positionWhiteReverse[currentPositionIndex - cube1].player != Player.SecondPlayer) {
                    listToReturn.Add(positionWhiteReverse[currentPositionIndex - cube1]);
                }
            }
            if (currentPositionIndex - cube1 == 0) { positionWhiteReverse[0].VisualizePosition(); }
            else if (currentPositionIndex - cube1 < 0) { //check if u have chips at bigger positions, if so cannot enable home, if not enable home
                CheckingChipsAtBiggerPositionInLastPosWhite(currentPositionIndex);
            }
            else {
                listToReturn.Add(positionWhiteReverse[currentPositionIndex - cube1]);
                positionWhiteReverse[currentPositionIndex - cube1].VisualizePosition();
            }
            return;
        }
        if (cube1 != 0) {
            if (currentPositionIndex - cube1 > 0) {
                listToReturn.Add(positionWhiteReverse[currentPositionIndex - cube1]);
                positionWhiteReverse[currentPositionIndex - cube1].VisualizePosition();
            }
            else if (currentPositionIndex - cube1 <= 0) {
                if (currentPositionIndex - cube1 == 0) {
                    visualizeHome = true;
                }
                else if (currentPositionIndex - cube1 < 0) //check if u have chips at bigger positions, if so cannot enable home, if not enable home
                    CheckingChipsAtBiggerPositionInLastPosWhite(currentPositionIndex);
            }
        }
        if (cube2 != 0) {
            if (currentPositionIndex - cube2 > 0) {
                listToReturn.Add(positionWhiteReverse[currentPositionIndex - cube2]);
                positionWhiteReverse[currentPositionIndex - cube2].VisualizePosition();
            }
            else if (currentPositionIndex - cube2 <= 0) {
                if (currentPositionIndex - cube2 == 0) {
                    visualizeHome = true;
                }
                else if (currentPositionIndex - cube2 < 0) //check if u have chips at bigger positions, if so cannot enable home, if not enable home
                    CheckingChipsAtBiggerPositionInLastPosWhite(currentPositionIndex);
            }
        }
        if (visualizeHome == true)
            positionWhiteReverse[0].VisualizePosition();
    }

    private void GetPosiblePositionsToMoveChipSetCubesInLastPosBlack(int cube1, int cube2) {//IsAllChipsInLastPositionBlack is already checking
        listToReturn = new List<PositionBase>();
        bool visualizeHome = false;
        bool isPareCubes = cube1 == cube2;
        int currentPositionIndex = positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());
        if (isPareCubes) {
            if ((currentPositionIndex - cube1) > 0) {
                if (positionBlackReverse[currentPositionIndex - cube1].player != Player.FirstPlayer) {
                    listToReturn.Add(positionBlackReverse[currentPositionIndex - cube1]);
                }
            }
            if (currentPositionIndex - cube1 == 0)//repeating -- extract to method
                positionBlackReverse[0].VisualizePosition();
            else if (currentPositionIndex - cube1 < 0)//check if u have chips at bigger positions, if so cannot enable home, if not enable home
                CheckingChipsAtBiggerPositionInLastPosBlack(currentPositionIndex);
            else {
                listToReturn.Add(positionBlackReverse[currentPositionIndex - cube1]);
                positionBlackReverse[currentPositionIndex - cube1].VisualizePosition();
            }
            return;
        }
        if (cube1 != 0) {
            if (currentPositionIndex - cube1 > 0) {
                listToReturn.Add(positionBlackReverse[currentPositionIndex - cube1]);
                positionBlackReverse[currentPositionIndex - cube1].VisualizePosition();
            }
            else if (currentPositionIndex - cube1 <= 0) {
                if (currentPositionIndex - cube1 == 0)
                    visualizeHome = true;
                else if (currentPositionIndex - cube1 < 0)//check if u have chips at bigger positions, if so cannot enable home, if not enable home
                    CheckingChipsAtBiggerPositionInLastPosBlack(currentPositionIndex);
            }
        }
        if (cube2 != 0) {
            if (currentPositionIndex - cube2 > 0) {
                listToReturn.Add(positionBlackReverse[currentPositionIndex - cube2]);
                positionBlackReverse[currentPositionIndex - cube2].VisualizePosition();
            }
            else if (currentPositionIndex - cube2 <= 0) {
                if (currentPositionIndex - cube2 == 0)
                    visualizeHome = true;
                else if (currentPositionIndex - cube2 < 0)//check if u have chips at bigger positions, if so cannot enable home, if not enable home
                    CheckingChipsAtBiggerPositionInLastPosBlack(currentPositionIndex);
            }
        }
        if (visualizeHome == true)
            positionBlackReverse[0].VisualizePosition();
    }

    public void SetSelectedCubes(int firstCube, int secondCube) {
        cubeFirst = firstCube;
        cubeLast = secondCube;
        CubeVisual1.SetState((Cube)cubeFirst);
        CubeVisual2.SetState((Cube)cubeLast);
        ActivateCubesVisual();
        if (cubeFirst == cubeLast) { pareIndex = 4; }

        if (gameRules == GameRules.SetCubes) {
            ChipButtonsEnable(PlayerToMove);
            Generator.DisableGameObject();
        }
        else {
            ChipButtonsEnableBoth();
        }

        MainBanner.DisbleBanner();
    }

    private void ActivateCubesVisual() {
        CubeVisual1.gameObject.SetActive(true);
        CubeVisual2.gameObject.SetActive(true);
    }

    public void DisableCubeVisuals() {//Button
        if (isEnableCubeVisuals) {
            var transparent = 0.2f;
            CubeVisual1.ChangeTransparency(transparent);
            CubeVisual2.ChangeTransparency(transparent);
        }
        else {
            var transparent = 1f;
            CubeVisual1.ChangeTransparency(transparent);
            CubeVisual2.ChangeTransparency(transparent);
        }
        isEnableCubeVisuals = !isEnableCubeVisuals;
    }

    public GameRules GetGameRules() {
        return gameRules;
    }
}
