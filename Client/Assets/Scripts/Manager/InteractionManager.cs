using UnityEngine;
using UnityEngine.AI;

// 交互管理器，专门处理对象交互的完整流程（寻路→到达→执行交互）
public class InteractionManager
{
    private static InteractionManager _instance;
    public static InteractionManager Instance => _instance ??= new InteractionManager();

    private const float InteractionCheckInterval = 0.1f;
    private IClickable _currentTarget;
    private Vector3 _targetPosition;
    private bool _isMovingToTarget;
    private float _lastInteractionCheck;
    private bool _initialized;
    
    private IClickable _nearbyTarget;
    private bool _wasInRange;

    private InteractionManager() { }

    public void Initialize()
    {
        if (_initialized) return;
        
        SubscribeToEvents();
        _initialized = true;
        
        Debug.Log("[InteractionManager] Initialized");
    }

    public void Cleanup()
    {
        UnsubscribeFromEvents();
        CancelCurrentInteraction();
        
        _nearbyTarget = null;
        _wasInRange = false;
        
        _initialized = false;
        
        Debug.Log("[InteractionManager] Cleaned up");
    }

    public void Update()
    {
        if (!_initialized) return;
        UpdateTargetInteraction();
        UpdateNearbyInteraction();
    }

    private void SubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Subscribe<ObjectInteractionEvent>(OnObjectInteraction);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe<ObjectInteractionEvent>(OnObjectInteraction);
        }
    }

    private void OnObjectInteraction(ObjectInteractionEvent eventData)
    {
        StartInteraction(eventData.Target, eventData.ClickPosition);
    }

    private void StartInteraction(IClickable target, Vector3 clickPosition)
    {
        if (target == null || !target.CanInteract) return;

        var player = PlayerMain.Instance;
        if (player == null) return;

        _currentTarget = target;
        _targetPosition = clickPosition;
        _isMovingToTarget = true;

        Vector3 targetObject = target.GetInteractionRange() > 0 ? 
            GetInteractionPosition(clickPosition, target) : clickPosition;

        MovePlayerToPosition(targetObject);

        Debug.Log($"[InteractionManager] Started interaction with {((MonoBehaviour)target).name}");
    }

    private Vector3 GetInteractionPosition(Vector3 targetPosition, IClickable target)
    {
        var player = PlayerMain.Instance;
        if (player == null) return targetPosition;

        Vector3 playerPos = player.transform.position;
        Vector3 direction = (targetPosition - playerPos).normalized;
        float interactionRange = Mathf.Max(1f, target.GetInteractionRange() - 0.5f);
        
        return targetPosition - direction * interactionRange;
    }

    private void MovePlayerToPosition(Vector3 position)
    {
        var player = PlayerMain.Instance;
        if (player == null) return;

        player.MoveToPosition(position);
    }

    private void UpdateTargetInteraction()
    {
        if (!_isMovingToTarget || _currentTarget == null) return;

        if (Time.time - _lastInteractionCheck < InteractionCheckInterval) return;
        _lastInteractionCheck = Time.time;

        var player = PlayerMain.Instance;
        if (player == null)
        {
            CancelCurrentInteraction();
            return;
        }

        if (!_currentTarget.CanInteract)
        {
            CancelCurrentInteraction();
            return;
        }

        float distanceToTarget = Vector3.Distance(player.transform.position, _targetPosition);
        float interactionRange = _currentTarget.GetInteractionRange();

        if (distanceToTarget <= interactionRange)
        {
            PerformInteraction();
        }
        else
        {
            var navAgent = player.GetComponent<NavMeshAgent>();
            if (navAgent != null && !navAgent.pathPending && navAgent.remainingDistance < 0.1f)
            {
                if (distanceToTarget <= interactionRange * 2f)
                {
                    PerformInteraction();
                }
                else
                {
                    Debug.Log("[InteractionManager] Player couldn't reach target, canceling interaction");
                    CancelCurrentInteraction();
                }
            }
        }
    }

    private void PerformInteraction()
    {
        if (_currentTarget == null) return;

        var player = PlayerMain.Instance;
        if (player == null) return;

        if (_currentTarget is Building building)
        {
            if (building.CanInteract)
            {
                building.OnInteract(_targetPosition);
            }
        }
        else if (_currentTarget is IClickable clickable)
        {
            clickable.OnClick(_targetPosition);
        }

        CancelCurrentInteraction();
    }

    private void CancelCurrentInteraction()
    {
        _currentTarget = null;
        _isMovingToTarget = false;
        _targetPosition = Vector3.zero;
    }

    private void UpdateNearbyInteraction()
    {
        var player = PlayerMain.Instance;
        if (player == null) return;

        IClickable nearestTarget = null;
        float nearestDistance = float.MaxValue;

        var colliders = Physics.OverlapSphere(player.transform.position, 5f);
        foreach (var collider in colliders)
        {
            var clickable = collider.GetComponent<IClickable>();
            if (clickable != null && clickable != _currentTarget)
            {
                if (clickable is Building building)
                {
                    float distance = Vector3.Distance(player.transform.position, collider.transform.position);
                    float interactionRange = clickable.GetInteractionRange();
                    
                    if (distance <= interactionRange && distance < nearestDistance)
                    {
                        nearestTarget = clickable;
                        nearestDistance = distance;
                    }
                }
            }
        }

        bool currentInRange = nearestTarget != null;
        bool targetChanged = nearestTarget != _nearbyTarget;

        if (currentInRange != _wasInRange || targetChanged)
        {
            if (_nearbyTarget is Building oldBuilding && (!currentInRange || targetChanged))
            {
                oldBuilding.OnLeaveInteractionRange();
            }

            if (nearestTarget is Building newBuilding && currentInRange)
            {
                newBuilding.OnEnterInteractionRange();
            }

            _nearbyTarget = nearestTarget;
            _wasInRange = currentInRange;
        }
    }

    public bool IsInteracting => _isMovingToTarget && _currentTarget != null;

    public IClickable CurrentTarget => _currentTarget;
} 