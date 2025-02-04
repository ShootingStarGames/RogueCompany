﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

namespace AStar
{
    public class PathRequestManager : MonoBehaviour
    {

        Queue<PathResult> results = new Queue<PathResult>();

        static AStar.PathRequestManager instance;
        Pathfinder pathfinder;

        void Awake()
        {
            instance = this;
            pathfinder = GetComponent<Pathfinder>();
        }

        void Update()
        {
            if (results.Count > 0)
            {
                int itemsInQueue = results.Count;
                lock (results)
                {
                    for (int i = 0; i < itemsInQueue; i++)
                    {
                        PathResult result = results.Dequeue();
                        result.callback(result.path, result.success, result.doublingValue);
                    }
                }
            }
        }
        /// <summary>
        /// 추적 요청
        /// </summary>
        public static void RequestPath(PathRequest request,float doublingValue)
        {
            ThreadStart threadStart = delegate
            {
                instance.pathfinder.FindPath(request, instance.FinishedProcessingPath, doublingValue);
            };
            threadStart.Invoke();
        }
        /// <summary>
        /// 회전 추적 요청
        /// </summary>
        public static void RequestPath(PathRequest request, float doublingValue, float radius)
        {
            ThreadStart threadStart = delegate
            {
                instance.pathfinder.FindPath(request, instance.FinishedProcessingPath, radius);
            };
            threadStart.Invoke();
        }

        public void FinishedProcessingPath(PathResult result)
        {
            lock (results)
            {
                results.Enqueue(result);
            }
        }


    }

    public struct PathResult
    {
        public Vector2[] path;
        public bool success;
        public float doublingValue;
        public Action<Vector2[], bool, float> callback;

        public PathResult(Vector2[] path, bool success, Action<Vector2[], bool, float> callback, float doublingValue)
        {
            this.path = path;
            this.success = success;
            this.callback = callback;
            this.doublingValue = doublingValue;
        }

    }

    public struct PathRequest
    {
        public Vector2 pathStart;
        public Vector2 pathEnd;
        public Action<Vector2[], bool, float> callback;

        public PathRequest(Vector2 _start, Vector2 _end, Action<Vector2[], bool, float> _callback)
        {
            pathStart = _start;
            pathEnd = _end;
            callback = _callback;
        }

    }
}
