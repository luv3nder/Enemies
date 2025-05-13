using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHands : MonoBehaviour
{
    public EnemyController EC;
    public SpriteRenderer spren;

    dataController DC;
    pos_animation PA;
    public Transform tip;
    public float defaultAngle = Mathf.PI / 2;
    public float rotationSpeed = 5, dashPower, returnSpeed = 0.95f, delayTime;
    public Vector2 startEndAcceleration;
    public float accelerationDistanceLimit;
    public bool noDash;
    Vector2 defaultPos, defaultOffset;
    float accValue;
    float curAngle;

    float timer, prepareTimer, delayTimer;

    public float targetAngle;
    public bool isDone;
    public Sprite defaultSprite;
    public int actionIndex = 0;

    void Start()
    {
        DC = EC.DC;
        PA = EC.PA;
        EC.HANDS = this;

        if (EC.isCopy)
            this.enabled = false;

        if (!spren)
            spren = GetComponent<SpriteRenderer>();

        defaultSprite = spren.sprite;

        curAngle = Mathf.PI / 2;

        defaultOffset = (Vector2)transform.position - EC.rb.position;

        actionIndex = 4;
    }
    void SetDegreesAngle(float angle)
    {
        transform.eulerAngles = new Vector3(0.0f, 0.0f, -EC.direction * ((angle * 180f / Mathf.PI) - 90));
    }
    void Update()
    {
        if (DC.CC().CHAR != null)
        {
            // default pos
            if (PA != null)
                defaultPos = EC.transform.position + PA.offsets[0];
            else
                defaultPos = (Vector2)EC.transform.position + defaultOffset * new Vector2(EC.direction, 1);

            switch (actionIndex)
            {
                case 1: // rotate to
                    prepareTimer -= Time.deltaTime;

                    float absAngle2 = Mathf.Abs(targetAngle);
                    float angleDif2 = absAngle2 - curAngle;

                    if (prepareTimer > 0)
                    {
                        if (Mathf.Abs(angleDif2) > rotationSpeed * Time.deltaTime * 2)
                        {
                            curAngle += Mathf.Sign(angleDif2) * rotationSpeed * Time.deltaTime;
                        }
                    }
                    else
                    {
                        actionIndex = noDash ? 0 : 2; // start dash
                        DC.FF.ChatEnemyData(actionIndex.ToString(), 2, 0);
                        curAngle = absAngle2;
                    }

                    SetDegreesAngle(curAngle);
                    transform.position = defaultPos;
                    break;
                case 2:
                    Dash();
                    break;
                case 3:
                    DashReturn();
                    return;

                case 4: // rotate back to default
                    targetAngle = defaultAngle;
                    float absAngle = Mathf.Abs(targetAngle);
                    float angleDif = absAngle - curAngle;

                    if (Mathf.Abs(angleDif) > rotationSpeed * Time.deltaTime * 2)
                    {
                        curAngle += Mathf.Sign(angleDif) * rotationSpeed * Time.deltaTime;

                        SetDegreesAngle(curAngle);
                        transform.position = defaultPos;
                    }
                    else
                    {
                        actionIndex = 0;
                        curAngle = absAngle;

                        if (PA != null)
                        {
                            PA.isOn[0] = true;
                            PA.curSteps[0] = 0;
                        }
                    }
                    break;
                case 0:
                    {
                        targetAngle = defaultAngle;
                        SetDegreesAngle(targetAngle);
                        transform.position = defaultPos;
                        break;
                    }
            }
        }
    }
    void DashReturn()
    {
        if (accValue > 0.01f)
        {
            Vector2 vectorDirection = new Vector2(Mathf.Sin(targetAngle), Mathf.Cos(targetAngle));
            transform.position = defaultPos + vectorDirection * accValue;
            accValue *= returnSpeed;
        }

        else
        {
            actionIndex = 4;
            DC.FF.ChatEnemyData(actionIndex.ToString(), 2, 0);
            accValue = 0;
        }
    }
    public void SetDash(float angle, float prepare, float delay)
    {

        if (PA != null)
        PA.isOn[0] = false;

        targetAngle = angle;
        isDone = false;
        actionIndex = 1;

        if (delay == 0)
            delayTimer = delayTime;
        else
            delayTimer = delay;

        prepareTimer = prepare;
        timer = 0;
        accValue = 0;
    }
    public void HandsBreak()
    {
        timer = 0;
        targetAngle = defaultAngle;
        transform.position = defaultPos;
        actionIndex = 0;
        accValue = 0;
    }
    void Dash()
    {
        timer += Time.deltaTime;

        Vector2 vectorDirection = DC.FF.AngleToVector(Mathf.Abs(targetAngle)) * new Vector2(EC.direction, 1);

        if (Mathf.Abs(accValue) < Mathf.Abs(accelerationDistanceLimit))
        {
            transform.position = defaultPos + vectorDirection * accValue;
            accValue = startEndAcceleration.x + startEndAcceleration.y * timer;
        }
        else
        {
            accValue = accelerationDistanceLimit;

            // rb dash
            if (!isDone)
            {
                isDone = true;
                EC.rb.AddForce(dashPower * vectorDirection);
            }


            delayTimer -= Time.deltaTime;

            if (delayTimer < 0)
            {
                actionIndex = 3; // return
                DC.FF.ChatEnemyData(actionIndex.ToString(), 2, 0);
            }
        }
    }
}
