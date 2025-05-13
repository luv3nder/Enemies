using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_beh_worm : MonoBehaviour
{
    dataController DC;
    Rigidbody2D rb;
    [HideInInspector] public enemy_controller EC;

    public int jointNumber;
    public GameObject[] jointPrefabs;
    public worm_joint[] joints;

    public float speed, rotationSpeed, agressiveness = 80, minDistance, maxDistance, chainDeathDelay, shakeRange = 1, hitSoundTime = 0.1f;
    
    bool isTrig, isStun;
    public Vector2 targetPos, dirtPos, startPos;
    float distance;
    float angle;

    // wait tick
    public float waitTime = 0.5f, maxFollowDistance = 15;
    float waitTimer, hitSoundTimer;

    private void Start()
    {
        DC = GM.Inst.DC;

        EC = GetComponent<enemy_controller>();
        rb = GetComponent<Rigidbody2D>();

        Instantiates();
        SwitchTarget(Vector2.zero, false);

        isTrig = EC.deathTrigger;

        startPos = rb.position;
    }
    private void Update()
    {
        DC.PR.SetShake(rb.position, new Vector2(1, 0.1f));

        if (EC.STATES.CheckHit())
            hitSoundTimer = Time.time;

        if (EC.isCopy)
            return;

        EC.direction = rb.linearVelocity.x > 0 ? 1 : -1;
        distance = Vector2.Distance(rb.position, targetPos);

        MainControls();
    }


    bool IsTooFar()
    {
        return Vector2.Distance(rb.position, startPos) > maxFollowDistance;
    }
    public void PlayHitSound()
    {
        if (Time.time > hitSoundTimer + hitSoundTime)
        {
            AudioClip curHitSound = DC.PP.hitSound;
            curHitSound = EC.AUDIO.customHitSound != null ? EC.AUDIO.customHitSound : curHitSound;
            DC.PR.PlaySound(curHitSound, rb.position);

            hitSoundTimer = Time.time;
        }
    }

    // main
    void Instantiates()
    {
        // random size - speed
        int resNumber = 2 * jointNumber / 3 + EC.spawnPos.x % (jointNumber / 3);
        speed *= 1 + (1 - ((float)resNumber / jointNumber)) * 0.2f;

        // inst
        joints = new worm_joint[resNumber];
        EC.parts = new enemy_controller[resNumber];

        for (int i = 0; i < resNumber; i++)
        {
            GameObject curObj = (i != resNumber - 1) ? jointPrefabs[0] : jointPrefabs[1];

            joints[i] = Instantiate(curObj, transform.position, Quaternion.identity).GetComponent<worm_joint>();

            // add to EC.parts array
            EC.parts[i] = joints[i].EC;

            // different animation start
            if (EC.parts[i].SA != null)
            EC.parts[i].SA.stepId = i % 3;

            joints[i].headScript = this;
            joints[i].sortIndex = EC.spren.sortingOrder - (i + 1);

            if (joints[i].anim)
            {
                int spritesNumber = joints[i].anim.spriteArray0.Length;
                joints[i].anim.PlayAnimation(0, i % 2 == 0 ? 0 : spritesNumber / 2);
            }

            if (joints[i].eyesSprite != null)
            joints[i].eyesSprite.sortingOrder = EC.spren.sortingOrder - (i + 1) + 1;


            if (i == 0)
            {
                joints[i].parentRb = rb;
            }
            else
            {
                joints[i].parentRb = joints[i - 1].GetComponent<Rigidbody2D>();
                joints[i - 1].childJoint = joints[i];
            }
        }
    }
    void MainControls()
    {
        EC.tilePos = DC.TT.GetTilePos(rb.position, false);

        // stun
        if (EC.STATES.CheckStun() && !isStun)
        {
            isStun = true;
            SwitchTarget(Vector2.zero, true);
        }

        if (!EC.STATES.CheckStun() && isStun)
            isStun = false;
        //

        ZeroAttController();
        FollowTarget();
        Rotation();

        Death();
    }
    void ZeroAttController()
    {
        waitTimer += Time.deltaTime;

        // check tick
        if (waitTimer > waitTime)
        {
            // follow char chance + is too far
            if (DC.FF.TrueRandom(100) < agressiveness || distance > maxDistance || distance < minDistance)
            {
                SwitchTarget(DC.CC().CHAR.rb.position, false);
            }

            // neutral
            else
            {
                SwitchTarget(Vector2.zero, true);
            }
        }
    }


    // movements
    void SwitchTarget(Vector2 pos, bool isOpposite)
    {
        //DC.TT.SetTestTile(targetPos, false, 0);
        waitTimer = 0;
        FindDirtTarget();

        // return
        if (IsTooFar())
            targetPos = startPos;
        // exact target pos
        else if (pos != Vector2.zero)
            targetPos = EC.CheckFrighten() || DC.CC().CheckEmit() || DC.CC().isUnderwater ? DC.FF.GetFrightenPos(EC.rb.position, pos) : pos;
        // target near char pos
        else
        {
            Vector2 boxPos = DC.CC().pos;
            float xRng = 3;
            float yRng = 3;

            Vector2 resultPos;

            // opposite
            if (isOpposite)
            {
                float dir = DC.CC().CHAR.rb.position.x > rb.position.x ? -1 : 1;
                resultPos = new Vector2(Random.Range(0, xRng), Random.Range(-yRng, yRng)) * dir;
            }
            // random
            else
                resultPos = new Vector2(Random.Range(-xRng, xRng), Random.Range(-yRng, yRng));

            targetPos = boxPos + resultPos;
        }

        //DC.TT.SetTestTile(targetPos, false, 2);

        //DC.FF.ChatTest("w: tpos " + dirtPos.ToString(), 2);
    }
    void FindDirtTarget()
    {
       // DC.TT.SetTestTile(dirtPos, false, 0);

        // angle
        float angle = DC.FF.GetAngle(rb.position, targetPos);
        float angleRange = Mathf.PI / 2;
        float distance = minDistance;

        // default
        Vector2 defaultPos = rb.position + new Vector2(Mathf.Sin(angle) * distance, Mathf.Cos(angle) * distance);

        // random array
        int number = 16;
        float angleSegment = angleRange * 2 / number; // x2
        List<int> randomIds = new List<int>();
        for (int i = 0; i < number; i++)
        {
            randomIds.Add(i);
        }

        bool isDone = false;
        while (randomIds.Count > 0)
        {
            int id = (int)DC.FF.TrueRandom(randomIds.Count);
            randomIds.Remove(randomIds[id]);

            // angle segment * random id
            float curAngle = angle - angleRange + angleSegment * id;
            Vector2 curPos = rb.position + new Vector2(Mathf.Sin(curAngle) * distance, Mathf.Cos(curAngle) * distance);
            Vector2Int curTilepos = DC.TT.GetTilePos(curPos, false);

            if (DC.TT.QIMR(curTilepos) && DC.TT.map[curTilepos.x, curTilepos.y] != 0)
            {
                dirtPos = DC.TT.GetWorldPos((Vector3Int)curTilepos);
                isDone = true;
                break;
            }
        }

        // default if no suitable
        if (!isDone)
        {
            //DC.FF.ChatTest("w: no ground", 8);
            dirtPos = defaultPos;
        }

        // apply test
        //DC.TT.SetTestTile(dirtPos, false, 3);
    }
    void FollowTarget()
    {

        Vector2 curTargetPos = targetPos;
        Vector2 curDirtPos = dirtPos;

        // follow
        float targetDistance = Vector2.Distance(rb.position, targetPos);
        float dirtDistance = Vector2.Distance(rb.position, dirtPos);

        bool followsDirt = targetDistance > minDistance;

        // follows dirt if distance > ???
        float targetAngle = GetAngle(followsDirt);

        // next dirt
        if (dirtDistance < 0.1f)
            FindDirtTarget();

        // rotation direction
        float minDiff = DC.FF.GetRotationDirection(angle, targetAngle, true);    

        // treshold
        if (Mathf.Abs(minDiff) > rotationSpeed * Time.deltaTime)
            angle += Mathf.Sign(minDiff) * rotationSpeed * Time.deltaTime;

        bool isStunned = EC.STATES.CheckStun();
        float stunMulti = isStunned ? 0.8f : 1f;
        float frzMulti = GetFreezMulti();

        rb.linearVelocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * speed  * frzMulti * stunMulti;
    }
    float GetAngle(bool isDirt)
    {
        if (isDirt)
            return Mathf.Atan2(dirtPos.x - transform.position.x, dirtPos.y - transform.position.y);
        else
            return Mathf.Atan2(targetPos.x - transform.position.x, targetPos.y - transform.position.y);
    }
    void Rotation()
    {
        float thisAngle;

        if (EC.rb.linearVelocity != Vector2.zero)
        {
            thisAngle = Mathf.Atan2(EC.rb.linearVelocity.y, EC.rb.linearVelocity.x);
            float rotationAngle = thisAngle / Mathf.PI * 180f - 90;
            transform.eulerAngles = new Vector3(1, 1, rotationAngle);
        }
    }


    float GetFreezMulti()
    {
        float result = 1;
        float step = 0.1f;
        for (int i = 0; i < EC.parts.Length; i++)
        {
            if (DC.CR.CheckFrost(EC.parts[i].PMS.accumOns))
                result -= step;
        }
        return result > 0.5f ? result : 0.5f;
    }

    void Death()
    {
        if (isTrig != EC.deathTrigger)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i].DeathSet(chainDeathDelay * (i + 1));
            }

            EC.Death(true, true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
