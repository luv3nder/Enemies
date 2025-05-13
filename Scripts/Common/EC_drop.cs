using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_drop : MonoBehaviour
{
    [HideInInspector] public EnemyController EC;
    dataController DC;
    public Vector3Int alwaysDrops;
    [Header("(item id, quantity, chance)")]
    [Tooltip("1 - spears \n 6 - grenades \n 11 - arrows \n 16 - plants \n 21 - potions \n 26 - spells \n 31 - food \n 36 - amulets \n 41 - tools \n 46 - ores \n 51 - resourses \n 56 - gems \n 61 - food \n 66 - heavy armr \n 71 - light armr \n 76 - mid armr \n 81 - rings \n 86 - crossbows \n 91 - swords \n 96 - tomes \n 101 - spells \n 106 - potions \n 111 - other \n 116 - hats")]
    public Vector3Int[] drops;
    public int maxItems = 1;

    void Start()
    {
        DC = GM.Inst.DC;

        if (maxItems == 0)
            maxItems = 2;
    }

    public void Drop(EnemyController EC)
    {
        this.EC = EC;

        // random order
        int[] random = DC.FF.ReturnRandomOrder(drops.Length, null);
        int count = 0;

        // always
        if (alwaysDrops != Vector3Int.zero && (alwaysDrops.z == 0 || alwaysDrops.z == 10 || DC.FF.TrueRandom(100) < alwaysDrops.z))
        {
            float angle = 0;
            // cores
            if (DC.IF.GetItemDropFly(alwaysDrops))
                angle = 0.001f;

            bool isRandom = angle == 0;
            DC.II.Drop(new Vector3Int(alwaysDrops.x, alwaysDrops.y, 0), EC.rb.position, 0, angle, DC.II.dropSpeed, isRandom);
        }

        // 100%
        List<int> hundreds = new List<int>();
        for (int i = 0; i < drops.Length; i++)
        {
            if (drops[i].z == 10)
            {
                hundreds.Add(i);
            }
        }

        if (hundreds.Count > 0)
            while (hundreds.Count > 0 && count < maxItems)
            {
                int id = (int)DC.FF.TrueRandom(hundreds.Count);
                int curId = hundreds[id];
                DC.II.Drop(new Vector3Int(drops[curId].x, drops[curId].y, 0), EC.rb.position, 0, EC.direction, DC.II.dropSpeed, true);
                hundreds.Remove(hundreds[id]);
                count++;
            }


        // other
        for (int i = 0; i < drops.Length; i++)
        {
            if (count >= maxItems)
                return;

            int randomId = random[i];

            if (drops[randomId].z != 10)
            {
                // chance id
                int chanceId = drops[randomId].z;
                float curChance = 99;

                // get real chance
                if (chanceId < DC.ST.dropChances.Length)
                    curChance = DC.ST.dropChances[chanceId];

                // modify
                float luckMulti = (1 + DC.CC().Skill_D4_luck() * 0.1f) * (DC.CR.CheckLuck(DC.CC().buffTimers) ? 1.3f : 1);
                curChance *= DC.ST.edropMulti * luckMulti;

                float mSpawnMulti = EC.manualSpawned ? 0.5f : 1;

                // drop
                if (DC.FF.TrueRandom(100) < curChance * mSpawnMulti)
                {
                    bool isRecipe = DC.FF.TrueRandom(100) < 70 && DC.IF.ItemIsRecipe(drops[randomId].x) && !EC.PMS.isBoss;

                    Vector3Int value = new Vector3Int(drops[randomId].x, drops[randomId].y, 0);
                    if (isRecipe)
                        value = new Vector3Int(DC.IF.blueprint, 1, drops[randomId].x);

                    if (DC.IF.GetItemDropFly(value)) // cores
                        DC.II.Drop(value, EC.rb.position, 0, 0, DC.II.dropSpeed * 2, false);
                    else // normal
                        DC.II.Drop(value, EC.rb.position, 0, EC.direction, DC.II.dropSpeed, true);

                    count++;

                    // rare dropped
                    if (chanceId <= 3)
                        count = maxItems;
                }
            }
        }
    }
}
