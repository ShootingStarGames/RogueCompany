﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "CRangeAttack", menuName = "SkillData/CRangeAttack")]
public class CRangeAttack : SkillData
{
    [SerializeField]
    float radius;

    public override BT.State Run(Character character, object temporary)
    {
        return ActiveSkillManager.Instance.RangeAttack(character, radius, delay, amount);
    }
}