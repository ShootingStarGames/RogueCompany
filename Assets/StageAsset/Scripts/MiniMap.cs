﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class MiniMap : MonoBehaviourSingleton<MiniMap>
{
    enum Direction { LEFT, RIGHT, TOP, DOWN }

    #region variables
    [SerializeField] private Sprite monsterIcon, bossIcon, eventIcon, storeIcon;
    [SerializeField] private GameObject playerIcon;
    [SerializeField] private Transform mask;
    [SerializeField] private Text floorT;
    [SerializeField] private GameObject[] title;
    [SerializeField] private Transform groupTransform;
    [SerializeField] private GameObject minimapObj;
    bool isDrawAll;
    Vector2 playerPositon;
    Vector2 oldPos;
    bool isToggle = false;
    // 0806 Test
    int isTranslate = 0;
    Vector2 iconV = Vector2.zero;
    float mapMoveX, mapMoveY;
    bool inDungeon= false; // 던전안에 들어갔는지 아닌지 확인
    [SerializeField] bool isTutorial = false;
    
    #endregion
    #region minimapData
    const int minimapBaseWidth = 400, minimapBaseHeight = 400;// 미니맵 사이즈
    int minmapSizeWidth, minmapSizeHeight;
    const int maskSize = 100;
    float width, height;
    int pixelNum = 8;// 미니맵 픽셀 

    #endregion
    #region mapData
    List<Map.Rect> roomList;
    float mapSizeWidth, mapSizeHeight; // 실제 맵 사이즈
    int mapSize;
    #endregion
    #region unityClass
    Texture2D texture;
    new RawImage renderer;
    #endregion
    #region colors
    Color[] mapColors;
    Color hallColor = new Color(.2f, .2f, .2f, 1);/* new Color((float)160 / 255, (float)174 / 255, (float)186 / 255);*/
    Color border = Color.white;
    Color ground = new Color(.1f, .1f, .1f, 1);
    Color doorColor = new Color(.1f, .1f, .1f, 1);
    Color clear = Color.clear;
    #endregion

    Direction Check(Map.Rect _rectA, Map.Rect _rectB)
    {
        if ((Mathf.Abs(_rectA.midX - _rectB.midX) == (float)(_rectA.width + _rectB.width) / 2) && (Mathf.Abs(_rectA.midY - _rectB.midY) < (float)(_rectA.height + _rectB.height) / 2))
        {
            if (_rectA.midX > _rectB.midX)
            {
                return Direction.LEFT;
            }
            return Direction.RIGHT;
        }
        else if ((Mathf.Abs(_rectA.midX - _rectB.midX) < (float)(_rectA.width + _rectB.width) / 2) && (Mathf.Abs(_rectA.midY - _rectB.midY) == (float)(_rectA.height + _rectB.height) / 2))
        {
            if (_rectA.midY > _rectB.midY)
            {
                return Direction.DOWN;
            }
            return Direction.TOP;
        }

        return Direction.RIGHT;
    }

    void titleMap(int activeTitle, int unActiveTitle)
    {
        title[activeTitle].SetActive(true);
        title[unActiveTitle].SetActive(false);
    }

    void EnableMask()
    {
        mask.GetComponent<Mask>().enabled = !mask.GetComponent<Mask>().enabled;
    }

    void Draw(Map.Rect _room, System.Action<Map.Rect> action)
    {
        ThreadStart threadStart = delegate
        {
            action(_room);
        };
        threadStart.Invoke();
    }

    void Draw(Map.Rect _room, float f, System.Action<Map.Rect, float> action)
    {
        ThreadStart threadStart = delegate
        {
            action(_room, f);
        };
        threadStart.Invoke();
    }

    #region DrawArray
    void DrawArray(int x, int y, Color color)
    {
        try
        {
            mapColors[x * (minmapSizeWidth + 1) + y] = color;
        }
        catch (System.IndexOutOfRangeException e)
        {
            System.Console.WriteLine("예외가 발생했지롱 : {0}", e.Message);
        }
    }
    #endregion
    #region DrawArea
    void DrawIcon(Map.Rect _rect,float percent)
    {
        if (!_rect.isRoom)
            return;
        Sprite sprite;
        switch (_rect.eRoomType)
        {
            default:
            case RoomType.EVENT:
                sprite = eventIcon;
                break;
            case RoomType.MONSTER:
                sprite = monsterIcon;
                break;
            case RoomType.BOSS:
                sprite = bossIcon;
                break;
            case RoomType.STORE:
                sprite = storeIcon;
                break;
        }

        int x = _rect.x * pixelNum + _rect.width * pixelNum / 2;
        int y = _rect.y * pixelNum + _rect.height * pixelNum / 2;
        Rect textureRect = sprite.textureRect;
        float rectWidth = textureRect.width;
        float rectHeight = textureRect.height;
        x -= (int)rectWidth / 2;
        y -= (int)rectHeight / 2;
        for (int i = 0; i < rectWidth; i++)
        {
            for (int j = 0; j < rectHeight; j++)
            {
                Color color = sprite.texture.GetPixel((int)textureRect.x + i, (int)textureRect.y + j);
                if (color != clear)
                {
                    DrawArray(x + i, y + j, color);
                }
            }
        }
    }
    void DrawRoom(Map.Rect _room, float percent)
    {
        int minX = _room.x * pixelNum;
        int maxX = (_room.x + _room.width) * pixelNum - 1;
        int minY = _room.y * pixelNum;
        int maxY = (_room.y + _room.height) * pixelNum - 1;

        float mapMidX = _room.midX * mapSize;
        float mapMidY = _room.midY * mapSize;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (x == minX || x == maxX ||
                    y == minY || y == maxY)
                {
                    if (y == maxY)
                    {
                        DrawArray(x, y + 1, border);
                        if (x == maxX)
                        {
                            DrawArray(x, y, ground);
                            DrawArray(x + 1, y, border);
                            DrawArray(x + 1, y + 1, border);
                        }
                        else if (x == minX)
                        {
                            DrawArray(x, y, border);
                        }
                    }
                    else if (x == maxX)
                    {
                        DrawArray(x, y, ground);
                        DrawArray(x + 1, y, border);
                        if (y == minY)
                        {
                            DrawArray(x, y, border);
                        }
                    }
                    else
                    {
                        DrawArray(x, y, border);
                    }
                }
                Draw(_room, percent, DrawIcon);
            }
        }
        for (int i = 0; i < _room.doorObjects.Count; i++)
        {
            if (!_room.isRoom)
                break;
            bool horizon = _room.doorObjects[i].GetComponent<Door>().GetHorizon();

            float gap;

            if (!horizon) // 세로
            {
                gap = mapMidY - _room.doorObjects[i].transform.position.y;

                int pos = (minX + maxX) / 2;

                int doorPos = (int)Mathf.Floor(_room.doorObjects[i].transform.position.x);
                float x = doorPos / mapSizeWidth;
                pos = (int)(x * minmapSizeWidth);
                if (gap < 0)
                {
                    //top
                    DrawArray(pos - 1, maxY + 1, doorColor);
                    DrawArray(pos, maxY + 1, doorColor);
                    DrawArray(pos + 1, maxY + 1, doorColor);
                }
                else
                {
                    //bottom
                    DrawArray(pos - 1, minY, doorColor);
                    DrawArray(pos, minY, doorColor);
                    DrawArray(pos + 1, minY, doorColor);
                }
            }
            else
            {
                gap = mapMidX - _room.doorObjects[i].transform.position.x;

                int pos = (minY + maxY) / 2;

                int doorPos = (int)Mathf.Floor(_room.doorObjects[i].transform.position.y);
                float y = doorPos / mapSizeHeight;
                pos = (int)(y * minmapSizeHeight);

                if (gap < 0)
                {
                    //right
                    DrawArray(maxX + 1, pos - 1, doorColor);
                    DrawArray(maxX + 1, pos, doorColor);
                    DrawArray(maxX + 1, pos + 1, doorColor);
                }
                else
                {
                    //left
                    DrawArray(minX, pos - 1, doorColor);
                    DrawArray(minX, pos, doorColor);
                    DrawArray(minX, pos + 1, doorColor);
                }
            }

        }

        texture.SetPixels(mapColors);

        texture.Apply();
    }
    void DrawHall(Map.Rect _room)
    {
        int minX = _room.x * pixelNum;
        int maxX = (_room.x + _room.width) * pixelNum - 1;
        int minY = _room.y * pixelNum;
        int maxY = (_room.y + _room.height) * pixelNum - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                DrawArray(x, y, hallColor);
            }
        }
    }
    void DrawRoomOutline(Map.Rect _room)
    {
        if (!_room.isRoom)
            return;
        for(int i=0;i<_room.edgeRect.Count;i++)
        {
            if (_room.edgeRect[i].isRoom)
                continue;
            DrawSideLine(_room, _room.edgeRect[i]);
        }
        for (int i = 0; i < _room.linkedEdgeRect.Count; i++)
        {
            if (_room.linkedEdgeRect[i].isRoom)
                continue;
            DrawSideLine(_room, _room.linkedEdgeRect[i]);
        }
    }
    void DrawSideLine(Map.Rect _roomA, Map.Rect _roomB)
    {
        int start = 0;
        int end = 0;
        int x = 0;
        int y = 0;
        bool leftOrRight = true;
        Direction direction = Check(_roomA, _roomB);
        List<int> yArr = new List<int>(4)
                {
                    _roomB.y * pixelNum,
                    (_roomB.y + _roomB.height) * pixelNum,
                    _roomA.y * pixelNum,
                    (_roomA.y + _roomA.height) * pixelNum
                };

        yArr.Sort();
        List<int> xArr = new List<int>(4)
                {
                    _roomB.x * pixelNum,
                    (_roomB.x + _roomB.width) * pixelNum,
                    _roomA.x * pixelNum,
                    (_roomA.x + _roomA.width) * pixelNum
                };

        xArr.Sort();
        switch (direction)
        {
            case Direction.LEFT:
                start = yArr[1];
                end = yArr[2] + 1;

                x = _roomA.x * pixelNum;
                break;
            case Direction.RIGHT:
                start = yArr[1];
                end = yArr[2];

                x = (_roomA.x + _roomA.width) * pixelNum;
                break;
            case Direction.TOP:
                start = xArr[1];
                end = xArr[2] + 1;

                y = (_roomA.y + _roomA.height) * pixelNum;
                leftOrRight = false;
                break;
            case Direction.DOWN:
                start = xArr[1];
                end = xArr[2];

                y = _roomA.y * pixelNum;
                leftOrRight = false;
                break;
            default:
                break;
        }

        if (leftOrRight)
        {
            for(int i = start; i< end; i++)
            {
                DrawArray(x, i, border);
            }
        }
        else
        {
            for (int i = start; i < end; i++)
            {
                DrawArray(i, y, border);
            }
        }
    }
    void DrawDoor(Map.Rect _room)
    {
        int minX = _room.x * pixelNum;
        int maxX = (_room.x + _room.width) * pixelNum - 1;
        int minY = _room.y * pixelNum;
        int maxY = (_room.y + _room.height) * pixelNum - 1;

        float mapMidX = _room.midX * mapSize;
        float mapMidY = _room.midY * mapSize;


        for (int i = 0; i < _room.doorObjects.Count; i++)
        {
            bool horizon = _room.doorObjects[i].GetComponent<Door>().GetHorizon();

            float gap;

            if (!horizon) // 세로
            {
                gap = mapMidY - _room.doorObjects[i].transform.position.y;

                int pos = (minX + maxX) / 2;

                int doorPos = (int)Mathf.Floor(_room.doorObjects[i].transform.position.x);
                float x = doorPos / mapSizeWidth;
                pos = (int)(x * minmapSizeWidth);
                if (gap < 0)
                {
                    //top
                    DrawArray(pos - 1, maxY + 1, doorColor);
                    DrawArray(pos, maxY + 1, doorColor);
                    DrawArray(pos + 1, maxY + 1, doorColor);
                }
                else
                {
                    //bottom
                    DrawArray(pos - 1, minY, doorColor);
                    DrawArray(pos, minY, doorColor);
                    DrawArray(pos + 1, minY, doorColor);
                }
            }
            else
            {
                gap = mapMidX - _room.doorObjects[i].transform.position.x;

                int pos = (minY + maxY) / 2;

                int doorPos = (int)Mathf.Floor(_room.doorObjects[i].transform.position.y);
                float y = doorPos / mapSizeHeight;
                pos = (int)(y * minmapSizeHeight);

                if (gap < 0)
                {
                    //right
                    DrawArray(maxX + 1, pos - 1, doorColor);
                    DrawArray(maxX + 1, pos, doorColor);
                    DrawArray(maxX + 1, pos + 1, doorColor);
                }
                else
                {
                    //left
                    DrawArray(minX, pos - 1, doorColor);
                    DrawArray(minX, pos, doorColor);
                    DrawArray(minX, pos + 1, doorColor);
                }
            }

        }

    }
    #endregion
    public void SetFloorText()
    {
        int floor = GameDataManager.Instance.GetFloor();
        if (RoomSetManager.Instance.FloorRoomSetGroups.Length <= floor)
        {
            floor = RoomSetManager.Instance.FloorRoomSetGroups.Length - 1;
        }
        floorT.text = RoomSetManager.Instance.FloorRoomSetGroups[floor].floorName;
    }

    public void ClearRoom(Map.Rect _room, float percent)
    {
        if (inDungeon)
        {
            HideEmoticon(true);
            inDungeon = false;
        }
        Draw(_room, percent, DrawRoom);
    }

    private void HideEmoticon(bool isActive)
    {
        playerIcon.gameObject.SetActive(isActive);
        if (title[0].gameObject.activeSelf == !isActive)
        {
            title[0].gameObject.SetActive(isActive);
        }
    }

    public void DrawMinimap()
    {
        isDrawAll = false;
        SetFloorText();
        renderer = minimapObj.GetComponent<RawImage>();
        roomList = RoomManager.Instance.GetRoomList(); //리스트 받아오기
        minmapSizeWidth = Map.MapManager.Instance.currentMapSet.mapSize.x * pixelNum; // 미니맵 전체 픽셀 사이즈
        minmapSizeHeight = Map.MapManager.Instance.currentMapSet.mapSize.y * pixelNum;
        mapSize = 3;
        mapSizeWidth = mapSize * Map.MapManager.Instance.currentMapSet.mapSize.x; // 실제 맵 크기
        mapSizeHeight = mapSize * Map.MapManager.Instance.currentMapSet.mapSize.y;

        if (Map.MapManager.Instance.currentMapSet.mapSize.x * pixelNum > Map.MapManager.Instance.currentMapSet.mapSize.y * pixelNum)
            minimapObj.GetComponent<RectTransform>().sizeDelta = new Vector2(minimapBaseWidth * (float)mapSizeWidth / mapSizeHeight, minimapBaseHeight);
        else
            minimapObj.GetComponent<RectTransform>().sizeDelta = new Vector2(minimapBaseWidth, minimapBaseHeight * (float)mapSizeHeight / mapSizeWidth);
        oldPos = new Vector2(transform.localPosition.x, transform.localPosition.y);

        width = minimapObj.GetComponent<RectTransform>().sizeDelta.x;
        height = minimapObj.GetComponent<RectTransform>().sizeDelta.y;

        texture = new Texture2D(minmapSizeWidth + 1, minmapSizeHeight + 1);
        mapColors = new Color[(minmapSizeWidth + 1) * (minmapSizeHeight + 1)];
        texture.filterMode = FilterMode.Point;
        renderer.texture = texture;
        DrawMap();
    } // 미니맵 그리는 함수

    public void HideMiniMap()
    {
        inDungeon = true;
        if (isToggle)
            HideMap();
        HideEmoticon(false);
        this.gameObject.SetActive(!this.gameObject.activeSelf);
    }

    public void HideMap()
    {
        mask.GetComponent<Mask>().enabled = !mask.GetComponent<Mask>().enabled;

        playerIcon.transform.localPosition = new Vector2(-maskSize, -maskSize);
        transform.localPosition = oldPos;
        renderer.color = new Color(1, 1, 1, 1);
        titleMap(0, 1);
        isToggle = false;
    }

    public void ToggleMinimap()
    {
        EnableMask();

        if (!inDungeon)
        {
            if (isToggle)
            {
                // 미니맵으로
                playerIcon.transform.localPosition = new Vector2(-maskSize, -maskSize);
                transform.localPosition = oldPos;
                renderer.color = new Color(1, 1, 1, 1);
                titleMap(0, 1);
            }
            else
            {
                // 크게 키운 맵
                minimapObj.transform.localPosition = new Vector2(-maskSize, -maskSize);
                transform.localPosition = new Vector2(maskSize, maskSize);
                renderer.color = new Color(1, 1, 1, 0.7f);
                titleMap(1, 0);
            }
            isToggle = !isToggle;
        }
    }

    void DrawMap()
    {

        for (int i = 0; i < mapColors.Length; i++)
        {
            mapColors[i] = ground;
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            if (!roomList[i].isRoom)
                Draw(roomList[i], DrawHall);
            else if(roomList[i].isDrawed)
                Draw(roomList[i], 1, DrawRoom);
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].isRoom)
                Draw(roomList[i], DrawRoomOutline);
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            if (!roomList[i].isRoom)
                Draw(roomList[i], DrawDoor);
        }

        for (int i = 0; i <= minmapSizeWidth; i++)
        {
            for (int j = 0; j <= minmapSizeHeight; j++)
            {
                if (i == 0 || i == minmapSizeWidth ||
                 j == 0 || j == minmapSizeHeight)
                {
                    DrawArray(i, j, border);
                }
            }
        }

        DrawEventRoom();

        texture.SetPixels(mapColors);

        texture.Apply();
    }

    void DrawAllRoom()
    {
        for(int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].isRoom)
                Draw(roomList[i], 1, DrawRoom);
        }
    }

    void DrawEventRoom()
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].eRoomType == RoomType.EVENT)
                Draw(roomList[i], 1, DrawRoom);
        }
    }
    
    bool FixedPlayer()
    {
        float maxValue = 34 * mapSize / 3  * Map.MapManager.Instance.currentMapSet.mapSize.x / 15;
        float minValue = 11.1f * mapSize / 3 * Map.MapManager.Instance.currentMapSet.mapSize.x / 15;

        bool isX = playerPositon.x >= maxValue || playerPositon.x <= minValue;
        bool isY = playerPositon.y >= maxValue || playerPositon.y <= minValue;

        if (isX && isY)
        {
            if (playerPositon.x >= maxValue)
                mapMoveX = maxValue;
            else
                mapMoveX = minValue;
            if (playerPositon.y >= maxValue)
                mapMoveY = maxValue;
            else
                mapMoveY = minValue;
            isTranslate = 3;
            return true;
        }
        else if (isX)
        {
            if (playerPositon.x >= maxValue)
                mapMoveX = maxValue;
            else
                mapMoveX = minValue;
            isTranslate = 1;
            return true;
        }
        else if (isY)
        {
            if (playerPositon.y >= maxValue)
                mapMoveY = maxValue;
            else
                mapMoveY = minValue;
            isTranslate = 2;
            return true;
        }
        else
        {
            playerIcon.transform.localPosition = new Vector2(-maskSize, -maskSize);
            isTranslate = 0;
            return false;
        }
    }

    void MovePlayerIconMinimap()
    {
        float _width = width / 2f;
        float _height = height / 2f;

        switch (isTranslate)
        {
            default:
                break;
            case 1:
                //Debug.Log("맵의 y축은 이동하고 플레이어 아이콘의 x축이 이동");
                minimapObj.transform.localPosition = new Vector2(-(mapMoveX / mapSizeWidth) * width + width / 2 - maskSize,
                    -(playerPositon.y / mapSizeHeight) * height + height / 2 - maskSize - 0.2f);
                iconV = new Vector2(playerPositon.x / mapSizeWidth * width - maskSize - _width / 2f, -maskSize);
                if (iconV.x > 0)
                    iconV.x -= maskSize * 2;
                playerIcon.transform.localPosition = iconV;
                break;
            case 2:
                //Debug.Log("맵의 x축은 이동하고 플레이어 아이콘의 y축이 이동");
                minimapObj.transform.localPosition = new Vector2(-(playerPositon.x / mapSizeWidth) * width + width / 2 - maskSize,
                    -(mapMoveY / mapSizeHeight) * height + height / 2 - maskSize - 0.2f);
                iconV = new Vector2(-maskSize, playerPositon.y / mapSizeHeight * height - maskSize - _height / 2f);
                if (iconV.y > 0)
                    iconV.y -= maskSize * 2;
                playerIcon.transform.localPosition = iconV;
                break;
            case 3:
                //Debug.Log("맵은 고정해두고 플레이어 아이콘만 x축 y축 둘 다 이동");
                minimapObj.transform.localPosition = new Vector2(-(mapMoveX / mapSizeWidth) * width + width / 2 - maskSize,
                    -(mapMoveY / mapSizeHeight) * height + height / 2 - maskSize - 0.2f);
                iconV = new Vector2(playerPositon.x / mapSizeWidth * width - maskSize - _width / 2f,
                    playerPositon.y / mapSizeHeight * height - maskSize - _height / 2f);
                if (iconV.x > 0)
                    iconV.x -= maskSize * 2;
                if (iconV.y > 0)
                    iconV.y -= maskSize * 2;
                playerIcon.transform.localPosition = iconV;
                break;
                
        }
    }

    void MovePlayerIcon()
    {
        iconV = new Vector2(playerPositon.x / mapSizeWidth * width - maskSize - width / 2,
            playerPositon.y / mapSizeHeight * height - maskSize - height / 2);
        playerIcon.transform.localPosition = iconV;
    } // 현재 플레이어 위치 to MiniMap

    void MoveMinimapIcon()
    {
        minimapObj.transform.localPosition = new Vector2(-(playerPositon.x / mapSizeWidth) * width + width / 2 - maskSize,
            -(playerPositon.y / mapSizeHeight) * height + height / 2 - maskSize - 0.2f);
    } // 현재 플레이어 위치 to MiniMap

    public void DrawDebuging()
    {
        if(isDrawAll)
        {
            isDrawAll = false;
            DrawMap();
        }
        else
        {
            isDrawAll = true;
            DrawAllRoom();
        }
    }
    #region UnityFunc
    private void Update()
    {
        playerPositon = PlayerManager.Instance.GetPlayerPosition();

        if (!isTutorial)
        {
            if (isToggle)
                MovePlayerIcon();
            else
            {
                if (FixedPlayer())
                    MovePlayerIconMinimap();
                else
                    MoveMinimapIcon();
            }
        }
    }
    #endregion
}
