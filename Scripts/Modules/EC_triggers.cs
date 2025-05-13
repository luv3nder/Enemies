using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_triggers : MonoBehaviour
{
    public EnemyController EC;
    public EC_triggers_conditions ECTC;
    [Header("[x - reload, y/z - dist]")]
    public Vector3[] trigArray;
    public bool[] noSight;

    public bool groundedOnly = true, startReload;

    public float allReload = 1.5f;

    [Header("TEST")]
    public float[] trigTimers;
    public float allReloadTimer;

    void Start()
    {
        if (EC.isCopy)
        {
            this.enabled = false;
            return;
        }

        if (allReload == 0)
            allReload = 1;

        EC.trigId = -1;
        trigTimers = new float[trigArray.Length];
        allReloadTimer = -allReload / 2 + EC.DC.FF.TrueRandom(allReload);

        if (startReload)
            for (int i = 0; i < trigTimers.Length; i++)
                trigTimers[i] = trigArray[i].x;
    }
    void Update()
    { 
        if (EC.targetRb && EC.trigId == -1)
        {
            float difficultyMulti = 1.5f - EC.DC.ST.difficulty * 0.25f;

            if (allReloadTimer > allReload * difficultyMulti)
                Triggers();

            allReloadTimer += Time.deltaTime * EC.DC.ST.trigSpeed * (EC.DC.CR.CheckFrost(EC.PMS.accumOns) ? 0.4f : 1); // ST settings + frost
        }
    }
    void Triggers()
    {
        // timers
        if (EC.STATES.CheckFollow() || EC.STATES.CheckSight() || EC.PMS.isBoss || !groundedOnly)
            for (int i = 0; i < trigArray.Length; i++)
            {
                if (trigTimers[i] < trigArray[i].x)
                    trigTimers[i] += Time.deltaTime * EC.DC.ST.trigSpeed;
            }

        // apply
        if (EC.isGrounded || EC.PMS.isGhost || EC.PMS.isBoss || !groundedOnly)
        {
            for (int i = 0; i < trigArray.Length; i++)
            {
                if (trigTimers[i] >= trigArray[i].x)
                {
                    float dist = Vector2.Distance(EC.rb.position, EC.targetRb.position);
                    if (dist >= trigArray[i].y && dist <= trigArray[i].z // check distance
                        && CheckConditions(i) // check conditions
                        && ((EC.STATES.CheckSight() && noSight != null && noSight.Length > 0 && noSight[i]) || EC.STATES.FindSight()))  // check in sight / no sight / follows
                    {
                        EC.trigId = i;
                        trigTimers[i] = 0; // single reload
                        allReloadTimer = 0;
                        break;
                    }
                }
            }
        }
    }

    bool CheckConditions(int index)
    {
        if (ECTC)
            return ECTC.CheckTrigs(index);
        return true;
    }
}
