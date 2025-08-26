using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 对话框类，管理单个3D文本对话框
public class Dialog
{
    public int Id { get; private set; }
    public GameObject TextObject { get; private set; }
    public TextMeshPro TmpText { get; private set; }
    public Transform Parent { get; private set; }
    public bool IsActive { get; private set; }
    public float LifeTime { get; private set; }
    private float _currentTime;

    public Dialog(int id, Transform parent, string text, Vector3 offset, float lifeTime = -1)
    {
        Id = id;
        Parent = parent;
        LifeTime = lifeTime;
        _currentTime = 0f;
        IsActive = true;

        CreateTextObject(text, offset);
    }

    private void CreateTextObject(string text, Vector3 offset)
    {
        TextObject = new GameObject($"Dialog_{Id}");
        TextObject.transform.SetParent(Parent);
        TextObject.transform.localPosition = offset;
        TextObject.transform.localRotation = Quaternion.identity;

        TmpText = TextObject.AddComponent<TextMeshPro>();
        
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Font/zh_cn_body");
        if (font != null)
        {
            TmpText.font = font;
        }
        else
        {
            Debug.LogWarning("[Dialog] 无法加载中文字体，使用默认字体");
        }

        TmpText.text = text;
        TmpText.color = Color.white;
        TmpText.fontSize = 5f;
        TmpText.alignment = TextAlignmentOptions.Center;
        TmpText.enableWordWrapping = false;

        TmpText.sortingOrder = 100;
        TmpText.renderer.sortingOrder = 100;
    }

    public void UpdateText(string newText)
    {
        if (TmpText != null)
            TmpText.text = newText;
    }

    public void UpdateColor(Color newColor)
    {
        if (TmpText != null)
            TmpText.color = newColor;
    }

    public void UpdateOffset(Vector3 newOffset)
    {
        if (TextObject != null)
            TextObject.transform.localPosition = newOffset;
    }

    public void UpdateFontSize(float fontSize)
    {
        if (TmpText != null)
            TmpText.fontSize = fontSize;
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        if (TextObject != null)
            TextObject.SetActive(active);
    }

    public bool UpdateLifeTime(float deltaTime)
    {
        if (LifeTime <= 0) return true;

        _currentTime += deltaTime;
        return _currentTime < LifeTime;
    }

    public void LookAtCamera(Camera camera)
    {
        if (camera != null && TmpText != null && IsActive)
        {
            TmpText.transform.rotation =  camera.transform.rotation;
        }
    }

    public void Destroy()
    {
        if (TextObject != null)
        {
            Object.Destroy(TextObject);
            TextObject = null;
            TmpText = null;
        }
        IsActive = false;
    }
}

// 对话框管理器，统一管理所有3D文本对话框
public class DialogManager
{
    private static DialogManager _instance;
    public static DialogManager Instance => _instance ??= new DialogManager();

    private Dictionary<int, Dialog> _dialogs = new Dictionary<int, Dialog>();
    private Camera _targetCamera;
    private int _nextDialogId = 1;
    private bool _globalVisible = true;
    private ConfigReader _dialogReader;

    private DialogManager()
    {
        _targetCamera = Camera.main;
    }
    
    private void EnsureConfigReader()
    {
        if (_dialogReader == null)
        {
            _dialogReader = ConfigManager.Instance.GetReader("Dialog");
            if (_dialogReader == null)
            {
                Debug.LogError("[DialogManager] 无法加载Dialog配置表");
            }
        }
    }

    public int CreateDialog(Transform parent, string text, Vector3 offset = default, float lifeTime = -1)
    {
        if (parent == null)
        {
            Debug.LogError("[DialogManager] 创建对话框失败：parent不能为null");
            return -1;
        }

        if (offset == default)
            offset = new Vector3(0, 2f, 0);

        int dialogId = _nextDialogId++;
        Dialog dialog = new Dialog(dialogId, parent, text, offset, lifeTime);
        dialog.SetActive(_globalVisible);
        _dialogs[dialogId] = dialog;

        Debug.Log($"[DialogManager] 创建对话框 ID:{dialogId}, 文本:'{text}'");
        return dialogId;
    }

    public Dialog GetDialog(int dialogId)
    {
        _dialogs.TryGetValue(dialogId, out Dialog dialog);
        return dialog;
    }

    public void UpdateDialogText(int dialogId, string newText)
    {
        if (_dialogs.TryGetValue(dialogId, out Dialog dialog))
        {
            dialog.UpdateText(newText);
        }
    }

    public void DestroyDialog(int dialogId)
    {
        if (_dialogs.TryGetValue(dialogId, out Dialog dialog))
        {
            dialog.Destroy();
            _dialogs.Remove(dialogId);
            Debug.Log($"[DialogManager] 销毁对话框 ID:{dialogId}");
        }
    }

    public void DestroyDialogsByParent(Transform parent)
    {
        List<int> toRemove = new List<int>();
        foreach (var kvp in _dialogs)
        {
            if (kvp.Value.Parent == parent)
            {
                kvp.Value.Destroy();
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int id in toRemove)
        {
            _dialogs.Remove(id);
        }

        if (toRemove.Count > 0)
        {
            Debug.Log($"[DialogManager] 销毁 {toRemove.Count} 个对话框（父对象：{parent.name}）");
        }
    }

    public void SetGlobalVisible(bool visible)
    {
        _globalVisible = visible;
        foreach (var dialog in _dialogs.Values)
        {
            dialog.SetActive(visible);
        }
        Debug.Log($"[DialogManager] 设置全局显示状态：{visible}");
    }

    public void SetTargetCamera(Camera camera)
    {
        _targetCamera = camera;
    }

    public int GetDialogCount()
    {
        return _dialogs.Count;
    }

    public void ClearAllDialogs()
    {
        foreach (var dialog in _dialogs.Values)
        {
            dialog.Destroy();
        }
        _dialogs.Clear();
        Debug.Log("[DialogManager] 清理所有对话框");
    }

    public void Update()
    {
        if (_dialogs.Count == 0) return;

        List<int> toRemove = new List<int>();

        foreach (var kvp in _dialogs)
        {
            Dialog dialog = kvp.Value;

            if (!dialog.UpdateLifeTime(Time.deltaTime))
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            dialog.LookAtCamera(_targetCamera);
        }

        foreach (int id in toRemove)
        {
            DestroyDialog(id);
        }
    }

    public string GetDialogContent(int dialogId)
    {
        EnsureConfigReader();
        
        if (_dialogReader == null)
        {
            Debug.LogError("[DialogManager] DialogReader未初始化");
            return null;
        }

        return _dialogReader.GetValue<string>(dialogId, "DialogContent", null);
    }

    public int CreateRandomDialog(Transform parent, int[] dialogIds, Vector3 offset = default, float lifeTime = 3f)
    {
        if (parent == null || dialogIds == null || dialogIds.Length == 0)
        {
            Debug.LogError("[DialogManager] 创建随机对话失败：参数无效");
            return -1;
        }

        int randomIndex = Random.Range(0, dialogIds.Length);
        int selectedDialogId = dialogIds[randomIndex];

        string dialogContent = GetDialogContent(selectedDialogId);
        if (string.IsNullOrEmpty(dialogContent))
        {
            Debug.LogWarning($"[DialogManager] 未找到对话ID {selectedDialogId} 的内容");
            return -1;
        }

        return CreateDialog(parent, dialogContent, offset, lifeTime);
    }

    public void Cleanup()
    {
        ClearAllDialogs();
        Debug.Log("[DialogManager] 清理完成");
    }
}