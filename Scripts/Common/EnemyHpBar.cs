using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHpBar : MonoBehaviour
{
    dataController DC;
    CanvasGroup canvasGroup;
    RectTransform recttransf;

    public EnemyController EC;
    public hp_underbar underBar;

    public RectTransform barRect;
    public float yOffset;
    public Rigidbody2D parentRb;

    float fadeTimer;
    float prevHealth;

    private void Start()
    {
        DC = EC.DC;

        prevHealth = EC.PMS.hitPoints;
        canvasGroup = GetComponent<CanvasGroup>();
        recttransf = GetComponent<RectTransform>();
        transform.parent = DC.HC.TRANSF;

        float scale = DC.ST.hpBarsScaling;
        transform.localScale = new Vector3(scale, scale, 1);
    }
    private void Update()
    {
        float health = EC.PMS.hitPoints;
        float maxHealth = EC.PMS.maxHp;
        underBar.SetUnderbar(health, maxHealth);

        Fade();
    }

    private void FixedUpdate()
    {
        Movements();
        Scale();
    }
    void Movements()
    {
        if (parentRb != null)
        {
            Vector3 targetpos = parentRb.position + new Vector2(0, 0.15f);
            recttransf.position = DC.CC().cameraScript.cam.WorldToViewportPoint(targetpos + Vector3.up * yOffset) * new Vector2(DC.ST.resolution.x, DC.ST.resolution.y);
        }
        else
            Destroy(gameObject);
    }

    void Scale()
    {
        float health = EC.PMS.hitPoints;
        float maxHealth = EC.PMS.maxHp;

        if (health / maxHealth >= 0 && health / maxHealth <= 1)
            barRect.localScale = new Vector3(health / maxHealth, 1, 1);
    }

    void Fade()
    {
        float health = EC.PMS.hitPoints;

        if (prevHealth != health)
        {
            prevHealth = health;
            canvasGroup.alpha = 1;
            fadeTimer = 2;
        }
        else
        {
            fadeTimer -= Time.deltaTime;
            if (fadeTimer <= 1)
            {
                if (fadeTimer > 0)
                    canvasGroup.alpha = fadeTimer;
                else
                    canvasGroup.alpha = 0;
            }
        }
    }
}
