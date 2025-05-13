using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_teleport : MonoBehaviour
{
    public dataController DC;
    public EnemyController EC;
    public EC_beh_walk EW;
    public ParticleSystem prepareEffect;
    public AudioClip tpSound;
    public GameObject teleportPrefab, preparePrefab;
    public float prepareTime = 0.5f, delayTime = 0.2f, yOffset;
    public int trigId;

    public Color tpColor;
    public Gradient tpGradient;

    [Header("[tp other]")]
    public bool tpOther;
    public bool tpOnLava = true;
    public float tpOtherRadius;
    bool isOn, isTeleporting;
    float timer;

    Vector3Int teleportPos;

    private void Start()
    {
        if (EC.isCopy)
        {
            this.enabled = false;
        }
    }

    [Header("x - min, y - max")]
    public Vector2Int radius;
    bool TriggerCheck()
    {
        return trigId == EC.trigId;
    }
    void Update()
    {
        if (tpOnLava)
        {
            if (EC.CheckInLava())
            {
                EC.targetRb = EC.rb;
                EC.liqIndex = 0;
                SetTP();
            }
        }

        // trig switch
        if (TriggerCheck() && !isOn)
        {
            isOn = true;
            PrepareFx(true);

            timer = 0;
        }

        // action
        Action();
    }

    public void PrepareFx(bool isOG)
    {
        if (DC.isMultiplayer && isOG)
            DC.NMI.EnemyFxServerRpc(DC.SID(), EC.arrayId, new Vector3Int(4, trigId, 0));

        if (prepareEffect)
            prepareEffect.Play();
    }

    void Action()
    {
        timer += Time.deltaTime;

        // tp
        if (isTeleporting)
        {
            EC.rb.bodyType = RigidbodyType2D.Kinematic;
            if (timer > delayTime)
                Tp();
        }
        else
        {
            EC.rb.bodyType = RigidbodyType2D.Dynamic;

            // on
            if (isOn)
            {
                if (EC.targetRb && timer > prepareTime)
                    SetTP();
                else
                    EC.STATES.stateTimers[3] = prepareTime; // no move
            }
        }
    }

    void SetTP()
    {
        timer = 0;

        if (!EC.targetRb)
        {
            isOn = false;
            EC.trigId = -1;
            DC.FF.ChatEnemyData("tp no target", 10, 6);
            return;
        }

        // tp pos find
        teleportPos = (Vector3Int)FindSpot(EC.targetRb.position);

        if (teleportPos != Vector3Int.zero)
        {
            isTeleporting = true;

            // fx
            Vector2 tppos = DC.TT.GetWorldPos(teleportPos);
            TeleportStartFx(true, EC.rb.position, tppos);

            EC.rb.position = DC.TT.GetWorldPos(new Vector3Int(10, 10));

            DC.FF.ChatEnemyData("tp found", 10, 0);
        }

        // no pos found
        else
        {
            isOn = false;
            EC.trigId = -1;
            DC.FF.ChatEnemyData("tp not found", 10, 8);
        }
    }
    public void TeleportStartFx(bool isOG, Vector2 from, Vector2 to)
    {
        if (DC.isMultiplayer && isOG)
            DC.NMI.EnemyTpServerRpc(DC.SID(), EC.arrayId, from, to);

        DC.FF.TeleportEffectSpawn(from, Vector2.zero, teleportPrefab, tpColor, tpGradient, tpSound);
        DC.FF.TeleportEffectSpawn(to, Vector2.zero, preparePrefab, tpColor, tpGradient, null);
    }

    public void TeleportEndFx(bool isOG, Vector2 to)
    {
        if (DC.isMultiplayer && isOG)
            DC.NMI.EnemyTpServerRpc(DC.SID(), EC.arrayId, Vector2.zero, to);

        DC.FF.TeleportEffectSpawn(to, Vector2.zero, teleportPrefab, tpColor, tpGradient, tpSound);
    }

    void Tp()
    {
        isTeleporting = false;

        Rigidbody2D curRb = EC.rb;

        Vector2 pos = DC.TT.GetWorldPos(teleportPos);        

        curRb.position = pos + Vector2.up * yOffset;
        TeleportEndFx(true, curRb.position);

        EC.direction = (int)Mathf.Sign(EC.targetRb.position.x - EC.rb.position.x);

        if (EW)
            EW.lastGroundedY = pos.y;

        curRb.linearVelocity = Vector2.zero;

        isOn = false;
        EC.trigId = -1;
    }

    Rigidbody2D FindEnemy(Vector2 pos, float size)
    {
        Collider2D[] hitCols = Physics2D.OverlapCircleAll(pos, size, DC.PP.allHitMask);

        // apply
        for (int i = 0; i < hitCols.Length; i++)
        {

            if (hitCols[i] != null)
            {
                EnemyController EC = hitCols[i].GetComponent<EnemyController>();
                if (EC && EC != this.EC && EC.PMS.isTeleportable)
                    return EC.rb;
            }
        }
        return null;
    }
    public Vector2Int FindSpot(Vector2 pos)
    {
        Vector2Int tPos = (Vector2Int)DC.TT.GetTilePos(pos, false);
        int[] rxo = DC.FF.ReturnRandomOrder(radius.y * 2 + 1, null); // random x order

        Vector2Int curRanges = !EC.CheckFrighten() ? radius : new Vector2Int(10, 20); 

        for (int y = -curRanges.y; y <= curRanges.y; y++)
        {
            for (int i = 0; i < rxo.Length; i++)
            {
                int x = rxo[i] - curRanges.y; // (half offset)
                Vector2Int cPos = tPos + new Vector2Int(x, y);

                if (DC.TT.QIMR(cPos) && DC.TT.watermap[cPos.x, cPos.y] == 0 && DC.TT.activemap[cPos.x, cPos.y] == 0 && DC.TT.TTobjs.ObjT(cPos.x, cPos.y, 0))
                    return cPos;
            }
        }

        return Vector2Int.zero;
    }
}
