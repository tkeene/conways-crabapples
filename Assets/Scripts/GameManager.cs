using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public AudioSource gameAudio;
    public AudioClip menuClick;
    public AudioClip pickUpPlant;
    public AudioClip placePlant;
    public AudioClip passTurn;
    public AudioClip undoMove;
    public AudioClip harvestReady;

    [Header("Level Select")]
    public GameObject levelSelectRoot;
    public List<TMPro.TextMeshProUGUI> levelSelectScores = new List<TMPro.TextMeshProUGUI>();

    [Header("Game Grid")]
    public GameObject gameRoot;
    public PlantCell plantCellPrefab;
    public GameObject plantGridRoot;
    public TMPro.TextMeshProUGUI levelNameLabel;
    public TMPro.TextMeshProUGUI scoreLabel;
    public TMPro.TextMeshProUGUI turnLabel;
    [UnityEngine.Serialization.FormerlySerializedAs("turnThresholdLabel")]
    public TMPro.TextMeshProUGUI requirementsLabel;
    public TMPro.TextMeshProUGUI budQuantityLabel;
    public UnityEngine.EventSystems.EventTrigger budItemTrigger;
    public GameObject budDraggableSprite;
    public GameObject helpRoot;
    public UnityEngine.UI.Image harvestButton;
    public Color harvestDisabledColor;
    public Color harvestReadyColor;

    public List<LevelData> levelData = new List<LevelData>();

    public static GameManager instance = null;

    const int kBoardSize = 12;
    const int kBudItemIndex = 0;

    private Dictionary<Vector2Int, PlantCell> spawnedPlantCells = new Dictionary<Vector2Int, PlantCell>();
    public bool isDraggingBud = false;
    private SCrabapplesGameState currentInitialGameState;
    private LevelData.SVictoryCondition[] currentVictoryConditions;
    private int currentLevelIndex = -1;
    private List<SCrabapplesGameState> undoHistory = new List<SCrabapplesGameState>();

    private void Awake()
    {
        instance = this;

        levelSelectRoot.SetActive(true);
        gameRoot.SetActive(false);
        helpRoot.SetActive(false);

        for (int x = 0; x < kBoardSize; x++)
        {
            for (int y = 0; y < kBoardSize; y++)
            {
                PlantCell spawnedCell = GameObject.Instantiate(plantCellPrefab, plantGridRoot.transform);
                float cellWidth = 1.0f / kBoardSize;
                (spawnedCell.transform as RectTransform).anchorMin = new Vector2(x * cellWidth, y * cellWidth);
                (spawnedCell.transform as RectTransform).anchorMax = new Vector2((x + 1) * cellWidth, (y + 1) * cellWidth);
                spawnedCell.xGridCoordinate = x;
                spawnedCell.yGridCoordinate = y;
                spawnedCell.onDragReleasedCallback += (x, y) => GUI_Game_EndDragItem(x, y);
                spawnedPlantCells[new Vector2Int(x, y)] = spawnedCell;
            }
        }

        UnityEngine.EventSystems.EventTrigger.Entry dragBudEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
        dragBudEvent.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        dragBudEvent.callback.AddListener((data) => GUI_Game_StartDragItem(kBudItemIndex, data as UnityEngine.EventSystems.PointerEventData));
        budItemTrigger.triggers.Add(dragBudEvent);
    }

    private void Update()
    {
        if (isDraggingBud)
        {
            budDraggableSprite.transform.position = Input.mousePosition + new Vector3(Screen.height * 0.1f, -Screen.height * 0.1f);

            if (Input.GetMouseButtonUp(0))
            {
                isDraggingBud = false;
            }
        }
        else
        {
            (budDraggableSprite.transform as RectTransform).offsetMin = Vector3.zero;
            (budDraggableSprite.transform as RectTransform).offsetMax = Vector3.zero;
        }

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S))
        {
            List<SCrabapplesGameState.SCellState> cellsToWrite = new List<SCrabapplesGameState.SCellState>();
            foreach (SCrabapplesGameState.SCellState cell in GetCurrentBoardState().board)
            {
                if (cell.occupied)
                {
                    cellsToWrite.Add(cell);
                }
            }
            string fileName = "savedLevel" + "_" + cellsToWrite.Count;
            GameObject savedLevelData = new GameObject();
            LevelData data = savedLevelData.AddComponent<LevelData>();
            data.myData = new LevelData.SLevelData();
            data.myData.levelName = fileName;
            data.myData.cells = cellsToWrite.ToArray();
            data.myData.playerColorlessBuds = 0;
            data.myData.victoryConditions = new LevelData.SVictoryCondition[0];
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(savedLevelData,
                System.IO.Path.Combine("Assets/Prefabs/LevelData/", fileName + ".prefab"));
            Debug.Log("Wrote " + fileName);
        }
#endif
    }

    public void DebugClickCell(int xGridCoordinate, int yGridCoordinate, bool clear)
    {
#if UNITY_EDITOR
        SCrabapplesGameState nextGameState = GetCurrentBoardState();
        SCrabapplesGameState.SCellState targetCell = nextGameState.board[xGridCoordinate, yGridCoordinate];
        targetCell.x = xGridCoordinate;
        targetCell.y = yGridCoordinate;
        if (clear)
        {
            targetCell.occupied = false;
            targetCell.calcified = false;
            targetCell.red = false;
            targetCell.green = false;
            targetCell.blue = false;
        }
        else
        {
            if (!targetCell.occupied)
            {
                targetCell.occupied = true;
            }
            else if (!targetCell.red && !targetCell.green && !targetCell.blue)
            {
                targetCell.red = true;
            }
            else if (targetCell.red && !targetCell.green)
            {
                targetCell.red = false;
                targetCell.green = true;
            }
            else if (targetCell.green && !targetCell.blue)
            {
                targetCell.green = false;
                targetCell.blue = true;
            }
            else if (targetCell.blue && !targetCell.red)
            {
                targetCell.red = true;
                targetCell.green = true;
                targetCell.blue = true;
            }
            else if (targetCell.red && targetCell.green && targetCell.blue)
            {
                targetCell.red = false;
                targetCell.green = false;
                targetCell.blue = false;
                if (!targetCell.calcified)
                {
                    targetCell.calcified = true;
                }
                else
                {
                    targetCell.occupied = false;
                    targetCell.calcified = false;
                }
            }
        }
        nextGameState.board[xGridCoordinate, yGridCoordinate] = targetCell;
        undoHistory.Add(nextGameState);
        RefreshBoard();
#endif
    }

    public void PlayAudio(AudioClip audioClip)
    {
        if (audioClip != null)
        {
            gameAudio.PlayOneShot(audioClip);
        }
    }

    private void InitializeLevel(int level)
    {
        LevelData.SLevelData levelToInitialize = levelData[level].myData;
        currentInitialGameState = new SCrabapplesGameState(kBoardSize, kBoardSize, turn: 0,
            budsLeft: levelToInitialize.playerColorlessBuds, score: 100);
        foreach (SCrabapplesGameState.SCellState cell in levelToInitialize.cells)
        {
            currentInitialGameState.board[cell.x, cell.y] = cell;
        }
        currentVictoryConditions = levelToInitialize.victoryConditions;
        levelNameLabel.text = "Level " + (level + 1) + ": " + levelToInitialize.levelName;
        InitializeLevel(currentInitialGameState);
    }

    private void InitializeLevel(SCrabapplesGameState currentInitialGameState)
    {
        undoHistory.Clear();
        undoHistory.Add(currentInitialGameState);
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        SCrabapplesGameState currentBoardState = GetCurrentBoardState();
        for (int x = 0; x < currentBoardState.width; x++)
        {
            for (int y = 0; y < currentBoardState.width; y++)
            {
                PlantCell currentCell = spawnedPlantCells[new Vector2Int(x, y)];
                currentCell.UpdateRenderer(currentBoardState.board[x, y]);
            }
        }
        turnLabel.text = "Turn: " + currentBoardState.currentTurn;
        budQuantityLabel.text = currentBoardState.playerBudsToPlace + "x";
        scoreLabel.text = "Score: " + currentBoardState.score;

        string victoryConditionDisplay = "";
        for (int i = 0; i < currentVictoryConditions.Length; i++)
        {
            LevelData.SVictoryCondition victoryCondition = currentVictoryConditions[i];
            int value = CheckCondition(victoryCondition);
            string conditionDescription = GetConditionDescription(victoryCondition);
            string conditionCount = " (" + currentBoardState.GetCount(victoryCondition.statToCheck) + ")";
            if (value < 0)
            {
                victoryConditionDisplay
                    += "<color=red>Fail: "
                    + conditionDescription + conditionCount
                    + "</color>";
            }
            else if (value == 0)
            {
                victoryConditionDisplay
                    += ""
                    + conditionDescription + conditionCount
                    + "";
            }
            else
            {
                victoryConditionDisplay
                    += "<color=green>Done: "
                    + conditionDescription + conditionCount
                    + "</color>";
            }

            if (i + 1 < currentVictoryConditions.Length)
            {
                victoryConditionDisplay += "\n\n";
            }
        }
        requirementsLabel.text = victoryConditionDisplay;
        requirementsLabel.fontSize = Mathf.Min(Screen.height / 30.0f);

        harvestButton.color = AreVictoryConditionsMet()
            ? harvestReadyColor : harvestDisabledColor;
        if (AreVictoryConditionsMet())
        {
            PlayAudio(harvestReady);
        }
    }

    private SCrabapplesGameState GetCurrentBoardState()
    {
        return undoHistory[undoHistory.Count - 1];
    }

    private bool AreVictoryConditionsMet()
    {
        bool canFinish = true;
        foreach (LevelData.SVictoryCondition victoryCondition in currentVictoryConditions)
        {
            if (CheckCondition(victoryCondition) <= 0)
            {
                canFinish = false;
            }
        }
        return canFinish;
    }

    private string GetConditionDescription(LevelData.SVictoryCondition victoryCondition)
    {
        string description = "";
        if (victoryCondition.statToCheck == LevelData.EVictoryCheck.Turns)
        {
            switch (victoryCondition.logic)
            {
                case LevelData.EVictoryLogic.AtLeast:
                    description = "Grow " + victoryCondition.number + " or more turns";
                    break;
                case LevelData.EVictoryLogic.NoMoreThan:
                    description = "Maximum " + victoryCondition.number + " turns";
                    break;
                default:
                    break;
            }
        }
        else
        {
            if (victoryCondition.number == 0 && victoryCondition.logic == LevelData.EVictoryLogic.NoMoreThan)
            {
                description = "Eliminate ";
            }
            else
            {
                switch (victoryCondition.logic)
                {
                    case LevelData.EVictoryLogic.AtLeast:
                        description += "Harvest at least " + victoryCondition.number + " ";
                        break;
                    case LevelData.EVictoryLogic.NoMoreThan:
                        description += "Harvest no more than " + victoryCondition.number + " ";
                        break;
                    default:
                        break;
                }
            }
            switch (victoryCondition.statToCheck)
            {
                case LevelData.EVictoryCheck.AnyPlants:
                    description += "living plants";
                    break;
                case LevelData.EVictoryCheck.RedPlants:
                    description += "red plants";
                    break;
                case LevelData.EVictoryCheck.GreenPlants:
                    description += "green plants";
                    break;
                case LevelData.EVictoryCheck.BluePlants:
                    description += "blue plants";
                    break;
                case LevelData.EVictoryCheck.TricolorPlants:
                    description += "tricolor plants";
                    break;
                case LevelData.EVictoryCheck.CalcifiedBuds:
                    description += "fossils";
                    break;
                case LevelData.EVictoryCheck.Turns:
                    description += "turns";
                    break;
                default:
                    break;
            }
        }

        return description;
    }

    private int CheckCondition(LevelData.SVictoryCondition victoryCondition)
    {
        int numberToCheck = 0;
        bool failureIsBad = false;
        if (victoryCondition.statToCheck == LevelData.EVictoryCheck.Turns)
        {
            numberToCheck = GetCurrentBoardState().currentTurn;
            failureIsBad = victoryCondition.logic == LevelData.EVictoryLogic.NoMoreThan;
        }
        else
        {
            numberToCheck = GetCurrentBoardState().GetCount(victoryCondition.statToCheck);
            failureIsBad = victoryCondition.logic == LevelData.EVictoryLogic.NoMoreThan
                && victoryCondition.number == 0;
        }

        bool isConditionMet = false;
        switch (victoryCondition.logic)
        {
            case LevelData.EVictoryLogic.AtLeast:
                isConditionMet = numberToCheck >= victoryCondition.number;
                break;
            case LevelData.EVictoryLogic.NoMoreThan:
                isConditionMet = numberToCheck <= victoryCondition.number;
                break;
            default:
                break;
        }
        int returnValue = 0;
        if (isConditionMet)
        {
            returnValue = 1;
        }
        else
        {
            returnValue = failureIsBad ? -1 : 0;
        }
        return returnValue;
    }

    #region GUI Methods

    public void GUI_LevelSelectClicked(int level)
    {
        currentLevelIndex = level;
        levelSelectRoot.SetActive(false);
        gameRoot.SetActive(true);
        InitializeLevel(level);
        PlayAudio(menuClick);
    }

    public void GUI_OpenUrl(string url)
    {
        UnityEngine.Application.OpenURL(url);
        PlayAudio(menuClick);
    }

    public void GUI_Game_GoBack()
    {
        levelSelectRoot.SetActive(true);
        gameRoot.SetActive(false);
        PlayAudio(menuClick);
    }

    public void GUI_Game_Harvest()
    {
        bool canFinish = AreVictoryConditionsMet();
        if (canFinish)
        {
            int score = GetCurrentBoardState().score;
            GUI_Game_GoBack();
            PlayAudio(harvestReady);
            levelSelectScores[currentLevelIndex].text = "Score: " + score;
        }
    }

    public void GUI_Game_Advance()
    {
        undoHistory.Add(GetCurrentBoardState().GetNextTurn());
        RefreshBoard();
        if (!AreVictoryConditionsMet())
        {
            PlayAudio(passTurn);
        }
    }

    public void GUI_Game_Undo()
    {
        if (undoHistory.Count > 1)
        {
            undoHistory.RemoveAt(undoHistory.Count - 1);
        }
        PlayAudio(undoMove);
        RefreshBoard();
    }

    public void GUI_Reset()
    {
        while (undoHistory.Count > 1)
        {
            undoHistory.RemoveAt(1);
        }
        PlayAudio(undoMove);
        RefreshBoard();
    }

    public void GUI_Game_Help()
    {
        helpRoot.SetActive(!helpRoot.activeSelf);
        PlayAudio(menuClick);
    }

    public void GUI_Game_StartDragItem(int itemIndex, UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (GetCurrentBoardState().playerBudsToPlace > 0)
        {
            isDraggingBud = true;
            PlayAudio(pickUpPlant);
        }
    }

    public void GUI_Game_EndDragItem(int x, int y)
    {
        if (GetCurrentBoardState().playerBudsToPlace > 0)
        {
            SCrabapplesGameState nextBoardState = GetCurrentBoardState().Clone();
            if (!nextBoardState.board[x, y].occupied)
            {
                nextBoardState.board[x, y].occupied = true;
                nextBoardState.playerBudsToPlace--;
                nextBoardState.score -= 5;
                undoHistory.Add(nextBoardState);
                RefreshBoard();
                if (!AreVictoryConditionsMet())
                {
                    PlayAudio(placePlant);
                }
            }
        }
    }

    #endregion
}
