﻿using UnityEngine;


public enum ControlTypeAbnormalStatusType { FREEZE, STUN, CHARM, END }
public enum AttackTypeAbnormalStatusType { POISON, BURN, END }

/*
 * 상태이상 처리
 * 
 * 공격계 (독, 화상)
 *  - 상태이상 공격 텀 A초 고정(현재 A = 0.5)
 *  - 한 틱당 공격력 D, 총 N회 적용(N = 6 고정, 총 3초 걸리는 꼴)
 * 
 * 제어계 (스턴, 빙결)
 *  - n초 적용
 *  - 새로운 것이 들어왔을 때 기존 상태이상에 상관없이 초기화 되어 새로운 상태이상으로 적용 
 */

public struct AttackTypeAbnormalStatusConstants
{
    public float EFFECT_DURATION_MAX;       // 효과 총 유지 시간
    public float TIME_PER_APPLIED_UNIT;     // 적용 단위당 시간
    public int EFFECT_APPLIED_COUNT_MAX;  // 효과 적용 최대 횟수 = 효과 총 유지 시간 / 적용 단위당 시간 
    public float FIXED_DAMAGE_PER_UNIT;     // 단위 당 고정 데미지 = 초당 고정 데미지 * 적용 단위당 시간
    public float PERCENT_DAMAGE_PER_UNIT;   // 단위 당 (최대 체력의) 퍼센트 데미지 = 초당 퍼센트 데미지 * 적용 단위당 시간
    public AttackTypeAbnormalStatusConstants(float EFFECT_DURATION_MAX, float TIME_PER_APPLIED_UNIT, float fixedDamagePerSecond, float percentDamagePerSecond)
    {
        this.EFFECT_DURATION_MAX = EFFECT_DURATION_MAX;
        this.TIME_PER_APPLIED_UNIT = TIME_PER_APPLIED_UNIT;
        // 실수 / 실수 = x.9999 같이 부동소수점 계산시 값 잘 못 나와 int 변환 시 버림 처리 되는 경우 방지
        this.EFFECT_APPLIED_COUNT_MAX = Mathf.RoundToInt(EFFECT_DURATION_MAX / TIME_PER_APPLIED_UNIT);
        this.FIXED_DAMAGE_PER_UNIT = fixedDamagePerSecond * TIME_PER_APPLIED_UNIT;
        this.PERCENT_DAMAGE_PER_UNIT = percentDamagePerSecond * TIME_PER_APPLIED_UNIT;
    }
}

static class AbnormalStatusConstants
{
    public static readonly AttackTypeAbnormalStatusConstants ENEMY_TARGET_POISON_INFO = new AttackTypeAbnormalStatusConstants(3, 0.5f, 3.7f, 0.007f);
    public static readonly AttackTypeAbnormalStatusConstants ENEMY_TARGET_BURN_INFO = new AttackTypeAbnormalStatusConstants(3, 0.5f, 1.5f, 0.025f);
    public static readonly AttackTypeAbnormalStatusConstants PLAYER_TARGET_POISON_INFO = new AttackTypeAbnormalStatusConstants(2, 1f, 1f, 0);
    public static readonly AttackTypeAbnormalStatusConstants PLAYER_TARGET_BURN_INFO = new AttackTypeAbnormalStatusConstants(3, 1f, 1f, 0);
    public const float CHARM_MOVE_SPEED_RATE = 0.5f;
}
