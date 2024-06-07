using System;
using System.Collections.Generic;
using UnityEngine;

public class BackgammonDecisionSystem : MonoBehaviour {
    //need to separate moves and scores when moving last chip in home[1-6] this is move to home and then each move in home
    //idea to remake moves by cubeBased : saparate logic to find move cu each cube and mix up moves
    //controlling in home moves: update pareindex and cube=0 in makeMoves and than run makeMoveInHome

    //need to change scores, higher up (+1 to +3)

    [SerializeField] private Game game;
    [SerializeField] private PositionsParent positionParent;
    [SerializeField] private int[] testChipIndexesInGame;

    private HomePositionHandler homePosition;
    private int chipsCount;
    private GameSequence bestMove;
    private GameSequence[] movesMade;
    private Player player;
    private Player opState;
    private bool tookFromHead;
    private bool movesWereMade;
    private bool botWon = false;
    private int cubeFirst;
    private int cubeLast;
    private int pareIndex = -1;
    private int chipIndexToMove;
    private int realPositionIndex;//need when all chips at home
    private int movesMadeCount;
    private PositionStateStruct[] boardState;
    private List<PositionBase> positions;
    private List<PositionBase> opponentPositions;
    private List<ChipBase> chipList;
    private List<int> chipIndexesInGame;

    private List<GameSequence[]> allPossibleMoves;
    private List<float> allPossibleMoveScores;


    public void GameStarted(int side) {//0 - white, 1 - black
        chipIndexesInGame = new List<int>();
        positions = new List<PositionBase>();
        if (side == 0) {
            positions = positionParent.GetPositionWhite();
            opponentPositions = positionParent.GetPositionBlack();
        }
        else {
            positions = positionParent.GetPositionBlack();
            opponentPositions = positionParent.GetPositionWhite();
        }
        homePosition = positions[0].GetComponent<HomePositionHandler>();
        player = (Player)side;
    }

    private void AsignBoardState() {
        boardState = new PositionStateStruct[25];
        boardState[0].playerState = Player.Empty;
        for (int i = 1; i < boardState.Length; i++) {
            boardState[i].positionIndex = i;
            boardState[i].playerState = positions[i].player;
            boardState[i].chipsCount = positions[i].GetComponent<PositionHandle>().GetChipListCount();
        }
    }

    public GameSequence[] BotMadeMoves(int cube1, int cube2) {
        AsignBoardState();
        cubeFirst = cube1;
        cubeLast = cube2;
        bool allInHome = !HaveingChipAtPositions(24, 7);
        if (allInHome) {
            movesMade = FindAndMoveInHome();
        }
        else {
            if (cubeFirst == cubeLast) {
                pareIndex = 4;
                movesMade = FindAllPossibleMovesPair();
            }
            else {
                pareIndex = 0;
                movesMade = FindAllPossibleMovesNotPair();
            }
            MakeMoves();
        }
        return movesMade;
    }

    private GameSequence[] FindAllPossibleMovesNotPair() {
        allPossibleMoves = new List<GameSequence[]>();
        allPossibleMoveScores = new List<float>();
        //first is this moves possible
        List<int> chipIndexes = new List<int>();
        List<int[]> positionChipIndexesAndCube = new List<int[]>();
        bool notTheFirstCube;
        bool notTheSecondCube;
        opState = player == Player.FirstPlayer ? Player.SecondPlayer : Player.FirstPlayer;

        //asign all chip indexes in game
        if (boardState[24].chipsCount > 0 && boardState[24].playerState == player) {
            chipIndexes.Add(chipList.IndexOf(positions[24].GetComponent<PositionHandle>().GetChipList()[0]));
            positionChipIndexesAndCube.Add(new int[] { 24, chipList.IndexOf(positions[24].GetComponent<PositionHandle>().GetChipList()[0]), 0 });
            Debug.Log("boardState[24].chipsCount > 0 -- TRUE");
        }
        for (int i = 0; i < chipIndexesInGame.Count; i++) {
            chipIndexes.Add(chipIndexesInGame[i]);
            positionChipIndexesAndCube.Add(new int[] { positions.IndexOf(chipList[i].GetCurrentPosition()), chipIndexesInGame[i], 0 });
        }

        //shuffle indexes for cube1 != cube2
        for (int i = 0; i < positionChipIndexesAndCube.Count; i++) {
            notTheFirstCube = false;
            notTheSecondCube = false;
            if (positionChipIndexesAndCube[i][0] - cubeFirst <= 0 || positions[positionChipIndexesAndCube[i][0] - cubeFirst].player == opState) {
                //not the first cube for i
                notTheFirstCube = true;
            }
            if (positionChipIndexesAndCube[i][0] - cubeLast <= 0 || positions[positionChipIndexesAndCube[i][0] - cubeLast].player == opState) {
                //neither of cubes for this chip to start so go on to the next one
                notTheSecondCube = true;
            }
            if (notTheSecondCube && notTheFirstCube) {
                continue;
            }
            for (int j = 0; j < positionChipIndexesAndCube.Count; j++) {//fix with the same index i == j
                //if i == j need to add cube from i cycle ex:(i not cube1 so it is cube2, in j cycle need to j+cube2)
                //need to check here if board.player is now op so add index or break
                if (notTheFirstCube) {// positionChipIndexesAndCube[i][0] can move with cubeLast
                    //add possible move indexes
                    if (i == j) {
                        if ((positionChipIndexesAndCube[j][0] - cubeLast - cubeFirst) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeLast - cubeFirst].player == opState)) {
                            allPossibleMoves.Add(new GameSequence[] {new GameSequence(player,positionChipIndexesAndCube[i][0],positionChipIndexesAndCube[i][0]-cubeLast,cubeFirst,cubeLast,positionChipIndexesAndCube[i][1],pareIndex,tookFromHead),
                                new GameSequence(player,positionChipIndexesAndCube[j][0]-cubeLast,positionChipIndexesAndCube[j][0]-cubeLast-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex,tookFromHead) });
                        }
                    }
                    else if ((positionChipIndexesAndCube[j][0] - cubeFirst) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeFirst].player == opState)) {
                        allPossibleMoves.Add(new GameSequence[] { new GameSequence(player,positionChipIndexesAndCube[i][0],positionChipIndexesAndCube[i][0]-cubeLast,cubeFirst,cubeLast,positionChipIndexesAndCube[i][1],pareIndex,tookFromHead),
                        new GameSequence(player,positionChipIndexesAndCube[j][0],positionChipIndexesAndCube[j][0]-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex,tookFromHead)});
                    }
                }
                else if (notTheSecondCube) {// positionChipIndexesAndCube[i][0] can move with cubeFirst
                    if (i == j) {
                        if ((positionChipIndexesAndCube[j][0] - cubeFirst - cubeLast) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeFirst - cubeLast].player == opState)) {
                            allPossibleMoves.Add(new GameSequence[] { new GameSequence(player,positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0]-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[i][1],pareIndex,tookFromHead),
                            new GameSequence(player,positionChipIndexesAndCube[j][0]-cubeFirst, positionChipIndexesAndCube[j][0]-cubeFirst-cubeLast,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex,tookFromHead)});
                        }
                    }
                    else if ((positionChipIndexesAndCube[j][0] - cubeLast) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeLast].player == opState)) {
                        allPossibleMoves.Add(new GameSequence[] { new GameSequence(player,positionChipIndexesAndCube[i][0],positionChipIndexesAndCube[i][0]-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[i][1],pareIndex,tookFromHead),
                        new GameSequence(player,positionChipIndexesAndCube[j][0],positionChipIndexesAndCube[j][0]-cubeLast,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex,tookFromHead)});
                    }
                }
                else {// in i cycle can move first or second cubes
                    if (i == j) {
                        //check if can move cube1-i cube2-j+cube1
                        if ((positionChipIndexesAndCube[i][0] - cubeFirst) > 0 && !(positions[positionChipIndexesAndCube[i][0] - cubeFirst].player == opState)) {
                            if ((positionChipIndexesAndCube[j][0] - cubeFirst - cubeLast) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeFirst - cubeLast].player == opState)) {
                                allPossibleMoves.Add(new GameSequence[] { new GameSequence(player,positionChipIndexesAndCube[i][0],positionChipIndexesAndCube[i][0]-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[i][1],pareIndex,tookFromHead),
                                new GameSequence(player,positionChipIndexesAndCube[j][0]-cubeFirst,positionChipIndexesAndCube[j][0]-cubeFirst-cubeLast,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex,tookFromHead)});
                            }
                        }
                        //check if can move cube2-i cube1-j+cube2
                        if ((positionChipIndexesAndCube[i][0] - cubeLast) > 0 && !(positions[positionChipIndexesAndCube[i][0] - cubeLast].player == opState)) {
                            if ((positionChipIndexesAndCube[j][0] - cubeLast - cubeFirst) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeLast - cubeFirst].player == opState)) {
                                allPossibleMoves.Add(new GameSequence[] {new GameSequence(player,positionChipIndexesAndCube[i][0],positionChipIndexesAndCube[i][0]-cubeLast,cubeFirst,cubeLast,positionChipIndexesAndCube[i][1],pareIndex,tookFromHead),
                                new GameSequence(player,positionChipIndexesAndCube[j][0]-cubeLast,positionChipIndexesAndCube[j][0]-cubeLast-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex,tookFromHead) });
                            }
                        }
                        continue;
                    }
                    //check if can move cube1-i cube2-j
                    if ((positionChipIndexesAndCube[i][0] - cubeFirst) > 0 && !(positions[positionChipIndexesAndCube[i][0] - cubeFirst].player == opState)) {
                        if ((positionChipIndexesAndCube[j][0] - cubeLast) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeLast].player == opState)) {
                            allPossibleMoves.Add(new GameSequence[] { new GameSequence(player,positionChipIndexesAndCube[i][0],positionChipIndexesAndCube[i][0]-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[i][1],pareIndex,tookFromHead),
                            new GameSequence(player,positionChipIndexesAndCube[j][0],positionChipIndexesAndCube[j][0]-cubeLast,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex,tookFromHead)});
                        }
                    }
                    //check if can move cube2-i cube1-j
                    if ((positionChipIndexesAndCube[i][0] - cubeLast) > 0 && !(positions[positionChipIndexesAndCube[i][0] - cubeLast].player == opState)) {
                        if ((positionChipIndexesAndCube[j][0] - cubeFirst) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeFirst].player == opState)) {
                            allPossibleMoves.Add(new GameSequence[] { new GameSequence(player,positionChipIndexesAndCube[i][0],positionChipIndexesAndCube[i][0]-cubeLast,cubeFirst,cubeLast,positionChipIndexesAndCube[i][1],pareIndex,tookFromHead),
                            new GameSequence(player,positionChipIndexesAndCube[j][0],positionChipIndexesAndCube[j][0]-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex,tookFromHead)});
                        }
                    }
                }
                if ((positionChipIndexesAndCube[j][0] - cubeFirst) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeFirst].player == opState)) {
                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex, tookFromHead) });
                }
                if ((positionChipIndexesAndCube[j][0] - cubeLast) > 0 && !(positions[positionChipIndexesAndCube[j][0] - cubeLast].player == opState)) {
                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeLast, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex, tookFromHead) });
                }
            }
            if ((positionChipIndexesAndCube[i][0] - cubeFirst) > 0 && !(positions[positionChipIndexesAndCube[i][0] - cubeFirst].player == opState)) {
                allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) });
            }
            //check if can move cube2-i cube1-j
            if ((positionChipIndexesAndCube[i][0] - cubeLast) > 0 && !(positions[positionChipIndexesAndCube[i][0] - cubeLast].player == opState)) {
                allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeLast, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) });
            }
        }

        float summaryScore = 0;
        //Second assign score summary for moves
        for (int i = 0; i < allPossibleMoves.Count; i++) {
            for (int j = 0; j < allPossibleMoves[i].Length; j++) {
                summaryScore += GetScoreForMove(allPossibleMoves[i][j]);
            }
            allPossibleMoveScores.Add(summaryScore);
            summaryScore = 0;
        }

        if (allPossibleMoves.Count != allPossibleMoveScores.Count) {
            throw new InvalidOperationException("Mismatch between move count and score count");
        }

        //Third find best score for moves
        int bestMoveIndex = 0;
        for (int m = 0; m < allPossibleMoveScores.Count; m++) {
            if (allPossibleMoveScores[bestMoveIndex] < allPossibleMoveScores[m]) {
                bestMoveIndex = m;
            }
        }

        if (allPossibleMoves.Count > 0)
            return allPossibleMoves[bestMoveIndex];//error beacouse there is empty allPossibleMoves so that is no 0 index
        return null;
    }

    private GameSequence[] FindAllPossibleMovesPair() {
        allPossibleMoves = new List<GameSequence[]>();
        allPossibleMoveScores = new List<float>();
        //first is this moves possible
        List<int> chipIndexes = new List<int>();
        List<int[]> positionChipIndexesAndCube = new List<int[]>();// posIndex, chipIndex, andCube(0)
        opState = player == Player.FirstPlayer ? Player.SecondPlayer : Player.FirstPlayer;
        //tookFromHead = false;

        if (game.GetMovesMade() < 2 && cubeFirst == 6) {
            return new GameSequence[] { new GameSequence(player, 24, 24-cubeFirst, cubeFirst, cubeLast, 0, pareIndex, tookFromHead),
                    new GameSequence(player,24,24-cubeFirst,cubeFirst,cubeLast, 1,pareIndex-1,tookFromHead)};

        }

        //asign all chip indexes in game
        if (boardState[24].chipsCount > 0 && boardState[24].playerState == player) {
            chipIndexes.Add(chipList.IndexOf(positions[24].GetComponent<PositionHandle>().GetChipList()[0]));
            positionChipIndexesAndCube.Add(new int[] { 24, chipList.IndexOf(positions[24].GetComponent<PositionHandle>().GetChipList()[0]), 0 });
        }
        for (int i = 0; i < chipIndexesInGame.Count; i++) {
            chipIndexes.Add(chipIndexesInGame[i]);
            positionChipIndexesAndCube.Add(new int[] { positions.IndexOf(chipList[i].GetCurrentPosition()), chipIndexesInGame[i], 0 });
        }




        for (int i = 0; i < positionChipIndexesAndCube.Count; i++) {
            if ((positionChipIndexesAndCube[i][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[i][0] - cubeFirst].playerState == opState) {
                //cant move
                continue;
            }
            for (int j = 0; j < positionChipIndexesAndCube.Count; j++) {
                if (i == j) {
                    if ((positionChipIndexesAndCube[j][0] - (cubeFirst * 2)) <= 0 || boardState[positionChipIndexesAndCube[j][0] - (cubeFirst * 2)].playerState == opState) {
                        //cant move, add only one move i
                        continue;
                    }
                }
                else if ((positionChipIndexesAndCube[j][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[j][0] - cubeFirst].playerState == opState) {
                    //cant move, add only one move i
                    continue;
                }
                for (int k = 0; k < positionChipIndexesAndCube.Count; k++) {
                    if (i == j) {
                        if (i == k) {
                            if ((positionChipIndexesAndCube[k][0] - (cubeFirst * 3)) <= 0 || boardState[positionChipIndexesAndCube[k][0] - (cubeFirst * 3)].playerState == opState) {
                                continue;
                            }
                        }
                        else {
                            if ((positionChipIndexesAndCube[k][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[k][0] - cubeFirst].playerState == opState) {
                                continue;
                            }
                        }
                    }
                    else if (j == k || i == k) {
                        if ((positionChipIndexesAndCube[k][0] - (cubeFirst * 2)) <= 0 || boardState[positionChipIndexesAndCube[k][0] - (cubeFirst * 2)].playerState == opState) {
                            continue;
                        }
                    }
                    else if ((positionChipIndexesAndCube[k][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[k][0] - cubeFirst].playerState == opState) {
                        //cant move, add only moves i j
                        continue;
                    }
                    for (int l = 0; l < positionChipIndexesAndCube.Count; l++) {
                        if (i == j) {
                            if (i == k) {
                                if (i == l) {//i == j == k == l
                                    if ((positionChipIndexesAndCube[l][0] - (cubeFirst * 4)) <= 0 || boardState[positionChipIndexesAndCube[l][0] - (cubeFirst * 4)].playerState == opState) {
                                        continue;
                                    }
                                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                    new GameSequence(player, positionChipIndexesAndCube[j][0]- cubeFirst, positionChipIndexesAndCube[j][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                    new GameSequence(player, positionChipIndexesAndCube[k][0]- (cubeFirst*2), positionChipIndexesAndCube[k][0] - (cubeFirst*3), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                    new GameSequence(player, positionChipIndexesAndCube[l][0]-(cubeFirst*3), positionChipIndexesAndCube[l][0] - (cubeFirst*4),cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                }
                                else {//i == j == k != l
                                    if ((positionChipIndexesAndCube[l][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[l][0] - cubeFirst].playerState == opState) {
                                        continue;
                                    }
                                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                    new GameSequence(player, positionChipIndexesAndCube[j][0]- cubeFirst, positionChipIndexesAndCube[j][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                    new GameSequence(player, positionChipIndexesAndCube[k][0]- (cubeFirst*2), positionChipIndexesAndCube[k][0] - (cubeFirst*3), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                    new GameSequence(player, positionChipIndexesAndCube[l][0], positionChipIndexesAndCube[l][0] - cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                }
                            }
                            else {//i != k
                                if (i == l) {//same as j == l, so i == j == l != k
                                    if ((positionChipIndexesAndCube[l][0] - (cubeFirst * 3)) <= 0 || boardState[positionChipIndexesAndCube[l][0] - (cubeFirst * 3)].playerState == opState) {
                                        continue;
                                    }
                                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                    new GameSequence(player, positionChipIndexesAndCube[j][0]- cubeFirst, positionChipIndexesAndCube[j][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex - 1, tookFromHead),
                                    new GameSequence(player, positionChipIndexesAndCube[k][0], positionChipIndexesAndCube[k][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                    new GameSequence(player, positionChipIndexesAndCube[l][0]-(cubeFirst*2), positionChipIndexesAndCube[l][0] - (cubeFirst*3),cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                }
                                else {//i!=l
                                    if (k == l) {//i==j!=k==l
                                        if ((positionChipIndexesAndCube[l][0] - (cubeFirst * 2)) <= 0 || boardState[positionChipIndexesAndCube[l][0] - (cubeFirst * 2)].playerState == opState) {
                                            continue;
                                        }
                                        allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                            new GameSequence(player, positionChipIndexesAndCube[j][0]- cubeFirst, positionChipIndexesAndCube[j][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex - 1, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[k][0], positionChipIndexesAndCube[k][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[l][0]-cubeFirst, positionChipIndexesAndCube[l][0] - (cubeFirst*2),cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                    }
                                    else {//i==j!=k!=l
                                        if ((positionChipIndexesAndCube[l][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[l][0] - cubeFirst].playerState == opState) {
                                            continue;
                                        }
                                        allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                            new GameSequence(player, positionChipIndexesAndCube[j][0]- cubeFirst, positionChipIndexesAndCube[j][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[k][0], positionChipIndexesAndCube[k][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[l][0], positionChipIndexesAndCube[l][0],cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                    }
                                }
                            }
                        }
                        else if (k == i) {
                            if (i == l) {//i==k==l!=j
                                if ((positionChipIndexesAndCube[l][0] - (cubeFirst * 3)) <= 0 || boardState[positionChipIndexesAndCube[l][0] - (cubeFirst * 3)].playerState == opState) {
                                    continue;
                                }
                                allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                new GameSequence(player, positionChipIndexesAndCube[k][0]-cubeFirst, positionChipIndexesAndCube[k][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                new GameSequence(player, positionChipIndexesAndCube[l][0]-(cubeFirst*2), positionChipIndexesAndCube[l][0] - (cubeFirst*3),cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                            }
                            else {
                                if (j == l) {//i==k!=l==j
                                    if ((positionChipIndexesAndCube[l][0] - (cubeFirst * 2)) <= 0 || boardState[positionChipIndexesAndCube[l][0] - (cubeFirst * 2)].playerState == opState) {
                                        continue;
                                    }
                                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                            new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[k][0]-cubeFirst, positionChipIndexesAndCube[k][0] - (cubeFirst * 2), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[l][0]-cubeFirst, positionChipIndexesAndCube[l][0] - (cubeFirst*2),cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                }
                                else {//i==k!=l!=j
                                    if ((positionChipIndexesAndCube[l][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[l][0] - cubeFirst].playerState == opState) {
                                        continue;
                                    }
                                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                            new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[k][0] - cubeFirst, positionChipIndexesAndCube[k][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[l][0], positionChipIndexesAndCube[l][0] - cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                }
                            }
                        }
                        else if (k == j) {
                            if (k == l) {//i!=j==k==l
                                if ((positionChipIndexesAndCube[l][0] - (cubeFirst * 3)) <= 0 || boardState[positionChipIndexesAndCube[l][0] - (cubeFirst * 3)].playerState == opState) {
                                    continue;
                                }
                                allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                new GameSequence(player, positionChipIndexesAndCube[k][0] - cubeFirst, positionChipIndexesAndCube[k][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                new GameSequence(player, positionChipIndexesAndCube[l][0]-(cubeFirst*2), positionChipIndexesAndCube[l][0] - (cubeFirst*3),cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                            }
                            else {
                                if (i == l) {//i==l!=k==j
                                    if ((positionChipIndexesAndCube[l][0] - (cubeFirst * 2)) <= 0 || boardState[positionChipIndexesAndCube[l][0] - (cubeFirst * 2)].playerState == opState) {
                                        continue;
                                    }
                                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                            new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[k][0]-cubeFirst, positionChipIndexesAndCube[k][0] - (cubeFirst * 2), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                            new GameSequence(player, positionChipIndexesAndCube[l][0]-cubeFirst, positionChipIndexesAndCube[l][0] - (cubeFirst*2),cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                }
                                else {//i!=l!=k==j
                                    if ((positionChipIndexesAndCube[l][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[l][0] - cubeFirst].playerState == opState) {
                                        continue;
                                    }
                                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                                    new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                                    new GameSequence(player, positionChipIndexesAndCube[k][0]-cubeFirst, positionChipIndexesAndCube[k][0] - (cubeFirst * 2), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                                    new GameSequence(player, positionChipIndexesAndCube[l][0], positionChipIndexesAndCube[l][0] - cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                                }
                            }
                        }
                        else {//i!=j!=k!=l
                            if (l == i || l == j || l == k) {
                                if ((positionChipIndexesAndCube[l][0] - (cubeFirst * 2)) <= 0 || boardState[positionChipIndexesAndCube[l][0] - (cubeFirst * 2)].playerState == opState) {
                                    continue;
                                }
                                allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                            new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                            new GameSequence(player, positionChipIndexesAndCube[k][0], positionChipIndexesAndCube[k][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                            new GameSequence(player, positionChipIndexesAndCube[l][0] - cubeFirst, positionChipIndexesAndCube[l][0] - (cubeFirst*2),cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});

                            }
                            else {
                                if ((positionChipIndexesAndCube[l][0] - cubeFirst) <= 0 || boardState[positionChipIndexesAndCube[l][0] - cubeFirst].playerState == opState) {
                                    continue;
                                }
                                allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                            new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                            new GameSequence(player, positionChipIndexesAndCube[k][0], positionChipIndexesAndCube[k][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead),
                            new GameSequence(player, positionChipIndexesAndCube[l][0], positionChipIndexesAndCube[l][0] - cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[l][1],pareIndex-3,tookFromHead)});
                            }
                        }

                    }
                    if (i == j) {
                        if (i == k) {
                            allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                            new GameSequence(player, positionChipIndexesAndCube[j][0] - cubeFirst, positionChipIndexesAndCube[j][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                            new GameSequence(player, positionChipIndexesAndCube[k][0] - (cubeFirst*2), positionChipIndexesAndCube[k][0] - (cubeFirst*3), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead)});

                        }
                        else {
                            allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                            new GameSequence(player, positionChipIndexesAndCube[j][0] - cubeFirst, positionChipIndexesAndCube[j][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                            new GameSequence(player, positionChipIndexesAndCube[k][0], positionChipIndexesAndCube[k][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead)});

                        }
                    }
                    else {
                        if (j == k || i == k) {
                            allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                            new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                            new GameSequence(player, positionChipIndexesAndCube[k][0] - cubeFirst, positionChipIndexesAndCube[k][0] - (cubeFirst*2), cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead)});
                        }
                        else {
                            allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) ,
                            new GameSequence(player, positionChipIndexesAndCube[j][0], positionChipIndexesAndCube[j][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[j][1], pareIndex-1, tookFromHead),
                            new GameSequence(player, positionChipIndexesAndCube[k][0], positionChipIndexesAndCube[k][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[k][1], pareIndex-2, tookFromHead)});
                        }
                    }
                }//need to add here if i==j and so on for k
                if (i == j) {
                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead),
                    new GameSequence(player,positionChipIndexesAndCube[j][0] - cubeFirst,positionChipIndexesAndCube[j][0]-(cubeFirst*2),cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex-1,tookFromHead)});
                }
                else {
                    allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead),
                    new GameSequence(player,positionChipIndexesAndCube[j][0],positionChipIndexesAndCube[j][0]-cubeFirst,cubeFirst,cubeLast,positionChipIndexesAndCube[j][1],pareIndex-1,tookFromHead)});
                }
            }
            allPossibleMoves.Add(new GameSequence[] { new GameSequence(player, positionChipIndexesAndCube[i][0], positionChipIndexesAndCube[i][0] - cubeFirst, cubeFirst, cubeLast, positionChipIndexesAndCube[i][1], pareIndex, tookFromHead) });
        }


        float summaryScore = 0;
        //Second assign score summary for moves
        for (int i = 0; i < allPossibleMoves.Count; i++) {
            for (int j = 0; j < allPossibleMoves[i].Length; j++) {
                summaryScore += GetScoreForMove(allPossibleMoves[i][j]);
            }
            allPossibleMoveScores.Add(summaryScore);
            summaryScore = 0;
        }

        if (allPossibleMoves.Count != allPossibleMoveScores.Count) {
            throw new InvalidOperationException("Mismatch between move count and score count");
        }

        //Third find best score for moves
        int bestMoveIndex = 0;
        for (int m = 0; m < allPossibleMoveScores.Count; m++) {
            if (allPossibleMoveScores[bestMoveIndex] < allPossibleMoveScores[m]) {
                bestMoveIndex = m;
            }
        }
        if (allPossibleMoves.Count > 0)
            return allPossibleMoves[bestMoveIndex];//error beacouse there is empty allPossibleMoves so that is no 0 index
        return null;
    }

    private GameSequence[] FindAndMoveInHome() {
        bool isPair = cubeFirst == cubeLast;
        int movesInHomeCount = isPair ? (chipIndexesInGame.Count >= 4 ? 4 : chipIndexesInGame.Count) : (chipIndexesInGame.Count >= 2 ? 2 : chipIndexesInGame.Count);
        int realPositionIndex;
        GameSequence[] movesInHome = new GameSequence[movesInHomeCount];

        //another approach is to look at position[cube as index].playerstate, and move right away or search for chips at bigger pos if this empty or op
        if (isPair) {
            for (int i = 0; i < movesInHomeCount; i++) {
                AsignBoardState();
                if (boardState[cubeFirst].playerState == player) {
                    movesInHome[i] = new GameSequence(player, cubeFirst, 0, cubeFirst, cubeLast, chipList.IndexOf(positions[cubeFirst].GetComponent<PositionHandle>().GetChipList()[0]), pareIndex, tookFromHead);
                    MakeMoveInHome(movesInHome[i]);
                }
                else if (HaveingChipAtPositions(cubeFirst, 6)) {
                    //add from [cubeFirst as index] to 6 in chipIndexesList and find score for each
                    List<ChipBase> chipListInBiggerPos = GetChipListAtBiggerPosInHome(cubeFirst);
                    float bestScore = 0;
                    GameSequence bestMoveInHome = new GameSequence();
                    for (int j = 0; j < chipListInBiggerPos.Count; j++) {
                        Debug.Log($"HaveingChipAtPositions(cubeFirst), index-{j}, count-{chipListInBiggerPos.Count}");
                        int positionIndex = positions.IndexOf(chipListInBiggerPos[j].GetCurrentPosition());
                        var move = new GameSequence(player, positionIndex, positionIndex - cubeFirst, cubeFirst, cubeLast, chipList.IndexOf(chipListInBiggerPos[j]), pareIndex, tookFromHead);
                        float moveScore = GetScoreForMoveInHome(move);
                        if (bestScore < moveScore) {
                            bestScore = moveScore;
                            bestMoveInHome = move;
                        }
                    }
                    movesInHome[i] = bestMoveInHome;
                    MakeMoveInHome(movesInHome[i]);
                }
                else {//move chip in closest position index
                    realPositionIndex = GetPositionIndexToMoveWithBiggerCube(cubeFirst);
                    movesInHome[i] = new GameSequence(player, realPositionIndex, 0, cubeFirst, cubeLast, chipList.IndexOf(positions[realPositionIndex].GetComponent<PositionHandle>().GetChipList()[0]), pareIndex, tookFromHead);
                    MakeMoveInHome(movesInHome[i]);
                }
            }
        }
        else {
            AsignBoardState();
            if (boardState[cubeFirst].playerState == player) {
                movesInHome[0] = new GameSequence(player, cubeFirst, 0, cubeFirst, cubeLast, chipList.IndexOf(positions[cubeFirst].GetComponent<PositionHandle>().GetChipList()[0]), pareIndex, tookFromHead);
                MakeMoveInHome(movesInHome[0]);
            }
            else if (HaveingChipAtPositions(cubeFirst, 6)) {
                //do score
                List<ChipBase> chipListInBiggerPos = GetChipListAtBiggerPosInHome(cubeFirst);
                float bestScore = 0;
                GameSequence bestMoveInHome = new GameSequence();
                for (int j = 0; j < chipListInBiggerPos.Count; j++) {
                    Debug.Log($"HaveingChipAtPositions(cubeFirst), index-{j}, count-{chipListInBiggerPos.Count}");
                    int positionIndex = positions.IndexOf(chipListInBiggerPos[j].GetCurrentPosition());
                    var move = new GameSequence(player, positionIndex, positionIndex - cubeFirst, cubeFirst, cubeLast, chipList.IndexOf(chipListInBiggerPos[j]), pareIndex, tookFromHead);
                    float moveScore = GetScoreForMoveInHome(move);
                    if (bestScore < moveScore) {
                        bestScore = moveScore;
                        bestMoveInHome = move;
                    }
                }
                movesInHome[0] = bestMoveInHome;
                MakeMoveInHome(movesInHome[0]);
            }
            else {
                //just move closeset
                realPositionIndex = GetPositionIndexToMoveWithBiggerCube(cubeFirst);
                movesInHome[0] = new GameSequence(player, realPositionIndex, 0, cubeFirst, cubeLast, chipList.IndexOf(positions[realPositionIndex].GetComponent<PositionHandle>().GetChipList()[0]), pareIndex, tookFromHead);
                MakeMoveInHome(movesInHome[0]);
            }
            if (movesInHomeCount > 1) {
                AsignBoardState();
                if (boardState[cubeLast].playerState == player) {
                    movesInHome[1] = new GameSequence(player, cubeLast, 0, cubeFirst, cubeLast, chipList.IndexOf(positions[cubeLast].GetComponent<PositionHandle>().GetChipList()[0]), pareIndex, tookFromHead);
                    MakeMoveInHome(movesInHome[1]);
                }
                else if (HaveingChipAtPositions(cubeLast, 6)) {
                    //do score
                    List<ChipBase> chipListInBiggerPos = GetChipListAtBiggerPosInHome(cubeLast);
                    float bestScore = 0;
                    GameSequence bestMoveInHome = new GameSequence();
                    for (int j = 0; j < chipListInBiggerPos.Count; j++) {
                        Debug.Log($"HaveingChipAtPositions(cubeLast), index-{j}, count-{chipListInBiggerPos.Count}");
                        int positionIndex = positions.IndexOf(chipListInBiggerPos[j].GetCurrentPosition());
                        var move = new GameSequence(player, positionIndex, positionIndex - cubeLast, cubeFirst, cubeLast, chipList.IndexOf(chipListInBiggerPos[j]), pareIndex, tookFromHead);
                        float moveScore = GetScoreForMoveInHome(move);
                        if (bestScore < moveScore) {
                            bestScore = moveScore;
                            bestMoveInHome = move;
                        }
                    }
                    movesInHome[1] = bestMoveInHome;
                    MakeMoveInHome(movesInHome[1]);
                }
                else {
                    //just move closeset
                    realPositionIndex = GetPositionIndexToMoveWithBiggerCube(cubeLast);
                    movesInHome[1] = new GameSequence(player, realPositionIndex, 0, cubeFirst, cubeLast, chipList.IndexOf(positions[realPositionIndex].GetComponent<PositionHandle>().GetChipList()[0]), pareIndex, tookFromHead);
                    MakeMoveInHome(movesInHome[1]);
                }
            }
        }

        return movesInHome;
    }

    private float GetScoreForMove(GameSequence move) {
        float score = 0;

        score += MoveToEmpty(move);
        score += MakeMoveOpenSelfPos(move);
        score += MoveFormingBarrier(move);
        score += MoveFromPositionWithHighChipCount(move);//including from head
        score += MoveBreakingBarrier(move);
        score += MoveFromSelfToSelf(move);
        score += MoveBlockingOpStart(move);
        score += MoveToSelfHome(move);
        score += ScoreDependingOnChipsCountInRegions(move);


        return score;
    }

    private float GetScoreForMoveInHome(GameSequence move) {
        float score = 0;

        score += MoveInHomeToEmpty(move);
        score += MoveInHomeToSelf(move);

        return score;
    }

    private float MoveToEmpty(GameSequence move) {
        float score = 0;
        int[] chipCountInParts = SumOfChipCountInFourParts();

        if (boardState[move.ToPositionIndex].chipsCount != 0) {
            return 0;
        }

        
        if (move.FromPositionIndex == 24) {
            score += 0.7f;
        }
        else {
            score += 0.5f;
        }

        return score;
    }

    private float MakeMoveOpenSelfPos(GameSequence move) {
        float score = 0;
        bool haveChipsAtBiggerPos = false;
        int[] partsOp = SumOfChipCountInFourPartsOponent();
        int[] parts = SumOfChipCountInFourParts();

        for (int i = move.FromPositionIndex; i < boardState.Length; i++) {
            if (boardState[i].playerState == player) {
                haveChipsAtBiggerPos = true;
                break;
            }
        }

        if (boardState[move.FromPositionIndex].chipsCount == 1) {
            if (move.FromPositionIndex > 6 && move.FromPositionIndex < 13 && partsOp[3] >= 1 && (parts[0] != 0 && parts[1] != 0)) {
                score -= 1f;
            }
            else if (move.FromPositionIndex > 6 && move.FromPositionIndex < 13 && partsOp[3] >= 1) {
                score -= 0.7f;
            }
            else if (haveChipsAtBiggerPos && (GetChipsCountInPosition(move.ToPositionIndex) < 3)) {
                score -= 0.1f;
            }
            else if (haveChipsAtBiggerPos) {
                score -= 0.3f;
            }
            else {
                score += 0.7f;
            }
        }
        return score;
    }

    private float MoveFromPositionWithHighChipCount(GameSequence move) {
        float score = 0;

        if (move.FromPositionIndex != 24) {
            if (GetChipsCountInPosition(move.FromPositionIndex) > 4 && GetChipsCountInPosition(move.ToPositionIndex) < 3) {
                score += 1f;
            }
            else if (GetChipsCountInPosition(move.FromPositionIndex) > 4) {
                score += 0.5f;
            }
            else if (GetChipsCountInPosition(move.ToPositionIndex) < 3) {
                score += 0.2f;
            }
        }
        else {//from head
            if (GetChipsCountInPosition(move.FromPositionIndex) > 4 && GetChipsCountInPosition(move.ToPositionIndex) <= 3) {
                score += 1f;
            }
            else if (GetChipsCountInPosition(move.FromPositionIndex) > 4) {
                score += 0.6f;
            }
            else if (GetChipsCountInPosition(move.ToPositionIndex) < 3) {
                score += 0.4f;
            }
            else {
                score += 0.2f;
            }
        }

        return score;
    }

    private float MoveFormingBarrier(GameSequence move) {
        float score = 0;

        if (FormingBlockingPositionsSequence(move)[0] == 6) {
            score = -2f;//or check barrier rule in FindAllPossibleMoves stage
        }

        if (FormingBlockingPositionsSequence(move)[0] > 1 && FormingBlockingPositionsSequence(move)[0] < 4) {
            score += 0.7f;
        }
        else if (FormingBlockingPositionsSequence(move)[0] >= 4) {
            score += 1;
        }

        return score;
    }

    private float MoveBreakingBarrier(GameSequence move) {
        float score = 0;

        int[] barrier = BreakingBarrier(move);//0 - before, 1 - after


        if (barrier[0] - barrier[1] > 2) {
            score -= 0.8f;
        }
        else if (barrier[0] - barrier[1] == 1) {
            score -= 0.3f;
        }

        return score;
    }

    private float MoveFromSelfToSelf(GameSequence move) {
        float score = 0;

        if (boardState[move.FromPositionIndex].chipsCount > 0 && boardState[move.ToPositionIndex].chipsCount > 0) {
            if (GetChipsCountInPosition(move.FromPositionIndex) > 3 && GetChipsCountInPosition(move.ToPositionIndex) < 3) {
                score += 0.6f;
            }
            else if (GetChipsCountInPosition(move.FromPositionIndex) > 3) {
                score += 0.4f;
            }
            else if (GetChipsCountInPosition(move.FromPositionIndex) <= 3) {
                score += 0.3f;
            }
            else {
                score += 0.2f;
            }
        }

        return score;
    }

    private float MoveBlockingOpStart(GameSequence move) {//can add improvements according to SumOfChipCountInFourParts
        float score = 0;
        int[] parts = SumOfChipCountInFourParts();

        if (move.ToPositionIndex > 6 && move.ToPositionIndex < 13) {
            if (boardState[move.ToPositionIndex].chipsCount > 2) {
                score += 0.8f;
            }
            else {
                score += 1f;
            }
        }

        return score;
    }

    private float MoveToSelfHome(GameSequence move) {
        float score = 0;
        int[] parts = SumOfChipCountInFourParts();

        if (move.ToPositionIndex < 7 && move.FromPositionIndex > 6) {
            if (boardState[move.ToPositionIndex].chipsCount == 0) {
                if (parts[2] > (parts[1] + parts[0])) {
                    score += 1f;
                }
                else if (parts[0] == 0) {
                    score += 0.9f;
                }
                else {
                    score += 0.5f;
                }
            }
            else {
                if (parts[0] == 0 && parts[1] == 0 && parts[2] != 0) {
                    score += 1f;
                }
                else if (parts[1] < 6) {
                    score += 0.5f;
                }
                else if (parts[0] != 0) {
                    score -= 0.6f;
                }
                else {
                    score += 0.2f;
                }
            }
        }



        return score;
    }

    private float ScoreDependingOnChipsCountInRegions(GameSequence move) {
        float score = 0;

        int[] parts = SumOfChipCountInFourParts();//0- selfStart, 1- opHome, 2- opStart, 3- selfHome

        if (move.FromPositionIndex > 19 && (parts[0] + parts[1]) < (parts[2] + parts[3])) {
            score += 0.7f;
        }
        else if (move.FromPositionIndex > 13 && (parts[0] + parts[1]) < (parts[2] + parts[3])) {
            score += 0.5f;
        }

        return score;
    }

    private float MoveInHomeToEmpty(GameSequence move) {
        float score = 0;

        //if from pos with chip count > 5 +0.9
        //if from pos with chip count > 3 +0.7
        //if from pos with chip count <= 3 +0.4

        if (boardState[move.ToPositionIndex].chipsCount != 0) {
            return score;
        }

        if (move.FromPositionIndex > 5) {
            score += 0.9f;
        }
        else if (move.FromPositionIndex > 3) {
            score += 0.7f;
        }
        else if (move.FromPositionIndex <= 3) {
            score += 0.4f;
        }
        return score;
    }

    private float MoveInHomeToSelf(GameSequence move) {
        float score = 0;

        //if from pos with chipCOunt > 5 to pos with chipcount < 4 +0.9
        //if from pos with chipCOunt > 5 to pos with chipcount >= 4 +0.6
        //if from pos with chipCOunt <= 5 to pos with chipcount < 4 +0.4
        //if from pos with chipCOunt <= 5 to pos with chipcount >= 4 +0.2

        if (move.FromPositionIndex < 5 && move.ToPositionIndex < 4) {
            score += 0.9f;
        }
        else if (move.FromPositionIndex > 5 && move.ToPositionIndex >= 4) {
            score += 0.6f;
        }
        else if (move.FromPositionIndex <= 5 && move.ToPositionIndex < 4) {
            score += 0.4f;
        }
        else if (move.FromPositionIndex <= 5 && move.ToPositionIndex >= 4) {
            score += 0.2f;
        }
        return score;
    }

    private int GetChipsCountInPosition(int positionIndex) {
        return boardState[positionIndex].chipsCount;
    }

    private int[] SumOfChipCountInFourParts() {
        int selfStart = 0;
        int opHome = 0;
        int opStart = 0;
        int selfHome = 0;
        for (int i = 1; i < 7; i++) {
            if (boardState[i].playerState == player)
                selfHome += boardState[i].chipsCount;
            if (boardState[i + 6].playerState == player)
                opStart += boardState[i + 6].chipsCount;
            if (boardState[i + 12].playerState == player)
                opHome += boardState[i + 12].chipsCount;
            if (boardState[i + 18].playerState == player)
                selfStart += boardState[i + 18].chipsCount;
        }

        return new int[] { selfStart, opHome, opStart, selfHome };
    }

    private int[] SumOfChipCountInFourPartsOponent() {
        int selfStart = 0;
        int opHome = 0;
        int opStart = 0;
        int selfHome = 0;
        for (int i = 1; i < 7; i++) {
            if (boardState[i].playerState != player)
                selfHome += boardState[i].chipsCount;
            if (boardState[i + 6].playerState != player)
                opStart += boardState[i + 6].chipsCount;
            if (boardState[i + 12].playerState != player)
                opHome += boardState[i + 12].chipsCount;
            if (boardState[i + 18].playerState != player)
                selfStart += boardState[i + 18].chipsCount;
        }

        return new int[] { selfStart, opHome, opStart, selfHome };
    }

    private int[] FormingBlockingPositionsSequence(GameSequence move) {
        int from = move.ToPositionIndex + 5 > 24 ? 24 : move.ToPositionIndex + 5;
        int to = move.ToPositionIndex - 5 <= 0 ? 0 : move.ToPositionIndex - 5;
        int sequenceCount = 0;
        int longestSequenceCount = 0;
        int startIndex = 0;
        for (int i = from; i <= to; i++) {
            if (boardState[i].playerState == player) {
                sequenceCount++;
            }
            else {
                longestSequenceCount = longestSequenceCount < sequenceCount ? sequenceCount : longestSequenceCount;
                startIndex = i - longestSequenceCount;
                sequenceCount = 0;
            }
        }
        return new int[] { longestSequenceCount, startIndex };
    }

    private int[] BreakingBarrier(GameSequence move) {
        int from = move.FromPositionIndex + 4 > 24 ? 24 : move.FromPositionIndex + 4;
        int to = move.FromPositionIndex - 4 <= 0 ? 0 : move.FromPositionIndex - 4;
        int barrierLenghtBefore = 0;
        int barrierLenghtAfter = 0;
        int sequenceCount = 0;

        for (int i = from; i <= to; i++) {
            if (boardState[i].playerState == player) {
                sequenceCount++;
            }
            else {
                barrierLenghtBefore = barrierLenghtBefore < sequenceCount ? sequenceCount : barrierLenghtBefore;
                sequenceCount = 0;
            }
        }

        for (int i = from; i <= to; i++) {
            if (boardState[i].playerState == player && i != move.FromPositionIndex) {
                sequenceCount++;
            }
            else {
                barrierLenghtAfter = barrierLenghtAfter < sequenceCount ? sequenceCount : barrierLenghtAfter;
                sequenceCount = 0;
            }
        }

        return new int[] { barrierLenghtBefore, barrierLenghtAfter };
    }

    private bool HaveingChipAtPositions(int startIndex, int endIndex) {
        if (startIndex > endIndex) {
            for (int i = startIndex; i >= endIndex; i--) {
                if (boardState[i].playerState == player && boardState[i].chipsCount > 0) {
                    return true;
                }
            }
        }
        else if (endIndex > startIndex) {
            for (int i = startIndex; i <= endIndex; i++) {
                if (boardState[i].playerState == player && boardState[i].chipsCount > 0) {
                    return true;
                }
            }
        }
        return false;
    }

    private int GetPositionIndexToMoveWithBiggerCube(int cube) {
        for (int i = cube - 1; i > 0; i--) {
            if (boardState[i].playerState == player) {
                return i;
            }
        }
        throw new NullReferenceException($"No position with chips from {cube} to 0.");
    }

    private List<ChipBase> GetChipListAtBiggerPosInHome(int fromIndex) {
        List<ChipBase> chipList = new List<ChipBase>();
        for (int i = fromIndex; i <= 6; i++) {
            if (boardState[i].playerState == player)
                chipList.Add(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
        }
        return chipList;
    }

    private void MakeMoves() {
        if (movesMade == null) {
            return;
        }
        //add all chipindexes in chipIndexesInGame
        for (int i = 0; i < movesMade.Length; i++) {
            Debug.Log($"move[{i}], fromIndex - {movesMade[i].FromPositionIndex}, toIndex - {movesMade[i].ToPositionIndex}");
            if (!chipIndexesInGame.Contains(movesMade[i].ChipIndex)) {
                chipIndexesInGame.Add(movesMade[i].ChipIndex);
                Debug.Log($"Active chip index added {movesMade[i].ChipIndex}");
            }
            positions[movesMade[i].ToPositionIndex].GetComponent<PositionHandle>().AddChip(chipList[movesMade[i].ChipIndex]);
            if (movesMade[i].PareIndex != 0) {
                pareIndex--;
                if (pareIndex == 0) {
                    cubeFirst = 0;
                    cubeLast = 0;
                }
            }
            else {
                if (movesMade[i].FromPositionIndex - movesMade[i].ToPositionIndex == movesMade[i].Cube1) {
                    cubeFirst = 0;
                }
                else if (movesMade[i].FromPositionIndex - movesMade[i].ToPositionIndex == movesMade[i].Cube2) {
                    cubeLast = 0;
                }
            }
        }
        bool allInHome = !HaveingChipAtPositions(24, 7);
        if (allInHome && !(cubeFirst == 0 && cubeLast == 0)) {
            //hjvae cube to move
            movesMade = FindAndMoveInHome();
        }
    }

    private void MakeMoveInHome(GameSequence move) {
        if (chipIndexesInGame.Contains(move.ChipIndex)) {
            chipIndexesInGame.Remove(move.ChipIndex);
            Debug.Log($"Active chip index removed {move.ChipIndex}");
        }
        if (move.ToPositionIndex <= 0) {
            botWon = homePosition.AddChipToHomeWin(chipList[move.ChipIndex]);
        }
        else {
            positions[move.ToPositionIndex].GetComponent<PositionHandle>().AddChip(chipList[move.ChipIndex]);
        }
        if (botWon) {
            game.BotWonGame(player);
            return;
        }
    }

    public void SetChips(List<ChipBase> chips) {
        chipList = new List<ChipBase>();
        chipList = chips;
    }

    public Player GetPlayerState() {
        return player;
    }

    /* private int FindCubeToMoveInHome() {
         realPositionIndex = -1;
         chipIndexToMove = -1;
         Debug.Log("Inside FindCubeToMoveInHome M");
         if (boardState[cubeFirst].playerState == player) {
             chipIndexToMove = chipList.IndexOf(positions[cubeFirst].GetComponent<PositionHandle>().GetChipList()[0]);
             realPositionIndex = cubeFirst;
             return cubeFirst;
         }
         else if (boardState[cubeLast].playerState == player) {
             chipIndexToMove = chipList.IndexOf(positions[cubeLast].GetComponent<PositionHandle>().GetChipList()[0]);
             realPositionIndex = cubeLast;
             return cubeLast;
         }
         else if (cubeFirst >= cubeLast) {
             if (HaveingChipsAtBiggerPositionsInHome(cubeFirst)) {
                 //have chips at bigger, than need to move normal
                 for (int i = 6; i >= cubeFirst; i--) {
                     if (boardState[i].playerState == player && (boardState[i - cubeFirst].playerState == player || boardState[i - cubeFirst].playerState == Player.Empty)) {
                         chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                         realPositionIndex = i;
                         return cubeFirst;
                     }
                 }
             }
             else {
                 realPositionIndex = GetPositionIndexToMoveWithBiggerCube(cubeFirst);
                 if (realPositionIndex == -1) {
                     return -1;
                 }
                 chipIndexToMove = chipList.IndexOf(positions[realPositionIndex].GetComponent<PositionHandle>().GetChipList()[0]);
                 return cubeFirst;
             }
         }
         else if (cubeLast > cubeFirst) {
             if (HaveingChipsAtBiggerPositionsInHome(cubeLast)) {
                 //have chips at bigger, than need to move normal
                 for (int i = 6; i >= cubeLast; i--) {
                     if (boardState[i].playerState == player && (boardState[i - cubeLast].playerState == player || boardState[i - cubeLast].playerState == Player.Empty)) {
                         chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                         realPositionIndex = i;
                         return cubeLast;
                     }
                 }
             }
             else {
                 realPositionIndex = GetPositionIndexToMoveWithBiggerCube(cubeLast);
                 if (realPositionIndex == -1) {
                     return -1;
                 }
                 chipIndexToMove = chipList.IndexOf(positions[realPositionIndex].GetComponent<PositionHandle>().GetChipList()[0]);
                 return cubeLast;
             }
         }
         return -1;
     }*/

}
