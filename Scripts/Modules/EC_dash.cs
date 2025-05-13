using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_dash : MonoBehaviour
{
    dataController DC;
    [Header("[necessary]")]
    public EnemyController EC;
    public ParticleSystem prepareEffect;
    public int trigId;

    public Material customMat;
    public AudioClip prepareSound, dashSound;

    [Header("_______________________ CONTACT")]
    public float attMulti = 1;
    public float attKnockback;
    public Vector2Int attEffect;

    [Header("_______________________ ATTACK")]
    public float attSpeed;
    public float attTime;
    public float attDelay, exitDelay = 0.5f;
    public float accelerationPower;


    [Header("_______________________ OTHER")]
    public int lavaBreakDistance = 8;
    public bool acceleration, targetOnStart, stopsOnPrepare, animates, allDirections, stopOnExit, onlyOnGrounded;

    projectile_effect effect;

    int direction;
    float timer, dashTimer, exitTimer;
    bool isActive, isDashing;

    Vector2 targetPos;
    float targetAngle;

    void Start()
    {
        if (prepareEffect)
            prepareEffect.Stop();

        // try to get (old)
        if (EC == null)
            EC = GetComponent<EnemyController>();

        if (EC.isCopy)
            this.enabled = false;

        DC = GM.Inst.DC;
    }
    void Update()
    {
        ActiveSwitch();

        if (isActive)
        {
            if (!EC.STATES.CheckFlick() && !EC.STATES.CheckStun() && EC.targetRb)
            {
                if (!isDashing)
                    PreparePhase();
                else
                    ActivePhase();
            }
            else
                TurnOff(true);
        }
    }
    void ActiveSwitch()
    {
        if (trigId == EC.trigId && !isActive)
        {
            if (prepareSound)
                DC.PR.PlaySound(prepareSound, EC.rb.position);

            isActive = true;
            EC.CONTACT.RefreshColArray();
        }
    }

    // prepare
    void PreparePhase()
    {
        // prepare set
        if (timer == 0 && !(onlyOnGrounded && !EC.isGrounded))
            SetPrepare();

        timer += Time.deltaTime;

        // start attacking
        if (IsPrepared() && !isDashing)
            SetAttack();
    }
    void SetPrepare()
    {
        PrepareFx(true);

        // set target pos
        if (targetOnStart)
            SetAttackAngle();

        // set att time
        EC.STATES.stateTimers[1] = attTime + attDelay;

        // set stop time
        if (stopsOnPrepare)
            EC.STATES.stateTimers[3] = attTime + attDelay;
    }
    public void PrepareFx(bool isOG)
    {
        if (DC.isMultiplayer && isOG)
            DC.NMI.EnemyFxServerRpc(DC.SID(), EC.arrayId, new Vector3Int(0, trigId, 0));

        if (EC.HANDS != null)
        {
            EC.HANDS.spren.sprite = EC.HANDS.defaultSprite;
            EC.HANDS.SetDash(DC.FF.GetAngle(EC.rb.position, EC.targetRb.position), attDelay, 0);
        }

        // anim
        if (animates)
            EC.ECA.SetAnim(4, 0);

        // timer
        EC.STATES.SetPrepare(attDelay);

        if (prepareEffect)
            prepareEffect.Play();
    }

    // dash
    void ActivePhase()
    {
        if (EC.STATES.stateTimers[1] > 0)
        {
            if (customMat != null)
                EC.STATES.effectMat = customMat;

            if (!EC.isCopy)
            {
                Attack();
                Dash();
            }
        }

        // exit delay
        else if (exitTimer <= exitDelay)
        {
            exitTimer += Time.deltaTime;

            if (stopOnExit)
            {
                EC.STATES.stateTimers[3] = 1;
                EC.rb.linearVelocity = Vector2.zero;
            }
        }

        // exit
        else
            TurnOff(false);
    }
    void SetAttack()
    {
        // set target pos
        if (!targetOnStart)
            SetAttackAngle();

        if (NoLavaCheck())
        {
            AttackFx(true);
            isDashing = true;
            //EC.buffTimers[7] = 0.1f; // invis break
        }
        else
            TurnOff(true);
    }
    public void AttackFx(bool isOG)
    {
        if (DC.isMultiplayer && isOG)
            DC.NMI.EnemyFxServerRpc(DC.SID(), EC.arrayId, new Vector3Int(0, trigId, 1));

            if (dashSound)
            DC.PR.PlaySound(dashSound, EC.rb.position);

        if (animates)
            EC.ECA.SetAnim(2, 0);

        DC.CC().SpawnDashEffect(EC.rb, attTime, EC.transform, EC.spren);
    }
    void Dash()
    {
        dashTimer += Time.deltaTime;
        float accel = accelerationPower == 0 ? 1 : accelerationPower * dashTimer;

        if (allDirections)
            EC.rb.linearVelocity = new Vector2(Mathf.Sin(targetAngle) * attSpeed, Mathf.Cos(targetAngle) * attSpeed) * accel * (EC.CheckInWater() ? 0.7f : 1f);

        else
            EC.rb.linearVelocity = new Vector2(direction * attSpeed, 0) * accel * (EC.CheckInWater() ? 0.7f : 1f);


        // dir
        EC.direction = (int)Mathf.Sign(EC.rb.linearVelocity.x);
    }
    void Attack()
    {
        float effectMulti = DC.CR.CheckStrong(EC.PMS.buffTimers) ? 1.5f : 1;
        EC.CONTACT.SetContact(effectMulti * attMulti, attKnockback, attEffect, 0.2f);
    }

    // off
    void TurnOff(bool breaks)
    {
        if (breaks && EC.HANDS)
            EC.HANDS.HandsBreak();

        if (animates)
            EC.ECA.SetAnim(0, 0);

        isActive = false;
        isDashing = false;
        timer = 0;
        exitTimer = 0;
        EC.trigId = -1; // trigger turn off
        EC.STATES.stateTimers[1] = 0; // att turn off 
        EC.STATES.stateTimers[3] = 0; // stop turn off
        gameObject.layer = DC.PP.creaturesLayer;

        if (stopOnExit)
            EC.rb.linearVelocity = Vector2.zero;

        if (effect != null && effect.parentRb != null)
            effect.parentRb = null;

        if (prepareEffect)
            prepareEffect.Stop();
    }


    void SetAttackAngle()
    {
        targetPos = EC.targetRb.position;
        targetAngle = DC.FF.GetAngle(EC.rb.position, targetPos);
        direction = EC.rb.position.x < EC.targetRb.position.x ? 1 : -1;
        EC.direction = direction;
    }
    bool IsPrepared()
    {
        return EC.STATES.stateTimers[5] <= 0;
    }
    bool NoLavaCheck()
    {
        for (int i = 0; i < lavaBreakDistance; i++)
            if (DC.FF.CheckLava(EC.rb.position, i * EC.direction, false, false))
            {
                //DC.FF.ChatTest("lava x: " + i, 2);
                return false;
            }

        DC.FF.ChatEnemyData("no lava", 1, 1);
        return true;
    }
}

