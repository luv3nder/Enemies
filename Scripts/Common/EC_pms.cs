using UnityEngine;

public class EC_pms : MonoBehaviour, IDamageable
{
    dataController DC;
    public EnemyController EC;

    public bool isAimable, isTeleportable, noFlip, noFrighten;
    public bool noBubbles, noSplash, noHpBar;
    public bool afraidOfLava = true, afraidOfWater, noLiquidDrag;

    [HideInInspector] public bool isBoss, isGhost;
    [HideInInspector] public int coinsDrop, expDrop;

    [HideInInspector] public int behaviourId;
    [HideInInspector] public float maxHp, hitPoints, mpPoints, spPoints, flPoints;
    [HideInInspector] public int attDamage, hitDefence, magDefence;
    public float speed, balance;


    public float balanceTime = 3, flickTime = 0.5f, hitNoMoveTime = 0.2f;
    public bool jumpKnock, angleKnock;

    [HideInInspector] public float[] accumTimers, accumDelays;
    [HideInInspector] public bool[] accumOns;
    float[] accumTicks;

    [HideInInspector] public float[] buffTimers;
    [HideInInspector] public int[] buffPowers;

    [HideInInspector] public projectile_effect[] activeBuffEffects, activeAccumEffects;

    [Header("[x - type, y - dmg multi]")]
    public Vector2[] vulnerabilities;
    public float[] effectResists;
    public bool noAccum;

    damageRender lastDamageRenderer;

    void Awake()
    {
        DC = GM.Inst.DC;

        // resists
        if (effectResists == null || effectResists.Length == 0)
            effectResists = new float[3];

        //0 emit 1 strong 3 healing 4 mana heal 5 bless 6 exp up 7 luck up 8 curse 9 phantom shots
        buffTimers = new float[13];
        buffPowers = new int[13];
        activeBuffEffects = new projectile_effect[13];

        //0 poison 1 fire 3 frost
        accumTimers = new float[3];
        accumDelays = new float[3];
        accumOns = new bool[3];
        activeAccumEffects = new projectile_effect[3];
        accumTicks = new float[3];
    }
    private void Start()
    {
        Instantiates();
    }
    void Update()
    {
        if (EC.PMS.hitPoints > 0)
        {
            DC.CR.CalculateEffects(gameObject, accumTimers, accumOns, accumDelays, accumTicks, buffTimers, null, EC.liqIndex, maxHp, effectResists, EC.biomeId);
            AllRegen();
        }
    }

    void Instantiates()
    {
        // load
        if (EC.enemyId != 0 && EC.enemyId != -1)
            for (int i = 0; i < DC.TT.enemyPms.GetLength(1); i++)
            {
                int pm = DC.TT.enemyPms[EC.enemyId - 1, i]; // int[,] массив с параметрами всех врагов

                if (pm != 0)
                {
                    if (i == 1)
                        behaviourId = pm;
                    if (i == 2)
                        coinsDrop = pm;
                    if (i == 3)
                        expDrop = pm;
                    if (i == 4)
                        maxHp = pm;
                    if (i == 5)
                        attDamage = pm;
                    if (i == 6)
                        hitDefence = pm;
                    if (i == 7)
                        magDefence = pm;
                    if (i == 8)
                        speed = (float)pm / 10;
                    if (i == 9)
                        balance = (float)pm / 10;
                }
            }

        // default set
        spPoints = 100;
        flPoints = 100;
        hitPoints = maxHp == 0 ? 100 : maxHp;
        balance = balance == 0 ? 1 : balance;
        speed = speed == 0 ? 0.5f : speed;
    }
    void AllRegen()
    {
        // stam
        if (spPoints < 100)
            spPoints += Time.deltaTime * 2;
        else
            spPoints = 100;

        // flick
        if (flPoints < 100)
            flPoints += Time.deltaTime * 5;
        else
            flPoints = 100;

        // hp
        if (hitPoints < maxHp)
        {
            if (DC.CR.CheckHealing(buffTimers) && !DC.CR.CheckPoison(accumOns))
                hitPoints += Time.deltaTime * maxHp * 0.1f;
        }
        else
            hitPoints = maxHp;
    }

    // hit
    public bool IsHit(float hitDamage, float knockBack, bool isPureDamage, int crit, Vector2 hitPos, Vector2 hitVlc, int RBID, Vector2Int effectPms, int dmgType)
    {
        Debug.Log("HIT id: " + EC.enemyId + " dmg: " + hitDamage + " RBID: " + RBID);


        // pure damage
        if (isPureDamage)
        {
            hitPoints -= hitDamage * (DC.ST.edmgMulti - 8);
            return true;
        }

        // normal damage                                                                          effect zone
        else if (EC.lifeTimer > 0.2f && hitPoints != 0 && !(hitDamage == 0 && effectPms.y != 0 && effectResists[effectPms.x] == 100))
        {
            // vulnerabilities
            hitDamage *= GetVulnerabilityMulti(dmgType);
            hitDamage = (int)hitDamage;

            // stun is on
            bool isStunned = false;
            if (EC.STATES.CheckStun() && knockBack > 0)
            {
                EC.STATES.stateTimers[8] = 0;
                isStunned = true;
                crit = 100;
            }

            // frost is on
            if (DC.CR.CheckFrost(accumOns) && knockBack > 0)
            {
                Instantiate(DC.PP.frostExplPrefab, EC.rb.position, Quaternion.identity);
                SetElement(2, false);
                crit = 100;
            }

            // crit
            bool isCritical = Random.Range(0, 100) < crit;

            //_______________ DMG
            // split
            float effectDmg = hitDamage * (effectPms.y / 100f);
            float normalDmg = hitDamage - effectDmg;

            // resist
            float intResist = effectResists[effectPms.x] - (DC.CR.CheckPoison(accumOns) ? 20 : 0);
            intResist = intResist < -100 ? -100 : intResist;
            float resist = 1 - intResist / 100f;

            effectDmg *= resist;

            // result
            float sumDmg = normalDmg + effectDmg;

            float defMulti = 1 - ((DC.CR.CheckFire(accumOns) ? 0.3f : 0) + (DC.CR.CheckPoison(accumOns) ? 0.1f : 0));
            defMulti /= isCritical ? 1.8f : 1; // crit apply
            float resultDmg = DC.FF.CalculateDamage(sumDmg, (int)(hitDefence * defMulti));

            resultDmg += resist > 1 ? 1 : 0; // always +1 dmg on vulnerable
            resultDmg *= DC.ST.edmgMulti;
            float hpResult = hitPoints - resultDmg;

            hitPoints = hpResult > 0 ? hpResult : 0;
            //_______________ 


            // line
            float backDamage = resultDmg / DC.ST.edmgMulti;
            if (backDamage < 1000 && (backDamage >= 1 || RBID < 0))
            {
                if (lastDamageRenderer && lastDamageRenderer.timer < 0.5f)
                    lastDamageRenderer.RefreshDamage(backDamage, effectPms, isCritical);
                else
                    lastDamageRenderer = DC.FF.WriteDamage(EC.rb, EC.rb.position, backDamage, isCritical, effectPms);
            }

            Debug.Log("def: " + hitDefence + " result dmg: " + resultDmg + " back: " + backDamage);

            // sound
            if (hitDamage != 0)
                EC.AUDIO.PlayHitSound(2, true, true);

            // hit shake
            if (hitDamage > 0)
            {
                Vector2 screenShake = new Vector2(DC.CR.hitShake.x * 1.5f, DC.CR.hitShake.y);
                DC.PR.SetShake(EC.rb.position, screenShake);
            }

            // exit
            if (hitPoints <= 0)
                return true;

            // stun
            float frostMulti = DC.CR.CheckFrost(accumOns) ? 1.5f : 1;
            float airMulti = EC.isGrounded ? 1 : 1.3f;
            float difficultyMulti = 1 + (DC.ST.difficulty * 0.3f);

            float stunMulti = (30 * frostMulti * airMulti) / (balance * difficultyMulti);
            spPoints -= knockBack * stunMulti;

            if (spPoints <= 0)
            {
                isStunned = true;
                spPoints = 100;

                EC.STATES.SetHit(balanceTime);
                DC.FF.WriteTitle(EC.rb, "stun", 0);
            }


            // flick
            float flickMulti = (60 * airMulti * frostMulti) / (balance * difficultyMulti);
            flPoints -= knockBack * flickMulti;

            bool isFlicked = false;
            if (flPoints <= 0)
            {
                flPoints = 100;
                isFlicked = true;
            }

            // flick apply
            if (isFlicked || isStunned || isGhost)
            {
                EC.STATES.SetFlick(flickTime);
            }
            else if (!isBoss && !isGhost) // ??? ghosts always flick ?
            {
                knockBack *= 0.6f;
            }

            if (knockBack > 0)
            {
                // off ground
                if (jumpKnock)
                {
                    EC.isGrounded = false;
                    EC.colIndex = 0;
                    EC.STATES.SetJump(0.3f);
                }

                EC.STATES.SetHit(0.3f);
                EC.STATES.SetNoMove(0.3f, false);
            }

            // sight
            if (RBID != 0)
                EC.STATES.SetSight(EC.STATES.sightTime, DC.IDRB(RBID));

            if (EC.isCopy)
                return true;

            // apply knockback
            if (knockBack > 0)
            {
                float dir;
                float angle;
                //if (hitRb)
                //{
                //    dir = rb.position.x - hitRb.position.x > 0 ? 1 : -1;
                //    dir = hitVlc != Vector2.zero ? Mathf.Sign(hitVlc.x) : dir;
                //}

                if (hitVlc != Vector2.zero)
                {
                    angle = DC.FF.GetAngle(Vector2.zero, hitVlc);
                    dir = hitVlc.x > 0 ? 1 : -1;
                }
                else
                {
                    dir = EC.rb.position.x - hitPos.x > 0 ? 1 : -1;
                    angle = DC.FF.GetAngle(hitPos, EC.rb.position);
                }

                Vector2 vectorAngle = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                float limitedKnockback = knockBack < 1.7f ? knockBack : 1.7f;

                if (angleKnock && (EC.isGrounded || isGhost || isBoss))
                    EC.rb.linearVelocity = DC.CR.knockbackPower * limitedKnockback * vectorAngle / balance;
                else
                    EC.rb.linearVelocity = DC.CR.knockbackPower * new Vector2(dir * limitedKnockback, jumpKnock ? limitedKnockback : 0) / balance;
            }

            // effect
            if (!noAccum && effectPms.y != 0) // (hp after dmg apply)
                ElementalHit(effectPms);

            return true;
        }
        else
            return false;
    }
    public int GetHitBehaviourId(int attType)
    {
        if (EC.isCopy)
            return -20;

        return behaviourId;
    }
    public int GetHitId()
    {
        return EC.arrayId;
    }
    public Vector2Int GetHitTilepos()
    {
        return EC.tilePos;
    }
    public float GetVulnerabilityMulti(int attType)
    {
        if (vulnerabilities.Length > 0)
        {
            for (int i = 0; i < vulnerabilities.Length; i++)
                if (vulnerabilities[i].x == attType)
                    return vulnerabilities[i].y;
        }
        return 1;
    }
    public void ElementalHit(Vector2Int effectPms)
    {
        // 0
        if (effectPms.y == 0)
            return;

        // hp drain
        if (accumOns[effectPms.x])
        {
            switch (effectPms.x)
            {
                case 0:
                    hitPoints -= 2;
                    break;
                case 1:
                    hitPoints -= 4;
                    break;
                case 2:
                    hitPoints -= 3;
                    break;
            }
        }

        // effect
        if (!accumOns[effectPms.x])
        {
            float intResist = effectResists[effectPms.x] - (DC.CR.CheckPoison(accumOns) ? 20 : 0);
            intResist = intResist < -100 ? -100 : intResist;
            float resMulti = 1 - intResist / 100f;
            resMulti = resMulti > 1 ? 1 + (resMulti - 1) / 2 : resMulti; // not too much

            float input = effectPms.y * resMulti * 0.7f;
            float sum = accumTimers[effectPms.x] + input;

            accumDelays[effectPms.x] = 0.5f;

            // instant chance
            float instChanceMulti = accumTimers[effectPms.x] / 100;
            if (DC.FF.TrueRandom(100) < instChanceMulti * resMulti * effectPms.y * 3)
                sum = 100;

            accumTimers[effectPms.x] = sum <= 100 ? sum : 100;


            // frz fire
            switch (effectPms.x)
            {
                case 1:
                    accumTimers[2] -= effectPms.y;
                    break;
                case 2:
                    accumTimers[1] -= effectPms.y;
                    break;
            }
        }
    }

    public void Heal(int value)
    {
        if (hitPoints + value <= maxHp)
            hitPoints += value;
        else
            hitPoints = maxHp;

        //heal render
        string line = "+" + (value).ToString();
        DC.FF.WriteLine(EC.rb.position, line, 1, false, EC.rb, null);
    }

    // elemental on/off
    public void SetBuff(int index, float setTime, int power, bool withText)
    {
        // on
        if (setTime != 0)
        {
            if (buffTimers[index] <= 0)
            {

                // text
                if (withText)
                {
                    string line = DC.IF.GetEffectName(index, false, true);
                    DC.FF.WriteBuff(EC.rb.position, line, index, false, EC.rb, null);
                }

                // new effect
                activeBuffEffects[index] = Instantiate(DC.PP.buffPrefabs[index], transform.position, Quaternion.identity).GetComponent<projectile_effect>();
                activeBuffEffects[index].parentRb = EC.rb;
            }

            // add time
            buffTimers[index] = setTime;
        }
        // off
        else if (activeBuffEffects[index] != null)
        {
            activeBuffEffects[index].parentRb = null;
            activeBuffEffects[index] = null;
        }
    }
    public void SetElement(int index, bool isOn)
    {
        if (isOn)
        {
            if (EC.enemyId != 0 && hitPoints > 0)
            {
                string line = DC.IF.GetEffectName(index, true, true);
                DC.FF.WriteLine(EC.rb.position, line, index + 1, false, EC.rb, null);
            }

            // spawn effect
            if (activeAccumEffects[index] == null)
            {
                activeAccumEffects[index] = Instantiate(DC.PP.accumPrefabs[index], transform.position, Quaternion.identity).GetComponent<projectile_effect>();
                activeAccumEffects[index].parentRb = EC.rb;
            }

            // apply
            accumTimers[index] = 100;
            accumOns[index] = true;

            DC.PR.PlaySound(DC.PP.elementalSounds[index], EC.rb.position);
        }
        // off
        else
        {
            accumTimers[index] = 0;
            accumOns[index] = false;

            // remove effect
            if (activeAccumEffects[index] != null)
            {
                activeAccumEffects[index].parentRb = null;
                activeAccumEffects[index] = null;
            }
        }
    }
}
