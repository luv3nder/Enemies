using UnityEngine;

public class EC_states : MonoBehaviour
{
    dataController DC;
    public EnemyController EC;

    public float[] stateTimers;

    public float followSightRadius = 1.5f, sightTime = 10;
    public bool noSight, ignoresInvis, targetChar;

    public Material customMat, effectMat;

    void Awake()
    {
        DC = EC.DC;

        stateTimers = new float[12];

        customMat = customMat != null ? customMat : DC.PP.materialArray[0];
    }
    void Update()
    {
        if (EC.PMS.hitPoints > 0)
        {
            States();

            // target char
            if (targetChar)
                EC.targetRb = DC.ClosestCC(transform.position).rb;

            // on kill target
            if (!EC.targetRb && stateTimers[4] > 0)
            {
                stateTimers[4] = 0;
                FindSight();
            }
        }
    }


    public void States()
    {

        Material actionMat = null;
        Material staticMat = effectMat == null ? customMat : effectMat;

        bool invulnerable = false;
        // states
        for (int i = 0; i < stateTimers.Length; i++)
        {
            if (stateTimers[i] > 0)
            {
                switch (i)
                {
                    case 0: // jump reload
                        break;

                    case 1: // attack
                        actionMat = DC.PP.materialArray[4];
                        break;

                    case 2: // is hit
                        actionMat = DC.PP.materialArray[1];
                        invulnerable = true;
                        break;

                    case 3: // no move
                        break;

                    case 4: // in sight

                        if (!EC.PMS.isBoss)
                        {
                            // lost target
                            if (EC.targetRb == null)
                            {
                                stateTimers[i] = 0;
                                stateTimers[9] = 0;
                            }

                            // invis break
                            if (EC.targetRb == DC.CC().rb && DC.CC().CheckInvisible())
                            {
                                EC.targetRb = null;
                                stateTimers[4] = 0;
                                stateTimers[9] = 0;
                            }

                            // follow trail on loose sight if follows char
                            if (stateTimers[i] > 0)
                            {
                                // follow trail
                                if (stateTimers[i] - Time.deltaTime <= 0)
                                {
                                    stateTimers[9] = 30;
                                    stateTimers[i] = 0;
                                }
                                // in sight
                                else
                                {
                                    stateTimers[9] = 0;
                                    Vector2Int tpos = DC.TT.GetTilePos(EC.targetRb.position, false);
                                    EC.targetPos = tpos;
                                }
                            }
                        }


                        break;
                    case 5: // flashing (prepare)

                        bool flashing = (int)(Time.time * 10) % 2 == 0;
                        actionMat = flashing ? DC.PP.materialArray[4] : null; // [3] prep mat
                        break;
                    case 6: // isJumping
                        break;
                    case 7: // effect hit
                        actionMat = DC.PP.materialArray[1];
                        break;
                    case 8: // stun
                            //if (!CheckFlicked())
                        DC.CC().pms.stPoints = 100;
                        actionMat = DC.PP.materialArray[5];
                        break;
                    case 9: // follow

                        // invis break
                        if (EC.targetRb == DC.CC().rb && DC.CC().CheckInvisible())
                        {
                            EC.targetRb = null;
                            stateTimers[4] = 0;
                            stateTimers[9] = 0;
                        }
                        break;

                    case 10: // flick time
                        actionMat = DC.PP.materialArray[5];
                        break;
                    case 11: // ???
                        break;
                }
                stateTimers[i] -= Time.deltaTime;
            }
        }

        // invis
        float dist = Vector2.Distance(EC.rb.position, DC.CC().pos);
        float invisDist = 1.5f;
        float invisMulti = dist < invisDist ? 1 - (dist / invisDist) : 0;

        float invisAlpha = DC.CR.CheckInvis(EC.PMS.buffTimers) && !(stateTimers[2] > 0 || stateTimers[3] > 0) ? 0.01f : 1;
        invisAlpha = DC.CR.CheckInvis(EC.PMS.buffTimers) && DC.CC().CheckEmit() ? invisMulti * 0.3f : invisAlpha;

        Color invisColor = new Color(1, 1, 1, invisAlpha);

        // apply all
        if (!EC.PMS.isBoss)
            EC.spren.color = invisColor;

        EC.isInvulnerable = invulnerable;

        effectMat = null;
        Material effMaterial = DC.CR.GetEffectMaterial(EC.PMS.accumOns, EC.PMS.buffTimers);
        effMaterial = effMaterial == null ? customMat : effMaterial;
        EC.spren.material = actionMat == null ? effMaterial : actionMat;

        if (EC.sprenArray != null && EC.sprenArray.Length > 0 && !EC.PMS.isBoss)
        {
            for (int i = 0; i < EC.sprenArray.Length; i++)
            {
                EC.sprenArray[i].material = EC.spren.material;
                EC.sprenArray[i].color = invisColor;
            }
        }
    }
    public bool CheckJump()
    {
        return stateTimers[6] > 0;
    }
    public void SetJump(float value)
    {
        stateTimers[6] = value;
    }

    // gets and checks
    public bool CheckHit()
    {
        return stateTimers[2] > 0;
    }
    public void SetHit(float value)
    {
        stateTimers[2] = value;
    }

    public bool CheckPrepare()
    {
        return stateTimers[5] > 0;
    }
    public void SetPrepare(float value)
    {
        stateTimers[5] = value;
    }

    public bool CheckStun()
    {
        return stateTimers[8] > 0;
    }
    public void SetStun(float value)
    {
        stateTimers[8] = value;
    }

    public bool CheckNoMove()
    {
        return stateTimers[3] > 0 || stateTimers[8] > 0; // 3 - no move, 8 - stun
    }
    public void SetNoMove(float time, bool overrides)
    {
        if (overrides || stateTimers[3] <= time)
            stateTimers[3] = time; // no move
    }

    public bool CheckFollow()
    {
        return stateTimers[9] > 0;
    }

    // sight
    public bool FindSight()
    {
        bool isClear = false;

        // chars
        for (int i = 0; i < DC.clientsNum; i++)
        {
            CCs CC = DC.CCid(i);

            float distance = Vector2.Distance(transform.position, CC.pos);
            if (distance < followSightRadius)
            {
                Vector2Int[] tileLine = DC.TT.GetTileLine(EC.tilePos + Vector2Int.down, CC.tilePos);
                isClear = DC.TT.TileLineCheckClear(tileLine);

                bool invisSight = ignoresInvis ? true : !CC.CheckInvisible();

                if (invisSight)
                {
                    if (noSight)
                    {
                        SetSight(15f, CC.rb);
                        return true;
                    }
                    else if (isClear)
                    {
                        SetSight(15f, CC.rb);
                        return true;
                    }
                }
            }
        }



        // enemies
        if (!isClear)
        {
            EnemyController[] ECs = DC.PR.enemyArray;
            for (int i = 0; i < ECs.Length; i++)
                // behaviour check
                if (ECs[i] && BehaviourAggro(ECs[i].PMS.behaviourId) && !DC.CR.CheckInvis(ECs[i].PMS.buffTimers))
                {
                    float distance = Vector2.Distance(transform.position, ECs[i].rb.position);

                    if (distance < followSightRadius)
                    {
                        Vector2Int[] tileLine = DC.TT.GetTileLine(EC.tilePos, ECs[i].tilePos);
                        isClear = DC.TT.TileLineCheckClear(tileLine);

                        if (noSight || isClear)
                        {
                            SetSight(15f, ECs[i].rb);
                            return true;
                        }
                    }
                }
        }

        return isClear;
    }
    public void SetSight(float value, Rigidbody2D targetRb)
    {
        EC.targetRb = targetRb;

        // first notice
        if (value > 0 && stateTimers[4] <= 0)
        {
            EC.direction = DC.FF.GetDirection(EC.rb.position, targetRb.position);
            EC.AUDIO.PlayPrepareSound();
        }

        stateTimers[4] = value;
    }
    public bool CheckSight()
    {
        return stateTimers[4] > 0;
    }
    bool BehaviourAggro(int behId)
    {
        if ((behId == 1 && EC.PMS.behaviourId == 2) || (behId == 2 && EC.PMS.behaviourId == 1))
            return true;
        else
            return false;
    }


    public bool CheckGrounded() //0.2f recommended
    {
        return Physics2D.OverlapCircle(EC.groundedCenter.position, EC.groundedSize, DC.PP.groundLayerMask);
    }

    public bool CheckFlick()
    {
        return stateTimers[10] > 0;
    }
    public void SetFlick(float value)
    {
        stateTimers[10] = value;
    }

    private void OnDrawGizmosSelected()
    {
        if (!noSight)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, followSightRadius);
        }
    }
}
