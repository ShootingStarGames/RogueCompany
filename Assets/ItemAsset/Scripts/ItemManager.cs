﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviourSingleton<ItemManager> {
    public GameObject customObject;
    public Sprite sprite;
    Queue<GameObject> objs;

    public void CallItemBox(Vector3 _position,Item _item)
    {
        if (objs == null)
            objs = new Queue<GameObject>();
        GameObject obj = Instantiate(customObject, _position, Quaternion.identity, this.transform);
        objs.Enqueue(obj);
        obj.AddComponent<ItemBox>();
        obj.GetComponent<ItemBox>().sprite = sprite;
        obj.GetComponent<ItemBox>().Init(_item);
    }

    public void DeleteObjs()
    {
        while(objs.Count>0)
        {
            Destroy(objs.Dequeue());
        }
    }

    public void DropItem(Item _item,Vector3 _position)
    {
        GameObject obj = Instantiate(customObject, _position, Quaternion.identity, this.transform);
        obj.AddComponent<ItemContainer>().Init(_item);

        StartCoroutine(CoroutineDropping(obj, new Vector2(Random.Range(-1, 2), 5)));
    }

    IEnumerator CoroutineDropping(GameObject _object, Vector2 _vector)
    {
        int g = 20;
        float lowerLimit = _object.transform.position.y;
        float elapsed_time = 0;
        float sX = _object.transform.position.x;
        float sY = _object.transform.position.y;
        float sZ = _object.transform.position.z;
        float vX = _vector.x;
        float vY = _vector.y;
        while (true)
        {
            elapsed_time += Time.deltaTime;
            float x = sX + vX * elapsed_time;
            float y = sY + vY * elapsed_time - (0.5f * g * elapsed_time * elapsed_time);
            _object.transform.position = new Vector3(x,y,sZ);
            if (_object.transform.position.y <= lowerLimit)
                break;
            yield return YieldInstructionCache.WaitForEndOfFrame;
        }
        _object.GetComponent<PolygonCollider2D>().enabled = true;
    }
}
