using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_buff : MonoBehaviour
{
    public dataController DC;
    public EnemyController EC;
    public int trigId;

    public ParticleSystem prepareEffect;
    public AudioClip buffSound;
    public GameObject buffPrefab;
    public float prepareTime = 0.5f, delayTime = 0.2f;

    [Header("[0 - buff, 1 - heal]")]
    public int type;
    [Header("[0 - allies, 1 - allies + self, 2 - character]")]
    public int applyType;
    public int animId = 2;
    public bool self, isBreakable = true;

    public Vector3Int buffValue;
    public bool withText = true;
    public float buffRadius;
    bool isActive;
    float timer;

    bool TriggerCheck()
    {
        return trigId == EC.trigId;
    }

    void Update()
    {
        // breaker
        if ((EC.STATES.CheckStun() || (isBreakable && EC.STATES.CheckFlick())) && isActive)
            TurnOff();

        // trig
        if (TriggerCheck() && !isActive)
        {
            isActive = true;

            EC.ECA.SetAnim(animId, 0);

            if (prepareEffect)
                prepareEffect.Play();
        }

        // on
        if (isActive)
        {
            timer += Time.deltaTime;

            if (timer > prepareTime)
                Action();
            else
                EC.STATES.stateTimers[3] = delayTime; // no move
        }
    }

    void Action()
    {
        if (self)
            EC.PMS.SetBuff(buffValue.x, buffValue.y, buffValue.z, withText);

        if (buffPrefab)
            Instantiate(buffPrefab, EC.rb.transform.position, Quaternion.identity);

        if (buffSound)
            DC.PR.PlaySound(buffSound, EC.rb.position);

        if (buffRadius != 0)
            DC.FF.BuffAll(EC.rb.position, Vector2.one * buffRadius, buffValue, EC.PMS.behaviourId, withText);

        TurnOff();
    }

    void TurnOff()
    {
        EC.trigId = -1;
        isActive = false;
        timer = 0;
    }
}
