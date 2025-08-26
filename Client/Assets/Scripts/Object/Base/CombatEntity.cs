using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Reflection;

// 战斗实体基类，提供攻击能力、装备系统、移动控制、对话系统和动画管理
public abstract class CombatEntity : DamageableObject, IAttacker
{
    protected float _baseAttack = 10f;
    protected float _baseDefense = 0f;    // 基础防御值
    protected float _attackCooldown = 1f;
    protected float _moveSpeed = 5f;
    [SerializeField] protected Transform _handPoint;           // 手部挂载点
    [SerializeField] protected SkinnedMeshRenderer _headMesh; // 头部渲染器
    [SerializeField] protected SkinnedMeshRenderer _bodyMesh; // 身体渲染器
    protected List<EquipBase> _equips = new List<EquipBase>();  // 装备列表

    // 对话系统相关
    protected float _dialogRange;            // 对话范围
    protected int[] _availableDialogIds;     // 可用对话ID列表
    protected float _lastDialogTime;         // 上次对话时间
    protected float _dialogCooldown = 5f;    // 对话冷却时间（秒）
    protected int _currentDialogId = -1;     // 当前对话框ID

    protected CooldownTimer _attackTimer;
    protected NavMeshAgent _navMeshAgent;
    
    // 动画系统相关
    protected Animator _animator;
    protected RuntimeAnimatorController _baseAnimatorController; // 基础动画控制器

    public float BaseAttack => _baseAttack;
    public bool CanAttack => _attackTimer.IsReady;
    public Transform HandPoint => _handPoint;            // 提供手部节点访问
    public SkinnedMeshRenderer HeadMesh => _headMesh;   // 提供头部渲染器访问
    public SkinnedMeshRenderer BodyMesh => _bodyMesh;   // 提供身体渲染器访问

    // 移动相关属性
    public float MoveSpeed => _moveSpeed;
    public bool IsMoving => _navMeshAgent != null && _navMeshAgent.hasPath && _navMeshAgent.remainingDistance > 0.1f;
    public bool HasNavMeshAgent => _navMeshAgent != null;
    public Vector3 Destination => _navMeshAgent != null && _navMeshAgent.hasPath ? _navMeshAgent.destination : transform.position;

    // 计算总攻击力
    public virtual float TotalAttack
    {
        get
        {
            float total = _baseAttack;
            foreach (var equip in _equips)
            {
                total += equip.GetAttackBonus();
            }
            return total;
        }
    }

    // 计算总防御力
    public override float Defense
    {
        get
        {
            float total = _baseDefense; // 使用自己的基础防御值而不是base.Defense
            foreach (var equip in _equips)
            {
                total += equip.GetDefenseBonus();
            }
            return total;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        // 默认把战斗实体归类为Monster，子类如Player可在Awake里覆盖
        SetObjectType(ObjectType.Monster);
        _attackTimer = new CooldownTimer(_attackCooldown);
        
        // 初始化NavMeshAgent
        InitializeNavMeshAgent();
        
        // 初始化动画系统
        InitializeAnimationSystem();
    }

    protected virtual void Update()
    {
        _attackTimer.Update();
    }

    protected virtual void OnValidate()
    {
        // 检查装备列表中是否有重复部位的装备
        Dictionary<EquipPart, int> partCounts = new Dictionary<EquipPart, int>();
        List<EquipPart> duplicateParts = new List<EquipPart>();
        
        foreach (var equip in _equips)
        {
            if (equip == null) continue;
            if (partCounts.ContainsKey(equip.EquipPart))
            {
                partCounts[equip.EquipPart]++;
            }
            else
            {
                partCounts[equip.EquipPart] = 1;
            }
        }
        
        foreach (var kvp in partCounts)
        {
            if (kvp.Value > 1)
            {
                duplicateParts.Add(kvp.Key);
            }
        }

        if (duplicateParts.Count > 0)
        {
            string duplicatePartsStr = string.Join(", ", duplicateParts);
            Debug.LogError($"CombatEntity has multiple equipment in parts: {duplicatePartsStr}");
            
            List<EquipBase> newEquips = new List<EquipBase>();
            HashSet<EquipPart> addedParts = new HashSet<EquipPart>();
            
            foreach (var equip in _equips)
            {
                if (equip == null) continue;
                if (!addedParts.Contains(equip.EquipPart))
                {
                    newEquips.Add(equip);
                    addedParts.Add(equip.EquipPart);
                }
            }
            
            _equips = newEquips;
        }
    }

    public virtual void PerformAttack(IDamageable target)
    {
        if (!CanAttack || target == null) return;

        // 创建伤害信息
        var damageInfo = new DamageInfo
        {
            Damage = TotalAttack,  // 使用总攻击力
            Type = DamageType.Physical,
            HitPoint = transform.position + transform.forward,
            Direction = transform.forward,
            Source = this
        };

        // 开始冷却
        _attackTimer.StartCooldown();
    }

    public virtual bool Equip(int equipId)
    {
        // 获取装备配置
        var equipReader = ConfigManager.Instance.GetReader("Equip");
        if (equipReader == null || !equipReader.HasKey(equipId))
        {
            Debug.LogError($"[CombatEntity] Equipment config not found: {equipId}");
            return false;
        }

        // 根据配置类型创建对应的装备组件
        EquipBase equip = null;
        EquipType equipType = equipReader.GetValue<EquipType>(equipId, "EquipType", EquipType.None);


        if (equip != null)
        {
            // 初始化装备配置
            equip.Init(equipId);
            
            // 调用原有装备方法
            EquipInternal(equip);
            return true;
        }
        
        return false;
    }

    protected virtual void EquipInternal(EquipBase equip)
    {
        if (equip == null) return;

        // 卸下同部位的装备
        UnequipByPart(equip.EquipPart);

        // 添加新装备
        _equips.Add(equip);
        equip.OnEquip(this);
        
        // 根据装备类型切换动画控制器
        SwitchAnimatorController(equip);
    }
    
    protected virtual void UnequipByPart(EquipPart part)
    {
        var equip = GetEquipByPart(part);
        if (equip != null)
        {
            equip.OnUnequip();
            _equips.Remove(equip);
            
            // 如果卸下的是手部装备，恢复基础动画控制器
            if (part == EquipPart.Hand)
            {
                RestoreBaseAnimatorController();
            }
        }
    }

    protected virtual EquipBase GetEquipByPart(EquipPart part)
    {
        foreach (var equip in _equips)
        {
            if (equip.EquipPart == part)
            {
                return equip;
            }
        }
        return null;
    }

    protected virtual void UseHandEquip()
    {
        var handEquip = GetEquipByPart(EquipPart.Hand);
        if (handEquip != null && handEquip.CanUse)
        {
            // 触发攻击动画
            TriggerAttackAnimation();
            
            // 使用装备
            handEquip.Use();
        }
    }

    public virtual List<int> GetEquippedItemIds()
    {
        List<int> equipIds = new List<int>();
        foreach (var equip in _equips)
        {
            // 使用反射获取私有字段_configId，或者通过公共属性获取
            if (equip != null)
            {
                // 假设EquipBase有ConfigId属性，如果没有则需要添加
                var configId = GetEquipConfigId(equip);
                if (configId > 0)
                {
                    equipIds.Add(configId);
                }
            }
        }
        return equipIds;
    }
    
    private int GetEquipConfigId(EquipBase equip)
    {
        // 使用反射获取私有字段_configId
        var field = typeof(EquipBase).GetField("_configId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (int)field.GetValue(equip);
        }
        return 0;
    }

    protected virtual void InitializeNavMeshAgent()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_navMeshAgent != null)
        {
            _navMeshAgent.speed = _moveSpeed;
            _navMeshAgent.stoppingDistance = 0.5f;
            _navMeshAgent.angularSpeed = 120f;
            _navMeshAgent.acceleration = 8f;
        }
    }
    
    protected virtual void InitializeAnimationSystem()
    {
        // 获取Animator组件
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
        
        // 设置基础动画控制器
        if (_animator != null && _baseAnimatorController != null)
        {
            _animator.runtimeAnimatorController = _baseAnimatorController;
        }
    }

    #region 移动接口

    public virtual bool MoveToPosition(Vector3 targetPosition)
    {
        if (_navMeshAgent == null)
        {
            Debug.LogWarning($"[{name}] NavMeshAgent component is missing!");
            return false;
        }

        if (!_navMeshAgent.enabled)
        {
            Debug.LogWarning($"[{name}] NavMeshAgent is disabled!");
            return false;
        }

        // 检查目标位置是否在NavMesh上
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
        {
            _navMeshAgent.SetDestination(hit.position);
            return true;
        }
        else
        {
            Debug.LogWarning($"[{name}] Target position is not on NavMesh: {targetPosition}");
            return false;
        }
    }

    public virtual bool MoveToTarget(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{name}] Target transform is null!");
            return false;
        }

        return MoveToPosition(target.position);
    }

    public virtual void StopMovement()
    {
        if (_navMeshAgent != null && _navMeshAgent.enabled)
        {
            _navMeshAgent.ResetPath();
        }
    }

    public virtual void SetMoveSpeed(float speed)
    {
        _moveSpeed = speed;
        if (_navMeshAgent != null)
        {
            _navMeshAgent.speed = speed;
        }
    }

    public virtual float GetRemainingDistance()
    {
        if (_navMeshAgent != null && _navMeshAgent.hasPath)
        {
            return _navMeshAgent.remainingDistance;
        }
        return 0f;
    }

    public virtual bool CanReachPosition(Vector3 targetPosition)
    {
        if (_navMeshAgent == null) return false;

        NavMeshPath path = new NavMeshPath();
        if (_navMeshAgent.CalculatePath(targetPosition, path))
        {
            return path.status == NavMeshPathStatus.PathComplete;
        }
        return false;
    }

    public virtual void SetNavMeshEnabled(bool enabled)
    {
        if (_navMeshAgent != null)
        {
            _navMeshAgent.enabled = enabled;
        }
    }

    #endregion

    #region 对话系统

    protected virtual void ShowEntityMessage(string message, float duration = 2f)
    {
        if (DialogManager.Instance == null) return;
        
        // 销毁之前的消息对话框（如果存在）
        if (_currentDialogId != -1)
        {
            DialogManager.Instance.DestroyDialog(_currentDialogId);
            _currentDialogId = -1;
        }
        
        Vector3 dialogOffset = new Vector3(0, 2.5f, 0); // 在实体头顶显示
        _currentDialogId = DialogManager.Instance.CreateDialog(
            transform,
            message,
            dialogOffset,
            duration
        );
    }

    protected virtual void ShowRandomDialogMessage(int[] dialogIds, float duration = 2f)
    {
        if (dialogIds == null || dialogIds.Length == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] 没有可用的对话ID");
            return;
        }

        // 销毁之前的消息对话框（如果存在）
        if (_currentDialogId != -1)
        {
            DialogManager.Instance.DestroyDialog(_currentDialogId);
            _currentDialogId = -1;
        }

        // 创建随机对话
        Vector3 dialogOffset = new Vector3(0, 2.5f, 0); // 在实体头顶显示
        _currentDialogId = DialogManager.Instance.CreateRandomDialog(
            transform,
            dialogIds,
            dialogOffset,
            duration
        );
    }

    protected virtual void TriggerRandomDialog()
    {
        if (_availableDialogIds == null || _availableDialogIds.Length == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] {gameObject.name} 没有可用的对话ID");
            return;
        }

        ShowRandomDialogMessage(_availableDialogIds, 3f);
        _lastDialogTime = Time.time;
    }

    protected virtual bool CanTriggerDialog()
    {
        return Time.time - _lastDialogTime >= _dialogCooldown;
    }

    protected virtual void ClearDialog()
    {
        if (_currentDialogId != -1 && DialogManager.Instance != null)
        {
            DialogManager.Instance.DestroyDialog(_currentDialogId);
            _currentDialogId = -1;
        }
    }

    #endregion
    
    #region 动画系统
    
    protected virtual void SwitchAnimatorController(EquipBase equip)
    {
        if (_animator == null || equip == null) return;

        // 获取动画控制器路径
        string animatorPath = equip.AnimatorPath;
        if (string.IsNullOrEmpty(animatorPath)) return;
        
        // 加载并设置动画控制器
        var animatorController = ResourceManager.Instance.Load<RuntimeAnimatorController>(animatorPath);
        if (animatorController != null)
        {
            _animator.runtimeAnimatorController = animatorController;
        }
    }
    
    protected virtual void RestoreBaseAnimatorController()
    {
        if (_animator != null && _baseAnimatorController != null)
        {
            _animator.runtimeAnimatorController = _baseAnimatorController;
        }
    }
    
    public virtual void TriggerAttackAnimation()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Apply");
        }
    }
    
    #endregion
} 