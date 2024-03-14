using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameModesSimulator : MonoBehaviour {
    [SerializeField] private GameObject chipWhite;//prefab
    [SerializeField] private GameObject chipBlack;

    private const string FREE_ASPECT = "AboutFreeAspectCycle";
    private const string FIXED_6 = "AboutFixed6Cycle";
    private const string SET_CUBES = "AboutSetCubesCycle";
    private const string aboutTextForFREE_ASPECT = "In this mode, all positions are open, and you can use either your own dice or generate them within the game.\nCompletely trust-based and simple.";
    private const string aboutTextForFIXED_6 = "Similar to the Free Aspect, but with a limit of 6 positions per move, using either personal dice or generating them.\nAlso trust-based.";
    private const string aboutTextForSET_CUBES = "Strictly by the rules, dice must be inserted or generated to control the game with built-in rules.";
    private string currentCoroutine;

    private List<ChipBase> chipsWhite = new List<ChipBase>();
    private List<ChipBase> chipsBlack = new List<ChipBase>();
    private List<PositionBase> positionWhiteReverse;
    private List<PositionBase> positionBlackReverse;

    private PositionsParent positionParent;
    private PositionHandle startPosWhite;
    private PositionHandle startPosBlack;
    [SerializeField] private CubeSelected Cube1;
    [SerializeField] private CubeSelected Cube2;
    [SerializeField] private Transform ChipsParent;
    [SerializeField] private TextMeshProUGUI aboutModesText;
    [SerializeField] private Transform freeAspectButtonTransform;
    [SerializeField] private Transform fixedSixButtonTransform;
    [SerializeField] private Transform setCubesButtonTransform;
    [SerializeField] private Transform selectedModeVisualTransform;
    private int chipsCount = 9;
    private GameRules gameRules;
    private float freezeTime = 1f;




    [SerializeField] int randomChipIndex;
    [SerializeField] int maxIndexPosition;
    [SerializeField] int cube1;
    [SerializeField] int cube2;
    [SerializeField] int moveIndexWithCube1;
    [SerializeField] int moveIndexWithCube2;
    [SerializeField] int whileTrueWarning;



    private void Awake() {
        positionParent = GetComponentInChildren<PositionsParent>();

    }

    private void Start() {
        positionParent.PositionsDisable();
        positionWhiteReverse = positionParent.GetPositionWhite();
        positionBlackReverse = positionParent.GetPositionBlack();
        startPosWhite = positionWhiteReverse[positionWhiteReverse.Count - 1].GetComponent<PositionHandle>();
        startPosWhite.player = Player.FirstPlayer;
        startPosBlack = positionBlackReverse[positionBlackReverse.Count - 1].GetComponent<PositionHandle>();
        startPosBlack.player = Player.SecondPlayer;
        currentCoroutine = FREE_ASPECT;
        InstantiateChips(chipsCount);
        Cube1.gameObject.SetActive(false);
        Cube2.gameObject.SetActive(false);
    }

    private void InstantiateChips(int countOfChips) {
        for (int i = 0; i < countOfChips; i++) {
            ChipBase chipFirst = Instantiate(chipWhite, ChipsParent).GetComponent<ChipBase>();
            chipsWhite.Add(chipFirst);
            ChipBase chipSecond = Instantiate(chipBlack, ChipsParent).GetComponent<ChipBase>();
            chipsBlack.Add(chipSecond);
        }
    }

    private void SetStartPositions() {
        for (int i = 0; i < positionWhiteReverse.Count - 1; i++) {
            positionWhiteReverse[i].ResetPosition();
            positionBlackReverse[i].ResetPosition();
        }
        for (int i = 0; i < chipsCount; i++) {
            startPosWhite.AddChip(chipsWhite[i]);
            startPosBlack.AddChip(chipsBlack[i]);
        }
        startPosWhite.player = Player.FirstPlayer;
        startPosBlack.player = Player.SecondPlayer;
    }

    public void ButtonAboutFreeAspect() {
        UnvisualizeAllPositions();
        StopCoroutine(currentCoroutine);
        if (currentCoroutine == SET_CUBES) {
            Cube1.gameObject.SetActive(false);
            Cube2.gameObject.SetActive(false);
        }
        SetStartPositions();
        currentCoroutine = FREE_ASPECT;
        StartCoroutine(currentCoroutine);
        UpdateAboutModesInfo();
    }

    public void ButtonAboutFixed6() {
        UnvisualizeAllPositions();
        StopCoroutine(currentCoroutine);
        if (currentCoroutine == SET_CUBES) {
            Cube1.gameObject.SetActive(false);
            Cube2.gameObject.SetActive(false);
        }
        SetStartPositions();
        currentCoroutine = FIXED_6;
        StartCoroutine(currentCoroutine);
        UpdateAboutModesInfo();
    }

    public void ButtonAboutSetCubes() {
        UnvisualizeAllPositions();
        StopCoroutine(currentCoroutine);
        SetStartPositions();
        currentCoroutine = SET_CUBES;
        StartCoroutine(currentCoroutine);
        UpdateAboutModesInfo();
        Cube1.gameObject.SetActive(true);
        Cube2.gameObject.SetActive(true);
    }

    public void StartPreviousCoroutine() {
        if (chipsBlack.Count > 0) {
            UnvisualizeAllPositions();
            SetStartPositions();
            if (currentCoroutine == null) {
                currentCoroutine = FREE_ASPECT;
            }
            StartCoroutine(currentCoroutine);
            UpdateAboutModesInfo();
        }
        else {// call in first AboutModesOpen Button Click (before start)
            Invoke(nameof(StartPreviousCoroutine), 0.1f);//calling method after start and setting positions and chips
        }
    }


    private IEnumerator AboutFreeAspectCycle() {
        int randomChipIndex;
        int randomPositionIndex;
        for (int i = 0; i < (chipsCount + 1) / 2; i++) {
            do {
                randomChipIndex = Random.Range(0, chipsWhite.Count);
            } while (chipsWhite[randomChipIndex].GetCurrentPosition().state == PositionState.WhiteLastPosition);
            int maxIndexPosition = positionWhiteReverse.IndexOf(chipsWhite[randomChipIndex].GetCurrentPosition());
            do {
                int minIndexPosition = maxIndexPosition - 11 <= 0 ? 1 : maxIndexPosition - 11;
                randomPositionIndex = Random.Range(minIndexPosition, maxIndexPosition);
            }
            while (positionWhiteReverse[randomPositionIndex].player == Player.SecondPlayer);

            for (int j = maxIndexPosition; j > 0; j--) {
                if (!(positionWhiteReverse[j].player == Player.SecondPlayer)) {
                    positionWhiteReverse[j].VisualizePosition();
                    yield return new WaitForSeconds(0.05f);
                }
            }
            positionWhiteReverse[randomPositionIndex].GetComponent<PositionHandle>().AddChip(chipsWhite[randomChipIndex]);
            positionWhiteReverse[randomPositionIndex].player = Player.FirstPlayer;
            UnvisualizeAllPositions();
            yield return new WaitForSeconds(1f);

            do {
                randomChipIndex = Random.Range(0, chipsBlack.Count);
            } while (chipsBlack[randomChipIndex].GetCurrentPosition().state == PositionState.BlackLastPosition);
            maxIndexPosition = positionBlackReverse.IndexOf(chipsBlack[randomChipIndex].GetCurrentPosition());
            do {
                int minIndexPosition = maxIndexPosition - 11 <= 0 ? 1 : maxIndexPosition - 11;
                randomPositionIndex = Random.Range(minIndexPosition, maxIndexPosition);
            }
            while (positionBlackReverse[randomPositionIndex].player == Player.FirstPlayer);
            for (int j = maxIndexPosition; j > 0; j--) {
                if (!(positionBlackReverse[j].player == Player.FirstPlayer)) {
                    positionBlackReverse[j].VisualizePosition();
                    yield return new WaitForSeconds(0.05f);
                }
            }
            positionBlackReverse[randomPositionIndex].GetComponent<PositionHandle>().AddChip(chipsBlack[randomChipIndex]);
            positionBlackReverse[randomPositionIndex].player = Player.SecondPlayer;
            UnvisualizeAllPositions();

            yield return new WaitForSeconds(1f);
        }
        SetStartPositions();
        yield return new WaitForSeconds(1f);
        StartCoroutine(currentCoroutine);
    }

    private IEnumerator AboutFixed6Cycle() {
        int randomChipIndex = 0;
        int randomPositionIndex = 0;

        for (int i = 0; i < (chipsCount + 1) / 2; i++) {
            randomChipIndex = Random.Range(0, chipsWhite.Count);
            int maxIndexPosition = positionWhiteReverse.IndexOf(chipsWhite[randomChipIndex].GetCurrentPosition());
            do {
                int minIndexPosition = maxIndexPosition - 6 <= 0 ? 1 : maxIndexPosition - 6;
                randomPositionIndex = Random.Range(minIndexPosition, maxIndexPosition);
            }
            while (positionWhiteReverse[randomPositionIndex].player == Player.SecondPlayer);
            for (int j = maxIndexPosition; j >= maxIndexPosition - 6; j--) {
                positionWhiteReverse[j].VisualizePosition();
                yield return new WaitForSeconds(0.1f);
            }
            positionWhiteReverse[randomPositionIndex].GetComponent<PositionHandle>().AddChip(chipsWhite[randomChipIndex]);
            positionWhiteReverse[randomPositionIndex].player = Player.FirstPlayer;
            UnvisualizeAllPositions();
            yield return new WaitForSeconds(1f);
            randomChipIndex = Random.Range(0, chipsBlack.Count);
            maxIndexPosition = positionBlackReverse.IndexOf(chipsBlack[randomChipIndex].GetCurrentPosition());
            do {
                int minIndexPosition = maxIndexPosition - 6 <= 0 ? 1 : maxIndexPosition - 6;
                randomPositionIndex = Random.Range(minIndexPosition, maxIndexPosition);
            }
            while (positionBlackReverse[randomPositionIndex].player == Player.FirstPlayer);
            for (int j = maxIndexPosition; j >= maxIndexPosition - 6; j--) {
                positionBlackReverse[j].VisualizePosition();
                yield return new WaitForSeconds(0.1f);
            }
            positionBlackReverse[randomPositionIndex].GetComponent<PositionHandle>().AddChip(chipsBlack[randomChipIndex]);
            positionBlackReverse[randomPositionIndex].player = Player.SecondPlayer;
            UnvisualizeAllPositions();

            yield return new WaitForSeconds(1f);
        }
        SetStartPositions();
        yield return new WaitForSeconds(1f);
        StartCoroutine(currentCoroutine);
    }

    private IEnumerator AboutSetCubesCycle() {
        List<ChipBase> activeChipWhite = new List<ChipBase>();
        List<ChipBase> activeChipBlack = new List<ChipBase>();
        //int randomChipIndex;
        //int maxIndexPosition;
        //int cube1;
        //int cube2;
        //ushort moveIndexWithCube1;
        //ushort moveIndexWithCube2;
        //int whileTrueWarning;
        float yieldWaitForSeconds = 0.2f;
        //generate first cube
        //generate second cube - not the same as first to not complicate pare movement
        //take random chip to made move, add chip to list with chips that can move
        //took from head true, cannot choose from all chip only from list above
        //


        for (int i = 0; i < (chipsCount + 1) / 2; i++) {
            cube1 = Random.Range(1, 7);
            Cube1.SetState((Cube)cube1);
            do {
                cube2 = Random.Range(1, 7);
                //Debug.Log(cube1 + " - cube1" + cube2 + " - cube2");
                yield return new WaitForSeconds(yieldWaitForSeconds);
            }
            while (cube2 == cube1);
            Cube2.SetState((Cube)cube2);

            do {
                randomChipIndex = Random.Range(0, chipsWhite.Count);
                maxIndexPosition = positionWhiteReverse.IndexOf(chipsWhite[randomChipIndex].GetCurrentPosition());
                moveIndexWithCube1 = (maxIndexPosition - cube1) < 0 ? 0 : (maxIndexPosition - cube1);
                moveIndexWithCube2 = (maxIndexPosition - cube2) < 0 ? 0 : (maxIndexPosition - cube2);
            }
            while (positionWhiteReverse[moveIndexWithCube1].player == Player.SecondPlayer);
            positionWhiteReverse[moveIndexWithCube1].VisualizePosition();
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionWhiteReverse[moveIndexWithCube2].VisualizePosition();
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionWhiteReverse[moveIndexWithCube1].GetComponent<PositionHandle>()?.AddChip(chipsWhite[randomChipIndex]);
            positionWhiteReverse[moveIndexWithCube1].player = Player.FirstPlayer;
            if (!activeChipWhite.Contains(chipsWhite[randomChipIndex]))
                activeChipWhite.Add(chipsWhite[randomChipIndex]);
            Cube1.SetState(Cube.Null);
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionWhiteReverse[moveIndexWithCube1].UnvisualizePosition();
            positionWhiteReverse[moveIndexWithCube2].UnvisualizePosition();
            yield return new WaitForSeconds(1f);

            whileTrueWarning = 0;

            do {
                randomChipIndex = Random.Range(0, activeChipWhite.Count);
                maxIndexPosition = positionWhiteReverse.IndexOf(activeChipWhite[randomChipIndex].GetCurrentPosition());
                moveIndexWithCube2 = (maxIndexPosition - cube2) < 0 ? 0 : (maxIndexPosition - cube2);
                whileTrueWarning += 1;
                //Debug.Log(whileTrueWarning + "-whileTrueWarning.  while-" + (positionWhiteReverse[moveIndexWithCube2].player != Player.SecondPlayer) + ". moveIndexWithCube2-" + moveIndexWithCube2);
                if (whileTrueWarning > 5) {
                    SetStartPositions();
                    yield return new WaitForSeconds(1f);
                    Invoke(nameof(StartPreviousCoroutine), freezeTime);
                    StopCoroutine(currentCoroutine);
                    yield return null;
                }
            } while (positionWhiteReverse[moveIndexWithCube2].player == Player.SecondPlayer);
            positionWhiteReverse[moveIndexWithCube2].VisualizePosition();
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionWhiteReverse[moveIndexWithCube2].GetComponent<PositionHandle>()?.AddChip(activeChipWhite[randomChipIndex]);
            positionWhiteReverse[moveIndexWithCube2].player = Player.FirstPlayer;
            Cube2.SetState(Cube.Null);
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionWhiteReverse[moveIndexWithCube2].UnvisualizePosition();
            yield return new WaitForSeconds(1f);

            //for black
            cube1 = Random.Range(1, 7);
            Cube1.SetState((Cube)cube1);
            do {
                cube2 = Random.Range(1, 7);
                //Debug.Log(cube1 + " - cube1" + cube2 + " - cube2");
                yield return new WaitForSeconds(yieldWaitForSeconds);
            }
            while (cube2 == cube1);
            Cube2.SetState((Cube)cube2);

            do {
                randomChipIndex = Random.Range(0, chipsBlack.Count);
                maxIndexPosition = positionBlackReverse.IndexOf(chipsBlack[randomChipIndex].GetCurrentPosition());
                moveIndexWithCube1 = (maxIndexPosition - cube1) < 0 ? 0 : (maxIndexPosition - cube1);
                moveIndexWithCube2 = (maxIndexPosition - cube2) < 0 ? 0 : (maxIndexPosition - cube2);
            }
            while (positionBlackReverse[moveIndexWithCube1].player == Player.FirstPlayer);
            positionBlackReverse[moveIndexWithCube1].VisualizePosition();
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionBlackReverse[moveIndexWithCube2].VisualizePosition();
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionBlackReverse[moveIndexWithCube1].GetComponent<PositionHandle>()?.AddChip(chipsBlack[randomChipIndex]);
            positionBlackReverse[moveIndexWithCube1].player = Player.SecondPlayer;
            if (!activeChipBlack.Contains(chipsBlack[randomChipIndex]))
                activeChipBlack.Add(chipsBlack[randomChipIndex]);
            Cube1.SetState(Cube.Null);
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionBlackReverse[moveIndexWithCube1].UnvisualizePosition();
            positionBlackReverse[moveIndexWithCube2].UnvisualizePosition();
            yield return new WaitForSeconds(1f);

            whileTrueWarning = 0;

            do {
                randomChipIndex = Random.Range(0, activeChipBlack.Count);
                maxIndexPosition = positionBlackReverse.IndexOf(activeChipBlack[randomChipIndex].GetCurrentPosition());
                moveIndexWithCube2 = (maxIndexPosition - cube2) < 0 ? 0 : (maxIndexPosition - cube2);
                whileTrueWarning += 1;
                //Debug.Log(whileTrueWarning + "-whileTrueWarning.  while-" + (positionBlackReverse[moveIndexWithCube2].player != Player.SecondPlayer) + ". moveIndexWithCube2-" + moveIndexWithCube2);
                if (whileTrueWarning > 5) {
                    SetStartPositions();
                    yield return new WaitForSeconds(1f);
                    Invoke(nameof(StartPreviousCoroutine), freezeTime);
                    StopCoroutine(currentCoroutine);
                    yield return null;
                }
            } while (positionBlackReverse[moveIndexWithCube2].player == Player.FirstPlayer);
            positionBlackReverse[moveIndexWithCube2].VisualizePosition();
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionBlackReverse[moveIndexWithCube2].GetComponent<PositionHandle>()?.AddChip(activeChipBlack[randomChipIndex]);
            positionBlackReverse[moveIndexWithCube2].player = Player.SecondPlayer;
            Cube2.SetState(Cube.Null);
            yield return new WaitForSeconds(yieldWaitForSeconds);
            positionBlackReverse[moveIndexWithCube2].UnvisualizePosition();
            yield return new WaitForSeconds(1f);
        }
        SetStartPositions();
        yield return new WaitForSeconds(1f);
        StartCoroutine(currentCoroutine);

    }

    private void UnvisualizeAllPositions() {
        foreach (var position in positionWhiteReverse) {
            position.UnvisualizePosition();
        }
    }

    private void UpdateAboutModesInfo() {
        if (currentCoroutine == FREE_ASPECT) { 
            aboutModesText.text = aboutTextForFREE_ASPECT;
            selectedModeVisualTransform.position = freeAspectButtonTransform.position;
        }
        else if (currentCoroutine == FIXED_6) { 
            aboutModesText.text = aboutTextForFIXED_6;
            selectedModeVisualTransform.position = fixedSixButtonTransform.position;
        }
        else if (currentCoroutine == SET_CUBES) { 
            aboutModesText.text = aboutTextForSET_CUBES;
            selectedModeVisualTransform.position = setCubesButtonTransform.position;
        }
    }
}