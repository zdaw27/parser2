// Auto-generated registry (loads JSON generated from XLSX)
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class GameDataRegistry
{
    public static readonly List<ArtifactCostsRow> ArtifactCostsList = new();
    public static readonly List<ArtifactsRow> ArtifactsList = new();
    public static readonly List<MonstersRow> MonstersList = new();
    public static readonly List<RebornRow> RebornList = new();
    public static readonly List<SkillsRow> SkillsList = new();
    public static readonly List<StagesRow> StagesList = new();
    public static readonly List<StatsRow> StatsList = new();
    public static readonly List<WeaponOptionsRow> WeaponOptionsList = new();
    public static readonly List<WeaponsRow> WeaponsList = new();

    static int GetInt(Dictionary<string, object> m, string k, int def = 0)
    {
        if (m == null || !m.TryGetValue(k, out var v) || v == null) return def;
        try
        {
            if (v is int i) return i;
            if (v is long l) return (int)l;
            var s = v.ToString();
            if (string.IsNullOrWhiteSpace(s)) return def;
            s = s.Replace("\u00A0", string.Empty).Replace(" ", string.Empty);
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return (int)Math.Round(d);
            return Convert.ToInt32(s, CultureInfo.InvariantCulture);
        }
        catch { return def; }
    }
    static float GetFloat(Dictionary<string, object> m, string k, float def = 0f)
    {
        if (m == null || !m.TryGetValue(k, out var v) || v == null) return def;
        try
        {
            if (v is float f)   return f;
            if (v is double d)  return (float)d;
            if (v is int i)     return i;
            if (v is long l)    return l;
            var s = v.ToString();
            if (string.IsNullOrWhiteSpace(s)) return def;
            s = s.Replace("\u00A0", "").Replace(" ", "");
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dd))
                return (float)dd;
            return Convert.ToSingle(s, CultureInfo.InvariantCulture);
        }
        catch { return def; }
    }
    static bool   GetBool  (Dictionary<string, object> m, string k, bool def=false){ if(m==null||!m.TryGetValue(k,out var v)) return def; if(v is bool b) return b; if(v is IConvertible c){ var s=c.ToString(CultureInfo.InvariantCulture); if(bool.TryParse(s,out var bb)) return bb; if(s=="1")return true; if(s=="0")return false;} return def; }
    static string GetString(Dictionary<string, object> m, string k, string def=""){ if(m==null||!m.TryGetValue(k,out var v)||v==null) return def; return v.ToString(); }
    static StatType ParseStatType(string s){ if(string.IsNullOrEmpty(s)) return StatType.Unknown; var key=s.Trim().Replace(" ","_").Replace("-","_"); if(!Enum.TryParse<StatType>(key,true,out var e)) e=StatType.Unknown; return e; }

    static class MiniJsonLite
    {
        public static List<Dictionary<string, object>> ParseArray(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int i=0; SkipWs(json, ref i); if(i>=json.Length||json[i]!='[') return null; i++;
            var list = new List<Dictionary<string, object>>();
            while(true){ SkipWs(json, ref i); if(i>=json.Length) break; if(json[i]==']'){ i++; break; } var obj=ParseObject(json, ref i); if(obj==null) break; list.Add(obj); SkipWs(json, ref i); if(i<json.Length && json[i]==','){ i++; continue; } }
            return list;
        }
        static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            SkipWs(s, ref i); if(i>=s.Length||s[i]!='{') return null; i++; var d=new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            while(true){ SkipWs(s, ref i); if(i>=s.Length) break; if(s[i]=='}'){ i++; break; } var key=ParseString(s, ref i); SkipWs(s, ref i); if(i>=s.Length||s[i] != ':') break; i++; var val=ParseValue(s, ref i); d[key]=val; SkipWs(s, ref i); if(i<s.Length && s[i]==','){ i++; continue; } }
            return d;
        }
        static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i); if(i>=s.Length) return null; char c=s[i];
            if(c=='"') return ParseString(s, ref i);
            if(c=='{')  return ParseObject(s, ref i);
            if(c=='['){ var arr=new List<object>(); i++; while(true){ SkipWs(s, ref i); if(i>=s.Length) break; if(s[i]==']'){ i++; break; } var v=ParseValue(s, ref i); arr.Add(v); SkipWs(s, ref i); if(i<s.Length && s[i]==','){ i++; continue; } } return arr; }
            int start=i; while(i<s.Length && ",}]\t\r\n ".IndexOf(s[i])<0) i++; var token=s.Substring(start,i-start);
            if(string.Equals(token,"true",StringComparison.OrdinalIgnoreCase)) return true;
            if(string.Equals(token,"false",StringComparison.OrdinalIgnoreCase)) return false;
            if(string.Equals(token,"null",StringComparison.OrdinalIgnoreCase)) return null;
            if(double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var num)) return num;
            return token;
        }
        static string ParseString(string s, ref int i)
        {
            if(i>=s.Length || s[i] != '"') return string.Empty; i++; var sb=new System.Text.StringBuilder();
            while(i<s.Length){ char c=s[i++]; if(c=='"') break; if(c=='\\'){ if(i>=s.Length) break; char e=s[i++]; switch(e){ case 'n': sb.Append('\n'); break; case 'r': break; case 't': sb.Append('\t'); break; case '"': sb.Append('"'); break; case '\\': sb.Append('\\'); break; default: sb.Append(e); break; } } else sb.Append(c);} return sb.ToString();
        }
        static void SkipWs(string s, ref int i){ while(i<s.Length){ char c=s[i]; if(c==' '||c=='\t'||c=='\n'||c=='\r') i++; else break; } }
    }

    static GameDataRegistry()
    {
        Load_ArtifactCosts();
        Load_Artifacts();
        Load_Monsters();
        Load_Reborn();
        Load_Skills();
        Load_Stages();
        Load_Stats();
        Load_WeaponOptions();
        Load_Weapons();
    }

    static void Load_ArtifactCosts()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/ArtifactCosts");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/ArtifactCosts"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = ArtifactCostsList; list.Clear();
        foreach (var map in rows)
        {
            var d = new ArtifactCostsRow();
            d.ID = GetInt(map, "ID", 0);
            d.ArtifactCount = GetInt(map, "ArtifactCount", 0);
            d.TreasureCount = GetInt(map, "TreasureCount", 0);
            list.Add(d);
        }
        Debug.Log($"[Registry] ArtifactCosts loaded: { ArtifactCostsList.Count } rows");
    }

    static void Load_Artifacts()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/Artifacts");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/Artifacts"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = ArtifactsList; list.Clear();
        foreach (var map in rows)
        {
            var d = new ArtifactsRow();
            d.ID = GetInt(map, "ID", 0);
            d.Name = GetString(map, "Name", "");
            d.Desc = GetString(map, "Desc", "");
            d.Icon = GetString(map, "Icon", "");
            d.costGrowth = GetInt(map, "costGrowth", 0);
            d.maxLevel = GetInt(map, "maxLevel", 0);
            d.damageMultiplier = GetFloat(map, "damageMultiplier", 0f);
            d.damageMultipleGrowth = GetFloat(map, "damageMultipleGrowth", 0f);
            d.statType = ParseStatType(GetString(map, "statType", ""));
            d.Value = GetFloat(map, "Value", 0f);
            d.valueGrowth = GetFloat(map, "valueGrowth", 0f);
            list.Add(d);
        }
        Debug.Log($"[Registry] Artifacts loaded: { ArtifactsList.Count } rows");
    }

    static void Load_Monsters()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/Monsters");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/Monsters"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = MonstersList; list.Clear();
        foreach (var map in rows)
        {
            var d = new MonstersRow();
            d.ID = GetInt(map, "ID", 0);
            d.Name = GetString(map, "Name", "");
            d.HP = GetInt(map, "HP", 0);
            d.Image = GetString(map, "Image", "");
            list.Add(d);
        }
        Debug.Log($"[Registry] Monsters loaded: { MonstersList.Count } rows");
    }

    static void Load_Reborn()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/Reborn");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/Reborn"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = RebornList; list.Clear();
        foreach (var map in rows)
        {
            var d = new RebornRow();
            d.ID = GetInt(map, "ID", 0);
            d.Stage = GetInt(map, "Stage", 0);
            d.Treasure = GetInt(map, "Treasure", 0);
            d.Fame = GetInt(map, "Fame", 0);
            list.Add(d);
        }
        Debug.Log($"[Registry] Reborn loaded: { RebornList.Count } rows");
    }

    static void Load_Skills()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/Skills");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/Skills"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = SkillsList; list.Clear();
        foreach (var map in rows)
        {
            var d = new SkillsRow();
            d.ID = GetInt(map, "ID", 0);
            d.Name = GetString(map, "Name", "");
            d.Desc = GetString(map, "Desc", "");
            d.Icon = GetString(map, "Icon", "");
            d.Type = GetString(map, "Type", "");
            d.Value = GetInt(map, "Value", 0);
            d.valueGrowth = GetFloat(map, "valueGrowth", 0f);
            d.coolTime = GetInt(map, "coolTime", 0);
            d.duration = GetInt(map, "duration", 0);
            list.Add(d);
        }
        Debug.Log($"[Registry] Skills loaded: { SkillsList.Count } rows");
    }

    static void Load_Stages()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/Stages");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/Stages"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = StagesList; list.Clear();
        foreach (var map in rows)
        {
            var d = new StagesRow();
            d.ID = GetInt(map, "ID", 0);
            d.Name = GetString(map, "Name", "");
            d.Gold = GetInt(map, "Gold", 0);
            d.monsterID = GetInt(map, "monsterID", 0);
            d.isBoss = GetInt(map, "isBoss", 0);
            d.bossTime = GetInt(map, "bossTime", 0);
            d.HPMultiplyer = GetInt(map, "HPMultiplyer", 0);
            d.Background = GetString(map, "Background", "");
            list.Add(d);
        }
        Debug.Log($"[Registry] Stages loaded: { StagesList.Count } rows");
    }

    static void Load_Stats()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/Stats");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/Stats"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = StatsList; list.Clear();
        foreach (var map in rows)
        {
            var d = new StatsRow();
            d.ID = GetInt(map, "ID", 0);
            d.statType = ParseStatType(GetString(map, "statType", ""));
            d.Desc = GetString(map, "Desc", "");
            d.Funtest = GetString(map, "Funtest", "");
            list.Add(d);
        }
        Debug.Log($"[Registry] Stats loaded: { StatsList.Count } rows");
    }

    static void Load_WeaponOptions()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/WeaponOptions");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/WeaponOptions"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = WeaponOptionsList; list.Clear();
        foreach (var map in rows)
        {
            var d = new WeaponOptionsRow();
            d.ID = GetInt(map, "ID", 0);
            d.Name = GetString(map, "Name", "");
            d.statType = ParseStatType(GetString(map, "statType", ""));
            d.minCommon = GetFloat(map, "minCommon", 0f);
            d.maxCommon = GetFloat(map, "maxCommon", 0f);
            d.growthCommon = GetFloat(map, "growthCommon", 0f);
            d.minUncommon = GetFloat(map, "minUncommon", 0f);
            d.maxUncommon = GetFloat(map, "maxUncommon", 0f);
            d.growthUncommon = GetFloat(map, "growthUncommon", 0f);
            d.minRare = GetFloat(map, "minRare", 0f);
            d.maxRare = GetFloat(map, "maxRare", 0f);
            d.growthRare = GetFloat(map, "growthRare", 0f);
            d.minLegend = GetFloat(map, "minLegend", 0f);
            d.maxLegend = GetFloat(map, "maxLegend", 0f);
            d.growthLegend = GetFloat(map, "growthLegend", 0f);
            d.minMythic = GetFloat(map, "minMythic", 0f);
            d.maxMythic = GetFloat(map, "maxMythic", 0f);
            d.growthMythic = GetFloat(map, "growthMythic", 0f);
            list.Add(d);
        }
        Debug.Log($"[Registry] WeaponOptions loaded: { WeaponOptionsList.Count } rows");
    }

    static void Load_Weapons()
    {
        var ta = Resources.Load<TextAsset>("CSVDataJson/Weapons");
        if (ta == null || string.IsNullOrEmpty(ta.text)) { Debug.LogError($"[Registry] JSON not found: CSVDataJson/Weapons"); return; }
        var rows = MiniJsonLite.ParseArray(ta.text);
        if (rows == null) { Debug.LogError("[Registry] JSON parse failed"); return; }
        var list = WeaponsList; list.Clear();
        foreach (var map in rows)
        {
            var d = new WeaponsRow();
            d.ID = GetInt(map, "ID", 0);
            d.Name = GetString(map, "Name", "");
            d.Desc = GetString(map, "Desc", "");
            d.Rank = GetInt(map, "Rank", 0);
            d.specialAbilityType = GetString(map, "specialAbilityType", "");
            d.specialAbilityValue = GetFloat(map, "specialAbilityValue", 0f);
            d.autoAttackDamage = GetInt(map, "autoAttackDamage", 0);
            d.autoAttackDamageGrowth = GetInt(map, "autoAttackDamageGrowth", 0);
            d.tapDamage = GetInt(map, "tapDamage", 0);
            d.tapDamageGrowth = GetInt(map, "tapDamageGrowth", 0);
            d.growthCost = GetInt(map, "growthCost", 0);
            d.growthCostChange = GetInt(map, "growthCostChange", 0);
            list.Add(d);
        }
        Debug.Log($"[Registry] Weapons loaded: { WeaponsList.Count } rows");
    }

}
