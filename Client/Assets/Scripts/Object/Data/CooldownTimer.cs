using UnityEngine;

// 冷却计时器，用于管理技能或行为的冷却时间
public class CooldownTimer
{
    public float CooldownTime { get; private set; }

    public float RemainingTime { get; private set; }

    public bool IsReady => RemainingTime <= 0;

    public CooldownTimer(float cooldownTime)
    {
        CooldownTime = cooldownTime;
        RemainingTime = 0;
    }

    public void StartCooldown()
    {
        RemainingTime = CooldownTime;
    }

    public void Update()
    {
        if (RemainingTime > 0)
        {
            RemainingTime -= Time.deltaTime;
        }
    }
} 