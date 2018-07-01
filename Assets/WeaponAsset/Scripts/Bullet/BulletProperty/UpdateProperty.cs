﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaponAsset;

/* UpdateProperty class
 * 총알 Update에 관련된 클래스
 * 
 * 원래는 bullet class에서 코루틴으로 돌아서 60fps 정도로 실행하게 하려 했으나 현재 일단은 fixedUpdate에서 updateProperty를 실행하게 함.
 * 
 * [현재]
 * 1. StraightMoveProperty
 *  - 총알 정해진 방향으로 일정한 속력으로 직선 운동
 *  - 사거리 개념(range)이 있어서 총알 사정거리 넘어서면 delegate로 받아온 총알 삭제 함수 실행.
 *  
 * 2. LaserUpdateProperty
 *  - 레이저 전용 update 속성으로 현재는 raycast 밖에 안함.
 *  
 * 3. SummonProperty
 *  - 일정 주기로(현재는 시간 단위이고 거리 단위는 고려) 총알을 따로 더 생성함
 *  - bulletPattern 포함
 * -------------------
 * [예정]
 * 1. LaserUpdateProperty 에서 레이저 sprite, material, color 이런 외형적인거나 레이저 폭 등등 추가 할게 많이 남아있음.
 * 
 * [미정]
 * 1.
 */


public abstract class UpdateProperty : BulletProperty
{

    public abstract UpdateProperty Clone();
    public abstract void Update();
}

/// <summary>
/// 등속 직선 운동 속성
/// </summary>
public class StraightMoveProperty : UpdateProperty
{
    private float moveSpeed;    // 속력
    private float range;

    private float lifeTime;  // bullet 생존 시간 * (1.0f / Time.fixedDeltaTime)
    private float timeCount; // 지나간 시간 카운트

    public override UpdateProperty Clone()
    {
        return new StraightMoveProperty();
    }

    public override void Init(Bullet bullet)
    {
        base.Init(bullet);
        timeCount = 0;
        if (bullet.info.speed != 0)
        {
            moveSpeed = bullet.info.speed;
        }
        if (bullet.info.range != 0)
        {
            range = bullet.info.range;
        }

        lifeTime = (range / moveSpeed);
    }

    // range / moveSpeed 시간 지나면 삭제
    public override void Update()
    {
        if (timeCount >= lifeTime)
        {
            delDestroyBullet();
        }
        timeCount += Time.fixedDeltaTime;
    }
}

/// <summary> 등가속도 직선? 운동 </summary>
public class AccelerationMotionProperty : UpdateProperty
{
    private float distance;     // 이동한 거리, range 체크용

    private float moveSpeed;    // 속력
    private float acceleration; // 가속도 (방향 일정)
    private float range;        // 사정거리
    // 속력이 변화하는 총 값 제한, ex) a = -1, limit = 10, 속력 v = 3-> -7까지만 영향받음. a = +2 limit 8, v = -2 => +6까지만
    private float deltaSpeedTotal;
    private float deltaSpeedTotalLimit;

    private bool acceleratesBullet;     // 가속도를 적용 할 것인가 말 것인가.

    private float deltaSpeed;

    public override UpdateProperty Clone()
    {
        return new AccelerationMotionProperty();
    }

    public override void Init(Bullet bullet)
    {
        base.Init(bullet);
        distance = 0;
        deltaSpeedTotal = 0;
        deltaSpeedTotalLimit = bullet.info.deltaSpeedTotalLimit;

        if (bullet.info.speed != 0)
        {
            moveSpeed = bullet.info.speed;
        }
        if (bullet.info.acceleration != 0)
        {
            acceleration = bullet.info.acceleration;
        }
        if (bullet.info.range != 0)
        {
            range = bullet.info.range;
        }
        acceleratesBullet = true;
    }

    public override void Update()
    {
        // 이동
        bullet.SetVelocity(moveSpeed);
        distance += moveSpeed * Time.fixedDeltaTime;

        // 사정거리 넘어가면 delete 속성 실행
        if (distance >= range)
        {
            delDestroyBullet();
        }

        // 가속화
        if (acceleratesBullet)
        {
            deltaSpeed = acceleration * Time.fixedDeltaTime;
            moveSpeed += deltaSpeed;
            deltaSpeedTotal += Mathf.Abs(deltaSpeed);

            // 속력 변한 총량이 limit 보다 커지면 가속도 적용 멈춤.
            if (deltaSpeedTotal >= deltaSpeedTotalLimit)
            {
                acceleratesBullet = false;
                if (acceleration > 0)
                {
                    moveSpeed -= deltaSpeedTotal - deltaSpeedTotalLimit;
                }
                else if (acceleration < 0)
                {
                    moveSpeed += deltaSpeedTotal - deltaSpeedTotalLimit;
                }

                if (Mathf.Abs(moveSpeed) < 0.1f)
                {
                    moveSpeed = 0f;
                }
            }

            // 속력이 음수가 되며 방향 자체가 바뀔 때
            if (moveSpeed < 0)
            {
                moveSpeed = -moveSpeed;
                acceleration = -acceleration;
                bullet.RotateDirection(180);
            }
        }
    }
}


/// <summary> laser bullet Update </summary>
public class LaserUpdateProperty : UpdateProperty
{
    private DelGetPosition ownerDirVec;
    private DelGetPosition ownerPos;
    private float addDirVecMagnitude;

    private LineRenderer lineRenderer;
    private RaycastHit2D hit;
    private int layerMask;
    private Vector3 pos;

    private bool AttackAble;

    public override void Init(Bullet bullet)
    {
        base.Init(bullet);

        delCollisionBullet = bullet.CollisionBullet;

        ownerDirVec = bullet.GetOwnerDirVec();
        ownerPos = bullet.GetOwnerPos();
        addDirVecMagnitude = bullet.GetAddDirVecMagnitude();
        lineRenderer = bullet.GetLineRenderer();
        pos = new Vector3();
        // 일단 Player 레이저가 Enemy에게 적용 하는 것만
        layerMask = (1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Enemy"));
    }

    public override UpdateProperty Clone()
    {
        return new LaserUpdateProperty();
    }

    public override void Update()
    {
        bulletTransform.position = ownerPos();
        pos = ownerPos() + (ownerDirVec() * addDirVecMagnitude);
        pos.z = 0;
        bullet.LaserStartPoint.position = pos;
        // 100f => 레이저에도 사정거리 개념을 넣게 된다면 이 부분 값을 변수로 처리할 예정이고 현재는 일단 raycast 체크 범위를 100f까지 함
        hit = Physics2D.Raycast(pos, ownerDirVec(), 100f, layerMask);
        // && (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Enemy")
        if (hit.collider != null)
        {
            lineRenderer.SetPosition(0, pos);
            lineRenderer.SetPosition(1, hit.point);
            bullet.LaserEndPoint.position = hit.point;
            delCollisionBullet(hit.collider);
        }
    }
}

/// <summary> 일정 텀(creationCycle)을 가지고 bulletPattern대로 총알을 소환하는 속성 </summary>
public class SummonProperty : UpdateProperty
{
    private BulletPattern bulletPattern; // 생성할 총알 패턴
    private float creationCycle; // 생성 주기
    private float timeCount; // time count

    private DelGetDirDegree bulletDirDegree;
    private DelGetPosition bulletDirVec;
    private DelGetPosition bulletPos;

    public SummonProperty(BulletPattern bulletPattern, float creationCycle)
    {
        this.bulletPattern = bulletPattern;
        this.creationCycle = creationCycle;
    }
    public override void Init(Bullet bullet)
    {
        base.Init(bullet);

        bulletDirDegree = bullet.GetDirDegree;
        bulletDirVec = () => { return Vector3.zero; };
        bulletPos = bullet.GetPosition;
        bulletPattern.Init(ownerBuff, transferBulletInfo, bulletDirDegree, bulletDirVec, bulletPos);
    }
    public override UpdateProperty Clone()
    {
        return new SummonProperty(bulletPattern.Clone(), creationCycle);
    }

    // 생성 주기마다 bulletPattern 실행
    public override void Update()
    {
        if (timeCount >= creationCycle)
        {
            timeCount -= creationCycle;
            bulletPattern.CreateBullet(1.0f);
        }
        timeCount += Time.fixedDeltaTime;
    }
}

/// <summary> 유도 총알 </summary>
public class HomingProperty : UpdateProperty
{
    private float lifeTime;
    private float timeCount;
    private float deltaAngle;
    private int enemyTotal;
    private float targettingCycle;

    private float startDelay;

    private RaycastHit2D hit;
    private List<RaycasthitEnemy> raycastHitEnemies;
    private RaycasthitEnemy raycasthitEnemyInfo;
    private int layerMask;
    private Transform enemyTransform;

    private Vector3 directionVector;
    private float directionDegree;
    private Vector3 differenceVector;
    private float differenceDegree;

    int raycasthitEnemyNum = 0;
    float minDistance = 1000f;
    int proximateEnemyIndex = -1;

    public override void Init(Bullet bullet)
    {
        base.Init(bullet);

        targettingCycle = 0;
        deltaAngle = 4f;

        directionVector = new Vector3(0f, 0f, 0f);
        differenceVector = new Vector3(0f, 0f, 0f);

        raycastHitEnemies = new List<RaycasthitEnemy>();
        raycasthitEnemyInfo = new RaycasthitEnemy();
        // 막히는 장애물 리스트 더 추가시 Wall 말고 더 넣어야됨.
        layerMask = 1 << LayerMask.NameToLayer("Wall");

        timeCount = 0;
        startDelay = bullet.info.startDelay;
        if (bullet.info.speed != 0)
        {
            lifeTime = bullet.info.range / bullet.info.speed;
        }
        else
            lifeTime = 20;

    }

    public override UpdateProperty Clone()
    {
        return new HomingProperty();
    }

    public override void Update()
    {
        if (timeCount < startDelay)
        {
            timeCount += Time.fixedDeltaTime;
            return;
        }
        if (timeCount >= lifeTime)
        {
            delDestroyBullet();
        }
        timeCount += Time.fixedDeltaTime;
        //targettingCycle -= Time.fixedDeltaTime;
        // 유도 대상 선정

        enemyTotal = EnemyManager.Instance.GetAliveEnemyTotal();

        List<Enemy> enemyList = EnemyManager.Instance.GetEnemyList;

        raycastHitEnemies.Clear();
        raycasthitEnemyNum = 0;
        minDistance = 1000f;
        proximateEnemyIndex = -1;

        // raycast로 bullet과 enemy 사이에 장애물이 없는 enmey 방향만 찾아낸다.
        for (int i = 0; i < enemyTotal; i++)
        {
            raycasthitEnemyInfo.index = i;
            raycasthitEnemyInfo.distance = Vector2.Distance(enemyList[i].transform.position, bulletTransform.position);
            hit = Physics2D.Raycast(bulletTransform.position, enemyList[i].transform.position - bulletTransform.position, raycasthitEnemyInfo.distance, layerMask);
            if (hit.collider == null)
            {
                raycastHitEnemies.Add(raycasthitEnemyInfo);
                raycasthitEnemyNum += 1;
            }
        }

        // 위에서 찾은 enmey들 중 distance가 가장 작은, 가장 가까운 enemy를 찾는다.
        for (int j = 0; j < raycasthitEnemyNum; j++)
        {
            if (raycastHitEnemies[j].distance <= minDistance)
            {
                minDistance = raycastHitEnemies[j].distance;
                proximateEnemyIndex = j;
            }
        }

        // 선정된 대상에게 유도 될 수 있도록 회전
        if (proximateEnemyIndex != -1)
        {
            enemyTransform = enemyList[raycastHitEnemies[proximateEnemyIndex].index].transform;

            differenceVector = enemyTransform.position - bulletTransform.position;
            // (Bx-Ax)*(Py-Ay) - (By-Ay)*(Px-Ax)
            if (((bullet.GetDirVector().x) * (differenceVector.y) - (bullet.GetDirVector().y) * (differenceVector.x)) >= 0)
            {
                bullet.RotateDirection(deltaAngle);
            }
            else if ((bullet.GetDirVector().x) * (differenceVector.y) - (bullet.GetDirVector().y) * (differenceVector.x) < 0)
            {
                bullet.RotateDirection(-deltaAngle);
            }
        }
    }
}
