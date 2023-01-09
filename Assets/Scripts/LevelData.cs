using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData : MonoBehaviour
{
    [System.Serializable]
    public struct SLevelData
    {
        public string levelName;
        public SCrabapplesGameState.SCellState[] cells;
        public int playerColorlessBuds;
        public SVictoryCondition[] victoryConditions;
    }
    public enum EVictoryCheck
    {
        AnyPlants = 0,
        RedPlants = 1,
        GreenPlants = 2,
        BluePlants = 3,
        TricolorPlants = 4,
        CalcifiedBuds = 5,
        Turns = 6,
    }
    public enum EVictoryLogic
    {
        AtLeast = 0,
        NoMoreThan = 1,
    }
    [System.Serializable]
    public struct SVictoryCondition
    {
        public EVictoryLogic logic;
        public int number;
        [UnityEngine.Serialization.FormerlySerializedAs("check")]
        public EVictoryCheck statToCheck;
    }

    public SLevelData myData;
}
