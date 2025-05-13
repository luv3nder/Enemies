using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_animation : MonoBehaviour
{
    public SpriteRenderer spren;
    public enemy_controller EC;
    [Header("0 (walk)")]    
    public Sprite[] spriteArray0;
    [Header("1 (idle)")]
    public Sprite[] spriteArray1;
    [Header("2 (attack)")]
    public Sprite[] spriteArray2;
    [Header("3 (air)")]
    public Sprite[] spriteArray3;
    [Header("4 (prepare)")]
    public Sprite[] spriteArray4;

    public bool[] looping;
    [Header("single frame time")]
    public float[] speed;

    public AudioSource stepStatic, flyStatic;
    public int stepStaticAnim;
    public AudioClip stepSound;

    public int animationId;
    public int[] stepIds;
    public bool isLooped;

    bool stepStaticOn, flyStaticOn;
    float stepStaticVol, flyStaticVol;

    int curSpriteId;

    float timer;
    void Start()
    {
        if (!spren)
        spren = GetComponent<SpriteRenderer>();

        if (!EC)
            EC = GetComponent<enemy_controller>();
    }
    public void SetAnim(int id, float animationSpeed)
    {
        if (GetSpriteArray(id) != null && GetSpriteArray(id).Length > 0)
        {
            if (animationSpeed != 0)
                speed[id] = animationSpeed;

            if (animationId != id)
            {
                isLooped = false;
                animationId = id;
                curSpriteId = 0;
                timer = 0;
                SwitchSprite();
            }
        }
    }

    void StepStaticVolume()
    {
        float speed = 5;

        if (GM.Inst.DC.IsPaused())
        {
            stepStatic.volume = 0;
            return;
        }

        if (stepStaticOn && !GM.Inst.DC.IsPaused())
        {
            if (stepStaticVol < 1)
                stepStaticVol += Time.deltaTime * speed;
            else
                stepStaticVol = 1;
        }
        else
        {
            if (stepStaticVol > 0)
                stepStaticVol -= Time.deltaTime * speed;
            else
                stepStaticVol = 0;
        }

        // apply
        stepStatic.volume = stepStaticVol;
    }

    void FlyStaticVolume()
    {
        float speed = 5;

        if (GM.Inst.DC.IsPaused())
        {
            flyStatic.volume = 0;
            return;
        }

        if (flyStaticOn && !GM.Inst.DC.IsPaused())
        {
            if (flyStaticVol < 1)
                flyStaticVol += Time.deltaTime * speed;
            else
                flyStaticVol = 1;
        }
        else
        {
            if (flyStaticVol > 0)
                flyStaticVol -= Time.deltaTime * speed;
            else
                flyStaticVol = 0;
        }

        // apply
        flyStatic.volume = flyStaticVol;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // tick
        if (timer > speed[animationId])
        {
            SwitchSprite();
            timer = 0;
        }

        if (stepStatic)
        {
            stepStaticOn = animationId == stepStaticAnim;

            StepStaticVolume();
        }

        if (flyStatic)
        {
            flyStaticOn = animationId == 3;
            FlyStaticVolume();
        }
    }

    void SwitchSprite()
    {
        Sprite[] curArray;

        switch (animationId)
        {
            default:
                curArray = spriteArray0;
                break;
            case 1:
                curArray = spriteArray1;
                break;
            case 2:
                curArray = spriteArray2;
                break;
            case 3:
                curArray = spriteArray3;
                break;
            case 4:
                curArray = spriteArray4;
                break;

        }

        if (curSpriteId < curArray.Length)
        {
            // set sprite
            spren.sprite = curArray[curSpriteId];


            // walk sound
            if (animationId == 0 && stepSound != null && !EC.CheckInWater())
            {
                for (int i = 0; i < stepIds.Length; i++)
                    if (curSpriteId == stepIds[i])
                    {
                        EC.AUDIO.PlaySound(0, stepSound);
                        //EC.DC.PR.PlaySound(stepSound, EC.rb.position);
                        break;
                    }

            }


            // next id
            if (curSpriteId + 1 == curArray.Length)
            {
                if (looping[animationId]) // loop check
                    curSpriteId = 0;
                else
                    isLooped = true;
            }
            else
            {
                curSpriteId++;
            }
        }
    }

    Sprite[] GetSpriteArray(int index)
    {
        switch (index)
        {
            default:
                return spriteArray0;
            case 1:
                return spriteArray1;
            case 2:
                return spriteArray2;
            case 3:
                return spriteArray3;
            case 4:
                return spriteArray4;

        }
    }
}
