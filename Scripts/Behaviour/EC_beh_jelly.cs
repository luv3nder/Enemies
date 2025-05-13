using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_beh_jelly : MonoBehaviour
{
    EnemyController EC;
    public dataController DC;

    public float dashSpeed, noMoveReduce = 0.99f, stopAtDistance;
    public static_animation anim;
    public Vector2 delayRange;
    public AudioClip dashSound;

    public int escapeRadius;
    public Vector2 targetPos;

    public bool noRotation, noFlip = true;

    float timer, curDelay;

    void Start()
    {
        EC = GetComponent<EnemyController>();
        DC = EC.DC;

        if (EC.isCopy)
        {
            this.enabled = false;
            return;
        }

        timer = 0.01f;

        Rotation();
        SetDelay();
    }
    void Update()
    {
        if (!noRotation)
            Rotation();

        if (EC.CheckHasTarget() && !EC.STATES.CheckHit() && !EC.STATES.CheckStun() && !EC.STATES.CheckFlick() && !EC.STATES.CheckNoMove())
            Movements();
        else
            EC.rb.linearVelocity *= noMoveReduce;
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
    void Movements()
    {
        timer += Time.deltaTime;

        float distance = Vector2.Distance(EC.rb.position, EC.targetRb.position);
        if (timer > curDelay && distance > stopAtDistance)
        {
            if (EC.STATES.FindSight())
                targetPos = EC.targetRb.position;
            else
                FindEscape();

            Dash();
        }
    }
    void Dash()
    {
        timer = 0;
        SetDelay();

        float angle = EC.GetAngle(targetPos);
        float effectsMulti = DC.CR.CheckFrost(EC.PMS.accumOns) ? 0.5f : 1;
        float waterMulti = EC.CheckInWater() ? 1f : 0.6f;

        Vector2 vectorAngle = DC.FF.AngleToVector(angle) * (EC.CheckFrighten() ? -1 : 1);

        if (DC.FF.TrueRandom(100) < 50)
            EC.rb.AddForce(vectorAngle * dashSpeed * effectsMulti * waterMulti);
        else
            EC.rb.AddForce(vectorAngle * dashSpeed * 1.5f * effectsMulti * waterMulti);

        if (!noFlip)
            EC.direction = (int)Mathf.Sign(angle);

        DashFx(true);

        if (EC.CONTACT)
            EC.CONTACT.RefreshColArray();
    }
    public void DashFx(bool isOG)
    {
        if (isOG && DC.isMultiplayer)
            DC.NMI.EnemyFxServerRpc(DC.SID(), EC.arrayId, new Vector3Int(2, 0, 0));

        anim.PlayAnimation(0, 0);
        DC.PR.PlaySound(dashSound, transform.position);
    }
    void SetDelay()
    {
        curDelay = delayRange.x + DC.FF.TrueRandom(delayRange.y - delayRange.x);
    }




    void FindEscape()
    {
        Vector2 tPos = EC.targetRb.position;
        Vector2Int targetDir = GetTargetDir(tPos);

        int radius = 1 + (int)DC.FF.TrueRandom(escapeRadius);
        Vector2Int targetTilepos = EC.tilePos + targetDir * radius;

        // main
        if (CheckCanEscape(targetTilepos))
        {
            targetPos = DC.TT.GetWorldPos((Vector3Int)targetTilepos);
            return;
        }

        // secondary
        if (FindTargetInArray(GetSecondaryDirs(targetDir, true), radius))
        {
            return;
        }

        // other
        else if (FindTargetInArray(GetSecondaryDirs(targetDir, false), radius))
        {
            return;
        }
    }
    bool FindTargetInArray(Vector2Int[] array, int radius)
    {
        for (int i = 0; i < array.Length; i++)
        {
            Vector2Int targetTilepos = EC.tilePos + array[i] * radius;
            if (CheckCanEscape(targetTilepos))
            {
                targetPos = DC.TT.GetWorldPos((Vector3Int)targetTilepos);
                return true;
            }
        }
        return false;
    }
    bool CheckCanEscape(Vector2Int tPos)
    {
        return DC.TT.map[tPos.x, tPos.y] == 0 && DC.TT.TileLineCheckClear(DC.TT.GetTileLine(EC.tilePos, tPos));
    }
    Vector2Int GetTargetDir(Vector2 targetPos)
    {
        return new Vector2Int((int)Mathf.Sign(EC.rb.position.x - targetPos.x), (int)Mathf.Sign(EC.rb.position.y - targetPos.y) * -1) * -1;
    }
    Vector2Int[] GetSecondaryDirs(Vector2Int curTargetDir, bool main)
    {
        Vector2Int[] result = new Vector2Int[main ? 2 : 5];
        int counter = 0;

        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int curDir = new Vector2Int(x, y);

                // closest diff
                int xDiff = Mathf.Abs(curDir.x - curTargetDir.x);
                int yDiff = Mathf.Abs(curDir.y - curTargetDir.y);

                bool diffBool = (curDir.x == curTargetDir.x && yDiff < 2)
                    || (curDir.y == curTargetDir.y && xDiff < 2);

                diffBool = main ? diffBool : !diffBool;


                if (!(x == 0 && y == 0) && curDir != curTargetDir
                    && diffBool)
                {
                    result[counter] = curDir;
                    counter++;
                }
            }
        return result;
    }
}
