using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeToSelect : MonoBehaviour {

    [SerializeField] private Cube cubeState;
    [SerializeField] private GameObject[] dots;
    [SerializeField] private CubeSelectedHandler cubeSelectedHandler;

    private void Start() {
        GetDotIndexes index = new GetDotIndexes();
        VisualiseState(index.GetIndexes(cubeState));
        
    }

    public void SetCubesStateInSelectedCube() {//onButton click
        cubeSelectedHandler.SetCubeSelectedState(cubeState);
    }

    private void VisualiseState(List<int> indexes){
        for (int i = 0; i < dots.Length; i++) {
            if (indexes.Contains(i)) {
                dots[i].SetActive(true);
            }
            else {
                dots[i].SetActive(false);
            }
        }
    }

    public Cube GetState() {
        return cubeState;
    }
}

public class GetDotIndexes {//handles indexes, set it one time in this class and then simply use it
    //This mesanic can be used for things that require the activation of some of the objects located under the fixed indices
    public List<int> GetIndexes(Cube cube) {
        List<int> indexesToReturn = new List<int>();
        switch (cube) {
            case Cube.One: 
                indexesToReturn.Add(0); break;
            case Cube.Two:
                indexesToReturn.Add(1);
                indexesToReturn.Add(6); break;
            case Cube.Three: 
                indexesToReturn.Add(0);
                indexesToReturn.Add(1);
                indexesToReturn.Add(6); break;
            case Cube.Four: 
                indexesToReturn.Add(1);
                indexesToReturn.Add(3);
                indexesToReturn.Add(4);
                indexesToReturn.Add(6); break;
            case Cube.Five:
                indexesToReturn.Add(1);
                indexesToReturn.Add(3);
                indexesToReturn.Add(4);
                indexesToReturn.Add(6);
                indexesToReturn.Add(0); break;
            case Cube.Six:
                indexesToReturn.Add(1);
                indexesToReturn.Add(2);
                indexesToReturn.Add(3);
                indexesToReturn.Add(4);
                indexesToReturn.Add(5);
                indexesToReturn.Add(6); break;
            case Cube.Null: break;

        }
        return indexesToReturn;
    }
}