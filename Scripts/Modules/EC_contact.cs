using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_contact : MonoBehaviour
{
    public EnemyController EC;
    dataController DC;
    public ParticleSystem contactParticles;

    [Header("_____________________________ PMS")]
    public Vector2 contactSize;
    public Transform contactTransf;
    public Transform[] contactTransfs;

    public float onHitReload = 0.5f; 
    float contactReloadTimer;

    public bool vlcAngle, noColArray, onlySetPos;

    [Header("_____________________________ ALWAYS")]
    public float onFactSpeed;
    public bool alwaysContactHit;
    public float alwaysKnockback = 0.6f, alwaysAttMulti = 1;
    public Vector2Int alwaysEffect;
    public float alwaysColReload = 1;

    [Header("_____________________________ KB")]
    public float hitKnock;
    public float hitKnockTime = 0.25f;

    [Header("_____________________________ TEST")]
    public int contactIndex; // 0 - normal, 1 - attack
    [HideInInspector] public float curAttMulti, curKnockback;
    [HideInInspector] public int curAttType, curAttId;
    public Rigidbody2D curRb;
    [HideInInspector] public Vector2 curHitPos, curHitSize;
    [HideInInspector] public Vector2Int curEffect;
    public float contactTimer;

    public Collider2D[] colArray;
    float[] colTimers;

    void Start()
    {
        DC = GM.Inst.DC;

        if (EC)
        {
            curRb = EC.rb;

            if (EC.isCopy)
                enabled = false;
        }

        RefreshColArray();
    }

    void Update()
    {
        // enemies
        if (EC)
        {
            // worms reload hit
            if (EC.STATES.CheckHit())
                contactReloadTimer = onHitReload;

            // contact
            if (!EC.STATES.CheckStun())
            {
                bool staticContact = alwaysContactHit || (onFactSpeed != 0 && EC.GetFactSpeed() >= onFactSpeed);

                if (staticContact || contactIndex != 0)
                    ContactHit();

                if (contactIndex == 0 && staticContact)
                    StaticRefreshColArray();
            }
        }
        // trap plates
        else
        {
            if (contactIndex != 0)
            {
                ContactHitNoEc();
                StaticRefreshColArray();
            }
        }

        // timer
        if (contactTimer > 0)
            contactTimer -= Time.deltaTime;
        else
        {
            contactTimer = 0;
            contactIndex = 0;
        }
    }

    public void SetContact(float curAttMulti, float curKnockback, Vector2Int curEffect, float time)
    {
        this.curAttMulti = curAttMulti == 0 ? alwaysAttMulti : curAttMulti;
        this.curKnockback = curKnockback == 0 ? alwaysKnockback : curKnockback;
        this.curEffect = curEffect == Vector2Int.zero ? alwaysEffect : curEffect;
        contactTimer = time == 0 ? 0.1f : time;

        contactIndex = 1;
    }

    public void SetContactNoEC(Vector2 hitPos, Vector2 hitSize, float damage, float knock, int attType, int attId, Vector2Int effect, Rigidbody2D rb, float time)
    {
        curAttMulti = damage == 0 ? alwaysAttMulti : damage;
        curKnockback = knock == 0 ? alwaysKnockback : knock;
        curEffect = effect == Vector2Int.zero ? alwaysEffect : effect;
        curAttId = attId;
        curAttType = attType;
        curHitPos = hitPos == Vector2.zero ? transform.position : hitPos;
        curHitSize = hitSize == Vector2.zero ? contactSize : hitSize;
        curRb = rb;

        if (contactIndex == 0 || curAttType != attType)
            RefreshColArray();

        contactTimer = time == 0 ? 0.1f : time;
        contactIndex = 1;
    }

    void ManyContactHits()
    {

    }
    void ContactHit()
    {
        contactReloadTimer -= Time.deltaTime;

        // particles on/off
        if (contactParticles)
        {
            if (contactReloadTimer > 0)
                contactParticles.Stop();
            else if (!contactParticles.isPlaying)
                contactParticles.Play();
        }

        if (EC.lifeTimer > 0.2f && contactReloadTimer <= 0)
        {
            float attMulti = contactIndex == 0 ? alwaysAttMulti : curAttMulti;
            float knockback = contactIndex == 0 ? alwaysKnockback : curKnockback;
            Vector2Int effect = contactIndex == 0 ? alwaysEffect : curEffect;
            //float angle = vlcAngle ? DC.FF.GetAngle(Vector2.zero, EC.rb.velocity) * 180 : 0;
            float angle = EC.transform.eulerAngles.z;


            Vector4 hitData = DC.FF.HitAlways(
            ContactPos(), // pos
            contactSize, // size
            angle,
            0,
            EC.PMS.attDamage * attMulti,
            knockback,
            0, // crit
            EC.PMS.behaviourId,
            0,
            0, // dmg type
            EC.rb.linearVelocity,
            effect,
            colArray,
            EC.arrayId, DC.PP.allCreaturesMask);


            int hitNum = (int)hitData.x;
            if (hitNum > 0)
            {

                // sight (static jellys)
                if (!EC.STATES.CheckSight())
                EC.STATES.FindSight();

                // knock
                if (hitKnock > 0)
                {
                    Vector2 KbPos = new Vector2(hitData.y, hitData.z);
                    float KbAngle = DC.FF.GetAngle(KbPos, EC.rb.position);
                    Vector2 KbVector = DC.FF.AngleToVector(KbAngle);

                    EC.STATES.SetNoMove(hitKnockTime, false);
                    //EC.rb.AddForce(KbVector * hitKnock);
                    EC.rb.linearVelocity = KbVector * hitKnock;

                    DC.FF.ChatTest("" + KbPos, 1);
                    DC.FF.ChatTest("" + DC.CC().pos, 2);
                    DC.FF.ChatTest("" + EC.rb.position, 3);

                }
            }
        }
    }
    void ContactHitNoEc()
    {
        float attDamage = curAttMulti;
        float knockback = curKnockback;
        Vector2Int effect = curEffect;
        float angle = 0;

        Vector4 hitData = DC.FF.HitAlways(
        onlySetPos ? curHitPos : ContactPos(), // pos
        curHitSize, // size
        angle,
        0,
        attDamage,
        knockback,
        0, // crit
        curAttId,
        curAttType,
                    0, // dmg type
        curRb.linearVelocity,
        effect,
        curAttType != 1 ? colArray : null, //  col array
        0, DC.PP.noGhostsMask);
    }

    Vector3 ContactPos()
    {
        if (contactTransf)
            return contactTransf.position;
        else
            return transform.position;
    }

    public void RefreshColArray()
    {
        if (colArray == null || colArray.Length < 10)
        {
            colTimers = new float[10];
            colArray = new Collider2D[10];
        }

        for (int i = 0; i < colArray.Length; i++)
        {
            colTimers[i] = alwaysColReload;
            colArray[i] = null;
        }
    }

    void StaticRefreshColArray()
    {
        for (int i = 0; i < colArray.Length; i++)
        {
            if (colArray[i] != null)
            {
                colTimers[i] -= Time.deltaTime;
                if (colTimers[i] <= 0)
                {
                    colTimers[i] = alwaysColReload;
                    colArray[i] = null;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(ContactPos(), contactSize);
    }
}
