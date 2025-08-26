using UnityEngine;

/// <summary>
/// 其他玩家角色
/// </summary>
public class Player : CombatEntity
{
    // 玩家状态属性
    private float _currentHunger;             // 当前饥饿值
    private float _currentSanity;             // 当前理智值
    
    // 重写DamageableObject的抽象属性
    public override float MaxHealth => GameSettings.PlayerMaxHealth;
    public override float Defense => base.Defense;
    public override bool CanInteract => _currentHealth > 0;
    public override float GetInteractionRange() => 2f;
    
    // 玩家状态属性
    public float MaxHunger => GameSettings.PlayerMaxHunger;
    public float CurrentHunger => _currentHunger;
    public float MaxSanity => GameSettings.PlayerMaxSanity;
    public float CurrentSanity => _currentSanity;

    // 重写OnClick方法实现玩家点击逻辑
    public override void OnClick(Vector3 clickPosition)
    {
        Debug.Log($"Player {gameObject.name} clicked");
    }

    protected override void Awake()
    {
        base.Awake();
        
        // 从NavMeshAgent获取移动速度（如果存在的话）
        if (_navMeshAgent != null)
        {
            _moveSpeed = _navMeshAgent.speed;
        }
        else
        {
            // 如果没有NavMeshAgent，使用默认速度
            _moveSpeed = 5f;
        }
        
        // 初始化玩家状态属性
        _currentHunger = MaxHunger;
        _currentSanity = MaxSanity;
        
        SetObjectType(ObjectType.Player);
    }

    protected override void Update()
    {
        base.Update();
        UpdateMovementRotation();
    }

    /// <summary>
    /// 处理移动朝向 - 朝向移动方向
    /// </summary>
    private void UpdateMovementRotation()
    {
        if (_navMeshAgent == null || !_navMeshAgent.hasPath) return;
        
        // 获取移动方向
        Vector3 moveDirection = _navMeshAgent.velocity.normalized;
        if (moveDirection != Vector3.zero)
        {
            // 只考虑水平方向
            moveDirection.y = 0;
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                float rotationSpeed = 15f;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    /// <summary>
    /// 重写攻击实现，玩家只能通过装备造成伤害
    /// </summary>
    public override void PerformAttack(IDamageable target)
    {
        // 玩家不能直接攻击，必须通过装备
    }

    /// <summary>
    /// 设置饥饿值
    /// </summary>
    public void SetHunger(float hunger)
    {
        _currentHunger = Mathf.Clamp(hunger, 0, MaxHunger);
    }

    /// <summary>
    /// 设置理智值
    /// </summary>
    public void SetSanity(float sanity)
    {
        _currentSanity = Mathf.Clamp(sanity, 0, MaxSanity);
    }
    
    /// <summary>
    /// 获取当前手部装备
    /// </summary>
    private EquipBase GetCurrentHandEquip()
    {
        return _equips.Find(equip => equip.EquipPart == EquipPart.Hand);
    }

    /// <summary>
    /// 朝向指定位置
    /// </summary>
    public void LookAt(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        directionToTarget.y = 0;
        
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = targetRotation;
        }
    }
} 