using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EC_beh_fish : MonoBehaviour
{
    public dataController DC;

    enemy_controller EC;
    Rigidbody2D rb;

    public float tresh = 0.1f;
    public int escapeRadius = 3;
    public float speed;
    public Vector2 targetPos;

    void Start()
    {
        EC = GetComponent<enemy_controller>();
        rb = GetComponent<Rigidbody2D>();

        projectile_effect PE = Instantiate(DC.PP.bubblePrefab, transform.position, Quaternion.identity).GetComponent<projectile_effect>();
        PE.parentRb = rb;

        targetPos = EC.rb.position;
    }
    void Update()
    {
        if (EC.STATES.CheckSight())
        {
            float dist = Vector2.Distance(EC.rb.position, targetPos);
            if (dist < tresh)
                FindEscape();
        }
        EC.direction = (int)Mathf.Sign(EC.rb.linearVelocity.x);
        FollowTarget();
        Animations();
    }

    void  Animations ()
    {
        float maxAnimTime = 0.5f;
        float minAnimTime = 0.1f;

        float animTime = 1 - (Mathf.Abs(rb.linearVelocity.x) / maxAnimTime);

        // limits
        animTime = animTime > minAnimTime ? animTime : minAnimTime;
        animTime = animTime < maxAnimTime ? animTime : maxAnimTime;

        EC.ECA.speed[0] = animTime;
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
        else if (FindTargetInArray(GetSecondaryDirs(targetDir, true), radius))
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
        return DC.TT.watermap[tPos.x, tPos.y] != 0 && DC.TT.map[tPos.x, tPos.y] == 0 && DC.TT.TileLineCheckClear(DC.TT.GetTileLine(EC.tilePos, tPos));
    }
    Vector2Int GetTargetDir(Vector2 targetPos)
    {
        return new Vector2Int((int)Mathf.Sign(EC.rb.position.x - targetPos.x), (int)Mathf.Sign(EC.rb.position.y - targetPos.y) * -1);
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

    void FollowTarget()
    {
        float dist = Vector2.Distance(targetPos, EC.rb.position);
        float angle = DC.FF.GetAngle(EC.rb.position, targetPos);
        Vector2 vectorAngle = DC.FF.AngleToVector(angle);
        rb.linearVelocity = vectorAngle * speed * dist;
    }

}
