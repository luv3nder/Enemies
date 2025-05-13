using UnityEngine;

public class EC_audio: MonoBehaviour
{
    dataController DC;

    public enemy_controller EC;
    public AudioSource audioSource;
    public AudioClip customHitSound, customPrepareSound, customDeathSound;
    public bool noSound;

    [Header("_______________________ LOOP")]
    public static_source loopSound;
    [Header("0 - always, 1 - air")]
    public int loopId;

    [Header("_______________________ IDLE")]
    public AudioClip idleSound;
    [Header("[x - min, y - max]")]
    public Vector2 idleSoundReloads;
    public bool onlyOnIdle = true;
    float idleSoundTimer;

    int curSoundPriority;
    float soundHitTimer;

    private void Start()
    {
        DC = EC.DC;
    }
    private void Update()
    {
        if (audioSource && !audioSource.isPlaying)
            curSoundPriority = 0;

        IdleSounds();
        LoopSounds();
    }
    public void PlaySound(int priority, AudioClip clip)
    {
        if (priority >= curSoundPriority)
            audioSource.PlayOneShot(clip);
    }
    public void PlayHitSound(int priority, bool isSeparate, bool setTimer)
    {
        if (Time.time > soundHitTimer)
        {
            AudioClip curHitSound = DC.PP.hitSound;
            curHitSound = customHitSound != null ? customHitSound : curHitSound;

            if (setTimer)
                soundHitTimer = Time.time + 0.1f;

            if (!isSeparate && audioSource)
            {
                if (priority >= curSoundPriority)
                    audioSource.PlayOneShot(curHitSound);
            }
            else
                DC.PR.PlayHit(curHitSound, EC.rb.position, EC.tilePos);
        }
    }
    public void PlayPrepareSound()
    {
        if (customPrepareSound)
        DC.PR.PlaySound(customPrepareSound, EC.rb.position);
    }
    public void PlayDeathSound()
    {
        if (customDeathSound == null)
            SoundManager.Instance.PlaySound(12, transform.position);
        else
            DC.PR.PlaySound(customDeathSound, transform.position);
    }

    void LoopSounds()
    {
        if (loopSound)
        {
            if (loopId == 0)
                loopSound.PlaySound();

            else if (loopId == 1)
            {
                if (EC.isGrounded)
                    loopSound.StopSound();
                else
                    loopSound.PlaySound();
            }
        }
    }
    void IdleSounds()
    {
        if (idleSound && (!onlyOnIdle || !EC.STATES.CheckSight()))
        {
            idleSoundTimer -= Time.deltaTime;
            if (idleSoundTimer <= 0)
            {
                idleSoundTimer = idleSoundReloads.x + DC.FF.TrueRandom(idleSoundReloads.y - idleSoundReloads.x);
                DC.PR.PlaySound(idleSound, EC.rb.position);
            }
        }
    }
}
