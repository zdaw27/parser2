#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ExcelDataReader; // ExcelDataReader.dll + ExcelDataReader.DataSet.dll (put under Assets/Plugins/Editor/)

public class XlsxToJsonAndRegistry : EditorWindow
{
    // ─────────────────────────────────────────────────────────────
    // Pref Keys
    // ─────────────────────────────────────────────────────────────
    const string PREF_XLSX_DIR = "X2JR_XlsxFolderPath";
    const string PREF_JSON_DIR = "X2JR_JsonOutFolder";
    const string PREF_SCRIPT_DIR = "X2JR_ScriptOutFolder";

    // UI 상태
    string _xlsxFolderPath;
    string _jsonOutFolder;
    string _outputScriptPath;

    // 파생 경로(읽기전용 라벨)
    string RegistryFilePath => string.IsNullOrEmpty(_outputScriptPath) ? "" : Path.Combine(_outputScriptPath, "GameDataRegistry.cs");
    string StatEnumFilePath => string.IsNullOrEmpty(_outputScriptPath) ? "" : Path.Combine(_outputScriptPath, "StatType.cs");

    Vector2 _scroll;

    [MenuItem("Tools/XLSX → JSON & Registry (Window)")]
    static void OpenWindow()
    {
        var w = GetWindow<XlsxToJsonAndRegistry>();
        w.titleContent = new GUIContent("XLSX → JSON / Registry");
        w.minSize = new Vector2(620, 360);
        w.Show();
    }

    void OnEnable()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        _xlsxFolderPath = EditorPrefs.GetString(PREF_XLSX_DIR, "Assets/Spreadsheets");
        _jsonOutFolder = EditorPrefs.GetString(PREF_JSON_DIR, "Assets/Resources/CSVDataJson");
        _outputScriptPath = EditorPrefs.GetString(PREF_SCRIPT_DIR, "Assets/Scripts/Generated");
    }

    void OnDisable()
    {
        SavePrefs();
    }

    void SavePrefs()
    {
        EditorPrefs.SetString(PREF_XLSX_DIR, _xlsxFolderPath ?? "");
        EditorPrefs.SetString(PREF_JSON_DIR, _jsonOutFolder ?? "");
        EditorPrefs.SetString(PREF_SCRIPT_DIR, _outputScriptPath ?? "");
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("XLSX → JSON / Row Classes / GameDataRegistry", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawPathPicker("XLSX Folder", ref _xlsxFolderPath, "Choose XLSX Folder");
        DrawPathPicker("JSON Output Folder", ref _jsonOutFolder, "Choose JSON Output Folder");
        DrawPathPicker("Script Output Folder", ref _outputScriptPath, "Choose Script Output Folder");

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_outputScriptPath)))
        {
            EditorGUILayout.LabelField("Generated Files (preview)", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("RegistryFilePath", RegistryFilePath);
            EditorGUILayout.LabelField("StatEnumFilePath", StatEnumFilePath);
        }

        // Resources 외부 경고
        var inResources = IsUnderResources(_jsonOutFolder);
        if (!inResources)
        {
            EditorGUILayout.HelpBox(
                "런타임/빌드에서 JSON을 읽으려면 JSON 출력 폴더가 반드시 Assets/Resources 하위여야 합니다.\n" +
                "지금 설정은 Resources 밖이므로 에디터에서만 파일 직접 읽기(폴백)로 동작합니다.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate", GUILayout.Height(32)))
            {
                try
                {
                    Run(_xlsxFolderPath, _jsonOutFolder, _outputScriptPath);
                    EditorUtility.DisplayDialog("Done", "Generation completed.", "OK");
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                }
            }

            if (GUILayout.Button("Open JSON Folder"))
            {
                if (Directory.Exists(ToAbsolute(_jsonOutFolder)))
                    EditorUtility.RevealInFinder(ToAbsolute(_jsonOutFolder));
                else
                    EditorUtility.DisplayDialog("Info", "JSON folder does not exist yet.", "OK");
            }

            if (GUILayout.Button("Open Script Folder"))
            {
                if (Directory.Exists(ToAbsolute(_outputScriptPath)))
                    EditorUtility.RevealInFinder(ToAbsolute(_outputScriptPath));
                else
                    EditorUtility.DisplayDialog("Info", "Script folder does not exist yet.", "OK");
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "• Place .xlsx under the XLSX Folder.\n" +
            "• JSON will be written to the JSON Output Folder.\n" +
            "• Row classes and GameDataRegistry/StatType will be written to the Script Output Folder.\n" +
            "• ExcelDataReader(.DataSet) DLLs are required (Editor only).",
            MessageType.Info);

        if (GUI.changed) SavePrefs();
    }

    void DrawPathPicker(string label, ref string field, string panelTitle)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(label);
            field = EditorGUILayout.TextField(field);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var startDir = string.IsNullOrEmpty(field) ? Application.dataPath : ToAbsolute(field);
                var sel = EditorUtility.OpenFolderPanel(panelTitle, startDir, "");
                if (!string.IsNullOrEmpty(sel))
                    field = ToProjectRelativeIfPossible(sel);
            }
        }
    }

    // 절대경로 → 프로젝트 상대(가능하면), 아니면 절대 유지
    static string ToProjectRelativeIfPossible(string absolutePath)
    {
        absolutePath = absolutePath.Replace('\\', '/');
        var proj = Application.dataPath.Replace('\\', '/');
        proj = proj.Substring(0, proj.Length - "Assets".Length); // 프로젝트 루트
        if (absolutePath.StartsWith(proj, StringComparison.OrdinalIgnoreCase))
            return absolutePath.Substring(proj.Length);
        return absolutePath; // 프로젝트 밖이면 절대경로 유지
    }

    // 프로젝트 상대→절대, 절대면 그대로
    static string ToAbsolute(string maybeProjectRelative)
    {
        if (string.IsNullOrEmpty(maybeProjectRelative)) return maybeProjectRelative;
        if (Path.IsPathRooted(maybeProjectRelative)) return Path.GetFullPath(maybeProjectRelative).Replace('\\', '/');
        var root = Application.dataPath.Replace('\\', '/');
        root = root.Substring(0, root.Length - "Assets".Length);
        return Path.GetFullPath(Path.Combine(root, maybeProjectRelative)).Replace('\\', '/');
    }

    static bool IsUnderResources(string path)
    {
        var resRoot = ToAbsolute("Assets/Resources").Replace('\\', '/');
        var pathAbs = ToAbsolute(path).Replace('\\', '/');
        return pathAbs.StartsWith(resRoot, StringComparison.OrdinalIgnoreCase);
    }

    static string EscapeForCSharp(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    // ─────────────────────────────────────────────────────────────
    // 메인 실행
    // ─────────────────────────────────────────────────────────────
    public static void Run(string xlsxFolderPath, string jsonOutFolder, string outputScriptPath)
    {
        if (string.IsNullOrEmpty(xlsxFolderPath) ||
            string.IsNullOrEmpty(jsonOutFolder) ||
            string.IsNullOrEmpty(outputScriptPath))
            throw new Exception("Paths are not set.");

        Directory.CreateDirectory(ToAbsolute(jsonOutFolder));
        Directory.CreateDirectory(ToAbsolute(outputScriptPath));

        var metas = new List<CsvMeta>();
        var statTypeValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var xlsx in Directory.GetFiles(ToAbsolute(xlsxFolderPath), "*.xlsx", SearchOption.TopDirectoryOnly))
        {
            using var fs = File.Open(xlsx, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(fs);

            var conf = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            };
            var ds = reader.AsDataSet(conf);
            if (ds == null || ds.Tables.Count == 0)
            {
                Debug.LogWarning($"[Gen] No sheets in {xlsx}");
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(xlsx);
            var table = PickBestTable(ds, fileName);
            if (table == null || table.Columns.Count == 0 || table.Rows.Count == 0)
            {
                Debug.LogWarning($"[Gen] Empty or no table in {xlsx}");
                continue;
            }

            // headers
            string[] headers = new string[table.Columns.Count];
            for (int c = 0; c < table.Columns.Count; c++)
                headers[c] = Clean(table.Columns[c].ColumnName);

            // type inference (bool/int/float/string)
            var fieldTypes = new string[headers.Length];
            for (int c = 0; c < headers.Length; c++)
            {
                string h = headers[c];
                if (EqualsIgnoreCase(h, "statType"))
                {
                    fieldTypes[c] = "StatType";
                    continue;
                }
                fieldTypes[c] = DecideTypeForColumn(table, c);
            }

            // enum values
            int statIdx = IndexOfIgnoreCase(headers, "statType");
            if (statIdx >= 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    var raw = row[statIdx]?.ToString();
                    raw = Clean(raw);
                    if (!string.IsNullOrEmpty(raw)) statTypeValues.Add(raw);
                }
            }

            // JSON out
            string jsonOut = Path.Combine(ToAbsolute(jsonOutFolder), fileName + ".json");
            WriteJsonFromDataTable(jsonOut, table, headers);

            metas.Add(new CsvMeta
            {
                FileName = fileName,
                Headers = headers,
                FieldTypes = fieldTypes
            });

            Debug.Log($"[Gen] Wrote JSON: {jsonOut} (rows: {table.Rows.Count})");
        }

        // outputs
        GenerateStatTypeEnum(statTypeValues, Path.Combine(ToAbsolute(outputScriptPath), "StatType.cs"));
        foreach (var m in metas) GenerateRowClass(m, ToAbsolute(outputScriptPath));
        GenerateRegistry(
            metas,
            Path.Combine(ToAbsolute(outputScriptPath), "GameDataRegistry.cs"),
            ToAbsolute(jsonOutFolder) // 절대경로 그대로 넘김
        );

        AssetDatabase.Refresh();
        Debug.Log("✅ XLSX→JSON/Classes/Registry 생성 완료");
    }

    // ─────────────────────────────────────────────────────────────
    // Pick best sheet
    // ─────────────────────────────────────────────────────────────
    private static DataTable PickBestTable(DataSet ds, string fileName)
    {
        if (ds == null || ds.Tables.Count == 0) return null;
        string[] want = { "Icon", "ID", "Name", "Desc" };

        DataTable best = null;
        int bestScore = int.MinValue;

        foreach (DataTable t in ds.Tables)
        {
            if (t.Columns.Count == 0) continue;

            int score = 0;
            for (int c = 0; c < t.Columns.Count; c++)
            {
                string h = Clean(t.Columns[c].ColumnName);
                foreach (var w in want)
                    if (string.Equals(h, w, StringComparison.OrdinalIgnoreCase))
                        score++;
            }
            // slight bias toward more columns (more data)
            score += t.Columns.Count / 10;

            if (score > bestScore)
            {
                best = t;
                bestScore = score;
            }
        }

        if (best != null)
        {
            var headers = Enumerable.Range(0, best.Columns.Count)
                .Select(ci => Clean(best.Columns[ci].ColumnName))
                .ToArray();
            Debug.Log($"[Gen] Picked sheet for {fileName}: \"{best.TableName}\"  headers=[{string.Join(", ", headers)}]");
        }
        else
        {
            Debug.LogWarning($"[Gen] No suitable sheet found in {fileName}");
        }
        return best ?? ds.Tables[0];
    }

    // ─────────────────────────────────────────────────────────────
    // meta
    // ─────────────────────────────────────────────────────────────
    private class CsvMeta
    {
        public string FileName;
        public string[] Headers;
        public string[] FieldTypes; // "int"/"float"/"bool"/"string"/"StatType"
        public string ClassName => MakeSafeTypeName(FileName) + "Row";
    }

    // ─────────────────────────────────────────────────────────────
    // type inference
    // ─────────────────────────────────────────────────────────────
    private static string DecideTypeForColumn(DataTable t, int col)
    {
        bool any = false;
        bool allBool = true;
        bool allNumeric = true;
        bool anyFloat = false;

        foreach (DataRow r in t.Rows)
        {
            var o = r[col];
            if (o == null || o == DBNull.Value) continue;

            string s = Clean(o.ToString());
            if (string.IsNullOrEmpty(s) || IsNaToken(s)) continue;

            any = true;

            if (IsBoolToken(s))
            {
                continue; // still bool-ok
            }
            else
            {
                allBool = false;
            }

            string n = NormalizeNumeric(s);
            if (float.TryParse(n, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                if (!int.TryParse(n, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    anyFloat = true;
            }
            else
            {
                allNumeric = false;
            }
        }

        if (!any) return "string";
        if (allBool && !allNumeric) return "bool";
        if (allNumeric) return anyFloat ? "float" : "int";
        return "string";
    }

    // ─────────────────────────────────────────────────────────────
    // JSON write
    // ─────────────────────────────────────────────────────────────
    private static void WriteJsonFromDataTable(string jsonPath, DataTable table, string[] headers)
    {
        var rows = new List<Dictionary<string, object>>(table.Rows.Count);

        foreach (DataRow dr in table.Rows)
        {
            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < headers.Length; c++)
            {
                string key = headers[c];
                object val = dr[c];

                if (val == null || val == DBNull.Value)
                {
                    row[key] = ""; // keep column with empty string
                    continue;
                }

                string raw = val.ToString() ?? "";
                string s = Clean(raw);

                if (string.IsNullOrEmpty(s))
                {
                    row[key] = "";
                    continue;
                }

                string norm = NormalizeNumeric(s);

                if (double.TryParse(norm, NumberStyles.Float, CultureInfo.InvariantCulture, out var nd))
                {
                    row[key] = nd;
                }
                else if (bool.TryParse(s, out var nb) || s == "0" || s == "1")
                {
                    row[key] = (s == "1") ? true : (s == "0") ? false : nb;
                }
                else
                {
                    row[key] = raw; // verbatim string (e.g., "Icon/Weapon/1_BrokenSword")
                }
            }

            rows.Add(row);
        }

        var json = MiniJsonWrite(rows);
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
        File.WriteAllText(jsonPath, json, new UTF8Encoding(false));
    }

    // ─────────────────────────────────────────────────────────────
    // Row class
    // ─────────────────────────────────────────────────────────────
    private static void GenerateRowClass(CsvMeta m, string outputScriptPath)
    {
        var sb = new StringBuilder(4096);
        sb.AppendLine("// Auto-generated class");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"public class {m.ClassName}");
        sb.AppendLine("{");

        var used = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < m.Headers.Length; i++)
        {
            string member = MakeSafeMemberName(m.Headers[i]);
            int suf = 1;
            string uniq = member;
            while (!used.Add(uniq))
                uniq = member + "_" + (++suf);

            sb.AppendLine($"    public {m.FieldTypes[i]} {uniq};");
        }
        sb.AppendLine("}");

        File.WriteAllText(Path.Combine(outputScriptPath, m.ClassName + ".cs"), sb.ToString(), new UTF8Encoding(false));
    }

    // ─────────────────────────────────────────────────────────────
    // Registry (loads JSON)  ← GetInt/ GetFloat 포함 + 경로 폴백
    // ─────────────────────────────────────────────────────────────
    private static void GenerateRegistry(List<CsvMeta> metas, string registryFilePath, string jsonOutFolderAbs)
    {
        // Resources 키 prefix 계산
        var resRootAbs = ToAbsolute("Assets/Resources").Replace('\\', '/');
        var jsonAbs = ToAbsolute(jsonOutFolderAbs).Replace('\\', '/');
        bool inResources = jsonAbs.StartsWith(resRootAbs, StringComparison.OrdinalIgnoreCase);
        string keyPrefix = inResources
            ? jsonAbs.Substring(resRootAbs.Length).TrimStart('/', '\\')  // 예: "CSVDataJson"
            : null;

        // C# 리터럴용 이스케이프
        string keyPrefixLit = keyPrefix == null ? "null" : $"\"{EscapeForCSharp(keyPrefix)}\"";
        string jsonAbsLit = $"\"{EscapeForCSharp(jsonAbs)}\"";

        var sb = new StringBuilder(65536);
        sb.AppendLine("// Auto-generated registry (loads JSON generated from XLSX)");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("public static class GameDataRegistry");
        sb.AppendLine("{");

        foreach (var m in metas)
            sb.AppendLine($"    public static readonly List<{m.ClassName}> {m.FileName}List = new();");

        sb.AppendLine();
        sb.AppendLine($"    const string KEY_PREFIX = {keyPrefixLit};");
        sb.AppendLine($"    const string JSON_ABS   = {jsonAbsLit};");

        // GetInt: 공백/ NBSP만 제거 → 정수/실수/과학표기 허용
        sb.AppendLine();
        sb.AppendLine("    static int GetInt(Dictionary<string, object> m, string k, int def = 0)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (m == null || !m.TryGetValue(k, out var v) || v == null) return def;");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            if (v is int i) return i;");
        sb.AppendLine("            if (v is long l) return (int)l;");
        sb.AppendLine("            var s = v.ToString();");
        sb.AppendLine("            if (string.IsNullOrWhiteSpace(s)) return def;");
        sb.AppendLine("            s = s.Replace(\"\\u00A0\", string.Empty).Replace(\" \", string.Empty);");
        sb.AppendLine("            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))");
        sb.AppendLine("                return (int)Math.Round(d);");
        sb.AppendLine("            return Convert.ToInt32(s, CultureInfo.InvariantCulture);");
        sb.AppendLine("        }");
        sb.AppendLine("        catch { return def; }");
        sb.AppendLine("    }");

        // GetFloat: 안전 파서 (스페이스/NBSP만 제거)
        sb.AppendLine();
        sb.AppendLine("    static float GetFloat(Dictionary<string, object> m, string k, float def = 0f)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (m == null || !m.TryGetValue(k, out var v) || v == null) return def;");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            if (v is float f)   return f;");
        sb.AppendLine("            if (v is double d)  return (float)d;");
        sb.AppendLine("            if (v is int i)     return i;");
        sb.AppendLine("            if (v is long l)    return l;");
        sb.AppendLine("            var s = v.ToString();");
        sb.AppendLine("            if (string.IsNullOrWhiteSpace(s)) return def;");
        sb.AppendLine("            s = s.Replace(\"\\u00A0\", \"\").Replace(\" \", \"\");");
        sb.AppendLine("            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dd))");
        sb.AppendLine("                return (float)dd;");
        sb.AppendLine("            return Convert.ToSingle(s, CultureInfo.InvariantCulture);");
        sb.AppendLine("        }");
        sb.AppendLine("        catch { return def; }");
        sb.AppendLine("    }");

        sb.AppendLine();
        sb.AppendLine("    static bool   GetBool  (Dictionary<string, object> m, string k, bool def=false){ if(m==null||!m.TryGetValue(k,out var v)) return def; if(v is bool b) return b; if(v is IConvertible c){ var s=c.ToString(CultureInfo.InvariantCulture); if(bool.TryParse(s,out var bb)) return bb; if(s==\"1\")return true; if(s==\"0\")return false;} return def; }");
        sb.AppendLine("    static string GetString(Dictionary<string, object> m, string k, string def=\"\"){ if(m==null||!m.TryGetValue(k,out var v)||v==null) return def; return v.ToString(); }");
        sb.AppendLine("    static StatType ParseStatType(string s){ if(string.IsNullOrEmpty(s)) return StatType.Unknown; var key=s.Trim().Replace(\" \",\"_\").Replace(\"-\",\"_\"); if(!Enum.TryParse<StatType>(key,true,out var e)) e=StatType.Unknown; return e; }");

        // 미니 JSON 파서 (간단)
        sb.AppendLine();
        sb.AppendLine("    static class MiniJsonLite");
        sb.AppendLine("    {");
        sb.AppendLine("        public static List<Dictionary<string, object>> ParseArray(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrEmpty(json)) return null;");
        sb.AppendLine("            int i=0; SkipWs(json, ref i); if(i>=json.Length||json[i]!='[') return null; i++;");
        sb.AppendLine("            var list = new List<Dictionary<string, object>>();");
        sb.AppendLine("            while(true){ SkipWs(json, ref i); if(i>=json.Length) break; if(json[i]==']'){ i++; break; } var obj=ParseObject(json, ref i); if(obj==null) break; list.Add(obj); SkipWs(json, ref i); if(i<json.Length && json[i]==','){ i++; continue; } }");
        sb.AppendLine("            return list;");
        sb.AppendLine("        }");
        sb.AppendLine("        static Dictionary<string, object> ParseObject(string s, ref int i)");
        sb.AppendLine("        {");
        sb.AppendLine("            SkipWs(s, ref i); if(i>=s.Length||s[i]!='{') return null; i++; var d=new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);");
        sb.AppendLine("            while(true){ SkipWs(s, ref i); if(i>=s.Length) break; if(s[i]=='}'){ i++; break; } var key=ParseString(s, ref i); SkipWs(s, ref i); if(i>=s.Length||s[i] != ':') break; i++; var val=ParseValue(s, ref i); d[key]=val; SkipWs(s, ref i); if(i<s.Length && s[i]==','){ i++; continue; } }");
        sb.AppendLine("            return d;");
        sb.AppendLine("        }");
        sb.AppendLine("        static object ParseValue(string s, ref int i)");
        sb.AppendLine("        {");
        sb.AppendLine("            SkipWs(s, ref i); if(i>=s.Length) return null; char c=s[i];");
        sb.AppendLine("            if(c=='\"') return ParseString(s, ref i);");
        sb.AppendLine("            if(c=='{')  return ParseObject(s, ref i);");
        sb.AppendLine("            if(c=='['){ var arr=new List<object>(); i++; while(true){ SkipWs(s, ref i); if(i>=s.Length) break; if(s[i]==']'){ i++; break; } var v=ParseValue(s, ref i); arr.Add(v); SkipWs(s, ref i); if(i<s.Length && s[i]==','){ i++; continue; } } return arr; }");
        sb.AppendLine("            int start=i; while(i<s.Length && \",}]\\t\\r\\n \".IndexOf(s[i])<0) i++; var token=s.Substring(start,i-start);");
        sb.AppendLine("            if(string.Equals(token,\"true\",StringComparison.OrdinalIgnoreCase)) return true;");
        sb.AppendLine("            if(string.Equals(token,\"false\",StringComparison.OrdinalIgnoreCase)) return false;");
        sb.AppendLine("            if(string.Equals(token,\"null\",StringComparison.OrdinalIgnoreCase)) return null;");
        sb.AppendLine("            if(double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var num)) return num;");
        sb.AppendLine("            return token;");
        sb.AppendLine("        }");
        sb.AppendLine("        static string ParseString(string s, ref int i)");
        sb.AppendLine("        {");
        sb.AppendLine("            if(i>=s.Length || s[i] != '\"') return string.Empty; i++; var sb=new System.Text.StringBuilder();");
        sb.AppendLine("            while(i<s.Length){ char c=s[i++]; if(c=='\"') break; if(c=='\\\\'){ if(i>=s.Length) break; char e=s[i++]; switch(e){ case 'n': sb.Append('\\n'); break; case 'r': break; case 't': sb.Append('\\t'); break; case '\"': sb.Append('\"'); break; case '\\\\': sb.Append('\\\\'); break; default: sb.Append(e); break; } } else sb.Append(c);} return sb.ToString();");
        sb.AppendLine("        }");
        sb.AppendLine("        static void SkipWs(string s, ref int i){ while(i<s.Length){ char c=s[i]; if(c==' '||c=='\\t'||c=='\\n'||c=='\\r') i++; else break; } }");
        sb.AppendLine("    }");

        // 정적 생성자: JSON 로드 (Resources→폴백 파일 읽기)
        sb.AppendLine();
        sb.AppendLine("    static GameDataRegistry()");
        sb.AppendLine("    {");
        foreach (var m in metas) sb.AppendLine($"        Load_{m.FileName}();");
        sb.AppendLine("    }");

        sb.AppendLine();
        foreach (var m in metas)
        {
            string key = (keyPrefix == null) ? null : $"{keyPrefix}/{m.FileName}";
            string keyLit = key == null ? "null" : $"\"{EscapeForCSharp(key)}\"";

            sb.AppendLine($"    static void Load_{m.FileName}()");
            sb.AppendLine("    {");
            sb.AppendLine("        string jsonText = null;");
            sb.AppendLine("        // 1) Resources 우선");
            sb.AppendLine("        if (KEY_PREFIX != null)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var ta = Resources.Load<TextAsset>({keyLit});");
            sb.AppendLine("            if (ta != null) jsonText = ta.text;");
            sb.AppendLine("        }");
            sb.AppendLine("#if UNITY_EDITOR");
            sb.AppendLine("        // 2) 에디터 폴백: 파일 직접 읽기");
            sb.AppendLine("        if (string.IsNullOrEmpty(jsonText))");
            sb.AppendLine("        {");
            sb.AppendLine($"            var p = System.IO.Path.Combine(JSON_ABS, \"{m.FileName}.json\");");
            sb.AppendLine("            if (System.IO.File.Exists(p))");
            sb.AppendLine("                jsonText = System.IO.File.ReadAllText(p, new UTF8Encoding(false));");
            sb.AppendLine("        }");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("        if (string.IsNullOrEmpty(jsonText))");
            sb.AppendLine("        {");
            sb.AppendLine($"            Debug.LogError($\"[Registry] JSON not found: {{(KEY_PREFIX==null? JSON_ABS : (\"Resources/\"+KEY_PREFIX))}}/{m.FileName}.json\");");
            sb.AppendLine("            return;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        var rows = MiniJsonLite.ParseArray(jsonText);");
            sb.AppendLine("        if (rows == null) { Debug.LogError(\"[Registry] JSON parse failed\"); return; }");
            sb.AppendLine($"        var list = {m.FileName}List; list.Clear();");
            sb.AppendLine("        foreach (var map in rows)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var d = new {m.ClassName}();");

            // member assignment with unique names in same order as class generation
            var usedMembers = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m.Headers.Length; i++)
            {
                string h = m.Headers[i];
                string t = m.FieldTypes[i];

                string member = MakeSafeMemberName(h);
                int suf = 1;
                string uniq = member;
                while (!usedMembers.Add(uniq))
                    uniq = member + "_" + (++suf);

                if (t == "int") sb.AppendLine($"            d.{uniq} = GetInt(map, \"{h}\", 0);");
                else if (t == "float") sb.AppendLine($"            d.{uniq} = GetFloat(map, \"{h}\", 0f);");
                else if (t == "bool") sb.AppendLine($"            d.{uniq} = GetBool(map, \"{h}\", false);");
                else if (t == "StatType") sb.AppendLine($"            d.{uniq} = ParseStatType(GetString(map, \"{h}\", \"\"));");
                else sb.AppendLine($"            d.{uniq} = GetString(map, \"{h}\", \"\");");
            }
            sb.AppendLine("            list.Add(d);");
            sb.AppendLine("        }");
            sb.AppendLine($"        Debug.Log($\"[Registry] {m.FileName} loaded: {{ {m.FileName}List.Count }} rows\");");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        File.WriteAllText(registryFilePath, sb.ToString(), new UTF8Encoding(false));
    }

    // ─────────────────────────────────────────────────────────────
    // StatType enum
    // ─────────────────────────────────────────────────────────────
    private static void GenerateStatTypeEnum(HashSet<string> rawValues, string statEnumFilePath)
    {
        var names = new List<string> { "Unknown" };
        if (rawValues != null)
        {
            foreach (var v in rawValues)
            {
                var id = ToEnumIdentifier(v);
                if (!string.IsNullOrEmpty(id) && !names.Contains(id))
                    names.Add(id);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated enum from XLSX statType values");
        sb.AppendLine("public enum StatType");
        sb.AppendLine("{");
        for (int i = 0; i < names.Count; i++)
            sb.AppendLine(i == 0 ? $"    {names[i]} = 0," : $"    {names[i]},");
        sb.AppendLine("}");
        File.WriteAllText(statEnumFilePath, sb.ToString(), new UTF8Encoding(false));
    }

    // ─────────────────────────────────────────────────────────────
    // utils
    // ─────────────────────────────────────────────────────────────
    private static string Clean(string s)
        => string.IsNullOrEmpty(s)
           ? string.Empty
           : s.Trim().Trim('\r', '\n', '\t', '\"', ' ')
               .Replace('\u00A0', ' ')
               .Trim();

    private static bool EqualsIgnoreCase(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static int IndexOfIgnoreCase(string[] arr, string target)
    {
        for (int i = 0; i < arr.Length; i++)
            if (EqualsIgnoreCase(Clean(arr[i]), target)) return i;
        return -1;
    }

    private static string NormalizeNumeric(string s)
    {
        s = Clean(s);
        if (string.IsNullOrEmpty(s)) return s;
        bool neg = s.StartsWith("(") && s.EndsWith(")");
        if (neg) s = s.Substring(1, s.Length - 2);
        s = s.Replace(",", "");
        if (s.EndsWith("%")) s = s.Substring(0, s.Length - 1);
        s = s.Trim();
        if (neg) s = "-" + s;
        return s;
    }

    private static bool IsBoolToken(string s)
    {
        s = Clean(s);
        return s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
               s == "0" || s == "1";
    }

    private static bool IsNaToken(string s)
    {
        s = Clean(s);
        return s.Equals("NA", StringComparison.OrdinalIgnoreCase) ||
               s.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
               s.Equals("-", StringComparison.OrdinalIgnoreCase);
    }

    private static string MakeSafeTypeName(string raw)
    {
        raw = Clean(raw);
        var sb = new StringBuilder(raw.Length + 4);
        foreach (var c in raw)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        var id = sb.ToString();
        if (string.IsNullOrEmpty(id)) id = "Sheet";
        if (char.IsDigit(id[0])) id = "_" + id;
        return id;
    }

    private static string MakeSafeMemberName(string raw)
    {
        raw = Clean(raw);
        var sb = new StringBuilder(raw.Length + 4);
        foreach (var c in raw)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        var id = sb.ToString();
        if (string.IsNullOrEmpty(id)) id = "Field";
        if (char.IsDigit(id[0])) id = "_" + id;
        return id;
    }

    private static string ToEnumIdentifier(string raw)
    {
        raw = Clean(raw);
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        var id = sb.ToString();
        if (string.IsNullOrEmpty(id)) id = "Unknown";
        if (char.IsDigit(id[0])) id = "_" + id;
        return id;
    }

    private static string MiniJsonWrite(List<Dictionary<string, object>> rows)
    {
        var sb = new StringBuilder(rows.Count * 64);
        sb.Append('[');
        for (int i = 0; i < rows.Count; i++)
        {
            if (i > 0) sb.Append(',');
            WriteDict(sb, rows[i]);
        }
        sb.Append(']');
        return sb.ToString();

        static void WriteDict(StringBuilder sb, Dictionary<string, object> d)
        {
            sb.Append('{');
            int k = 0;
            foreach (var kv in d)
            {
                if (k++ > 0) sb.Append(',');
                WriteString(sb, kv.Key);
                sb.Append(':');
                WriteVal(sb, kv.Value);
            }
            sb.Append('}');
        }
        static void WriteVal(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            switch (v)
            {
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case IFormattable f: sb.Append(f.ToString(null, CultureInfo.InvariantCulture)); break;
                default: WriteString(sb, v.ToString()); break;
            }
        }
        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                if (c == '"' || c == '\\') { sb.Append('\\').Append(c); }
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') { /* skip */ }
                else sb.Append(c);
            }
            sb.Append('"');
        }
    }
}
#endif
