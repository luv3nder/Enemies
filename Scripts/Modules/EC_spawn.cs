using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_spawn : MonoBehaviour
{
    public int trigId;
    public enemy_controller EC;

    [Header("(!) ADD THIS TO EC > SPAWN FOR ON DEATH")]

    public int enemyId;
    public int number;
    public AudioClip prepSound, spawnSound;
    public GameObject[] minionsArray;

    public ParticleSystem prepEffect;
    public float delay;

    [Header("_________________Position")]
    public Vector2 posOffset, startVlc;
    public float chance, angle, angleRange, spawnOffset;

    float timer;

    void Update()
    {
        if (TriggerCheck())
        {
            if (MinionsArraySpaceCheck() <= 0)
            {
                TrigExit();
                return;
            }


            if (timer == 0)
            {
                if (prepSound)
                    EC.DC.PR.PlaySound(prepSound, GetPos());
                if (prepEffect)
                    prepEffect.Play();
            }

            timer += Time.deltaTime;

            if (timer > delay)
            TrigSpawn();
        }
    }

    bool TriggerCheck()
    {
        return EC && trigId != -1 && trigId == EC.trigId;
    }

    void TrigExit()
    {
        timer = 0;
        EC.trigId = -1;
    }

    public void TrigSpawn()
    {
        for (int i = 0; i < number; i++)
        {
            if (MinionsArraySpaceCheck() > 0 && (chance == 0 || chance == 100 || EC.DC.FF.TrueRandom(100) < chance))
            {
                float curRange = EC.DC.FF.TrueRandom(angleRange);
                float curAngle = angle + curRange - angleRange / 2;

                Vector2 posOffset = EC.DC.FF.AngleToVector(curAngle) * spawnOffset;

                GameObject tempo = Instantiate(EC.DC.PP.enemyPrefabs[enemyId - 1], GetPos() + posOffset, Quaternion.identity);
                enemy_controller curEC = tempo.GetComponent<enemy_controller>();
                curEC.enemyId = enemyId;
                curEC.rb.linearVelocity = startVlc;
                curEC.DROP.drops = new Vector3Int[0];             

                AddMinionToArray(tempo);
            }
        }

        if (spawnSound)
            EC.DC.PR.PlaySound(spawnSound, EC.rb.position);

        TrigExit();
    }
    void AddMinionToArray(GameObject tempo)
    {
        for (int i = 0; i < minionsArray.Length; i++)
        {
            if (minionsArray[i] == null)
            {
                minionsArray[i] = tempo;
                break;
            }
        }
    }
    int MinionsArraySpaceCheck()
    {
        int result = 0;
        for (int i = 0; i < minionsArray.Length; i++)
        {
            if (minionsArray[i] == null)
                result++;
        }
        return result;
    }





    public void SpawnToArray(dataController DC)
    {
        for (int i = 0; i < number; i++)
        {
            if (chance == 0 || chance == 100 || DC.FF.TrueRandom(100) < chance)
            {
                float curRange = DC.FF.TrueRandom(angleRange);
                float curAngle = angle + curRange - angleRange / 2;

                Vector2 posOffset = DC.FF.AngleToVector(curAngle) * spawnOffset;

                DC.PR.EnemySpawn(GetPos() + posOffset, enemyId - 1, false, DC.TT.NightCheck() ? 1 : 0, true);
            }
        }
    }

    Vector2 GetPos()
    {
        return (Vector2)transform.position + posOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + posOffset, 0.02f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere((Vector2)transform.position + posOffset, spawnOffset);
    }
}
