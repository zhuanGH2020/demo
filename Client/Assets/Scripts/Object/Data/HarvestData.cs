using UnityEngine;
using System.Collections.Generic;

// 采集互动的动作类型
public enum ActionType
{
    None = 0,
    Pull = 1,        // 拉拽动作（草类）
    Chop = 2,        // 砍伐动作（树木）
    Pick = 3,        // 采摘动作（浆果）
    Mine = 4,        // 挖掘动作（石头）
    Collect = 5      // 收集动作（掉落物）
}

// 掉落物品信息结构体，定义物品掉落的参数和概率计算
[System.Serializable]
public struct DropItem
{
    public int itemId;
    public int minCount;
    public int maxCount;
    public float dropRate;

    public DropItem(int itemId, int count) : this(itemId, count, count, 1.0f) { }
    
    public DropItem(int itemId, int minCount, int maxCount, float dropRate)
    {
        this.itemId = itemId;
        this.minCount = minCount;
        this.maxCount = maxCount;
        this.dropRate = dropRate;
    }

    public int GetActualDropCount()
    {
        if (Random.value > dropRate) return 0;
        return Random.Range(minCount, maxCount + 1);
    }
}

// 采集信息结构体，包含采集动作所需的所有数据
[System.Serializable]
public struct HarvestInfo
{
    public List<DropItem> drops;
    public float harvestTime;
    public bool destroyAfterHarvest;
    public ActionType actionType;
    public bool requiresTool;
    public ToolType requiredToolType;

    public HarvestInfo(List<DropItem> drops, float harvestTime = 0f, bool destroyAfterHarvest = true, 
                      ActionType actionType = ActionType.None, bool requiresTool = false, 
                      ToolType requiredToolType = ToolType.None)
    {
        this.drops = drops ?? new List<DropItem>();
        this.harvestTime = harvestTime;
        this.destroyAfterHarvest = destroyAfterHarvest;
        this.actionType = actionType;
        this.requiresTool = requiresTool;
        this.requiredToolType = requiredToolType;
    }

    public static HarvestInfo CreateSimple(int itemId, int count, ActionType actionType = ActionType.Pull)
    {
        var drops = new List<DropItem> { new DropItem(itemId, count) };
        return new HarvestInfo(drops, 0f, true, actionType);
    }
} 