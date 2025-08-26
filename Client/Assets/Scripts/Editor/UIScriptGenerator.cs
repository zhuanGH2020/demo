using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic; // Added for List
using System.Linq; // Added for All

// UI脚本自动生成器 - 为Prefab生成对应的View和Model脚本
public class UIScriptGenerator
{
    [MenuItem("Assets/Auto Gen UI Script", false, 51)]
    public static void GenerateUIScript()
    {
        GameObject selectedObject = Selection.activeGameObject;
        
        if (selectedObject == null)
        {
            return;
        }
        
        if (PrefabUtility.GetPrefabAssetType(selectedObject) == PrefabAssetType.NotAPrefab)
        {
            EditorUtility.DisplayDialog("错误", "请选择一个Prefab预制体", "确定");
            return;
        }
        
        string prefabName = selectedObject.name;
        string className = GetClassNameFromPrefab(prefabName);
        
        // 获取生成脚本的目标路径
        string scriptPath = GetScriptGenerationPath(prefabName);
        
        // 检查文件是否已存在
        string viewPath = Path.Combine(scriptPath, $"{className}View.cs");
        string modelPath = Path.Combine(scriptPath, $"{className}Model.cs");
        bool viewExists = File.Exists(viewPath);
        bool modelExists = File.Exists(modelPath);
        
        GenerateViewScript(prefabName, className, scriptPath);
        GenerateModelScript(prefabName, className, scriptPath);
        UpdateGameMain(className);
        
        AssetDatabase.Refresh();
        
        // 根据文件状态显示不同的提示
        string message;
        if (viewExists && modelExists)
        {
            message = $"已更新 {className}View.cs 的PrefabPath路径";
        }
        else if (viewExists || modelExists)
        {
            message = $"已生成缺失文件并更新 {className} 脚本";
        }
        else
        {
            message = $"已生成 {className}View.cs 和 {className}Model.cs，并已添加到GameMain";
        }
        
        EditorUtility.DisplayDialog("成功", message, "确定");
    }
    
    [MenuItem("Assets/Auto Gen UI Script", true)]
    public static bool ValidateGenerateUIScript()
    {
        return Selection.activeGameObject != null;
    }
    
    private static void GenerateViewScript(string prefabName, string className, string scriptPath)
    {
        string filePath = Path.Combine(scriptPath, $"{className}View.cs");
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        
        // 查找预制体的实际路径
        string prefabPath = FindPrefabPath(prefabName);
        
        // 如果文件已存在，只更新PrefabPath
        if (File.Exists(filePath))
        {
            UpdatePrefabPath(filePath, prefabPath);
            Debug.Log($"已更新 {className}View.cs 中的 PrefabPath 为: {prefabPath}");
            return;
        }
        
        // 文件不存在，生成完整文件
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine();
        sb.AppendLine($"// {className}View - {prefabName}UI视图");
        sb.AppendLine($"public class {className}View : BaseView");
        sb.AppendLine("{");
        sb.AppendLine($"    public static string PrefabPath => \"{prefabPath}\";");
        sb.AppendLine();
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        Initialize();");
        sb.AppendLine("        SubscribeEvents();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine("        ");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private void SubscribeEvents()");
        sb.AppendLine("    {");
        sb.AppendLine("        // 订阅数据变化事件");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private void OnDataChanged()");
        sb.AppendLine("    {");
        sb.AppendLine("        ");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"已生成新文件 {className}View.cs");
    }
    
    private static string FindPrefabPath(string prefabName)
    {
        // 搜索所有预制体文件
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            
            // 精确匹配文件名
            if (fileName.Equals(prefabName, System.StringComparison.OrdinalIgnoreCase))
            {
                // 转换为Resources相对路径
                if (assetPath.StartsWith("Assets/Resources/"))
                {
                    // 去掉"Assets/Resources/"前缀和".prefab"后缀
                    return assetPath.Substring("Assets/Resources/".Length).Replace(".prefab", "");
                }
                else
                {
                    // 如果不在Resources文件夹下，返回完整的Assets相对路径（去掉"Assets/"前缀）
                    return assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length).Replace(".prefab", "") : assetPath.Replace(".prefab", "");
                }
            }
        }
        
        // 如果没有找到，返回默认路径
        Debug.LogWarning($"未找到预制体 {prefabName}，使用默认路径");
        return $"Prefabs/UI/ui{prefabName.ToLower()}";
    }
    
    private static void UpdatePrefabPath(string filePath, string newPrefabPath)
    {
        string content = File.ReadAllText(filePath);
        
        // 使用正则表达式匹配并替换PrefabPath
        string pattern = @"public\s+static\s+string\s+PrefabPath\s*=>\s*""[^""]*"";";
        string replacement = $"public static string PrefabPath => \"{newPrefabPath}\";";
        
        if (System.Text.RegularExpressions.Regex.IsMatch(content, pattern))
        {
            content = System.Text.RegularExpressions.Regex.Replace(content, pattern, replacement);
            File.WriteAllText(filePath, content, Encoding.UTF8);
        }
        else
        {
            Debug.LogWarning($"未找到PrefabPath属性，无法更新路径");
        }
    }
    
    private static void GenerateModelScript(string prefabName, string className, string scriptPath)
    {
        string filePath = Path.Combine(scriptPath, $"{className}Model.cs");
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine($"// {className}数据模型 - 负责{prefabName}数据管理");
        sb.AppendLine($"public class {className}Model");
        sb.AppendLine("{");
        sb.AppendLine($"    private static {className}Model _instance;");
        sb.AppendLine($"    public static {className}Model Instance");
        sb.AppendLine("    {");
        sb.AppendLine("        get");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_instance == null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                _instance = new {className}Model();");
        sb.AppendLine("            }");
        sb.AppendLine("            return _instance;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private {className}Model()");
        sb.AppendLine("    {");
        sb.AppendLine("        ");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private void NotifyDataChanged()");
        sb.AppendLine("    {");
        sb.AppendLine("        ");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
    
    private static void UpdateGameMain(string className)
    {
        string gameMainPath = Path.Combine(Application.dataPath, "Scripts", "GameMain.cs");
        
        if (!File.Exists(gameMainPath))
        {
            Debug.LogError("GameMain.cs not found!");
            return;
        }
        
        string content = File.ReadAllText(gameMainPath);
        string modelInitLine = $"var {className.ToLower()}Model = {className}Model.Instance;";
        
        if (content.Contains($"{className}Model.Instance"))
        {
            Debug.Log($"{className}Model already initialized in GameMain");
            return;
        }
        
        string startMethodStart = "void Start()";
        string startMethodEnd = "    }";
        
        int startIndex = content.IndexOf(startMethodStart);
        if (startIndex == -1)
        {
            Debug.LogError("Could not find Start method in GameMain.cs");
            return;
        }
        
        int openBraceIndex = content.IndexOf("{", startIndex);
        if (openBraceIndex == -1)
        {
            Debug.LogError("Could not find opening brace for Start method");
            return;
        }
        
        int braceCount = 1;
        int currentIndex = openBraceIndex + 1;
        int closeBraceIndex = -1;
        
        while (currentIndex < content.Length && braceCount > 0)
        {
            if (content[currentIndex] == '{')
            {
                braceCount++;
            }
            else if (content[currentIndex] == '}')
            {
                braceCount--;
                if (braceCount == 0)
                {
                    closeBraceIndex = currentIndex;
                    break;
                }
            }
            currentIndex++;
        }
        
        if (closeBraceIndex == -1)
        {
            Debug.LogError("Could not find closing brace for Start method");
            return;
        }
        
        string beforeClose = content.Substring(0, closeBraceIndex);
        string afterClose = content.Substring(closeBraceIndex);
        
        string newContent = beforeClose + "\n        " + modelInitLine + "\n    " + afterClose;
        
        File.WriteAllText(gameMainPath, newContent, Encoding.UTF8);
        Debug.Log($"已在GameMain.cs中添加{className}Model的初始化");
    }

    private static string GetScriptGenerationPath(string prefabName)
    {
        // 获取类名（去掉ui_前缀等）
        string className = GetClassNameFromPrefab(prefabName);
        
        // 按照GamePlay模块组织，每个模块有自己的文件夹
        return Path.Combine(Application.dataPath, "Scripts/GamePlay", className);
    }
    
    private static string GetClassNameFromPrefab(string prefabName)
    {
        // 处理ui_或ui开头的命名格式
        string nameWithoutPrefix = prefabName;
        
        if (prefabName.StartsWith("ui_", System.StringComparison.OrdinalIgnoreCase))
        {
            // 去掉ui_前缀
            nameWithoutPrefix = prefabName.Substring(3);
        }
        else if (prefabName.StartsWith("ui", System.StringComparison.OrdinalIgnoreCase) && prefabName.Length > 2)
        {
            // 去掉ui前缀（无下划线情况）
            nameWithoutPrefix = prefabName.Substring(2);
        }
        
        // 转换为PascalCase
        return ToPascalCase(nameWithoutPrefix);
    }
    
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        // 如果包含下划线，按下划线分割处理
        if (input.Contains("_"))
        {
            string[] parts = input.Split('_');
            StringBuilder result = new StringBuilder();
            
            foreach (string part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    // 每个部分首字母大写，其余小写
                    result.Append(char.ToUpper(part[0]));
                    if (part.Length > 1)
                    {
                        result.Append(part.Substring(1).ToLower());
                    }
                }
            }
            
            return result.ToString();
        }
        
        // 处理驼峰命名或全小写情况
        return ConvertCamelCaseToPascalCase(input);
    }
    
    private static string ConvertCamelCaseToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        StringBuilder result = new StringBuilder();
        bool isAllLowerCase = input.All(c => !char.IsUpper(c));
        
        if (isAllLowerCase)
        {
            // 全小写情况：只将首字母大写，其余保持小写
            result.Append(char.ToUpper(input[0]));
            if (input.Length > 1)
            {
                result.Append(input.Substring(1).ToLower());
            }
        }
        else
        {
            // 包含大写字母的情况：保持驼峰结构，确保首字母大写
            result.Append(char.ToUpper(input[0]));
            
            for (int i = 1; i < input.Length; i++)
            {
                char currentChar = input[i];
                result.Append(currentChar);
            }
        }
        
        return result.ToString();
    }
} 