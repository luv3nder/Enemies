using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_jump : MonoBehaviour
{
    public enemy_controller EC;

    public int trigId;

    public ParticleSystem prepareEffect;
    public GameObject landedPrefab;
    public Transform landedTarget, landFxTarget;
    public AudioClip jumpSound, landSound;

    public float delayTime, jumpPower, jumpAngle, knockBack;
    public int lavaBreakDistance = 6;

    [Header("___________________________ LAND")]
    public bool landedHit;
    public float landAttMulti, landKnock;

    [Header("___________________________ JUMP")]
    public bool jumpContact;
    public float jumpAttMulti, jumpKnock;

    [Header("[x - range, y - time")]
    public Vector2 screenShake;
    public Vector2 hitSize;

    bool isGrounded;
    bool isOn;
    float timer;
    float lastGroundedTime;

    void Start()
    {
        isGrounded = true;
    }

    void Update()
    {
        if (TriggerCheck() && !isOn)
        {
            EC.ECA.SetAnim(4, 0);
            EC.trigId = -1;
            isOn = true;
        }

        if (EC.STATES.CheckStun())
            TurnOff();

        // jump
        if (isOn)
        {
            timer += Time.deltaTime;

            // action
            if (NoLavaCheck())
            {
                if (isGrounded && timer > delayTime)
                    Jump();
                else
                    EC.STATES.stateTimers[3] = 1; // [3] no move
            }
            else
                TurnOff();
        }

        // land timer
        if (isGrounded)
            lastGroundedTime = Time.time;

        // jump contact
        else if (jumpContact)
            EC.CONTACT.SetContact(jumpAttMulti, jumpKnock, Vector2Int.zero, 0);

        // landed hit
        if (isGrounded != EC.isGrounded)
        {
            if (landedHit && !isGrounded && Time.time - lastGroundedTime > 0.3f)
                LandedHit();

            TurnOff();
            isGrounded = EC.isGrounded;
        }
    }

    bool NoLavaCheck()
    {
        for (int i = 0; i < lavaBreakDistance; i++)
            if (EC.DC.FF.CheckLava(EC.rb.position, i * EC.direction, false, false))
            {
                //DC.FF.ChatTest("lava x: " + i, 2);
                return false;
            }
        return true;
    }
    void Jump()
    {
        EC.colIndex = 0;
        EC.isGrounded = false;
        isGrounded = false;

        EC.ECA.SetAnim(3, 0);
        EC.STATES.stateTimers[3] = 1;

        if (jumpContact)
            EC.CONTACT.RefreshColArray();

        PrepareFx(true);

        EC.STATES.stateTimers[0] = 0.25f; // [0] jump reload
        EC.STATES.SetJump(0.25f);

        Vector2 vlc = EC.rb.linearVelocity;
        EC.rb.linearVelocity = new Vector2(vlc.x, 0) + EC.DC.FF.AngleToVector(jumpAngle * EC.direction) * jumpPower;
    }

    public void PrepareFx(bool isOG)
    {
        if (EC.DC.isMultiplayer && isOG)
            EC.DC.NMI.EnemyFxServerRpc(EC.DC.SID(), EC.arrayId, new Vector3Int(3, trigId, 0));

        if (jumpSound)
            EC.DC.PR.PlaySound(jumpSound, landedTarget.position);

        if (prepareEffect)
            prepareEffect.Play();
    }
    public void AttackFx(bool isOG)
    {
        if (EC.DC.isMultiplayer && isOG)
            EC.DC.NMI.EnemyFxServerRpc(EC.DC.SID(), EC.arrayId, new Vector3Int(3, trigId, 0));

        EC.DC.PR.PlaySound(landSound, landedTarget.position);
        EC.DC.PR.SetShake(landedTarget.position, screenShake);

        if (landedPrefab)
        {
            GameObject tempo = Instantiate(landedPrefab, landFxTarget.position, Quaternion.identity);
            tempo.transform.parent = GM.Inst.effectsTransform;
        }
    }

    void LandedHit()
    {
        float multi = Time.time - lastGroundedTime;
        EC.DC.FF.HitAll(landedTarget.position, hitSize * multi, 0.3f, EC.PMS.attDamage, false, knockBack, 0, EC.PMS.behaviourId, 0, 0, Vector2Int.zero, EC.arrayId, EC.DC.PP.allHitMask);

        AttackFx(true);
    }
    bool TriggerCheck()
    {
        return trigId == EC.trigId;
    }


    void TurnOff()
    {
        isOn = false;
        timer = 0;
        EC.STATES.stateTimers[1] = 0; // att turn off 
        EC.STATES.stateTimers[3] = 0; // stop turn off

        if (prepareEffect)
            prepareEffect.Stop();
    }

    private void OnDrawGizmos()
    {
        if (landedTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(landedTarget.position, hitSize);
        }
    }
}
