using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_beh_walk : MonoBehaviour
{
    dataController DC;

    [Header("[necessary]")]
    public enemy_controller EC;
    [Header("[necessary if has pos animation]")]
    public pos_animation PA;


    [Header("____________________________ PMS")]
    [Header("(air, water): (0, 0) for (1, 1)")]
    public Vector2 gravityModifier;
    [Header("0 - normal, 1 - stop, 2 - slide")]
    [Range(0, 2)]
    public int attBehaviour;
    public bool noFallDamage, noWater = false;
    [Header("(x - dist, y - height")]
    public Vector2Int wallCheckDistance, cliffCheckDistance;

    public AudioClip jumpSound;

    public bool alwaysFrighten, stayOnPassive, noAirTurn, runOnActive;
    public float runMulti;
    Vector2 prevTilepos;

    int prevTargetDir;
    float noclipTimer;
    float turnTimer, activeTimer;
    public float lastGroundedY;
    bool isMoving, isFrighten, canTurn;
    Vector2 curJumpPower;

    int curColId;

    void Start()
    {
        DC = GM.Inst.DC;

        // try to get (old)
        if (EC == null)
            EC = GetComponent<enemy_controller>();

        if (EC.isCopy)
        {
            this.enabled = false;
            return;
        }

        if (PA == null)
            PA = GetComponent<pos_animation>();

        lastGroundedY = EC.rb.position.y;
        Turn(false, true, 1);
    }

    private void FixedUpdate()
    {
        curColId = 0;
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        bool grContact = false;
        bool plContact = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.contacts[i].normal.y > 0.6f)
            {
                if (collision.gameObject.layer == DC.PP.groundLayer)
                    grContact = true;

                if (collision.gameObject.layer == DC.PP.platformLayer)
                    plContact = true;
            }
        }

        if (EC.STATES.stateTimers[6] > 0)
            curColId = 0;
        else if (grContact)
            curColId = 1;
        else if (plContact)
            curColId = 2;

        EC.colIndex = curColId;
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        EC.colIndex = 0;
    }

    // main
    void Update()
    {
        Animations();

        if (!DC.IsPaused() && DC.TT.IsInMapRange(EC.tilePos.x, EC.tilePos.y, DC.TT.map))
        {
            // noclip
            if (noclipTimer > 0)
                noclipTimer -= Time.deltaTime;
            else
                noclipTimer = 0;

            if (!EC.STATES.CheckStun())
            {
                CheckGround();

                if (!isFrighten && EC.CheckFrighten())
                {
                    int dir = (int)Mathf.Sign(EC.DC.ClosestCC(transform.position).pos.x - EC.rb.position.x);
                    isFrighten = true;
                    Turn(true, true, 2);
                }

                if (isFrighten && !EC.CheckFrighten())
                {
                    isFrighten = false;
                }

                // 0 vlc wall turn/jump
                if (isMoving && EC.isGrounded && EC.GetFactSpeed() < 0.05f && activeTimer > 0.5f)
                    CheckWall();

                OnTileposChange();

                if (isActive())
                {
                    activeTimer += Time.deltaTime;
                    ActiveMovements();
                }
                else
                {
                    activeTimer = 0;
                    PassiveMovements();
                }
            }
            // inactive
            else
                EC.rb.linearVelocity = DC.CR.CalculateSpeed(EC.rb, null, 0, 0, 0, EC.colIndex, false, EC.CheckInWater() , EC.CheckInWater() , false, gravityModifier, EC.PMS.buffTimers, EC.PMS.buffPowers);
        }
    }

    bool isActive()
    {
        return EC.STATES.CheckSight() || EC.STATES.stateTimers[9] > 0 || EC.CheckFrighten();  // [4] in sight [9] follows trail
    }


    private void ActiveMovements()
    {
        float speed = EC.PMS.speed;
        float effectMulti = DC.CR.CalculateSpeedMulti(EC.PMS.accumOns, EC.PMS.buffTimers, EC.liqIndex);
        float curRunMulti = runOnActive ? runMulti : 1;
        int targetDir = (int)Mathf.Sign(EC.targetPos.x - EC.tilePos.x) * (alwaysFrighten || EC.CheckFrighten() ? -1 : 1);

        if (runOnActive)
            EC.CONTACT.alwaysContactHit = true;

        // turn
        if (targetDir != prevTargetDir)
        {
            float turnDelayTime = 1 + DC.FF.TrueRandom(1f);
            Turn(false, true, turnDelayTime);
        }
        else if (targetDir * EC.direction < 0 && Time.time > turnTimer)
        {
            float turnDelayTime = 1 + DC.FF.TrueRandom(1f);
            Turn(true, true, turnDelayTime);
        }

        prevTargetDir = targetDir;

        // always move on sight
        isMoving = EC.STATES.CheckSight();

        // apply vlc
        if (EC.STATES.CheckNoMove() || !isMoving || (!EC.isGrounded && noAirTurn)) //[3] no move
        {
            EC.rb.linearVelocity = DC.CR.CalculateSpeed(EC.rb, null, 0, 0, 0, EC.colIndex, false, EC.CheckInWater(), EC.CheckInWater(), false, gravityModifier, EC.PMS.buffTimers, EC.PMS.buffPowers);
            //Debug.Log("VLC 0: " + EC.rb.linearVelocity);
        }
        else
        {
            EC.rb.linearVelocity = DC.CR.CalculateSpeed(EC.rb, null, speed * effectMulti * curRunMulti, EC.direction  * (EC.STATES.CheckStun() ? 0 : 1), 0, EC.colIndex, false, EC.CheckInWater(), EC.CheckInWater(), false, gravityModifier, EC.PMS.buffTimers, EC.PMS.buffPowers);
            Debug.Log("VLC 1: " + EC.rb.linearVelocity + " spd: " + speed + " effect: " + effectMulti + " dir: " + EC.direction + " stun: " + EC.STATES.CheckStun() + " col: " + EC.colIndex);
        }
    }

    private void PassiveMovements()
    {
        float speed = EC.PMS.speed * 0.8f;

        if (runOnActive)
            EC.CONTACT.alwaysContactHit = false;

        // no move
        if (stayOnPassive)
        {
            isMoving = false;
            EC.rb.linearVelocity = DC.CR.CalculateSpeed(EC.rb, null, 0, 0, 0, EC.colIndex, false, EC.CheckInWater(), EC.CheckInWater(), false, gravityModifier, EC.PMS.buffTimers, EC.PMS.buffPowers);
            return;
        }

        // normal
        float effectMulti = DC.CR.CalculateSpeedMulti(EC.PMS.accumOns, EC.PMS.buffTimers, EC.liqIndex);

        // turn
        if (Time.time > turnTimer)
        {
            float nextTime = 0.5f + DC.FF.TrueRandom(1f);
            if (DC.FF.TrueRandom(100) < 40)
            {
                Turn(DC.FF.TrueRandom(100) < 50, false, nextTime);
            }
            else
                Turn(DC.FF.TrueRandom(100) < 50, true, nextTime);
        }

        // move
        if (isMoving)
            EC.rb.linearVelocity = DC.CR.CalculateSpeed(EC.rb, null, speed * effectMulti / 2, EC.direction, 0, EC.colIndex, false, EC.CheckInWater(), EC.CheckInWater(), false, gravityModifier, EC.PMS.buffTimers, EC.PMS.buffPowers);
        else
            EC.rb.linearVelocity = DC.CR.CalculateSpeed(EC.rb, null, 0, 0, 0, EC.colIndex, false, EC.CheckInWater(), EC.CheckInWater(), false, gravityModifier, EC.PMS.buffTimers, EC.PMS.buffPowers);
    }
    void Noclip()
    {
        if (EC.colIndex == 2 && (PathfinderCheck(false) || alwaysFrighten || EC.CheckFrighten()) && !DC.FF.CheckLava(EC.rb.position, 0, true, false))
            noclipTimer = 0.5f;
    }
    void Animations()
    {     
        if (EC.STATES.stateTimers[1] <= 0 && EC.STATES.stateTimers[3] <= 0) // attack, no move
        {
            if (EC.isGrounded || EC.STATES.CheckGrounded())
            {
                // walk
                if (EC.GetFactSpeed() > 0.1f)
                {
                    if (PA != null)
                        PA.SwitchState(true);

                    if (isActive() && runOnActive)
                        EC.ECA.SetAnim(4, 0);
                    else
                        EC.ECA.SetAnim(0, 0);
                }

                // idle
                else
                {
                    if (PA != null)
                        PA.SwitchState(false);
                    EC.ECA.SetAnim(1, 0);
                }
            }

            // jump
            else
            {
                if (PA != null)
                    PA.SwitchState(false);
                EC.ECA.SetAnim(3, 0);
            }
        }
    }
    bool CanJumpCheck()
    {
        //              jump reload               no move                is jumping
        return EC.STATES.stateTimers[0] <= 0 && EC.STATES.stateTimers[3] <= 0 && EC.STATES.stateTimers[6] <= 0  && EC.isGrounded && !EC.STATES.CheckStun();
    }

    // tile pos change
    void OnTileposChange()
    {
        if (prevTilepos != EC.tilePos)
        {
            prevTilepos = EC.tilePos;

            // noclip
            Noclip();

            if (CheckLava() && !EC.CheckInLava() && !EC.CheckInWater())
            {
                //DC.FF.WriteTest(EC.rb, "liq turn", 0, false);
                Turn(true, isActive(), 1);
            }


            // can jump
            else if (CanJumpCheck())
            {
                // cliff check
                if (CheckCliff())
                {
                    DC.FF.WriteTest(EC.rb, "c      ", 1, false);

                    // jump
                    if (PathfinderCheck(true) && CanJumpCheck() && CheckJumpCliffUp(EC.tilePos))
                        Jump();
                    else if (PathfinderCheck(false) && CanJumpCheck() && CheckJumpCliffDown(EC.tilePos))
                        Jump();


                    // char cliff turn
                    else if (DC.FF.CheckLava(EC.rb.position, EC.direction, false, noWater) || (PathfinderCheck(true) && !noFallDamage))
                    {
                        DC.FF.WriteTest(EC.rb, "    turn", 3, false);
                        Turn(true, isActive(), 1);
                    }
                }
                else
                {
                    // wall
                    if (CheckJumpWall(false) && PathfinderCheck(true))
                    {
                        DC.FF.WriteTest(EC.rb, "l jump", 6, false);
                        Jump();
                    }

                    // lava 
                    if (!EC.CheckInLava() && DC.FF.CheckLava(EC.rb.position, EC.direction, false, noWater))
                    {
                        DC.FF.WriteTest(EC.rb, "l      ", 2, false);

                        // jump
                        if (PathfinderCheck(true) && CheckJumpCliffUp(EC.tilePos) && CanJumpCheck())
                            Jump();
                        else if (PathfinderCheck(false) && CheckJumpCliffDown(EC.tilePos) && CanJumpCheck())
                            Jump();

                        // turn
                        else
                        {
                            DC.FF.WriteTest(EC.rb, "    turn", 2, false);
                            Turn(true, isActive(), 2);
                        }
                    }
                }
            }
        }
    }

    bool PathfinderCheck (bool isUp)
    {
        bool result;
        if (isUp)
        {
            result = EC.targetPos.y <= EC.tilePos.y;
            DC.FF.ChatEnemyData("up " + result.ToString(), 0, result ? 1 : 8);
        }
        else
        {
            result = EC.targetPos.y > EC.tilePos.y + 1;
            DC.FF.ChatEnemyData("down " + result.ToString(), 0, result ? 1 : 8);
        }
        return result;
    }

    // actions
    void Turn(bool flipDirection, bool move, float time)
    {
        if (flipDirection && canTurn)
        {
            EC.direction = -EC.direction;
            EC.STATES.stateTimers[4] -= 5; // [4] sight reduce ???
            EC.STATES.stateTimers[9] -= 5; // [9] follow reduce ???
        }

        canTurn = move; // inactive single no turn
        isMoving = move;

        turnTimer = Time.time + time;


    }
    public void Jump()
    {
        Vector2 angleVlc = DC.CR.JumpGetAngleVlc(Vector2.zero, curJumpPower * DC.CR.tileSize, EC.direction, DC.CR.groundGravity * (gravityModifier.x > 0 ? gravityModifier.x : 1));
        DC.FF.ChatEnemyData(angleVlc.ToString(), 3, 0);

        if (angleVlc != Vector2.zero)
        {
            if (jumpSound)
            DC.PR.PlaySound(jumpSound, EC.rb.position);

            EC.isGrounded = false;
            EC.colIndex = 0;
            EC.STATES.stateTimers[0] = 0.25f; // [0] jump reload
            EC.STATES.SetJump(0.25f);

            Vector2 vectorSpd = DC.FF.AngleToVector(angleVlc.x) * angleVlc.y * 1.2f;

            EC.rb.linearVelocity = vectorSpd; //  * (1 - (gravityModifier.x > 0 ? gravityModifier.x : 1) / 2)
        }
        else
        {
            Turn(true, isActive(), 3);
        }
    }

    // jump checks
    void CheckWall()
    {
        // side pos
        bool isWall = false;
        for (int i = 1; i <= wallCheckDistance.x; i++)
        {
            Vector2Int tPos = EC.tilePos + new Vector2Int(EC.direction * i, 0);
            if (DC.TT.Tig(tPos, 1))
                isWall = true;
        }

        if (CanJumpCheck() && isWall)
        {
            if (CheckJumpWall(false))
            {
                DC.FF.WriteTest(EC.rb, "w jump" + curJumpPower, 8, false);
                Jump();
            }
            else
            {
                DC.FF.WriteTest(EC.rb, "w turn", 1, false);
                Turn(true, isActive(), 2);
            }
        }
    }
    bool CheckJumpWall(bool checkHeightOne)
    {
        // checks for suitable pos

        int dir = EC.direction;
        for (int x = 1; x < wallCheckDistance.x; x++)
            for (int y = wallCheckDistance.y; y >= 0; y--)
            {
                Vector2Int pos = EC.tilePos + new Vector2Int(x * dir, -y);
                Vector2Int upPos = pos - Vector2Int.up;
                Vector2Int sidePos = pos - Vector2Int.right * dir;
                Vector2Int sideDownPos = pos - Vector2Int.right * dir + Vector2Int.up;

                if (DC.TT.QIMR(pos) && DC.TT.Tig(pos, 1)
                    && DC.TT.QIMR(upPos) && DC.TT.QIMR(sidePos) && DC.TT.QIMR(sideDownPos))
                {
                    if (!DC.TT.Tig(upPos, 1) && !DC.TT.Tig(sidePos, 1) &&
                        (checkHeightOne || !DC.TT.Tig(sideDownPos, 0)))
                    {
                        curJumpPower = new Vector2(EC.direction, y >= 1 ? y + 3 : 2); // 2 is minimum power
                        //DC.FF.ChatTitle("" + pos, 1);
                        return true;
                    }

                }
            }
        return false;
    }
    bool CheckCliff()
    {
        int dir = EC.direction;
        Vector2Int tpos = EC.tilePos;

        for (int y = 0; y < cliffCheckDistance.y + 1; y++)
        {
            Vector2Int curPos = new Vector2Int(tpos.x + dir, tpos.y + y);
            if (DC.TT.QIMR(curPos) && DC.TT.Tig(curPos, 2)) // 2 +bridges
                return false;
        }
        return true;
    }
    bool CheckLava()
    {
        int dir = EC.direction;
        Vector2Int tpos = EC.tilePos;
        Vector2Int curPos = new Vector2Int(tpos.x + dir, tpos.y + 1);
        int liqId = DC.TT.watermap[curPos.x, curPos.y];

        if (liqId >= 2 || (noWater && liqId == 1))
            return true;
        else
            return false;
    }
    bool CheckJumpCliffUp(Vector2Int tpos) // checks for suitable pos
    {
        int dir = EC.direction;

        for (int y = 0; y < wallCheckDistance.y; y++) // inv
            for (int x = 1; x < cliffCheckDistance.x; x++)
            {
                // angle tile check      
                Vector2Int pos = tpos + new Vector2Int(x * dir, -y);
                Vector2Int posUp = tpos + new Vector2Int(x * dir, -y - 1);
                Vector2Int posUp2 = tpos + new Vector2Int(x * dir, -y - 2);

                //         center                           up         
                if (DC.TT.QIMR(pos) && DC.TT.Tig(pos, 1) && !DC.TT.Tig(posUp, 1) && !DC.TT.Tig(posUp2, 1))
                {
                    DC.FF.WriteTest(EC.rb, "   found up", 1, false);
                    curJumpPower = new Vector2(EC.direction * (x + 1), y + 3);
                    return true;
                }
            }

        return false;
    }
    bool CheckJumpCliffDown(Vector2Int tpos) // checks for suitable pos
    {
        int dir = EC.direction;
        for (int y = 1; y < cliffCheckDistance.y * 2; y++) // inverted
            for (int x = 1; x < cliffCheckDistance.x; x++)
            {
                // angle tile check
                Vector2Int pos = tpos + new Vector2Int(x * dir, y);
                Vector2Int posUp = tpos + new Vector2Int(x * dir, y - 1);

                //                      center                      up                       water
                if (DC.TT.QIMR(pos) && DC.TT.Tig(pos, 2) && !DC.TT.Tig(posUp, 2) && DC.TT.watermap[posUp.x, posUp.y] != 2)
                {
                    DC.FF.WriteTest(EC.rb, "   found down", 2, false);
                    curJumpPower = new Vector2(EC.direction * x, -y + 1);
                    return true;
                }
            }

        return false;
    }



    // ground checks
    private void CheckGround() //& fall damage
    {

        bool isCol = EC.colIndex != 0; // [6] jump

        // noclip (no platforms)
        if (!EC.isGrounded || isCol) // [5] is jumping or [6] noclip
        {
            // stay on platform
            if ((EC.isGrounded || CheckPlatform()) && noclipTimer == 0)
                gameObject.layer = EC.isInvulnerable ? DC.PP.fishmanInvLayer : DC.PP.creaturesLayer;
            else
                gameObject.layer = EC.isInvulnerable ? DC.PP.fishmanInvNoclipLayer : DC.PP.creaturesNoclipLayer;
        }

        // fall damage
        if (!noFallDamage && !EC.isGrounded && isCol)
        {
            float maxSafe = DC.CR.fallDist;
            float diff = Mathf.Abs(lastGroundedY - EC.rb.position.y);
            float dmgDiff = diff - maxSafe;

            if (dmgDiff > 0 && !EC.CheckInWater())
            {
                float multi = Mathf.Pow(1 + (dmgDiff * DC.CR.fallDmg), 2);
                if (multi > 1)
                    EC.PMS.isHit((int)multi, 0.2f, false, 0, EC.rb.position, Vector2.zero, EC.arrayId, Vector2Int.zero, 1);
            }
        }

        //jump end
        //if (isCol || rb.velocity.y < 0)
        //    EC.stateTimers[6] = 0;

        EC.isGrounded = isCol && EC.STATES.stateTimers[6] <= 0;

        // last grounded
        if (EC.isGrounded || (!EC.isGrounded && EC.rb.linearVelocity.y > 0.02f))
            lastGroundedY = EC.rb.position.y;
    }
    bool CheckPlatform()
    {
        return Physics2D.OverlapCircle(EC.groundedCenter.transform.position, 0.01f, DC.PP.platformLayerMask);
        //return Physics2D.Raycast(groundCheck.transform.position, Vector2.down, 0.04f, DC.PP.groundLayerMask);
    }


    private void OnDrawGizmosSelected() // wall checks
    {
        Vector3 startPoint = transform.position;
        Vector3 endPoint = startPoint + (Vector3.right * wallCheckDistance.x * 0.16f);
        Vector3 height = endPoint + (Vector3.up * wallCheckDistance.y * 0.16f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startPoint, endPoint);
        Gizmos.DrawLine(endPoint, height);
    }
}
