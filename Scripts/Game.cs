using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using TMPro;
using System.Runtime.InteropServices;

public class Game : MonoBehaviour {

    public event EventHandler OnPositionButon; //an event that executes code from PositionHandle.cs, but is called by Game.cs

    public event EventHandler OnChipButon;//an event that executes code from ChipBlack.cs or ChipWhite.cs, but is called by Game.cs

    private List<ChipBase> chipsWhite = new List<ChipBase>();
    private List<ChipBase> chipsBlack = new List<ChipBase>();

    [SerializeField] private GameObject chipWhite;//prefab
    [SerializeField] private GameObject chipBlack;
    [SerializeField] private GameObject DeselectButton;
    [SerializeField] private GameObject SettingsButton;
    [SerializeField] private GameObject UndoButton;
    [SerializeField] private CubeSelected CubeVisual1;
    [SerializeField] private CubeSelected CubeVisual2;
    [SerializeField] private Generator Generator;
    [SerializeField] private Transform ChipsParent;
    [SerializeField] private BotController BotController;
    [SerializeField] private BotController BotController1;

    private bool isEnableCubeVisuals = true;//using in method DisableCubeVisuals to make cubes transparent
    private int previousPositionIndex;
    private int IllegalPositionIndexForWhite;//need to simplify barrier rule while checking whether there are any possible moves after the move is made (method:IsAllChipsCanNotMakeMoveInSetCubes)
    private int IllegalPositionIndexForBlack;
    //private int currentPositionIndex;
    private int pareIndex;//shows how much moves left if pare cubes
    private bool tookFromHead;//to control the number of chips taken from the head
    private bool GeneratorOnlyInSetCubes;
    private bool UndoButtonDisabled;
    private bool SettingsButtonDisabled;
    public bool isSimulationOn;
    private bool gameWithBot;
    private bool gameBotVsBot;
    private bool botWon = false;

    private PositionBase startPosWhite;
    private PositionBase startPosBlack;
    private List<PositionBase> positionWhiteReverse;
    private List<PositionBase> positionBlackReverse;
    private PositionsParent positionParent;//assigns and settles indexes for positions and gives them to Game.cs
    private List<PositionBase> listToReturn;//will be transferred to processing when you click on the position, returning in method GetPosiblePositionsToMoveChipLast()

    [SerializeField] private MainBannerController MainBanner;//is used for everything related to the banner (selecting the type of game, settings, choosing dice, etc.)
    public SoundManager SoundManager;


    public ChipBase currentChip { get; set; }//is saved when you click on a chip for further actions on it.(deselection and moving it to another position)
    public PositionHandle currentPosition { get; set; }
    private PositionHandle previousPosition;//added for future modifications like undo

    private Stack<GameSequence> gameSequence;
    private const int chipsCount = 15;
    private GameRules gameRules;
    private Player PlayerToMove = (Player)1;
    private int cubeFirst;
    private int cubeLast;
    private int movesMade;
    private float freezeTimeBeforeChangePlayerToMove = 1f;
    public int pointsToWinWhite { get; private set; }
    public int pointsToWinBlack { get; private set; }

    private void Awake() {
        positionParent = GetComponentInChildren<PositionsParent>();

    }

    private void Start() {
        MainBanner.ActivateSellectGameMode();
        SoundManager.PlaySoundOnce(Sound.Background);
        positionParent.PositionsDisable();
        UndoButton.SetActive(false);
        DisableButtonDeseletChip();
        DisableSettingButtonInGame();
        InstantiateChips(chipsCount);
        positionWhiteReverse = positionParent.GetPositionWhite();
        positionBlackReverse = positionParent.GetPositionBlack();
        positionBlackReverse[0].GetComponent<HomePositionHandler>().SetCountOfChips(chipsCount);//transfer information about the number of created chips to the "home" position Black
        positionWhiteReverse[0].GetComponent<HomePositionHandler>().SetCountOfChips(chipsCount);//transfer information about the number of created chips to the "home" position White
        DisableHomeBoth();
        startPosWhite = positionWhiteReverse[positionWhiteReverse.Count - 1];//positions are counted from the end, so the starting position will be the last one
        startPosBlack = positionBlackReverse[positionBlackReverse.Count - 1];

        tookFromHead = false;//it is needed to limit the taking of chips from the "head" according to the rules
                             // movesMade = 0;//this variable is needed in order to move 2 chips to the sixth position on the first move when the dice roll 6 6, according to the rules. also for further updates of the game and statistics
        GeneratorOnlyInSetCubes = false;
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
                    //Debug.Log("TryGetComponent(out HomePositionHandler homePosition) - true");
                    ButtonHome();
                }
            }
        }
    }

    public void ButtonChip() {//a method that is called when you click on a chip
        bool gotPosiblePosition = false;

        currentChip?.DeselectChipVisual();

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
    }

    public void ButtonPosition() {
        SoundManager.PlaySoundOnce(Sound.ChipMove);
        previousPositionIndex = currentChip.GetPlayerState() == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition()) : positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());
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
        else {
            int curentChipIndex = currentChip.GetPlayerState() == Player.FirstPlayer ? chipsWhite.IndexOf(currentChip) : chipsBlack.IndexOf(currentChip);
            var index = currentChip.GetPlayerState() == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentPosition) : positionBlackReverse.IndexOf(currentPosition);
            //previousPositionIndex = currentChip.GetPlayerState() == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition()) : positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());
            GameSequenceAddMove(cubeFirst, cubeLast, curentChipIndex, index, currentChip.GetPlayerState());
        }
        if (gameSequence.TryPeek(out var gameSequencePare)) {
            UndoButton.SetActive(true);
        }
        currentChip = null;
        //Testing();
    }

    private void MadeMoveCube1(int currentPositionIndex, int cube1) {
        cubeFirst = 0;
        CubeVisual1.gameObject.SetActive(false);
        CheckingForChangePlayerToMove(cubeFirst == cubeLast);
        int curentChipIndex = currentChip.GetPlayerState() == Player.FirstPlayer ? chipsWhite.IndexOf(currentChip) : chipsBlack.IndexOf(currentChip);
        GameSequenceAddMove(cube1, cubeLast, curentChipIndex, currentPositionIndex);
    }

    private void MadeMoveCube2(int currentPositionIndex, int cube2) {
        cubeLast = 0;
        CubeVisual2.gameObject.SetActive(false);
        CheckingForChangePlayerToMove(cubeFirst == cubeLast);
        int curentChipIndex = currentChip.GetPlayerState() == Player.FirstPlayer ? chipsWhite.IndexOf(currentChip) : chipsBlack.IndexOf(currentChip);
        GameSequenceAddMove(cubeFirst, cube2, curentChipIndex, currentPositionIndex);
    }

    private void MadeMoveInSetCubes(int currentPositionIndex) {
        movesMade++;
        int variationBetweenIndexes = previousPositionIndex - currentPositionIndex;

        if (variationBetweenIndexes == (cubeFirst + cubeLast)) {
            CubeVisual1.gameObject.SetActive(false);
            CubeVisual2.gameObject.SetActive(false);
            UndoButton.SetActive(false);
            ChipButtonsDisableBoth();
            UndoButtonDisabled = true;
            SettingsButtonDisabled = true;
            Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
            int curentChipIndex = currentChip.GetPlayerState() == Player.FirstPlayer ? chipsWhite.IndexOf(currentChip) : chipsBlack.IndexOf(currentChip);
            GameSequenceAddMove(cubeFirst, cubeLast, curentChipIndex, currentPositionIndex);
            return;
        }
        else if (variationBetweenIndexes == cubeFirst) {
            MadeMoveCube1(currentPositionIndex, cubeFirst);
        }
        else if (variationBetweenIndexes == cubeLast) {
            MadeMoveCube2(currentPositionIndex, cubeLast);
        }

        if (previousPositionIndex == 24 && variationBetweenIndexes != 0) {//a rule that does not allow you to take more than one chip from the "head"
            DisableChipButtonsAtSatartPos();
            tookFromHead = true;
        }
    }

    private void MadeMoveInSetCubesHome(int currentPositionIndex) {//method when chip moved to home. called in ButtonHome()
        movesMade++;
        if (currentPositionIndex == cubeFirst) {
            MadeMoveCube1(currentPositionIndex, cubeFirst);
            return;
        }

        //To deal with the fact that the dice can be larger than the index I used in the last position, I added three additional bools
        bool firstLessThanSecond = cubeFirst < cubeLast;
        bool firstLessThanIndex = currentPositionIndex > cubeFirst;
        bool lastLessThanIndex = currentPositionIndex > cubeLast;
        if (firstLessThanIndex && lastLessThanIndex) {
            //Debug.LogError("firstLessThanIndex && lastLessThanIndex - true");
            return;
        }
        else if (!firstLessThanIndex && !lastLessThanIndex) {
            if (firstLessThanSecond) {
                MadeMoveCube1(currentPositionIndex, cubeFirst);
                return;
            }
            else if (!firstLessThanSecond) {
                MadeMoveCube2(currentPositionIndex, cubeLast);
                return;
            }
        }
        else if (firstLessThanIndex) {
            MadeMoveCube2(currentPositionIndex, cubeLast);
            return;
        }
        else {
            MadeMoveCube1(currentPositionIndex, cubeFirst);
            return;
        }
    }

    private void MadeMoveInSetCubesPare(int currentPositionIndex, bool isLastPos) {
        //Debug.Log("MadeMoveInSetCubesPare"); 
        int variationBetweenIndexes;

        if (isLastPos) variationBetweenIndexes = currentPositionIndex > cubeFirst ? previousPositionIndex - currentPositionIndex : currentPositionIndex;
        else variationBetweenIndexes = previousPositionIndex - currentPositionIndex;

        int curentChipIndex = currentChip.GetPlayerState() == Player.FirstPlayer ? chipsWhite.IndexOf(currentChip) : chipsBlack.IndexOf(currentChip);

        if (variationBetweenIndexes == (cubeFirst)) {
            GameSequenceAddMove(cubeFirst, cubeLast, curentChipIndex, currentPositionIndex);
            pareIndex -= 1;
        }
        else if (variationBetweenIndexes == (cubeFirst * 2)) {
            GameSequenceAddMove(cubeFirst, cubeLast, curentChipIndex, currentPositionIndex);
            pareIndex -= 2;
        }
        else if (variationBetweenIndexes == (cubeFirst * 3)) {
            GameSequenceAddMove(cubeFirst, cubeLast, curentChipIndex, currentPositionIndex);
            pareIndex -= 3;
        }
        else if (variationBetweenIndexes == (cubeFirst * 4)) {
            GameSequenceAddMove(cubeFirst, cubeLast, curentChipIndex, currentPositionIndex);
            pareIndex -= 4;
        }

        if (previousPositionIndex == 24 && variationBetweenIndexes != 0) {
            if (movesMade < 2 && cubeFirst == 6) {
                if (pareIndex == 2) {
                    DisableChipButtonsAtSatartPos();
                    tookFromHead = true;
                }
            }
            else {
                DisableChipButtonsAtSatartPos();
                tookFromHead = true;
            }
        }
        movesMade++;
        CheckingForChangePlayerToMove(cubeFirst == cubeLast);
    }

    private void MadeMoveInSetCubesPareHome(int index) {
        movesMade++;
        int curentChipIndex = currentChip.GetPlayerState() == Player.FirstPlayer ? chipsWhite.IndexOf(currentChip) : chipsBlack.IndexOf(currentChip);
        int currentPositionIndex = PlayerToMove == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition()) : positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());
        if (index == cubeFirst) {
            GameSequenceAddMove(cubeFirst, cubeLast, curentChipIndex, currentPositionIndex);
            pareIndex -= 1;
            CheckingForChangePlayerToMove(cubeFirst == cubeLast);
            return;
        }
        bool cubeLessThanIndex = cubeFirst < index;
        if (!cubeLessThanIndex) {
            GameSequenceAddMove(cubeFirst, cubeLast, curentChipIndex, currentPositionIndex);
            pareIndex -= 1;
            CheckingForChangePlayerToMove(cubeFirst == cubeLast);
        }
    }

    private void CheckingForChangePlayerToMove(bool isPare) {
        if (IsAllChipsCanNotMakeMoveInSetCubes(isPare)) {
            UndoButton.SetActive(false);
            ChipButtonsDisableBoth();
            UndoButtonDisabled = true;
            SettingsButtonDisabled = true;
            Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
            return;
        }
        if (isPare) {
            if (pareIndex == 0) {
                UndoButton.SetActive(false);
                ChipButtonsDisableBoth();
                UndoButtonDisabled = true;
                SettingsButtonDisabled = true;
                Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
            }
        }
        else {
            if (cubeFirst == 0 && cubeLast == 0) {
                UndoButton.SetActive(false);
                ChipButtonsDisableBoth();
                UndoButtonDisabled = true;
                SettingsButtonDisabled = true;
                Invoke(nameof(ChangePlayerToMove), freezeTimeBeforeChangePlayerToMove);
            }
        }
    }

    private void ChangePlayerToMove() {
        PlayerToMove = PlayerToMove == 0 ? (Player)1 : (Player)0;
        if (gameWithBot && PlayerToMove == BotController.GetPlayerState()) {
            Generator.OnButtonClick();
            UndoButton.SetActive(false);
            UndoButtonDisabled = false;
            SettingsButtonDisabled = false;
            tookFromHead = false;
            return;
        }
        else if (gameBotVsBot) {
            Generator.OnButtonClick();
            UndoButton.SetActive(false);
            UndoButtonDisabled = false;
            SettingsButtonDisabled = false;
            return;
        }
        if (GeneratorOnlyInSetCubes) {
            Generator.OnButtonClick();
        }
        else {
            DisableSettingButtonInGame();
            MainBanner.ActivateChooseCubesMode(PlayerToMove);
            Generator.EnableGameObject();
        }
        UndoButton.SetActive(false);
        UndoButtonDisabled = false;
        SettingsButtonDisabled = false;
        tookFromHead = false;
    }

    public void ButtonHome() {
        if (currentChip == null) {
            Debug.LogError("currentChip can`t be null in ButtonHome method!");
        }
        previousPositionIndex = PlayerToMove == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentPosition) : positionBlackReverse.IndexOf(currentPosition);
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

    public void BotWonGame(Player winner) {
        botWon = true;
        ChipButtonsDisableBoth();
        positionParent.PositionsDisable();
        DisableButtonDeseletChip();
        currentChip = null;
        SetStartPositions();
        ResetHomePositions();
        MainBanner.ActivatePlayerWonMode("Winner player " + ((int)winner + 1));//this banner will have the same game selection as at the start 
        UndoButton.gameObject.SetActive(false);
    }

    private void PlayerWonGame() {
        Player winner = currentChip.GetPlayerState();
        currentChip = null;
        SetStartPositions();
        ResetHomePositions();
        MainBanner.ActivatePlayerWonMode("Winner player " + ((int)winner + 1));//this banner will have the same game selection as at the start 
        UndoButton.gameObject.SetActive(false);
    }

    public void ButtonToTheMainMenu() {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void StartLongGameFreeAspect() {//Button
        gameWithBot = false;
        gameBotVsBot = false;
        gameRules = GameRules.FreeAspect;
        RestartGameHelper();
        ChipButtonsEnableBoth();
        CubeVisual1.gameObject.SetActive(false);
        CubeVisual2.gameObject.SetActive(false);
        MainBanner.DisbleBanner();
        Generator.NotSetCubesPosition();
        if (GameSettings.Generator == true)
            Generator.EnableGameObject();
        movesMade = 0;
    }

    public void StartGameFixedSix() {//Button
        gameWithBot = false;
        gameBotVsBot = false;
        gameRules = GameRules.FixedSix;
        RestartGameHelper();
        ChipButtonsEnableBoth();
        CubeVisual1.gameObject.SetActive(false);
        CubeVisual2.gameObject.SetActive(false);
        MainBanner.DisbleBanner();
        Generator.NotSetCubesPosition();
        if (GameSettings.Generator == true)
            Generator.EnableGameObject();
        movesMade = 0;
    }

    public void StartGameSetCubes() {//Button
        gameWithBot = false;
        gameBotVsBot = false;
        RestartGameHelper();
        gameRules = GameRules.SetCubes;
        PlayerToMove = Player.SecondPlayer;
        ChangePlayerToMove();//banner is activated here
        Generator.SetCubesPosition();
        movesMade = 0;
    }

    public void StartGameVsBot() {
        gameBotVsBot = false;
        gameWithBot = true;
        RestartGameHelper();
        gameRules = GameRules.SetCubes;
        PlayerToMove = Player.SecondPlayer;
        ChangePlayerToMove();
        Generator.SetCubesPosition();
        movesMade = 0;
    }

    public void StartGameBotVsBot() {
        gameWithBot = false;
        gameBotVsBot = true;
        gameRules = GameRules.FreeAspect;
        PlayerToMove = Player.SecondPlayer;
        RestartGameHelper();
        movesMade = 0;
        Generator.SetCubesPosition();
        BotController.SetChips(chipsBlack);
        BotController1.SetChips(chipsWhite);
        ChangePlayerToMove();

    }

    private void RestartGameHelper() {
        UndoButton.SetActive(false);
        EnableSettingButtonInGame();
        positionParent.PositionsDisable();
        DisableButtonDeseletChip();
        SetStartPositions();
        ResetHomePositions();
        gameSequence = new Stack<GameSequence>();
    }

    private void ResetHomePositions() {
        positionBlackReverse[0].ResetPosition();
        positionWhiteReverse[0].ResetPosition();
    }

    private void InstantiateChips(int countOfChips) {
        for (int i = 0; i < countOfChips; i++) {
            ChipBase chipFirst = Instantiate(chipWhite, ChipsParent).GetComponent<ChipBase>();
            chipFirst.SetGame(this);
            chipsWhite.Add(chipFirst);
            ChipBase chipSecond = Instantiate(chipBlack, ChipsParent).GetComponent<ChipBase>();
            chipSecond.SetGame(this);
            chipsBlack.Add(chipSecond);
        }
    }

    private void SetStartPositions() {
        MainBanner.DisbleBanner();
        startPosWhite.ResetPosition();
        startPosBlack.ResetPosition();
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

    private void ChipButtonsEnable() {
        if (PlayerToMove == Player.FirstPlayer) {
            foreach (var i in chipsWhite) i.ButtonTrue();
            foreach (var i in chipsBlack) i.ButtonFalse();
        }
        else if (PlayerToMove == Player.SecondPlayer) {
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
        currentChip?.DeselectChipVisual();
        currentChip = null;
        DisableButtonDeseletChip();
        positionParent.PositionsDisable();//disable colliders
    }

    public void ButtonResetGame() {
        switch (gameRules) {
            case GameRules.FixedSix: StartGameFixedSix(); break;
            case GameRules.FreeAspect: StartLongGameFreeAspect(); break;
            case GameRules.SetCubes: StartGameSetCubes(); break;
        }
    }

    public void ButtonSettings() {//in game
        if (!SettingsButtonDisabled) {
            CalculatePointsToWin();
            MainBanner.ActivateSettingsMode();
            ButtonDeseletChip();
            UndoButton.SetActive(false);
        }
    }

    public void ButtonGeneratorOnOff() {
        if (gameRules != GameRules.SetCubes) {
            if (GameSettings.Generator == true) DisableGeneratorAndCubes();
            else EnableGeneratorAndCubes();
        }//for SetCubes handels in MainBannerController [ActivateChooseCubesMode]
        GameSettings.Generator = !GameSettings.Generator;
    }

    private void EnableGeneratorAndCubes() {
        // Generator.//EnableGameObject();
        CubeVisual1.gameObject.SetActive(true);
        CubeVisual2.gameObject.SetActive(true);
    }

    private void DisableGeneratorAndCubes() {
        // Generator.//DisableGameObject();
        CubeVisual1.gameObject.SetActive(false);
        CubeVisual2.gameObject.SetActive(false);
    }

    private void EnableSettingButtonInGame() {
        SettingsButton.SetActive(true);
    }

    private void DisableSettingButtonInGame() {
        SettingsButton.SetActive(false);
    }

    public List<PositionBase> GetPosiblePositionsToMoveChipLast() {
        return listToReturn;
    }

    private void DisableChipButtonsAtSatartPos() {//control of taking chips from the "head"
        if (PlayerToMove == Player.FirstPlayer) {
            foreach (var i in startPosWhite.GetComponent<PositionHandle>().GetChipList()) i.ButtonFalse();
        }
        else if (PlayerToMove == Player.SecondPlayer) {
            foreach (var i in startPosBlack.GetComponent<PositionHandle>().GetChipList()) i.ButtonFalse();
        }
    }


    private void GetPosiblePositionsToMoveChipSetCubes(int cube1, int cube2) {
        listToReturn = new List<PositionBase>();
        bool isPareCubes = cube1 == cube2;
        //just control if it`s first move, than move it without player
        List<int> posibleIndexes = new List<int>();
        int currentPositionIndex = PlayerToMove == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition()) : positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());

        if (isPareCubes) {
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
            //int variationIndexCube1 = (currentPositionIndex - cube1) < 0 ? 0 : currentPositionIndex - cube1;
            //int variationIndexCube2 = (currentPositionIndex - cube2) < 0 ? 0 : currentPositionIndex - cube2;

            int variationIndexCube1 = (ushort)(currentPositionIndex - cube1);
            int variationIndexCube2 = (ushort)(currentPositionIndex - cube2);


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
                    if (PlayerToMove == Player.FirstPlayer) {
                        if (variationIndexCube1 > 0 && positionWhiteReverse[variationIndexCube1].player != Player.SecondPlayer)
                            posibleIndexes.Add(variationIndexCube1);
                        if (variationIndexCube2 > 0 && positionWhiteReverse[variationIndexCube2].player != Player.SecondPlayer)
                            posibleIndexes.Add(variationIndexCube2);
                        if (positionWhiteReverse[variationIndexCube1].player != Player.SecondPlayer | positionWhiteReverse[variationIndexCube2].player != Player.SecondPlayer)
                            if ((currentPositionIndex - cube1 - cube2) > 0)
                                posibleIndexes.Add(currentPositionIndex - cube1 - cube2);
                    }
                    else {
                        if (variationIndexCube1 > 0 && positionBlackReverse[variationIndexCube1].player != Player.FirstPlayer)
                            posibleIndexes.Add(variationIndexCube1);
                        if (variationIndexCube2 > 0 && positionBlackReverse[variationIndexCube2].player != Player.FirstPlayer)
                            posibleIndexes.Add(variationIndexCube2);
                        if (positionBlackReverse[variationIndexCube1].player != Player.FirstPlayer | positionBlackReverse[variationIndexCube2].player != Player.FirstPlayer)
                            if ((currentPositionIndex - cube1 - cube2) > 0)
                                posibleIndexes.Add(currentPositionIndex - cube1 - cube2);
                    }
                }
            }
        }


        if (PlayerToMove == Player.FirstPlayer) {
            int positionToRemove = CheckingBarrierRulePosToRemoveWhiteChips(posibleIndexes);//rule - that u cannot block all 6 last positions for oponent
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
            int positionToRemove = CheckingBarrierRulePosToRemoveBlackChips(posibleIndexes);
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

    private int CheckingBarrierRulePosToRemoveWhiteChips(List<int> posibleIndexes) {//a rule that does not allow closing all positions from which you can enter the "home"
        int barrierSequence = 0;
        int indexToRemove = -1;

        for (int i = 0; i < posibleIndexes.Count; i++) {
            for (int j = positionWhiteReverse.Count - 1; j > 0; j--) {//Count - 1 because home included
                if (positionWhiteReverse[j].player == Player.FirstPlayer || (positionWhiteReverse[j].player == Player.Empty && posibleIndexes[i] == j)) {//check whether it is already occupied or will be occupied
                    barrierSequence++;
                }
                else {
                    barrierSequence = 0;
                }
                if (barrierSequence >= 6) {
                    bool isBarrier = true;
                    for (int k = positionBlackReverse.IndexOf(positionWhiteReverse[j]); k > 0; k--) {//checking if there is an opponent's chip in front of the barrier
                        if (positionBlackReverse[k].player == Player.SecondPlayer) {
                            isBarrier = false;
                        }
                    }
                    if (isBarrier) {
                        indexToRemove = posibleIndexes[i];
                        IllegalPositionIndexForWhite = indexToRemove;
                        return indexToRemove;
                    }
                }
            }
        }
        return indexToRemove;
    }


    private int CheckingBarrierRulePosToRemoveBlackChips(List<int> posibleIndexes) {//a rule that does not allow closing all positions from which you can enter the "home"
        int barrierSequence = 0;
        int indexToRemove = -1;

        for (int i = 0; i < posibleIndexes.Count; i++) {
            for (int j = positionBlackReverse.Count - 1; j > 0; j--) {
                if (positionBlackReverse[j].player == Player.SecondPlayer || (positionBlackReverse[j].player == Player.Empty && posibleIndexes[i] == j)) {
                    barrierSequence++;
                }
                else {
                    barrierSequence = 0;
                }
                if (barrierSequence >= 6) {
                    bool isBarrier = true;
                    for (int k = positionWhiteReverse.IndexOf(positionBlackReverse[j]); k > 0; k--) {
                        if (positionWhiteReverse[k].player == Player.FirstPlayer) {
                            isBarrier = false;
                        }
                    }
                    if (isBarrier) {
                        indexToRemove = posibleIndexes[i];
                        IllegalPositionIndexForBlack = indexToRemove;
                        return indexToRemove;
                    }
                }
            }
        }
        return indexToRemove;
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
        int currentPositionIndex = currentChip?.GetPlayerState() == Player.FirstPlayer ? positionWhiteReverse.IndexOf(currentChip.GetCurrentPosition()) : positionBlackReverse.IndexOf(currentChip.GetCurrentPosition());
        if (gameRules == GameRules.FreeAspect) {
            int supplIndex = 0;//is needed to prevent jumping through 6 positions occupied by the opponent
            if (currentChip?.GetPlayerState() == Player.FirstPlayer) {
                for (int i = currentPositionIndex; i >= 0; i--) {
                    if (positionWhiteReverse[i].player == Player.SecondPlayer) supplIndex++;
                    else supplIndex = 0;
                    if (supplIndex == 6) {
                        for (int j = i + supplIndex; j < currentPositionIndex; j++) {
                            listToReturn.Add(positionWhiteReverse[j]);
                            positionWhiteReverse[j].VisualizePosition();
                        }
                        return;
                    };
                }
                for (int k = currentPositionIndex; k > 0; k--) {
                    if (positionWhiteReverse[k].player != Player.SecondPlayer) {
                        listToReturn.Add(positionWhiteReverse[k]);
                        positionWhiteReverse[k].VisualizePosition();
                    }
                }
                if (IsAllChipsInLastPositionWhite()) {
                    positionWhiteReverse[0].VisualizePosition();
                }

            }
            else if (currentChip?.GetPlayerState() == Player.SecondPlayer) {
                for (int i = currentPositionIndex; i >= 0; i--) {
                    if (positionBlackReverse[i].player == Player.FirstPlayer) supplIndex++;
                    else supplIndex = 0;
                    if (supplIndex == 6) {
                        for (int j = i + supplIndex; j < currentPositionIndex; j++) {
                            listToReturn.Add(positionBlackReverse[j]);
                            positionBlackReverse[j].VisualizePosition();
                        }
                        return;
                    };
                }
                for (int k = currentPositionIndex; k > 0; k--) {
                    if (positionBlackReverse[k].player != Player.FirstPlayer) {
                        listToReturn.Add(positionBlackReverse[k]);
                        positionBlackReverse[k].VisualizePosition();
                    }
                }
                if (IsAllChipsInLastPositionBlack()) {
                    positionBlackReverse[0].VisualizePosition();
                }
            }
        }
        else if (gameRules == GameRules.FixedSix) {
            if (currentChip?.GetPlayerState() == Player.FirstPlayer) {
                int maxIndex = (currentPositionIndex - 6) <= 0 ? 0 : currentPositionIndex - 6;
                isAllChipsInLastPos = IsAllChipsInLastPositionWhite();
                for (int i = currentPositionIndex; i >= maxIndex; i--) {
                    if (!isAllChipsInLastPos && i == 0) continue;
                    listToReturn.Add(positionWhiteReverse[i]);
                    positionWhiteReverse[i].VisualizePosition();
                }
            }
            else if (currentChip?.GetPlayerState() == Player.SecondPlayer) {
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
        for (int i = 1; i <= 6; i++) {
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
        for (int i = 1; i <= 6; i++) {
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
                    positionWhiteReverse[currentPositionIndex - cube1].VisualizePosition();
                }
                if (positionWhiteReverse[currentPositionIndex - (cube1 * 2)].player != Player.SecondPlayer) {
                    listToReturn.Add(positionWhiteReverse[currentPositionIndex - (cube1 * 2)]);
                    positionWhiteReverse[currentPositionIndex - (cube1 * 2)].VisualizePosition();
                }
                if (positionWhiteReverse[currentPositionIndex - (cube1 * 3)].player != Player.SecondPlayer) {
                    listToReturn.Add(positionWhiteReverse[currentPositionIndex - (cube1 * 3)]);
                    positionWhiteReverse[currentPositionIndex - (cube1 * 3)].VisualizePosition();
                }
                if (positionWhiteReverse[currentPositionIndex - (cube1 * 4)].player != Player.SecondPlayer) {
                    listToReturn.Add(positionWhiteReverse[currentPositionIndex - (cube1 * 4)]);
                    positionWhiteReverse[currentPositionIndex - (cube1 * 4)].VisualizePosition();
                }
            }
            if (currentPositionIndex - cube1 == 0) { positionWhiteReverse[0].VisualizePosition(); }
            else if (currentPositionIndex - cube1 < 0) { //check if u have chips at bigger positions, if so cannot enable home, if not enable home
                CheckingChipsAtBiggerPositionInLastPosWhite(currentPositionIndex);
            }
            return;
        }
        if (cube1 != 0) {
            if (currentPositionIndex - cube1 > 0) {
                listToReturn.Add(positionWhiteReverse[currentPositionIndex - cube1]);
                positionWhiteReverse[currentPositionIndex - cube1].VisualizePosition();
            }
            else if (currentPositionIndex - cube1 == 0) {
                visualizeHome = true;
            }
            else if (currentPositionIndex - cube1 < 0) {//check if u have chips at bigger positions, if so cannot enable home, if not enable home
                CheckingChipsAtBiggerPositionInLastPosWhite(currentPositionIndex);
            }
        }
        if (cube2 != 0) {
            if (currentPositionIndex - cube2 > 0) {
                listToReturn.Add(positionWhiteReverse[currentPositionIndex - cube2]);
                positionWhiteReverse[currentPositionIndex - cube2].VisualizePosition();
            }
            else if (currentPositionIndex - cube2 == 0) {
                visualizeHome = true;
            }
            else if (currentPositionIndex - cube2 < 0) {//check if u have chips at bigger positions, if so cannot enable home, if not enable home
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
                    positionBlackReverse[currentPositionIndex - cube1].VisualizePosition();
                }
                if (positionBlackReverse[currentPositionIndex - (cube1 * 2)].player != Player.FirstPlayer) {
                    listToReturn.Add(positionBlackReverse[currentPositionIndex - (cube1 * 2)]);
                    positionBlackReverse[currentPositionIndex - (cube1 * 2)].VisualizePosition();
                }
                if (positionBlackReverse[currentPositionIndex - (cube1 * 3)].player != Player.FirstPlayer) {
                    listToReturn.Add(positionBlackReverse[currentPositionIndex - (cube1 * 3)]);
                    positionBlackReverse[currentPositionIndex - (cube1 * 3)].VisualizePosition();
                }
                if (positionBlackReverse[currentPositionIndex - (cube1 * 4)].player != Player.FirstPlayer) {
                    listToReturn.Add(positionBlackReverse[currentPositionIndex - (cube1 * 4)]);
                    positionBlackReverse[currentPositionIndex - (cube1 * 4)].VisualizePosition();
                }
            }
            if (currentPositionIndex - cube1 == 0)//repeating -- extract to method
                positionBlackReverse[0].VisualizePosition();
            else if (currentPositionIndex - cube1 < 0)//check if u have chips at bigger positions, if so cannot enable home, if not enable home
                CheckingChipsAtBiggerPositionInLastPosBlack(currentPositionIndex);
            return;
        }
        if (cube1 != 0) {
            if (currentPositionIndex - cube1 > 0) {
                listToReturn.Add(positionBlackReverse[currentPositionIndex - cube1]);
                positionBlackReverse[currentPositionIndex - cube1].VisualizePosition();
            }
            else if (currentPositionIndex - cube1 == 0) {
                visualizeHome = true;
            }
            else if (currentPositionIndex - cube1 < 0) {//check if u have chips at bigger positions, if so cannot enable home, if not enable home
                CheckingChipsAtBiggerPositionInLastPosBlack(currentPositionIndex);
            }
        }
        if (cube2 != 0) {
            if (currentPositionIndex - cube2 > 0) {
                listToReturn.Add(positionBlackReverse[currentPositionIndex - cube2]);
                positionBlackReverse[currentPositionIndex - cube2].VisualizePosition();
            }
            else if (currentPositionIndex - cube2 == 0) {
                visualizeHome = true;
            }
            else if (currentPositionIndex - cube2 <= 0) {//check if u have chips at bigger positions, if so cannot enable home, if not enable home
                CheckingChipsAtBiggerPositionInLastPosBlack(currentPositionIndex);
            }
        }
        if (visualizeHome == true)
            positionBlackReverse[0].VisualizePosition();
    }

    private void VisualizeCubesAfterUndo(int cube1, int cube2) {
        cubeFirst = cube1;
        if (cubeFirst >= 1 && cubeFirst <= 6) {
            CubeVisual1.SetState((Cube)cubeFirst);
            CubeVisual1.gameObject.SetActive(true);
        }
        else {
            CubeVisual1.gameObject.SetActive(false);
        }
        cubeLast = cube2;
        if (cubeLast >= 1 && cubeLast <= 6) {
            CubeVisual2.SetState((Cube)cubeLast);
            CubeVisual2.gameObject.SetActive(true);
        }
        else {
            CubeVisual2.gameObject.SetActive(false);
        }
    }

    public void ButtonUndo() {
        if (!UndoButtonDisabled) {
            ButtonDeseletChip();

            GameSequence previousMove = gameSequence.Peek();
            if (gameWithBot && previousMove.PlayerMadeMove == BotController.GetPlayerState()) {
                int whileTrueError = 0;
                GameSequence previousMoveMadeByBot = new GameSequence();
                Player botPlayerState = BotController.GetPlayerState();
                if (botPlayerState == Player.FirstPlayer) {
                    previousMoveMadeByBot = gameSequence.Peek();
                    while (previousMoveMadeByBot.PlayerMadeMove == botPlayerState) {
                        positionWhiteReverse[previousMoveMadeByBot.FromPositionIndex].GetComponent<PositionHandle>().AddChip(chipsWhite[previousMoveMadeByBot.ChipIndex]);
                        gameSequence.Pop();
                        movesMade--;
                        previousMoveMadeByBot = gameSequence.Peek();
                        whileTrueError++;
                        if (whileTrueError > 5) {
                            Debug.LogError("whileTrueError in UndoButton");
                            return;
                        }
                    }
                    PlayerToMove = Player.SecondPlayer;
                }
                else if (botPlayerState == Player.SecondPlayer) {
                    previousMoveMadeByBot = gameSequence.Peek();
                    while (previousMoveMadeByBot.PlayerMadeMove == botPlayerState) {
                        positionBlackReverse[previousMoveMadeByBot.FromPositionIndex].GetComponent<PositionHandle>().AddChip(chipsBlack[previousMoveMadeByBot.ChipIndex]);
                        gameSequence.Pop();
                        movesMade--;
                        previousMoveMadeByBot = gameSequence.Peek();
                        whileTrueError++;
                        if (whileTrueError > 5) {
                            Debug.LogError("whileTrueError in UndoButton");
                            return;
                        }
                    }
                    PlayerToMove = Player.FirstPlayer;
                }
                previousMove = gameSequence.Peek();
            }
            if (gameRules == GameRules.SetCubes) {

                ChipButtonsDisableBoth();

                if (previousMove.PlayerMadeMove == Player.FirstPlayer) {
                    positionWhiteReverse[previousMove.FromPositionIndex].GetComponent<PositionHandle>().AddChip(chipsWhite[previousMove.ChipIndex]);
                    PlayerToMove = Player.FirstPlayer;
                    Invoke(nameof(ChipButtonsEnable), 0.5f);
                }
                else {
                    positionBlackReverse[previousMove.FromPositionIndex].GetComponent<PositionHandle>().AddChip(chipsBlack[previousMove.ChipIndex]);
                    PlayerToMove = Player.SecondPlayer;
                    Invoke(nameof(ChipButtonsEnable), 0.5f);
                }
                VisualizeCubesAfterUndo(previousMove.Cube1, previousMove.Cube2);
                pareIndex = previousMove.PareIndex;
                tookFromHead = previousMove.TookFromHead;
                movesMade--;
                gameSequence.Pop();

                if (tookFromHead)
                    Invoke(nameof(DisableChipButtonsAtSatartPos), 0.501f);
            }
            else {
                if (previousMove.PlayerMadeMove == Player.FirstPlayer) {
                    positionWhiteReverse[previousMove.FromPositionIndex].GetComponent<PositionHandle>().AddChip(chipsWhite[previousMove.ChipIndex]);
                    PlayerToMove = Player.FirstPlayer;
                }
                else {
                    positionBlackReverse[previousMove.FromPositionIndex].GetComponent<PositionHandle>().AddChip(chipsBlack[previousMove.ChipIndex]);
                    PlayerToMove = Player.SecondPlayer;
                }
                VisualizeCubesAfterUndo(previousMove.Cube1, previousMove.Cube2);
                gameSequence.Pop();
            }
            if (!gameSequence.TryPeek(out GameSequence result)) {
                UndoButton.SetActive(false);
            }
        }
    }

    private void GameSequenceAddMove(int cube1, int cube2, int chipIndex, int currentPositionIndex, Player PlayerMadeMoveNotSetCubes = Player.Empty) {
        if (gameRules == GameRules.SetCubes) {
            if (previousPositionIndex != currentPositionIndex) {
                gameSequence.Push(new GameSequence(PlayerToMove, previousPositionIndex, currentPositionIndex, cube1, cube2, chipIndex, pareIndex, tookFromHead));
            }
        }
        else {
            if (previousPositionIndex != currentPositionIndex) {
                gameSequence.Push(new GameSequence(PlayerMadeMoveNotSetCubes, previousPositionIndex, currentPositionIndex, cube1, cube2, chipIndex, pareIndex, tookFromHead));
                //Debug.Log(gameSequence.Peek().ToString());
            }
        }
    }

    private void GameSequenceAddMove(Player PlayerMadeMove, int fromPosIndex, int toPosIndex, int cube1, int cube2, int chipIndex) {
        gameSequence.Push(new GameSequence(PlayerMadeMove, fromPosIndex, toPosIndex, cube1, cube2, chipIndex, pareIndex, tookFromHead));
    }

    public void SetSelectedCubes(int firstCube, int secondCube) {
        cubeFirst = firstCube;
        cubeLast = secondCube;
        CubeVisual1.SetState((Cube)cubeFirst);
        CubeVisual2.SetState((Cube)cubeLast);
        ActivateCubesVisual();
        EnableSettingButtonInGame();
        if (gameWithBot && PlayerToMove == BotController.GetPlayerState()) {
            ChipButtonsDisableBoth();
            Generator.DisableGameObject();
            GameSequence[] movesMadeByBot = BotController.BotMadeMoves(firstCube, secondCube);
            foreach (var move in movesMadeByBot) {
                GameSequenceAddMove(move.PlayerMadeMove, move.FromPositionIndex, move.ToPositionIndex, move.Cube1, move.Cube2, move.ChipIndex);
                movesMade++;
            }
            Invoke(nameof(ChangePlayerToMove), 1.5f);
            return;
        }
        else if (gameBotVsBot) {
            ChipButtonsDisableBoth();
            Generator.DisableGameObject();
            GameSequence[] movesMadeByBot;
            if (PlayerToMove == BotController.GetPlayerState()) {
                movesMadeByBot = BotController.BotMadeMoves(firstCube, secondCube);
            }
            else {
                movesMadeByBot = BotController1.BotMadeMoves(firstCube, secondCube);
            }
            foreach (var move in movesMadeByBot) {
                GameSequenceAddMove(move.PlayerMadeMove, move.FromPositionIndex, move.ToPositionIndex, move.Cube1, move.Cube2, move.ChipIndex);
                movesMade++;
            }
            if (!botWon)
                Invoke(nameof(ChangePlayerToMove), 1.5f);
            return;
        }
        if (cubeFirst == cubeLast) { pareIndex = 4; }

        if (gameRules == GameRules.SetCubes) {
            ChipButtonsEnable();
            Generator.DisableGameObject();
        }
        else {
            ChipButtonsEnableBoth();
        }
        if (gameSequence.TryPeek(out GameSequence result)) {
            UndoButton.gameObject.SetActive(true);
        }
    }

    public void DisableMainBanner() {
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

    public void SimulationOn() {
        isSimulationOn = true;
    }

    public void SimulationOff() {
        isSimulationOn = false;
    }

    public void UndoOn() {
        if (gameSequence.TryPeek(out GameSequence result)) {
            UndoButton.SetActive(true);
        }
    }

    public void SetGeneratorOnlyMode(bool GeneratorOnly) {
        GeneratorOnlyInSetCubes = GeneratorOnly;
        //
    }

    private void CalculatePointsToWin() {
        pointsToWinWhite = 0;
        pointsToWinBlack = 0;
        for (int i = 0; i < chipsCount; i++) {
            pointsToWinWhite += positionWhiteReverse.IndexOf(chipsWhite[i].GetCurrentPosition());
            pointsToWinBlack += positionBlackReverse.IndexOf(chipsBlack[i].GetCurrentPosition());
        }
    }

    public GameRules GetGameRules() {
        return gameRules;
    }

    public int GetMovesMade() {
        return movesMade;
    }

    private void Testing() {
        int startIndex = 18;
        int endIndex = 23;
        List<PositionHandle> listToReturn = new List<PositionHandle>();
        if (startIndex > endIndex) {
            for (int i = startIndex; i > endIndex - 1; i--) {
                listToReturn.Add(positionWhiteReverse[i].GetComponent<PositionHandle>());
            }
        }
        else if (endIndex > startIndex) {
            for (int i = startIndex; i < endIndex + 1; i++) {
                listToReturn.Add(positionWhiteReverse[i].GetComponent<PositionHandle>());
            }
        }
        for (int i = 0; i < listToReturn.Count; i++) {
            for (int j = 0; j < listToReturn.Count - 1; j++)
                if (listToReturn[j].GetChipListCount() > listToReturn[j + 1].GetChipListCount()) {
                    var position = listToReturn[j];
                    listToReturn.Remove(position);
                    listToReturn.Insert(j + 1, position);
                }
        }
        for (int i = 0; i < listToReturn.Count; i++) {
            Debug.Log("sortedList, ChipCount - " + listToReturn[i].GetChipListCount() + ", position - " + listToReturn[i]);
        }

    }
}
