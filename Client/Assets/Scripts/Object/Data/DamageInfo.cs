using UnityEngine;

// 伤害信息结构体，包含伤害计算和处理所需的所有数据
public struct DamageInfo
{
    public float Damage;

    public DamageType Type;

    public Vector3 HitPoint;

    public IAttacker Source;

    public Vector3 Direction;
} 