using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EC_beh_bird : MonoBehaviour
{
    dataController DC;
    Rigidbody2D rb;
    EnemyController EC;
    EC_animation ECA;

    public AudioSource flyAudio;

    public float speed;
    public float timeToChange;
    public int curStateIndex; 
    public float animationSpeed;

    float angle;
    float changeTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        DC = GM.Inst.DC;
        EC = GetComponent<EnemyController>();
        ECA = GetComponent<EC_animation>();
        // down
        SwitchState(0, 0);
    }
    void Update()
    {
        changeTime += Time.deltaTime;

        Movements();
        Animations();
    }
    void Animations()
    {
        switch (curStateIndex)
        {
            case 2: // idle
                ECA.SetAnim(1, 0);
                if (flyAudio.isPlaying)
                    flyAudio.Stop();
                break;
            default: // fly
                ECA.SetAnim(3, 0);
                if (!flyAudio.isPlaying)
                    flyAudio.Play();
                break;
        }
    }
    public bool CheckHeadGrounded() //0.2f
    {
        return Physics2D.OverlapCircle(transform.position + new Vector3(0,0.2f,0), 0.5f, DC.PP.groundLayerMask);
    }
    void Movements()
    {
        switch (curStateIndex)
        {
            case 0: // fly down
                FlyDown();
                break;

            case 1: // fly up
                FlyUp();
                break;

            case 2: // sit
                Sit();
                break;
        }
    }

    void FlyUp()
    {
        ECA.SetAnim(3, 0);
        float radius = 0.1f;
        int direction = 1;
        float curAngle;

        // hit direction
        if (changeTime > 0.2f)
        {
            // bounce off walls
            if (Physics2D.Raycast(transform.position, new Vector2(-1, 1), radius, DC.PP.groundLayerMask) && rb.linearVelocity.x < 0)
            {
                angle = -(Mathf.PI / 4) * 3;
                rb.linearVelocity = new Vector2(speed * Mathf.Sin(angle), speed * Mathf.Cos(angle)) / 3;
                changeTime = 0;
            }

            else if (Physics2D.Raycast(transform.position, new Vector2(1, 1), radius, DC.PP.groundLayerMask) && rb.linearVelocity.x > 0)
            {
                angle = (Mathf.PI / 4) * 3;
                rb.linearVelocity = new Vector2(speed * Mathf.Sin(angle), speed * Mathf.Cos(angle)) / 3;
                changeTime = 0;
            }

            else if (Physics2D.Raycast(transform.position, new Vector2(-1, 0), radius, DC.PP.groundLayerMask))
            {
                angle = Mathf.PI / 4;
                rb.linearVelocity = new Vector2(speed * Mathf.Sin(angle), speed * Mathf.Cos(angle)) / 3;
                changeTime = 0;
            }

            else if (Physics2D.Raycast(transform.position, new Vector2(1, 0), radius, DC.PP.groundLayerMask))
            {
                angle = -Mathf.PI / 4;
                rb.linearVelocity = new Vector2(speed * Mathf.Sin(angle), speed * Mathf.Cos(angle)) / 3;
                changeTime = 0;
            }
            //
        }
        // fly direction
        if (changeTime > 0.1f && rb.linearVelocity.y < 0 && !Physics2D.OverlapCircle(transform.position, radius, DC.PP.groundLayerMask))
        {
            angle = Mathf.PI / 4;
            direction = Random.Range(-1, 2);
        }

        curAngle = angle * direction;

        Vector2 vlcInput = new Vector2(speed * Mathf.Sin(curAngle), speed * Mathf.Cos(curAngle)) * Time.deltaTime * 4;

        // inertia
        float maxSpeed = speed;
        Vector2 vlc = rb.linearVelocity += vlcInput;

        rb.linearVelocity = new Vector2(Mathf.Abs(vlc.x) > maxSpeed ? maxSpeed * Mathf.Sign(vlc.x) : vlc.x, Mathf.Abs(vlc.y) > maxSpeed ? maxSpeed * Mathf.Sign(vlc.y) : vlc.y);
        EC.direction = (int)Mathf.Sign(rb.linearVelocity.x);
    }
    void FlyDown()
    {
        Vector2 vlcInput = Vector2.zero;

        ECA.SetAnim(3, 0);
        // freak out 
        if (EC.STATES.stateTimers[4] > 0)
            SwitchState(1, EC.direction);

        if (!EC.STATES.CheckGrounded())
        {
            vlcInput = new Vector2(speed * EC.direction / 2, -speed);
        }
        else
            SwitchState(2, 0);

        rb.linearVelocity = vlcInput;
    }
    void Sit()
    {
        ECA.SetAnim(1, 0);
        if (EC.STATES.stateTimers[4] > 0) // freak out
            SwitchState(1, (int)Mathf.Sign(rb.position.x - DC.CC().CHAR.rb.position.x));

        rb.linearVelocity = Vector2.zero;
    }
    void SwitchState(int id, int direction)
    {
        curStateIndex = id;
        changeTime = 0;
        angle = Random.Range(0.5f, 1f);

        if (direction == 0)
            EC.direction = Random.Range(0, 100) < 50 ? 1 : -1;
        else
            EC.direction = direction;


    }
}


