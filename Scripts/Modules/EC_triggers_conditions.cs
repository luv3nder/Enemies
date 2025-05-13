using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_triggers_conditions : MonoBehaviour
{
    public EnemyController EC;

    [Header("[0 - any, 1 - no, 2 - only]")]
    public int[] waterTrigs;
    public int[] tileDirtTrigs;

    [Header("___________________________ Not on effect")]
    public int[] notOnEffect;
    public int[] notOnEffectTrigs;

    [Header("___________________________ Not on buff")]
    public int[] notOnBuff;
    public int[] notOnBuffTrigs;

    [Header("___________________________ HP less than")]
    public int hpLess;
    public int[] hpLessTrigs;
    public bool CheckTrigs(int index)
    {
        Vector2Int etp = EC.tilePos;

        // water
        if (CheckHasTrig(index, waterTrigs))
        {

            if (EC.CheckInWater() || EC.DC.TT.watermap[etp.x, etp.y] != 0)
                return false;
        }

        // dirts
        if (CheckHasTrig(index, tileDirtTrigs))
        {
            if (EC.DC.TT.map[etp.x, etp.y] != 0)
                return false;
        }

        // effects
        if (CheckHasTrig(index, notOnEffectTrigs))
        {
            for (int i = 0; i < notOnEffect.Length; i++)
            {
                if (EC.PMS.accumOns[notOnEffect[i]])
                {
                    return false;
                }
            }
        }

        // buffs
        if (CheckHasTrig(index, notOnBuffTrigs))
        {
            for (int i = 0; i < notOnBuff.Length; i++)
            {
                if (EC.PMS.buffTimers[notOnBuff[i]] > 0)
                {
                    return false;
                }
            }
        }

        // hp less
        if (CheckHasTrig(index, hpLessTrigs))
        {
            if (EC.PMS.hitPoints / EC.PMS.maxHp > hpLess)
                    return false;
        }

        return true;
    }

    bool CheckHasTrig(int index, int[] trigArray)
    {
        for (int i = 0; i < trigArray.Length; i++)
        {
            if (index == trigArray[i])
                return true;
        }
        return false;
    }
}
