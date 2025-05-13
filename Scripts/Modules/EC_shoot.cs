using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_shoot : MonoBehaviour
{
    dataController DC;

    public string name;
    public EnemyController EC;
    public ParticleSystem prepareEffect;

    public int trigId;

    public bool isOnImpact;
    bool impactCheck;

    [Header("[0 - strong, 1 - haste, 2 - arcane]")]
    public int scaleFrom;

    public bool spawnAtTip, stunBreak = true, flickBreak = true, stopsOnPrepare = true, death;
    public GameObject bullet;
    public AudioClip prepareSound, shootSound;
    public Vector2 volumePitch;

    [Header("[0 for automatic]")]
    [Range(0f, 3.14f)]
    public float exactAngle;
    public bool charAngle, targetOnStart = true;
    public float angleOffset;

    [Range(0f, 3f)]
    public float scale = 1;
    public float attDelay, attTime, minusOneReloadTime;
    public float yOffset;

    [Header("[x - number, y - reload time]")]
    public Vector2 repeat;

    bool isActive;
    int curRepeat;
    float attAngle;
    float timer;

    public Sprite handsSprite;
    void Start()
    {
        DC = GM.Inst.DC;

        if (prepareEffect)
            prepareEffect.Stop();

        if (EC.isCopy)
            this.enabled = false;

        curRepeat = (int) repeat.x;
    }
    void Update()
    {
        if (EC && EC.targetRb)
        {
            // breaker
            if (isActive && ((stunBreak && EC.STATES.CheckStun()) || (flickBreak && EC.STATES.CheckFlick())) && trigId != -1)
                TurnOff(true);

            // set on trigger
            TriggerCheck();

            // set on impact
            if (isOnImpact)
                ImpactCheck();

            // active
            if (isActive)
                PreparePhase();
        }
        else
            TurnOff(false);
    }

    void TriggerCheck()
    {
        if (trigId == EC.trigId && !isActive)
        {
            if (EC.ECA)
                EC.ECA.SetAnim(2, 0);

            isActive = true;
        }
    }
    void ImpactCheck()
    {
        if (EC.STATES.CheckHit() && !impactCheck)
        {
            isActive = true;
            impactCheck = true;
        }
        if (!EC.STATES.CheckHit() && impactCheck)
        {
            impactCheck = false;
        }
    }

    // prepare
    void PreparePhase()
    {
        // set dir
        if (attAngle != 0)
        {
            float dir = Mathf.Sign(EC.targetRb.position.x - EC.rb.position.x);
            bool isAutomatic = EC.PMS.isGhost || exactAngle == 0;
            EC.direction = isAutomatic ? (int)Mathf.Sign(attAngle) : (int)dir;
        }

        // prepare
        if (timer == 0)
            SetPrepare();

        timer += Time.deltaTime;

        // is prepared
        if (IsPrepared())
        {
            // hands shot
            if (EC.HANDS)
            {
                if (EC.HANDS.isDone)
                    Attack();
            }
            // normal shot
            else
                Attack();
        }
    }
    void SetPrepare()
    {
        PrepareFx(true);

        // angle
        if (targetOnStart)
        {
            attAngle = GetAngle();
            if (attAngle != 0)
            {
                // hands
                if (EC.HANDS != null)
                    EC.HANDS.SetDash(attAngle, attDelay, 0);
            }
            else
            {
                TurnOff(true);
                return;
            }
        }

        // sets
        if (stopsOnPrepare)
            EC.STATES.stateTimers[3] = attTime; // stop set

        EC.STATES.stateTimers[1] = attTime; // att set         
        EC.STATES.SetPrepare(attDelay);
    }
    public void PrepareFx(bool isOG)
    {
        if (EC.HANDS)
        {
            if (handsSprite != null)
                EC.HANDS.spren.sprite = handsSprite;
            else
                EC.HANDS.spren.sprite = EC.HANDS.defaultSprite;
        }

        if (prepareSound)
            EC.AUDIO.PlayPrepareSound();

        if (EC.ECA)
            EC.ECA.SetAnim(4, 0);

        if (prepareEffect)
            prepareEffect.Play();

        if (DC.isMultiplayer && isOG)
            DC.NMI.EnemyFxServerRpc(DC.SID(), EC.arrayId, new Vector3Int(1, trigId, 0));
    }

    // shoot
    void Attack()
    {
        AttackFx(true);

        Vector3 pos = GetShootPos();

        if (!targetOnStart)
        {
            attAngle = GetAngle();
            if (attAngle != 0)
                EC.direction = (int)Mathf.Sign(EC.targetRb.position.x - EC.rb.position.x);
        }

        // scale att
        float effectMulti = 1;
        switch (scaleFrom)
        {
            case 0:
                effectMulti = DC.CR.CheckStrong(EC.PMS.buffTimers) ? 1.5f : 1;
                break;
            case 1:
                effectMulti = DC.CR.CheckHaste(EC.PMS.buffTimers) ? 1.5f : 1;
                break;
            case 2:
                effectMulti = DC.CR.CheckMHealing(EC.PMS.buffTimers) ? 1.5f : 1;
                break;
        }

        float attDmg = EC.PMS.attDamage * effectMulti * scale;

        DC.PR.BulletSpawn(bullet, pos, attAngle, attDmg, 0, Vector2Int.zero, EC.PMS.behaviourId, EC.arrayId);

        if (death)
            EC.Death(false, false);

        RepeatAttack();
    }
    public void AttackFx(bool isOG)
    {
        if (prepareEffect)
            prepareEffect.Stop();

        if (EC.HANDS != null)
            EC.HANDS.isDone = false;

        if (shootSound)
            DC.PR.PlaySoundVeryExtended(shootSound, transform.position, volumePitch, EC.tilePos);

        if (DC.isMultiplayer && isOG)
            DC.NMI.EnemyFxServerRpc(DC.SID(), EC.arrayId, new Vector3Int(1, trigId, 1));
    }
    void RepeatAttack()
    {
        // end
        if (!EC.STATES.FindSight() || curRepeat <= 0)
        {
            TurnOff(false);
        }
        else
            curRepeat--;

        timer = 0;
    }

    // off
    void TurnOff(bool breaks)
    {
        if (EC.HANDS)
        {
            if (breaks)
                EC.HANDS.HandsBreak();
        }

        if (prepareEffect)
            prepareEffect.Stop();

        EC.trigId = -1;
        EC.STATES.SetPrepare(0);
        curRepeat = (int)repeat.x;
        isActive = false;
        timer = 0;
    }

    float GetAngle()
    {
        // pos, dir
        Vector2 pos = GetShootPos();
        float dir = Mathf.Sign(EC.targetRb.position.x - pos.x);

        // angle
        float attAngle = DC.FF.GetAngle(EC.rb.position, EC.targetRb.position);

        if (!charAngle)
        attAngle = exactAngle == 0 ? DC.CR.AttackGetAngle(bullet, pos, EC.targetRb.position, dir) : exactAngle * EC.direction;

        return attAngle + angleOffset * dir;
    }
    bool IsPrepared()
    {
        return EC.STATES.stateTimers[5] <= 0;
    }
    bool HandsCheck()
    {
        return spawnAtTip && EC.HANDS != null && EC.HANDS.tip != null;
    }
    Vector2 GetShootPos()
    {
        // pos
        Vector2 pos = transform.position + new Vector3(0, yOffset);
        if (HandsCheck())
            pos = EC.HANDS.tip.position;

        return pos;
    }
}
