using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public dataController DC;

    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public SpriteRenderer spren;
    [HideInInspector] public EC_animation ECA;

    public pos_animation PA;
    public static_animation SA;

    public EC_drop DROP;
    public EC_spawn SPAWN;
    public EC_contact CONTACT;
    public EC_audio AUDIO;
    public EC_pms PMS;
    public EC_states STATES;
    public EnemyNetwork NW;
    public Collider2D COL;

    public int enemyId, arrayId;
    public bool isCopy;
    public Vector2 netPos;
    public Vector2Int spawnPos;

    public EnemyHands HANDS;
    public EnemyController[] parts;

    [Header("______________________________FX")]
    public SpriteRenderer[] sprenArray;
    public GameObject[] pfxArray;

    [Header("______________________________OTHER")]
    public float hpBarYOffset;
    public int colRadius;
    public Transform groundedCenter;
    public float groundedSize = 0.5f;

    [Header("______________________________DEATH")]
    public GameObject customDeath;
    public bool deathTrigger;
    public int offscreenTime = 5;

    [Header("(range, time)")]
    public Vector2 deathShake;
    public GameObject deathExplotion;
    
    [Header("______________________________TEST")]
    public int trigId;
    public int direction;

    public Rigidbody2D targetRb;
    public Vector2Int tilePos;
    public Vector2Int targetPos;
    public int biomeId;

    public float lifeTimer;
    public float deathTimer;

    public Vector2 factSpeed;
    Vector2 prevFactPos;

    [HideInInspector] public Vector2Int prevPos, prevCharPos;
    [HideInInspector] public bool nightSpawned, manualSpawned;

    [HideInInspector] public bool isInvulnerable, isGrounded;
    [HideInInspector] public int colIndex, liqIndex;

    EnemyHpBar hpBar;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spren = GetComponent<SpriteRenderer>();
        ECA = GetComponent<EC_animation>();

        AUDIO.EC = this;
        PMS.EC = this;
        STATES.EC = this;

        trigId = -1;

        if (groundedCenter == null)
            groundedCenter = transform;
    }
    void Start()
    {
        // effects
        for (int i = 0; i < pfxArray.Length; i++)
        {
            projectile_effect tempoFx = Instantiate(pfxArray[i], transform.position, Quaternion.identity).GetComponent<projectile_effect>();
            tempoFx.parentRb = rb;
            tempoFx.transform.parent = DC.PR.effectsTransform;
        }

        // hp bar
        if (!PMS.noHpBar && !PMS.isBoss && hpBar == null)
            SpawnHpBar();

        // network copy
        if (isCopy)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!PMS.isGhost)
            DC.CC().cameraScript.AddOcclusionRb(rb, colRadius);
    }
    void Update()
    {
        if (DC.isMultiplayer)
            Network();

        if (!DC.IsPaused() && !isCopy)
        {
            lifeTimer += Time.deltaTime;

            // active
            OnTilePosChange();
            OutOfScreen();

            if (!PMS.noFlip)
                Flip();

            if (COL)
            {
                if (isGrounded)
                    COL.enabled = true;
                else
                    COL.enabled = false;
            }

            // passive
            if (!PMS.isGhost)
            {
                GetWater();
                GetLava();

                // drags
                if (!PMS.noLiquidDrag && !PMS.isBoss && !PMS.isGhost)
                {
                    if (liqIndex == 1)
                        rb.linearDamping = 2;
                    else if (liqIndex > 1)
                        rb.linearDamping = 12;
                    else
                        rb.linearDamping = 0;
                }
            }

            // death
            if (PMS.hitPoints <= 0)
            {
                if (!deathTrigger)
                    Death(true, true);
                else
                    deathTrigger = false;
            }
        }
        else
            rb.linearVelocity = Vector2.zero;
    }
    private void FixedUpdate()
    {
        FactSpeed();
    }

    // main
    public void SpawnHpBar()
    {
        if (DC.HC != null)
        {
            hpBar = Instantiate(DC.PP.hpBarPrefab, rb.position, Quaternion.identity).GetComponent<EnemyHpBar>();
            hpBar.EC = this;
            hpBar.transform.parent = DC.HC.transform;
            hpBar.parentRb = rb;
            hpBar.yOffset = hpBarYOffset;
        }
    }
    void OnTilePosChange()
    {
        tilePos = DC.TT.GetTilePos(rb.position, false);

        // on self move
        if (prevPos != tilePos)
        {
            prevPos = tilePos;

            // biome id
            if (DC.TT.QIMR(prevPos))
                biomeId = DC.TT.biomemap[prevPos.x, prevPos.y];

            STATES.FindSight();
        }

        // on player move
        Vector2Int charPos = DC.ClosestCC(transform.position).tilePos;
        if (prevCharPos != charPos)
        {
            prevCharPos = charPos;
            STATES.FindSight();
        }
    }

    public float GetAngle(Vector2 targetPos)
    {
        return Mathf.Atan2(targetPos.x - transform.position.x, targetPos.y - transform.position.y);
    }
    public float GetDistance(Vector2 targetPos)
    {
        return Vector2.Distance(rb.position, targetPos);
    }

    public bool CheckHasTarget()
    {
        return targetRb != null;
    }
    public bool CheckFrighten()
    {
        if (PMS.noFrighten)
            return false;

        return STATES.stateTimers[11] > 0 || (DC.ST.doFrighten && DC.NN.isFrighten > 0);
    }

    // liquids
    void GetWater()
    {
        if (Physics2D.OverlapCircle(groundedCenter.transform.position, groundedSize, DC.PP.waterLayerMask))
        {
            if (!CheckInWater())
            {
                if (!PMS.noSplash)
                    DC.FF.WaterSplash(rb.position, rb.linearVelocity.y, 1);

                // fire out
                if (PMS.accumOns[1])
                {
                    DC.PR.PlaySound(DC.PP.burnSound, rb.position);
                    PMS.SetElement(1, false);
                }

                liqIndex = 1;
            }
        }
        else if (CheckInWater())
        {
            if (!PMS.noSplash)
                DC.FF.WaterSplash(rb.position, rb.linearVelocity.y, 1);
            liqIndex = 0;
        }
    }
    void GetLava()
    {
        Vector2 checkPos = groundedCenter ? groundedCenter.position + Vector3.up * 0.05f : transform.position;
        Collider2D inLavaHit = Physics2D.OverlapCircle(checkPos, 0.005f, DC.PP.lavaLayerMask);

        if (!DC.TT.QIMR(tilePos))
            return;

        Debug.Log("TILEPOS: " + tilePos);
        int lqIndex = DC.TT.watermap[tilePos.x, tilePos.y + 1];

        // in lava
        if (inLavaHit)
        {
            if (!CheckInLava() && lqIndex > 1)
            {
                if (lqIndex == 2)
                    PMS.accumTimers[1] = 100;
                if (lqIndex == 3)
                    PMS.accumTimers[0] = 100;
                if (lqIndex == 4)
                    PMS.accumTimers[2] = 100;

                if (!PMS.isGhost)
                    DC.FF.WaterSplash(rb.position, rb.linearVelocity.y, lqIndex);

                liqIndex = 2;
            }
        }
        else if (CheckInLava())
        {
            liqIndex = 0;
        }

        // damage
        if (CheckInLava() && PMS.afraidOfLava && !PMS.isBoss && !PMS.isGhost)
        {
            float val = 200 + PMS.maxHp / 3;
            PMS.hitPoints -= val * Time.deltaTime;

            // cores drop
            if (PMS.hitPoints <= 0 && DC.FF.TrueRandom(100) < 50)
                LavaCoreDrop();
        }
    }
    void LavaCoreDrop()
    {
        int lavaId = DC.TT.watermap[tilePos.x, tilePos.y + 1];
        int dropItemId = 0;

        switch (lavaId)
        {
            case 2:
                dropItemId = DC.IF.core_red;
                break;
            case 3:
                dropItemId = DC.IF.core_green;
                break;
            case 4:
                dropItemId = DC.IF.core_blue;
                break;
            case 5:
                dropItemId = DC.IF.core_pink;
                break;
        }

        if (dropItemId != 0)
        {
            DC.II.Drop(new Vector3Int(dropItemId, 1, 0), rb.position, 0.5f, 0, 1, false);
        }
    }
    public bool CheckInWater()
    {
        return liqIndex == 1;
    }
    public bool CheckInLava()
    {
        return liqIndex >= 2;
    }

    // death
    public void Death(bool drop, bool map)
    {
        if (drop)
        {
            if (!CheckInLava())
            {
                // gold
                int rndGold = (int)DC.FF.TrueRandom(PMS.coinsDrop / (DC.CR.CheckLuck(DC.CC().buffTimers) ? 1 : 2));
                int resultGold = (int)((PMS.coinsDrop / 2 + rndGold) * DC.ST.eGoldMulti);
                resultGold /= manualSpawned ? 2 : 1;
                DC.II.MoneyExpDrop(resultGold, rb.position, false);

                // exp
                if (PMS.expDrop != 0)
                    DC.II.MoneyExpDrop(PMS.expDrop / (manualSpawned ? 2 : 1), rb.position, true);

                // drops
                if (DROP)
                    DROP.Drop(this);
            }

            if (SPAWN)
                SPAWN.SpawnToArray(DC);

            // leech points
            if (DC.FF.TrueRandom(100) < DC.CC().Skill_I2_leech() * 5)
                DC.FF.SpawnPoints(transform.position);

            // death shake
            if (deathShake != Vector2.zero)
                DC.PR.SetShake(transform.position, deathShake);

            // death effect
            GameObject deathObj = DC.CR.deathPrefab;

            if (customDeath != null)
                deathObj = customDeath;

            GameObject curDeath = Instantiate(deathObj, transform.position, Quaternion.identity);
            curDeath.transform.parent = DC.PR.effectsTransform;

            AUDIO.PlayDeathSound();

            // for days respawn
            if (!PMS.isBoss && enemyId > 0)
            {
                if (DC.TT.mapCellPms[spawnPos.x, spawnPos.y] == null)
                    DC.TT.mapCellPms[spawnPos.x, spawnPos.y] = new int[] { 10, nightSpawned ? 1 : 0 };

                // maxbiome +
                int biome = DC.TT.biomemap[spawnPos.x, spawnPos.y];
                if (biome != 0 && DC.OBJ.maxBiomeEnemies[biome - 1][enemyId - 1] != -1)
                    DC.OBJ.maxBiomeEnemies[biome - 1][enemyId - 1]++;
            }

            if (deathExplotion != null)
                DeathExplotion();
        }
        else if (map)
        {
            // set map back
            if (nightSpawned)
                DC.TT.nightmap[spawnPos.x, spawnPos.y] = enemyId;
            else
                DC.TT.enemymap[spawnPos.x, spawnPos.y] = enemyId;

            if (DC.isMultiplayer)
                DC.NMI.EnemymapChangedServerRpc(DC.SID(), spawnPos, enemyId, nightSpawned);

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i])
                    parts[i].Death(false, false);
            }
        }

        if (PMS.isBoss)
        {
            DC.HC.bossBar.HideBar();
        }

        if (gameObject)
            Destroy(gameObject);
    }
    void DeathExplotion()
    {
        Vector2 spawnPos = rb.position;
        Instantiate(deathExplotion, spawnPos, Quaternion.identity);
    }
    void OutOfScreen()
    {
        if (enemyId != 0 && GetDistance(DC.ClosestCC(transform.position).pos) > 6)
        {
            deathTimer += Time.deltaTime;

            if (deathTimer > offscreenTime)
            {
                Death(false, true);
            }
        }
        else
        {
            deathTimer = 0;
        }
    }

    // other
    void FactSpeed()
    {
        if (rb.position != prevFactPos)
        {
            Vector2 diff = rb.position - prevFactPos;
            factSpeed = diff / Time.deltaTime;
            prevFactPos = rb.position;
        }
        else
            factSpeed = Vector2.zero;
    }
    public float GetFactSpeed()
    {
        return Mathf.Abs(Vector2.Distance(Vector2.zero, factSpeed));
    }
    public void Flip()
    {
        if (direction < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }

    // network
    void Network()
    {
        if (enemyId != -1)
            return;

        if (!isCopy)
            DC.NMI.SyncEnemyStatsServerRpc(
                DC.SID(),
                arrayId,
                transform.position,
                transform.eulerAngles,
                PMS.hitPoints,
                trigId,
                direction,
                ECA ? ECA.animationId : 0,
                isGrounded,
                HANDS ? HANDS.transform.eulerAngles : Vector3.zero,
                HANDS ? HANDS.transform.eulerAngles : Vector3.zero);
        else
        {
            STATES.States();
            Flip();

            float netDistance = Vector2.Distance(transform.position, netPos);
            transform.position = netDistance < 0.2f ? Vector2.Lerp(transform.position, netPos, 0.6f) : netPos;

            tilePos = DC.TT.GetTilePos(groundedCenter.transform.position, true);
            lifeTimer += Time.deltaTime;

            if (PMS.hitPoints <= 0)
                Death(false, false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundedCenter != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(groundedCenter.transform.position, new Vector2(groundedSize, groundedSize));
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(groundedCenter.transform.position, groundedCenter.transform.position + Vector3.up * 0.05f);
        }
    }
}
