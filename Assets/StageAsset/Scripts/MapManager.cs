﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Map
{
    public class MapManager : MonoBehaviour
    {
        private static MapManager instance;
        public ObjectPool objectPool;
        public Material spriteMaterial;
        public int width, height, size, area, floor;
        Map map;
        public static MapManager Getinstance()
        {
            if(instance == null)
            {
                instance = GameObject.FindObjectOfType(typeof(MapManager)) as MapManager;
            }
            return instance;
        }
        public void LightTurn()
        {
            if(spriteMaterial.color == Color.white)
                spriteMaterial.color = Color.grey;
            else
                spriteMaterial.color = Color.white;
        }
        public void PlayerPositionToMap()
        {
            if (map != null)
                MiniMap.GetInstance().PlayerPositionToMap();
        }
        public void GenerateMap()
        {
            if (map != null)
            {
                map.Dispose();
                map = null;
            }
            map = new Map(width, height, size, area, floor, objectPool);
            map.Generate();
            MiniMap.GetInstance().GetRoomList();
            MiniMap.GetInstance().DrawMinimap();
        }
 
        #region UnityFunc
        private void Update()
        {
        }
        #endregion
    }

    class Map
    {
        Rect mainRect;
        Queue<Rect> rects, blocks;
        List<Rect> halls, rooms;
        List<Rect> necessaryBlocks, necessaryRooms;
        Tilemap wallTileMap, floorTileMap, shadowTileMap;
        const float MaxHallRate = 0.2f;
        int MinumRoomArea = 4;
        int TotalHallArea = 0;
        int width;
        int height;
        int size;
        int floor;
        ObjectPool objectPool;

        public Map(int _width,int _height,int _size, int _area,int _floor, ObjectPool _objectPool)
        {
            mainRect = new Rect(0, 0, _width, _height, _size);
            width = _width;
            height = _height;
            size = _size;
            MinumRoomArea = _area;
            floor = _floor;
            objectPool = _objectPool;
            rects = new Queue<Rect>();
            blocks = new Queue<Rect>();
            halls = new List<Rect>(_width * _height);
            rooms = new List<Rect> (_width * _height);
            necessaryBlocks = new List<Rect>(_width * _height);
            necessaryRooms = new List<Rect>(_width * _height);
            floorTileMap = TileManager.GetInstance().floorTileMap;
            wallTileMap = TileManager.GetInstance().wallTileMap;
            shadowTileMap = TileManager.GetInstance().shadowTileMap;
        } // 생성자

        #region MakeMap
        public void Generate() 
        {
            CreateMap();

            if (rooms.Count > 0)
            {
                LinkAllRects();
                DrawTile();
                LinkDoor(rooms[0]);
                AssignAllRoom();
                rooms.AddRange(halls);
                RoomManager.Getinstance().SetRoomList(rooms, halls[0]);
                RoomManager.Getinstance().LoadMaskObject();
            }
        } // office creates

        void RefreshData()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].customObjects != null)
                    for (int j = 0; j < rooms[i].customObjects.Length; j++)
                        Object.DestroyImmediate(rooms[i].customObjects[j].GetComponent<CustomObject>());
                if (rooms[i].doorObjects != null)
                    for (int j = 0; j < rooms[i].doorObjects.Count; j++)
                        Object.DestroyImmediate(rooms[i].doorObjects[j].GetComponent<CustomObject>());
                if (rooms[i].maskObject != null)
                    Object.Destroy(rooms[i].maskObject);
            }

            objectPool.Deactivation();

            rects.Clear();
            halls.Clear();
            blocks.Clear();
            rooms.Clear();
            necessaryBlocks.Clear();
            TotalHallArea = 0;
      
        } // 데이터 초기화

        void NecessaryRoomSet()
        {
            Rect room = new Rect(0, 0, 2, 2, size);

            necessaryBlocks.Add(room);
        } // 필수 방 세팅

        bool CreateMap()
        {
            Random.InitState((int)System.DateTime.Now.Ticks);
            RefreshData();
            rects.Enqueue(mainRect);

            NecessaryRoomSet();
            RectToBlock();
            bool success = BlockToRoom();

            return success;
        } // 맵 만들기 (실패 시 false 리턴)

        void RectToBlock()
        {
            Rect rect;

            while (rects.Count > 0 && ((float)TotalHallArea / mainRect.area < MaxHallRate))
            {
                rect = rects.Dequeue();
                if (rect.area > MinumRoomArea)
                    SplitHall(rect);
                else blocks.Enqueue(rect);  
            }

            while (rects.Count > 0)
            {
                rect = rects.Dequeue();
                if(rect.area > 1)
                    blocks.Enqueue(rect);
            }

        } // Rects -> Blocks;

        bool BlockToRoom()
        {
            for(int i = 0; i < necessaryBlocks.Count; i++)
            {
                necessaryRooms.Add(necessaryBlocks[i]);
            }
            Rect block;
            while (blocks.Count > 0)
            {
                block = blocks.Dequeue();
                if ((block.area > MinumRoomArea || block.width / block.height >= 3 || block.height / block.width >= 3) && !FitRectCheck(block, necessaryRooms))
                    SplitBlock(block);
                else
                {
                    block.isRoom = true;
                    rooms.Add(block);
                }
            }
            if (necessaryRooms.Count > 0)
                return false;

            while (blocks.Count > 0)
            {
                block = blocks.Dequeue();
                block.isRoom = false;
                halls.Add(block);
            }
            return true;
        } // Blocks -> Rooms;

        void DrawTile()
        {
            RandomTile floor = TileManager.GetInstance().floorTile;
            TileBase[] wall = TileManager.GetInstance().wallTile;
            TileBase[] shadow = TileManager.GetInstance().shadowTile;
            Rect rect;
            floorTileMap.ClearAllTiles();
            wallTileMap.ClearAllTiles();
            shadowTileMap.ClearAllTiles();
            for (int i = 0; i < width * size; i++) // 전체 맵 그리기
            {
                for (int j = 0; j < height * size; j++)
                {
                    floorTileMap.SetTile(new Vector3Int(i, j, 0), floor);
                    if (i == 0 || j == 0 || i == width * size - 1 || j == height * size - 1)
                    {
                        if (i == 0 & j == 0)
                            wallTileMap.SetTile(new Vector3Int(i, j, 0), wall[1]);
                        else if (i == 0 && j == height * size - 1)
                            wallTileMap.SetTile(new Vector3Int(i, j, 0), wall[5]);
                        else if (i == width * size - 1 && j == 0)
                            wallTileMap.SetTile(new Vector3Int(i, j, 0), wall[1]);
                        else if (i == width * size - 1 && j == height * size - 1)
                            wallTileMap.SetTile(new Vector3Int(i, j, 0), wall[7]);
                        else if (i == 0)
                            wallTileMap.SetTile(new Vector3Int(i, j, 0), wall[3]);
                        else if (j == 0)
                            wallTileMap.SetTile(new Vector3Int(i, j, 0), wall[1]);
                        else if (i == width * size - 1)
                            wallTileMap.SetTile(new Vector3Int(i, j, 0), wall[4]);
                        else
                            wallTileMap.SetTile(new Vector3Int(i, j, 0), wall[6]);
                    }
                }
            } // 전체 맵 그리기
            for (int index = 0; index < rooms.Count; index++) 
            {
                rect = rooms[index];
                for(int e = 0; e < rect.edgeRect.Count; e++)
                {            
                    if ((Mathf.Abs(rect.midX - rect.edgeRect[e].midX) < (float)(rect.width + rect.edgeRect[e].width) / 2) && (Mathf.Abs(rect.midY - rect.edgeRect[e].midY) == (float)(rect.height + rect.edgeRect[e].height) / 2))
                    {
                        if (rect.midY > rect.edgeRect[e].midY && rect.isRoom && rect.edgeRect[e].isRoom) // 위쪽
                            rooms[index].downExist = true;
                    } // 세로로 붙음
                }
                if (rect.downExist)
                {
                    for (int x = rect.x; x < rect.x + rect.width; x++)
                    {
                        for (int y = rect.y; y < rect.y + rect.height; y++)
                        {
                            for (int i = 0; i < size; i++)
                            {
                                for (int j = 0; j < size; j++)
                                {
                                    if (size * x + i == rect.x * size || size * y + j == rect.y * size || size * x + i == (rect.x + rect.width) * size - 1 || size * y + j == (rect.y + rect.height) * size - 1)
                                    {
                                        if (size * x + i == rect.x * size & size * y + j == rect.y * size)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[3]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), shadow[1]);
                                        }
                                        else if (size * x + i == rect.x * size && size * y + j == (rect.y + rect.height) * size - 1)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[5]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[2]);
                                        }
                                        else if (size * x + i == (rect.x + rect.width) * size - 1 && size * y + j == rect.y * size)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[4]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i + 1, size * y + j, 0), shadow[1]);
                                        }
                                        else if (size * x + i == (rect.x + rect.width) * size - 1 && size * y + j == (rect.y + rect.height) * size - 1)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[7]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[0]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i + 1, size * y + j, 0), shadow[1]);
                                        }
                                        else if (size * x + i == rect.x * size)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[3]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), shadow[1]);
                                        }
                                        else if (size * y + j == rect.y * size)
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), null);
                                        else if (size * x + i == (rect.x + rect.width) * size - 1)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[4]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i + 1, size * y + j, 0), shadow[1]);
                                        }
                                        else
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[6]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[0]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                } // 아래 방이 있을 경우
                else
                {
                    for (int x = rect.x; x < rect.x + rect.width; x++)
                    {
                        for (int y = rect.y; y < rect.y + rect.height; y++)
                        {
                            for (int i = 0; i < size; i++)
                            {
                                for (int j = 0; j < size; j++)
                                {
                                    if (size * x + i == rect.x * size || size * y + j == rect.y * size || size * x + i == (rect.x + rect.width) * size - 1 || size * y + j == (rect.y + rect.height) * size - 1)
                                    {
                                        if (size * x + i == rect.x * size & size * y + j == rect.y * size)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[0]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), shadow[1]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[0]);
                                        }
                                        else if (size * x + i == rect.x * size && size * y + j == (rect.y + rect.height) * size - 1)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[5]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[2]);
                                        }
                                        else if (size * x + i == (rect.x + rect.width) * size - 1 && size * y + j == rect.y * size)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[2]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[0]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i + 1, size * y + j, 0), shadow[1]);
                                        }
                                        else if (size * x + i == (rect.x + rect.width) * size - 1 && size * y + j == (rect.y + rect.height) * size - 1)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[7]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[0]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i + 1, size * y + j, 0), shadow[1]);
                                        }
                                        else if (size * x + i == rect.x * size)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[3]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), shadow[1]);
                                        }
                                        else if (size * y + j == rect.y * size)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[1]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[0]);
                                        }
                                        else if (size * x + i == (rect.x + rect.width) * size - 1)
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[4]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i + 1, size * y + j, 0), shadow[1]);
                                        }
                                        else
                                        {
                                            wallTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), wall[6]);
                                            shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[0]);
                                        }
                                    }                               
                                }
                            }
                        }
                    }
                }
            } // 방그리기
            for (int index = 0; index < halls.Count; index++)
            {
                rect = halls[index];
                for (int x = rect.x; x < rect.x + rect.width; x++)
                {
                    for (int y = rect.y; y < rect.y + rect.height; y++)
                    {
                        for (int i = 0; i < size; i++)
                        {
                            for (int j = 0; j < size; j++)
                            {
                                floorTileMap.SetTile(new Vector3Int(size * x + i, size * y + j, 0), floor);
                                if (size * y + j == height * size - 1)
                                {
                                    if(size * x + i == rect.x * size)
                                    {
                                        shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[2]);
                                    }
                                    else
                                        shadowTileMap.SetTile(new Vector3Int(size * x + i, size * y + j - 1, 0), shadow[0]);
                                }
                            }
                        }
                    }
                }
            } // 홀 그리기

        } // 바닥 타일, 벽 타일 드로잉

        bool FitRectCheck(Rect _currentRect ,Rect _rectA, Rect _rectB , List<Rect> _list)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                if ((_rectA.width >= _list[i].width && _rectA.height >= _list[i].height) || (_rectB.width >= _list[i].width && _rectB.height >= _list[i].height))
                {
                    return true;
                }
            }
            for (int i = 0; i < _list.Count; i++)
            {
                if (_currentRect.width >= _list[i].width && _currentRect.height >= _list[i].height)
                {
                    necessaryRooms.Add(_list[i]);
                    _list.RemoveAt(i);
                    return false;
                }
            }
            return true;
        } // check Rects for fit necessary Room

        bool FitRectCheck(Rect _rect, List<Rect> _list)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                if (_rect.width == _list[i].width && _rect.height == _list[i].height)
                {
                    _list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        } // check blocks for fit necessary Room

        void SplitHall(Rect _currentRect)
        {
            Rect hall = null;

            Rect rect_a = null, rect_b = null;
            RandomBlockSplit(_currentRect, out rect_a, out hall, out rect_b);

            if (FitRectCheck(_currentRect, rect_a, rect_b, necessaryBlocks))
            {
                rects.Enqueue(rect_a);
                rects.Enqueue(rect_b);
                hall.isRoom = false;
                hall.isClear = true;
                halls.Add(hall);
                TotalHallArea += hall.area;
            }
            else
            {
                blocks.Enqueue(_currentRect);
            }

        } // split rects -> rects & halls

        void SplitBlock(Rect _currentBlock)
        {
            Rect block_a = null;
            Rect block_b = null;
            RandomRoomSplit(_currentBlock, out block_a, out block_b);
            blocks.Enqueue(block_a);
            blocks.Enqueue(block_b);
        } // split blocks -> blocks

        void RandomBlockSplit(Rect _currentRect, out Rect _rectA, out Rect _hall, out Rect _rectB)
        {
            bool flag = true;

            if (_currentRect.width > _currentRect.height)
            {
                flag = true;
            }
            else if (_currentRect.width < _currentRect.height)
            {
                flag = false;
            }
            else
            {
                if (CoinFlip(50))
                    flag = true;
                else
                    flag = false;
            }

            if(flag)
            {
                int x1 = (int)((_currentRect.x + 0.5f) + _currentRect.width * (float)Random.Range(4, 7) /10);
                int x2 = x1 + 1;
                _rectA = new Rect(_currentRect.x, _currentRect.y, x1 - _currentRect.x, _currentRect.height, size);
                _hall = new Rect(_rectA.x + _rectA.width, _currentRect.y, 1, _currentRect.height, size);
                _rectB = new Rect(_hall.x + _hall.width, _currentRect.y, _currentRect.width - _rectA.width - _hall.width, _currentRect.height, size);
            }
            else
            {
                int y1 = (int)((_currentRect.y + 0.5f) + _currentRect.height * (float)Random.Range(4, 7) / 10);
                int y2 = y1 + 1;
                _rectA = new Rect(_currentRect.x, _currentRect.y, _currentRect.width, y1 - _currentRect.y, size);
                _hall = new Rect(_currentRect.x, _rectA.y + _rectA.height, _currentRect.width, 1, size);
                _rectB = new Rect(_currentRect.x, _hall.y + _hall.height, _currentRect.width, _currentRect.height - _rectA.height - _hall.height, size);
            }

        } // split 덩어리 and 홀 and 덩어리

        bool RandomRoomSplit(Rect _currentBlock, out Rect _blockA,out Rect _blockB) 
        {
            bool flag = true;

            if (_currentBlock.width > _currentBlock.height)
            {
                flag = true;
            }
            else if (_currentBlock.width < _currentBlock.height)
            {
                flag = false;
            }
            else
            {
                if (CoinFlip(50))
                    flag = true;
                else
                    flag = false;
            }

            
            if(flag) // 가로
            {
                int width = (int)((_currentBlock.width + 0.5f) * (float)Random.Range(4, 7) / 10);
                _blockA = new Rect(_currentBlock.x, _currentBlock.y, width, _currentBlock.height, size);
                _blockB = new Rect(_currentBlock.x + width, _currentBlock.y, _currentBlock.width - width, _currentBlock.height, size);
            }
            else
            {
                int height = (int)((_currentBlock.height + 0.5f) * (float)Random.Range(4, 7) / 10);
                _blockA = new Rect(_currentBlock.x, _currentBlock.y, _currentBlock.width, height, size);
                _blockB = new Rect(_currentBlock.x, _currentBlock.y + height, _currentBlock.width, _currentBlock.height - height, size);
            }
            return true;
        } // split 방 and 방

        void LinkAllRects() // 모든 Rects(Rooms and Halls) 연결
        {
            for(int i = 0; i < rooms.Count - 1; i++)
            {
                for (int k = 0; k < halls.Count; k++)
                {
                    LinkRects(rooms[i], halls[k]);
                }
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    LinkRects(rooms[i], rooms[j]);
                }
            }
            for(int i = 0; i < halls.Count; i++)
            {
                for (int k = 0; k < halls.Count; k++)
                {
                    LinkRects(halls[i], halls[k]);
                }
            }
        }

        void LinkRects(Rect _rectA, Rect _rectB) // 두개의 방을 직접 연결
        {
            if((Mathf.Abs(_rectA.midX - _rectB.midX) == (float)(_rectA.width + _rectB.width)/2) && (Mathf.Abs(_rectA.midY - _rectB.midY) < (float)(_rectA.height + _rectB.height) / 2))
            {
                _rectA.EdgeRect(_rectB);
            }
            else if ((Mathf.Abs(_rectA.midX - _rectB.midX) < (float)(_rectA.width + _rectB.width) / 2) &&(Mathf.Abs(_rectA.midY - _rectB.midY) == (float)(_rectA.height + _rectB.height) / 2))
            {
                _rectA.EdgeRect(_rectB);
            }
        }

        void LinkDoor(Rect _rect)
        {
            _rect.visited = true;

            for (int i = 0; i < _rect.edgeRect.Count; i++)
            {
                if ((_rect.isRoom || _rect.edgeRect[i].isRoom) && !_rect.edgeRect[i].visited && _rect.eRoomType != RoomType.BOSS)
                {
                    DrawDoorTile(_rect, _rect.edgeRect[i]); //문 놓을 곳에 타일 지우기
                    LinkDoor(_rect.edgeRect[i]);
                }
            }
        } // 신장 트리 연결 & loop
        
        void DrawDoorTile(Rect _rectA,Rect _rectB) 
        {
            GameObject obj = null;
            if ((Mathf.Abs(_rectA.midX - _rectB.midX) == (float)(_rectA.width + _rectB.width) / 2) && (Mathf.Abs(_rectA.midY - _rectB.midY) < (float)(_rectA.height + _rectB.height) / 2))
            {
                float mainY = _rectA.midY;
                float subY = _rectB.midY;
                int y;

                List<int> yArr = new List<int>(4);
                yArr.Add(_rectB.y * size);
                yArr.Add((_rectB.y + _rectB.height) * size);
                yArr.Add(_rectA.y * size);
                yArr.Add((_rectA.y + _rectA.height) * size);

                yArr.Sort();

                y = Random.Range(yArr[1]  + size / 3, yArr[2] - size / 3);

                if (_rectA.midX > _rectB.midX) // 오른쪽
                {
                    wallTileMap.SetTile(new Vector3Int(_rectA.x * size, y, 0), null);
                    wallTileMap.SetTile(new Vector3Int(_rectA.x * size - 1, y, 0), null);
                    if (_rectB.isRoom) // 이미지에 따라 수정해야함
                        obj = CreateDoorObject(_rectA.x * size, y + 0.5f, true);
                    else
                        obj = CreateDoorObject(_rectA.x * size, y + 0.5f, true);
                }
                else // 왼쪽
                {
                    wallTileMap.SetTile(new Vector3Int(_rectB.x * size, y, 0), null);
                    wallTileMap.SetTile(new Vector3Int(_rectB.x * size - 1, y, 0), null);
                    if (_rectA.isRoom) // 이미지에 따라 수정해야함
                        obj = CreateDoorObject(_rectB.x * size, y + 0.5f, true);
                    else
                        obj = CreateDoorObject(_rectB.x * size, y + 0.5f, true);
                }
            } // 가로로 붙음
            else if ((Mathf.Abs(_rectA.midX - _rectB.midX) < (float)(_rectA.width + _rectB.width) / 2) && (Mathf.Abs(_rectA.midY - _rectB.midY) == (float)(_rectA.height + _rectB.height) / 2))
            {
                float mainX = _rectA.midX;
                float subX = _rectB.midX;
                int x;

                if (_rectA.width >= _rectB.width)
                {
                    x = Random.Range(_rectB.x * size + 2, (_rectB.x + _rectB.width) * size - 2);
                }
                else
                {
                    x = Random.Range(_rectA.x * size + 2, (_rectA.x + _rectA.width) * size - 2);
                }
                List<int> xArr = new List<int>(4);
                xArr.Add(_rectB.x * size);
                xArr.Add((_rectB.x + _rectB.width) * size);
                xArr.Add(_rectA.x * size);
                xArr.Add((_rectA.x + _rectA.width) * size);

                xArr.Sort();

                x = Random.Range(xArr[1] + size / 3, xArr[2] - size / 3);

                if (_rectA.midY > _rectB.midY) // 위쪽
                {
                    wallTileMap.SetTile(new Vector3Int(x, _rectA.y * size, 0), null);
                    wallTileMap.SetTile(new Vector3Int(x, _rectA.y * size - 1, 0), null);
                    if(_rectB.isRoom) // 아래에 있는 방이 Room 일 경우 한칸 아래 설치
                        obj = CreateDoorObject(x + 0.5f, _rectA.y * size - 0.5f, false);
                    else
                        obj = CreateDoorObject(x + 0.5f, _rectA.y * size + 0.5f, false);
                }
                else // 아래쪽
                {
                    wallTileMap.SetTile(new Vector3Int(x, _rectB.y * size, 0), null);
                    wallTileMap.SetTile(new Vector3Int(x, _rectB.y * size - 1, 0), null);
                    if (_rectA.isRoom) // 위에 있는 방이 Room 일 경우 한칸 아래 설치
                        obj = CreateDoorObject(x + 0.5f, _rectB.y * size - 0.5f, false);
                    else
                        obj = CreateDoorObject(x + 0.5f, _rectB.y * size + 0.5f, false);
                }
           
            } // 세로로 붙음

            _rectA.doorObjects.Add(obj);
            _rectB.doorObjects.Add(obj);

        } // Door 부분 타일 floor로 변경

        GameObject CreateDoorObject(float x,float y,bool isHorizon)
        {
            GameObject obj = objectPool.GetPooledObject();
            obj.AddComponent<Door>();
            obj.GetComponent<Door>().SetAxis(isHorizon);
            if (isHorizon)
                obj.GetComponent<Door>().sprite = RoomSetManager.GetInstance().doorSprites[0];
            else
                obj.GetComponent<Door>().sprite = RoomSetManager.GetInstance().doorSprites[1];
            obj.GetComponent<Door>().Init();
            obj.transform.position = new Vector3(x, y, y);

            return obj;
        } // Door Object 생성

        void AssignAllRoom()
        {
            RoomSetManager roomSetManager = RoomSetManager.GetInstance();

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomSet roomSet = roomSetManager.LoadRoomSet(rooms[i].width, rooms[i].height, 1);
                roomSet.x = rooms[i].x;
                roomSet.y = rooms[i].y;
                rooms[i].eRoomType = roomSet.roomType;
                rooms[i].customObjects = AssignRoom(roomSet);
                rooms[i].gage = roomSet.gage;
            }

            CreateStartPoint();
        } // 모든 룸 셋 배치

        GameObject[] AssignRoom(RoomSet _roomSet)
        {
            if (_roomSet == null)
                return null;
            GameObject[] customObjects = new GameObject[_roomSet.objectDatas.Count];
            for (int i = 0; i < _roomSet.objectDatas.Count; i++)
            {
                customObjects[i] = objectPool.GetPooledObject();
                customObjects[i].transform.position = new Vector3(_roomSet.x * size + _roomSet.objectDatas[i].position.x, _roomSet.y * size + _roomSet.objectDatas[i].position.y, _roomSet.y * size + _roomSet.objectDatas[i].position.y);
                _roomSet.objectDatas[i].LoadObject(customObjects[i]);
            }
            return customObjects;
        } // 룸 셋 배치

        void CreateStartPoint()
        {
            halls[0].customObjects = new GameObject[1];
            halls[0].customObjects[0] = objectPool.GetPooledObject();
            halls[0].customObjects[0].AddComponent<StartPoint>();
            halls[0].customObjects[0].GetComponent<StartPoint>().Init();
            halls[0].customObjects[0].transform.position = new Vector3((halls[0].areaLeftDown.x + halls[0].areaRightTop.x) / 2, (halls[0].areaLeftDown.y + halls[0].areaRightTop.y) / 2, (halls[0].areaLeftDown.y + halls[0].areaRightTop.y) / 2);
            //if (halls[0].width > halls[0].height)
            //    halls[0].customObjects[0].transform.position = new Vector3(halls[0].areaLeftDown.x + (halls[0].areaRightTop.x - halls[0].areaLeftDown.x) * 0.1f, (halls[0].areaLeftDown.y + halls[0].areaRightTop.y) / 2, (halls[0].areaLeftDown.y + halls[0].areaRightTop.y) / 2);
            //else
            //    halls[0].customObjects[0].transform.position = new Vector3((halls[0].areaLeftDown.x + halls[0].areaRightTop.x) / 2, halls[0].areaLeftDown.y + (halls[0].areaRightTop.y - halls[0].areaLeftDown.y) * 0.1f, (halls[0].areaLeftDown.y + halls[0].areaRightTop.y) / 2);
            halls[0].customObjects[0].GetComponent<StartPoint>().SetPosition();
        } // 스타트 포인트
        #endregion

        public void Dispose()
        {
            RefreshData();
        }

        bool CoinFlip(int percent)
        {
            return Random.Range(0, 100) < percent;
        } // 코인 플립 확률에 따른 yes or no 반환
    }

    public class Rect
    {
        public readonly int x;
        public readonly int y;
        public readonly int width;
        public readonly int height;
        public readonly int area;
        public readonly float midX, midY;
        public readonly int size;
        public int gage;
        public Vector2 areaLeftDown, areaRightTop;
        public bool visited;
        public List<Rect> edgeRect;
        public GameObject[] customObjects;
        public List<GameObject> doorObjects;
        public GameObject maskObject;
        public bool isRoom;
        public bool downExist;
        public bool isClear;
        public RoomType eRoomType;

        public Rect(int _x,int _y,int _width,int _height,int _size)
        {
            x = _x;
            y = _y;
            width = _width;
            height = _height;
            area = width * height;
            midX = x + (float)width / 2;
            midY = y + (float)height / 2;
            size = _size;
            areaLeftDown = new Vector2(x * size, y * size);
            areaRightTop = new Vector2((x + width) * size, (y + height) * size);
            visited = false;
            downExist = false;
            isClear = false;
            edgeRect = new List<Rect>();
            doorObjects = new List<GameObject>();
        }

        public void EdgeRect(Rect _rect)
        {
            if (!edgeRect.Contains(_rect) && _rect != this)
            {
                edgeRect.Add(_rect);
                _rect.edgeRect.Add(this);
            }
        }

        public void LoadMaskObject()
        {
            maskObject.transform.localPosition = new Vector3(midX * size, midY * size , 0);

            if (isRoom) // 방
            {
                if (downExist)
                {
                    maskObject.transform.localPosition = new Vector3(midX * size, midY * size - 0.5f, 0);
                    maskObject.transform.localScale = new Vector3(width * size * 2, height * size * 2 + 2);
                    areaLeftDown = new Vector2(areaLeftDown.x + 0.1875f, areaLeftDown.y);
                    areaRightTop = new Vector2(areaRightTop.x - 0.1875f, areaRightTop.y - 1);
                }
                else
                {
                    maskObject.transform.localScale = new Vector3(width * size * 2, height * size * 2);
                    areaLeftDown = new Vector2(areaLeftDown.x + 0.1875f, areaLeftDown.y + 1);
                    areaRightTop = new Vector2(areaRightTop.x - 0.1875f, areaRightTop.y - 1);
                }
            }
            else // 홀
            {
                if (width > height)
                {
                    maskObject.transform.localScale = new Vector3(width * size * 2, height * size * 2 + 4);
                    areaLeftDown = new Vector2(areaLeftDown.x + 0.1875f, areaLeftDown.y);
                    areaRightTop = new Vector2(areaRightTop.x - 0.1875f, areaRightTop.y);
                }
                else
                {
                    maskObject.transform.localScale = new Vector3(width * size * 2 + 1.35f, height * 2 * size);
                    areaLeftDown = new Vector2(areaLeftDown.x, areaLeftDown.y + 1);
                    areaRightTop = new Vector2(areaRightTop.x, areaRightTop.y - 1);
                }
            }
        }

        public bool IsContain(Vector2 _position)
        {
            if (_position.x > areaLeftDown.x && _position.x < areaRightTop.x &&
                _position.y > areaLeftDown.y && _position.y < areaRightTop.y)
                return true;

            return false;
        }

        public void Dispose()
        {
            System.GC.Collect();
        }
    }
}


