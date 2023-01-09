using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlantCell : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public int xGridCoordinate = -1;
    public int yGridCoordinate = -1;
    public UnityEngine.UI.Image redPlant;
    public UnityEngine.UI.Image greenPlant;
    public UnityEngine.UI.Image bluePlant;
    public UnityEngine.UI.Image tricoloredPlant;
    public UnityEngine.UI.Image colorlessPlant;
    public UnityEngine.UI.Image calcifiedPlant;
    public UnityEngine.UI.Image redCrystal;
    public UnityEngine.UI.Image greenCrystal;
    public UnityEngine.UI.Image blueCrystal;
    public UnityEngine.UI.Image selectionBracket;

    public UnityEngine.UI.Image background;
    public Sprite[] backgroundTileSelection;

    public System.Action<int, int> onDragReleasedCallback = null;

    private void Awake()
    {
        selectionBracket.gameObject.SetActive(false);
        UpdateRenderer(new SCrabapplesGameState.SCellState());
    }

    public void UpdateRenderer(SCrabapplesGameState.SCellState cellState)
    {
        redPlant.gameObject.SetActive(false);
        greenPlant.gameObject.SetActive(false);
        bluePlant.gameObject.SetActive(false);
        tricoloredPlant.gameObject.SetActive(false);
        colorlessPlant.gameObject.SetActive(false);
        calcifiedPlant.gameObject.SetActive(false);
        redCrystal.gameObject.SetActive(false);
        greenCrystal.gameObject.SetActive(false);
        blueCrystal.gameObject.SetActive(false);

        if (cellState.calcified)
        {
            redCrystal.gameObject.SetActive(cellState.red);
            greenCrystal.gameObject.SetActive(cellState.green);
            blueCrystal.gameObject.SetActive(cellState.blue);
            calcifiedPlant.gameObject.SetActive(!cellState.red && !cellState.green && !cellState.blue);
        }
        else if (cellState.occupied)
        {
            if (cellState.red && cellState.green && cellState.blue)
            {
                tricoloredPlant.gameObject.SetActive(true);
            }
            else if (!cellState.red && !cellState.green && !cellState.blue)
            {
                colorlessPlant.gameObject.SetActive(true);
            }
            else
            {
                redPlant.gameObject.SetActive(cellState.red);
                greenPlant.gameObject.SetActive(cellState.green);
                bluePlant.gameObject.SetActive(cellState.blue);
            }
        }

        int backgroundIndex = (cellState.x % 2) + (cellState.y % 2) * 2;
        background.sprite = backgroundTileSelection[backgroundIndex];
    }

    public void OnDrop(PointerEventData eventData)
    {
        onDragReleasedCallback?.Invoke(xGridCoordinate, yGridCoordinate);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameManager.instance.isDraggingBud)
        {
            selectionBracket.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        selectionBracket.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftControl))
        {
            GameManager.instance.DebugClickCell(xGridCoordinate, yGridCoordinate, clear: false);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            GameManager.instance.DebugClickCell(xGridCoordinate, yGridCoordinate, clear: true);
        }
#endif
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }

}
