using UnityEngine;
using System;

// 输入管理器，统一处理玩家键盘和鼠标输入，专注于输入捕获和事件分发
public class InputManager
{
    private static InputManager _instance;
    public static InputManager Instance => _instance ??= new InputManager();

    private bool _enableInput = true;

    public event Action<Vector3> OnMoveInput;
    
    public event Func<Vector3, bool> OnLeftClickHighPriority;
    public event Action<Vector3> OnLeftClickLowPriority;
    public event Action OnRightClick;
    
    public event Action<Vector3> OnAttackClick;
    
    public event Action<bool> OnAttackHold;
    
    public Action OnUseEquipInput;
    public Action<int> OnEquipShortcutInput;
    
    private bool _isHoldingAttack = false;
    private float _holdStartTime = 0f;
    private bool _hasTriggeredHold = false;
    private const float HOLD_THRESHOLD = 0.15f;

    private InputManager() { }

    public void Update()
    {
        if (!_enableInput) return;
        
        HandleMovementInput();
        HandleMouseInput();
        HandleKeyboardInput();
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        OnMoveInput?.Invoke(moveDirection);
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isHoldingAttack = true;
            _hasTriggeredHold = false;
            _holdStartTime = Time.time;
            
            Vector3 mouseWorldPos = Camera.main ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
            
            HandleLeftClick(mouseWorldPos);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (_isHoldingAttack || _hasTriggeredHold)
            {
                _isHoldingAttack = false;
                _hasTriggeredHold = false;
                OnAttackHold?.Invoke(false);
            }
        }
        else if (Input.GetMouseButton(0) && _isHoldingAttack && !_hasTriggeredHold)
        {
            if (Time.time - _holdStartTime >= HOLD_THRESHOLD)
            {
                OnAttackHold?.Invoke(true);
                _hasTriggeredHold = true;
            }
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
        }
        
        HandleMouseHover();
    }

    private void HandleLeftClick(Vector3 mouseWorldPos)
    {
        if (InputUtils.IsPointerOverUI())
        {
            if (DebugManager.Instance.IsUIPathPrintEnabled)
            {
                InputUtils.PrintClickedUIPath();
            }
            return;
        }

        bool eventConsumed = false;
        
        if (InputUtils.GetMouseWorldHit(out RaycastHit hit))
        {
            if (OnLeftClickHighPriority != null)
            {
                foreach (Func<Vector3, bool> handler in OnLeftClickHighPriority.GetInvocationList())
                {
                    if (handler(mouseWorldPos))
                    {
                        eventConsumed = true;
                        break;
                    }
                }
            }
            
            if (!eventConsumed)
            {
                var clickable = hit.collider.GetComponent<IClickable>();
                if (clickable != null && clickable.CanInteract)
                {
                    clickable.OnClick(hit.point);
                    eventConsumed = true;
                }
            }
        }
        else
        {
            if (OnLeftClickHighPriority != null)
            {
                foreach (Func<Vector3, bool> handler in OnLeftClickHighPriority.GetInvocationList())
                {
                    if (handler(mouseWorldPos))
                    {
                        eventConsumed = true;
                        break;
                    }
                }
            }
        }
        
        if (!eventConsumed)
        {
            OnLeftClickLowPriority?.Invoke(mouseWorldPos);
            OnAttackClick?.Invoke(mouseWorldPos);
        }

        EventManager.Instance.Publish(new ClickOutsideUIEvent(mouseWorldPos));
    }

    private void HandleRightClick()
    {
        OnRightClick?.Invoke();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnUseEquipInput?.Invoke();
        }
    }

    public void SetInputEnabled(bool enable)
    {
        _enableInput = enable;
    }

    public bool IsInputEnabled => _enableInput;
    
    private void HandleMouseHover()
    {
        if (InputUtils.GetMouseWorldHit(out RaycastHit hit))
        {
            EventManager.Instance.Publish(new MouseHoverEvent(hit.collider.gameObject, hit.point));
        }
        else
        {
            EventManager.Instance.Publish(new MouseHoverExitEvent());
        }
    }
} 