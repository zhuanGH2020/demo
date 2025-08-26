using System.Collections.Generic;
using UnityEngine;

// 装备管理器，统一管理所有装备相关逻辑和作为装备系统的单一数据源
public class EquipManager
{
    private static EquipManager _instance;
    public static EquipManager Instance => _instance ??= new EquipManager();
    
    private Dictionary<EquipPart, int> _equippedItems = new Dictionary<EquipPart, int>();
    
    private EquipManager() { }
    
    public bool EquipItem(int itemId, EquipPart equipPart)
    {
        var itemConfig = ItemManager.Instance.GetItem(itemId);
        if (itemConfig == null || !itemConfig.IsEquip())
        {
            Debug.LogWarning($"[EquipManager] 物品 {itemId} 不是装备类型");
            return false;
        }
        
        var equipReader = ConfigManager.Instance.GetReader("Equip");
        if (equipReader == null || !equipReader.HasKey(itemId))
        {
            Debug.LogWarning($"[EquipManager] 无法获取装备 {itemId} 的配置");
            return false;
        }

        EquipPart configEquipPart = equipReader.GetValue<EquipPart>(itemId, "Type", EquipPart.None);
        
        if (configEquipPart != equipPart)
        {
            Debug.LogWarning($"[EquipManager] 物品 {itemId} 不能装备到 {equipPart} 部位，配置部位为 {configEquipPart}");
            return false;
        }
        
        if (!PackageModel.Instance.HasEnoughItem(itemId, 1))
        {
            Debug.LogWarning($"[EquipManager] 背包中没有物品 {itemId}");
            return false;
        }
        
        UnequipItem(equipPart);
        
        PackageModel.Instance.RemoveItem(itemId, 1);
        
        var player = PlayerMain.Instance;
        if (player != null)
        {
            bool equipSuccess = player.Equip(itemId);
            if (equipSuccess)
            {
                _equippedItems[equipPart] = itemId;
                
                EventManager.Instance.Publish(new EquipChangeEvent(equipPart, itemId, true));
                
                Debug.Log($"[EquipManager] 成功装备物品 {itemId} 到 {equipPart} 部位");
                return true;
            }
        }
        
        return false;
    }
    
    public bool UnequipItem(EquipPart equipPart)
    {
        if (!_equippedItems.ContainsKey(equipPart))
        {
            return false;
        }
        
        int equipId = _equippedItems[equipPart];
        if (equipId <= 0) return false;
        
        _equippedItems.Remove(equipPart);
        
        var player = PlayerMain.Instance;
        if (player != null)
        {
            var equipComponent = GetEquipComponentByPart(equipPart);
            if (equipComponent != null)
            {
                RemoveEquipComponent(player, equipComponent);
            }
        }

        bool addSuccess = PackageModel.Instance.AddItem(equipId, 1);
        if (!addSuccess)
        {
            Debug.LogWarning($"[EquipManager] 背包已满，无法卸下装备 {equipId}");
        }
        
        EventManager.Instance.Publish(new EquipChangeEvent(equipPart, equipId, false));
        
        Debug.Log($"[EquipManager] 成功卸下 {equipPart} 部位的装备 {equipId}");
        return true;
    }
    
    public int GetEquippedItemId(EquipPart equipPart)
    {
        return _equippedItems.ContainsKey(equipPart) ? _equippedItems[equipPart] : 0;
    }
    
    public Dictionary<EquipPart, int> GetAllEquippedItemIds()
    {
        return new Dictionary<EquipPart, int>(_equippedItems);
    }
    
    public bool HasEquippedItem(EquipPart equipPart)
    {
        return _equippedItems.ContainsKey(equipPart) && _equippedItems[equipPart] > 0;
    }
    
    public void LoadEquippedItemsFromSave(List<int> equippedItems)
    {
        _equippedItems.Clear();
        
        if (equippedItems == null || equippedItems.Count == 0) 
        {
            return;
        }
        
        var player = PlayerMain.Instance;
        if (player == null) 
        {
            return;
        }
        
        foreach (int equipId in equippedItems)
        {
            if (equipId <= 0) continue;
            
            var equipReader = ConfigManager.Instance.GetReader("Equip");
            if (equipReader == null || !equipReader.HasKey(equipId)) 
            {
                Debug.LogWarning($"[EquipManager] Equipment config not found: {equipId}");
                continue;
            }
            
            EquipPart equipPart = equipReader.GetValue<EquipPart>(equipId, "Type", EquipPart.None);
            if (equipPart == EquipPart.None)
            {
                Debug.LogWarning($"[EquipManager] Invalid equipment part for item {equipId}");
                continue;
            }
            
            bool equipSuccess = player.Equip(equipId);
            if (equipSuccess)
            {
                _equippedItems[equipPart] = equipId;
            }
            else
            {
                Debug.LogWarning($"[EquipManager] Failed to equip {equipId} to {equipPart}");
            }
        }
        
        if (_equippedItems.Count > 0)
        {
            EventManager.Instance.Publish(new EquipRefreshEvent(_equippedItems.Count));
        }
    }
    
    public void SyncPlayerEquipmentState()
    {
        var player = PlayerMain.Instance;
        if (player == null) return;
        
        Debug.Log($"[EquipManager] Syncing Player equipment state, current managed items: {_equippedItems.Count}");
        
        foreach (var kvp in _equippedItems)
        {
            var equipPart = kvp.Key;
            var managedEquipId = kvp.Value;
            
            var actualEquip = GetEquipComponentByPart(equipPart);
            if (actualEquip != null)
            {
                if (actualEquip.EquipPart != equipPart)
                {
                    Debug.LogWarning($"[EquipManager] Equipment part mismatch for {equipPart}: managed={managedEquipId}, actual part={actualEquip.EquipPart}");
                }
            }
            else
            {
                Debug.LogWarning($"[EquipManager] Equipment component not found for {equipPart}, managed ID: {managedEquipId}");
            }
        }
    }

    private EquipBase GetEquipComponentByPart(EquipPart equipPart)
    {
        var player = PlayerMain.Instance;
        if (player == null) return null;
        
        var equipField = typeof(CombatEntity).GetField("_equips", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (equipField != null)
        {
            var equipsList = equipField.GetValue(player) as List<EquipBase>;
            if (equipsList != null)
            {
                foreach (var equip in equipsList)
                {
                    if (equip != null && equip.EquipPart == equipPart)
                    {
                        return equip;
                    }
                }
            }
        }
        
            return null;
    }
    
    private void RemoveEquipComponent(PlayerMain player, EquipBase equip)
    {
        if (equip == null) return;
        
        equip.OnUnequip();
        
        var equipField = typeof(CombatEntity).GetField("_equips", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (equipField != null)
        {
            var equipsList = equipField.GetValue(player) as List<EquipBase>;
            if (equipsList != null)
            {
                equipsList.Remove(equip);
            }
        }
        
        Object.Destroy(equip);
    }
} 