using UnityEngine;

// 地图管理器，负责地图随机生成、怪物刷新等游戏逻辑
public class MapManager
{
    private static MapManager _instance;
    public static MapManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MapManager();
            }
            return _instance;
        }
    }

    private float _monsterSpawnInterval = GameSettings.MapSpawnInterval;
    private Vector2 _spawnPosition = GameSettings.MapDefaultSpawnPosition;
    private bool _enableSpawn = true;
    private bool _randomSpawn = true;
    private int[] _spawnableMonsterIds = GameSettings.MapDefaultMonsterIds;
    
    private float _lastSpawnTime;

    public float SpawnInterval => _monsterSpawnInterval;
    public Vector2 SpawnPosition => _spawnPosition;
    public bool IsSpawnEnabled => _enableSpawn;
    public bool IsRandomSpawn => _randomSpawn;
    public int[] SpawnableMonsterIds => _spawnableMonsterIds;

    private MapManager()
    {
        ValidateParameters();
        _lastSpawnTime = Time.time;
        Debug.Log("[MapManager] MapManager initialized");
    }

    public void UpdateSpawning()
    {
        if (!_enableSpawn) return;
        
        if (Time.time - _lastSpawnTime >= _monsterSpawnInterval)
        {
            SpawnMonster();
            _lastSpawnTime = Time.time;
        }
    }

    public void StartMonsterSpawn()
    {
        _lastSpawnTime = Time.time;
    }

    public void StopMonsterSpawn()
    {
        _enableSpawn = false;
    }

    private void SpawnMonster()
    {
        if (!_enableSpawn) return;

        int selectedMonsterId = SelectMonsterToSpawn();
        if (selectedMonsterId <= 0) return;

        var monsterConfig = ConfigManager.Instance.GetReader("Monster");
        if (monsterConfig == null || !monsterConfig.HasKey(selectedMonsterId))
        {
            return;
        }

        string prefabPath = monsterConfig.GetValue<string>(selectedMonsterId, "PrefabPath", "");
        if (string.IsNullOrEmpty(prefabPath))
        {
            return;
        }

        GameObject monsterPrefab = ResourceManager.Instance.Load<GameObject>(prefabPath);
        if (monsterPrefab == null)
        {
            return;
        }

        Vector3 actualSpawnPosition = MapUtils.GetSafeSpawnPosition(_spawnPosition);
        
        GameObject monsterInstance = Object.Instantiate(monsterPrefab, actualSpawnPosition, Quaternion.identity);
        
        var monsterComponent = monsterInstance.GetComponent<Monster>();
        if (monsterComponent != null)
        {
            monsterComponent.Init(selectedMonsterId);
        }

        EventManager.Instance.Publish(new MonsterSpawnedEvent(monsterInstance, actualSpawnPosition));
    }

    private int SelectMonsterToSpawn()
    {
        if (_spawnableMonsterIds == null || _spawnableMonsterIds.Length == 0)
            return 0;

        if (_randomSpawn)
        {
            int randomIndex = Random.Range(0, _spawnableMonsterIds.Length);
            return _spawnableMonsterIds[randomIndex];
        }
        else
        {
            int index = Time.frameCount % _spawnableMonsterIds.Length;
            return _spawnableMonsterIds[index];
        }
    }

    public void SetSpawnInterval(float interval)
    {
        _monsterSpawnInterval = interval;
    }

    public void SetSpawnPosition(Vector2 position)
    {
        _spawnPosition = position;
    }

    public void SetSpawnableMonsterIds(int[] monsterIds)
    {
        _spawnableMonsterIds = monsterIds;
    }

    public void AddSpawnableMonster(int monsterId)
    {
        if (_spawnableMonsterIds == null)
        {
            _spawnableMonsterIds = new int[] { monsterId };
        }
        else
        {
            var newList = new int[_spawnableMonsterIds.Length + 1];
            _spawnableMonsterIds.CopyTo(newList, 0);
            newList[_spawnableMonsterIds.Length] = monsterId;
            _spawnableMonsterIds = newList;
        }
    }

    public void SetRandomSpawn(bool random)
    {
        _randomSpawn = random;
    }

    public void SetSpawnEnabled(bool enabled)
    {
        _enableSpawn = enabled;
        
        if (enabled)
        {
            StartMonsterSpawn();
        }
    }

    public void ManualSpawnMonster()
    {
        SpawnMonster();
    }

    public void Cleanup()
    {
        _enableSpawn = false;
    }

    private void ValidateParameters()
    {
        if (_monsterSpawnInterval < GameSettings.MapMinSpawnInterval)
        {
            _monsterSpawnInterval = GameSettings.MapMinSpawnInterval;
        }
        
        if (_spawnableMonsterIds == null || _spawnableMonsterIds.Length == 0)
        {
            _spawnableMonsterIds = GameSettings.MapDefaultMonsterIds;
        }
    }
} 