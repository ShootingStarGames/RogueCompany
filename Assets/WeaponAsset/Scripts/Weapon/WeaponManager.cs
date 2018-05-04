﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaponData;
using DelegateCollection;
using UnityEngine.UI;

/* onwer 하위 Object에 붙어서 무기 관리할 예정인 매니저 클래스
 * 
 * player는 최대 3개까지 무기 수용할 예정.
 */

public class WeaponManager : MonoBehaviour {

    #region variables

    [SerializeField]
    private List<Weapon> equipWeaponSlot;      // 무기 장착 슬룻 (최대 3개)
    [SerializeField]
    private int currentWeaponIndex;         // 현재 사용 무기 index
    private int weaponCount;               // 현재 장착된 무기 갯수 
    [SerializeField]
    private int weaponCountMax;            // 무기 장착 최대 갯수 
    private static WeaponManager instance = null;
    private Transform objTransform;
    private DelGetDirDegree ownerDirDegree;
    private DelGetPosition ownerDirVec;
    private DelGetPosition ownerPos;
    private BuffManager ownerBuff;

    // 디버그용 차징 ui
    public GameObject chargedGaugeUI;
    public Slider chargedGaugeSlider;

    #endregion
    #region getter
    public static WeaponManager Instance { get { return instance; } }
    public bool GetAttackAble()
    {
        if(equipWeaponSlot[currentWeaponIndex].GetWeaponState() == WeaponState.Idle)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public Vector3 GetPosition() { return objTransform.position; }
    #endregion
    #region setter
    #endregion
    #region UnityFunction
    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != null)
            Destroy(gameObject);
        objTransform = GetComponent<Transform>();
    }
    // Use this for initialization
    void Start()
    {
        //weaponCountMax = 5;  // 원래 3인데 테스트용으로 inspecter창에서 값 받음;
        weaponCount = weaponCountMax;
        Init();
        OnOffWeaponActive();
    }

    // 공격 테스트용
    bool isReleaseSpaceBar = false;

    // Update is called once per frame
    void Update()
    {
        //-------------------- 공격 테스트 용
        if(Input.GetKey(KeyCode.Space))
        {
            AttackButtonDown();
            isReleaseSpaceBar = true;
        }
        else if(isReleaseSpaceBar == true)
        {
            isReleaseSpaceBar = false;
            AttackButtonUP();
        }

        //---------------------------------

        // 바라보는 방향으로 무기 회전
        if (Player.Instance.GetRightDirection())
        {
            // 우측
            transform.rotation = Quaternion.Euler(0f, 0f, Player.Instance.GetDirDegree());
        }
        else
        {
            // 좌측
            transform.rotation = Quaternion.Euler(0f, 0f, Player.Instance.GetDirDegree() - 180f);
        }
    }
    #endregion

    #region Function
    public void Init()
    {
        // Onwer 정보 등록
        // 방향, Position 리턴 함수 등록,나중에 어디에(onwer 누구냐에 따라서 다름, player, enmey, object) 붙는지에 따라 초기화
        // 지금은 테스트용으로 Player꺼 등록
        ownerDirDegree = Player.Instance.GetDirDegree;
        ownerDirVec = Player.Instance.GetRecenteInputVector;
        ownerPos = GetPosition;
        ownerBuff = Player.Instance.GetBuffManager();

        for (int i = 0; i < weaponCountMax; i++)
        {
            equipWeaponSlot[i].SetownerDirDegree(ownerDirDegree);
            equipWeaponSlot[i].SetOwnerPos(ownerPos);
            equipWeaponSlot[i].SetOwnerDirVec(ownerDirVec);
            equipWeaponSlot[i].SetOwnerBuff(ownerBuff);
            equipWeaponSlot[i].Init(this);
        }
    }

    /// <summary>
    /// 차징 공격에 사용되는 차징 게이지 UI Update
    /// </summary>
    /// <param name="chargedVaule"></param>
    public void UpdateChargingUI(float chargedVaule)
    {
        if(chargedVaule == 0)
        {
            chargedGaugeUI.SetActive(false);
            return;
        }
        chargedGaugeUI.SetActive(true);
        // 나중에 변수 하나둬서 position y값 지정할 수 있게 해서 어떤 owner에 붙여도 ui 올바른 위치에 뜨게 할 예정
        chargedGaugeSlider.value = chargedVaule; 
    }

    // 공격 버튼 누를 때, 누르는 중일 때
    public void AttackButtonDown()
    {
        if (GetAttackAble())
        {
            equipWeaponSlot[currentWeaponIndex].StartAttack();
        }
    }

    // 공격 버튼 뗐을 때
    public void AttackButtonUP()
    {
        equipWeaponSlot[currentWeaponIndex].StopAttack();
    }
    
    // 전체 무기 Active off, 현재 착용 무기만 on
    public void OnOffWeaponActive()
    {
        for(int i = 0; i < weaponCountMax; i++)
        {
            equipWeaponSlot[i].gameObject.SetActive(false);
        }
        equipWeaponSlot[currentWeaponIndex].gameObject.SetActive(true);
    }

    // 무기 교체, changeNextWepaon 값 true : 다음 무기, false : 이전 무기
    public void ChangeWeapon(bool changeNextWeapon)
    {
        if(equipWeaponSlot[currentWeaponIndex].GetWeaponState() == WeaponState.Idle)
        {
            // 다음 무기로 교체
            if (changeNextWeapon)
            {
                equipWeaponSlot[currentWeaponIndex].gameObject.SetActive(false);
                currentWeaponIndex = (currentWeaponIndex + 1) % weaponCountMax;
                equipWeaponSlot[currentWeaponIndex].gameObject.SetActive(true);
            }
            // 이전 무기로 교체
            else
            {
                equipWeaponSlot[currentWeaponIndex].gameObject.SetActive(false);
                currentWeaponIndex = (currentWeaponIndex - 1 + weaponCountMax) % weaponCountMax;
                equipWeaponSlot[currentWeaponIndex].gameObject.SetActive(true);
            }
        }
    }


    // return 값으로 나온 버려진 무기를 item Class에 넘겨서 Player 바로 밑에 버려진 아이템 구현
    /// <param name="weapon">추가 </param>
    /// <summary>
    /// 무기 습득
    /// 슬룻 남을 때 : 무기 습득하고 습득한 무기 착용
    /// 슬룻 꽉찰 때 : 습득 무기 착용과 동시에 버려진 무기 return
    /// </summary>
    /// <param name="pickedWeapon">습득한 무기</param>
    /// <returns>버려질 무기</returns>
    public Weapon PickAndDropWeapon(Weapon pickedWeapon)
    {
        // 무기 습득하고 습득한 무기 착용
        if (weaponCount < weaponCountMax)
        {
            equipWeaponSlot.Add(pickedWeapon);
            currentWeaponIndex = weaponCount++;
            OnOffWeaponActive();
            return null;            
        }
        // 현재 착용중인 무기 버리고 습득 무기로 바꾸고 장착
        else
        {
            Weapon dropedWeapon = equipWeaponSlot[currentWeaponIndex];
            equipWeaponSlot[currentWeaponIndex] = pickedWeapon;
            OnOffWeaponActive();
            return dropedWeapon;
        }
    }
    #endregion
}