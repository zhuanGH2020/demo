using UnityEngine;

// 所有游戏对象的基类，提供uid、类型和通用工具，自动注册到ObjectManager
public abstract class ObjectBase : MonoBehaviour
{
    private int _uid = 0;                 // Unique identifier
    private int _configId = 0;            // 配置表ID
    private ObjectType _objectType = ObjectType.Other; // Object category

    public int Uid => _uid;
    public int ConfigId => _configId;
    public ObjectType ObjectType => _objectType;
    public Vector3 Position => transform.position;

    protected virtual void Awake()
    {
        if (_uid == 0)
        {
            _uid = ResourceUtils.GenerateUid();
        }
    }

    protected virtual void OnEnable()
    {
        ObjectManager.Instance?.Register(this);
    }

    protected virtual void OnDisable()
    {
        if (ObjectManager.HasInstance)
        {
            ObjectManager.Instance.Unregister(this);
        }
    }

    public void SetUid(int uid)
    {
        if (_uid != 0 && _uid == uid) return;
        _uid = uid;
    }

    public void SetConfigId(int configId)
    {
        _configId = configId;
    }

    public void SetObjectType(ObjectType type)
    {
        _objectType = type;
    }

    public ConfigReader GetConfig()
    {
        // 对于Other类型，不支持配置表
        if (_objectType == ObjectType.Other)
        {
            return null;
        }

        string configName = _objectType.ToString();
        return ConfigManager.Instance.GetReader(configName);
    }

    public T GetOrAddComponent<T>() where T : Component
    {
        var c = GetComponent<T>();
        return c != null ? c : gameObject.AddComponent<T>();
    }

    public ObjectState GetObjectState()
    {
        return GetComponent<ObjectState>();
    }
} 