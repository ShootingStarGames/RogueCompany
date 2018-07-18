﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    #region serializeFiled
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private float hp;
    [SerializeField]
    private float speed;
    [SerializeField]
    private RuntimeAnimatorController animatorController;
    [SerializeField]
    private List<WeaponInfo> weaponInfo;
    [SerializeField]
    private BT.Task task;
    #endregion
  
    #region property
    public Sprite Sprite
    {
        get
        {
            return sprite;
        }
    }
    public float HP
    {
        get
        {
            return hp;
        }
    }
    public float Speed
    {
        get
        {
            return speed;
        }
    }
    public RuntimeAnimatorController AnimatorController
    {
        get
        {
            return animatorController;
        }
    }
    public List<WeaponInfo> WeaponInfo
    {
        get
        {
            return weaponInfo;
        }
    }
    public BT.Task Task
    {
        get
        {
            return task;
        }
    }
    #endregion

}