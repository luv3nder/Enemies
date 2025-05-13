using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_beh_mask : MonoBehaviour
{
    public dataController DC;
    public static_animation SA;
    public enemy_controller EC;
    Rigidbody2D rb;

    public float followChance;
    public float stunSlowdown = 0.02f;
    public float speed;
    public float inertiaChange;
    public float switchTargetTime = 5;
    public float followDistance = 10;

    public Vector2 targetPos;

    public Vector2 alphaDistance;
    float alphaTimer;

    bool followsChar;
    float followTimer;
    Vector2 startPos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = rb.position;
        SwitchTarget(Vector2.zero);
        targetPos = rb.position;

        if (EC.isCopy)
            enabled = false;
    }

    bool IsTooFar()
    {
        return Vector2.Distance(rb.position, startPos) > followDistance;
    }
    
    void AlphaChange()
    {
        if (alphaTimer < 1)
            alphaTimer += Time.deltaTime;
        else
            alphaTimer = 1;

        float dist = Vector2.Distance(rb.position, DC.CC().rb.position);
        float distMulti = (dist - alphaDistance.x) * alphaTimer;
        float invisMulti = distMulti < alphaDistance.y ? (1 - distMulti / alphaDistance.y) : 0;
        EC.spren.color = DC.FF.ChangeColorAlpha(EC.spren.color, invisMulti);
    }
    public void SwitchTarget(Vector2 target)
    {
        // reload
        followTimer = switchTargetTime;

        // follow chance
        if (followChance != 0 && DC.FF.TrueRandom(100) < followChance)
        {
            followsChar = true;
            return;
        }

        // exact
        if (target != Vector2.zero)
        {
            followsChar = false;
            targetPos = target;
            return;
        }

        // return
        if (IsTooFar())
        {
            followsChar = false;
            targetPos = startPos;
        }
        // follow
        else if (EC.targetRb && EC.targetRb != EC.rb)
        {
            followsChar = false;
            float range = 1;
            Vector2 offset = new Vector2(Random.Range(-range, range), Random.Range(0, range)) + DC.CC().CHAR.rb.linearVelocity; // + vlc
            targetPos = EC.targetRb.position + offset;
        }
        // static
        else
        {
            EC.STATES.stateTimers[3] = 0.5f;
            targetPos = startPos;
        }
    }
    void MainControls()
    {
        followTimer -= Time.deltaTime;

        EC.tilePos = DC.TT.GetTilePos(rb.position, false);
        float tDistance = Vector2.Distance(rb.position, targetPos);

        if (followsChar && EC.targetRb)        
            targetPos = EC.targetRb.position;

        if (tDistance < 0.2f || followTimer <= 0)
            SwitchTarget(Vector2.zero);
    }
    void Update()
    {
        if (DC.CC().CHAR != null && !EC.STATES.CheckNoMove())
        {
            FollowTarget();
            MainControls();

            if (alphaDistance != Vector2.zero)
                AlphaChange();
        }
        else
            rb.linearVelocity *= (1 - stunSlowdown);
    }
   
    void FollowTarget()
    {
        Vector2 curVlc = rb.linearVelocity * (EC.STATES.CheckNoMove() ? 0.98f : 1); // slow down on no move

        float angle = DC.FF.GetAngle(EC.rb.position, targetPos);
        float effectsMulti = DC.CR.CheckFrost(EC.PMS.accumOns) ? 0.5f : 1;
        float speedLmt = speed * effectsMulti;

        // input
        Vector2 vlcInput = (EC.CheckFrighten() ? -1 : 1) * DC.FF.AngleToVector(angle) * inertiaChange * effectsMulti * Time.deltaTime;

        curVlc += vlcInput;
        // apply
        rb.linearVelocity = DC.CR.SpeedLimit(curVlc, Vector2.one * speedLmt);
        EC.direction = rb.linearVelocity.x > 0 ? 1 : -1;
    }
}
