# 파싱 전 Row 클래스 타입 분석 (BEFORE)

생성 일시: 2025-10-31

## 📊 전체 통계

### int 필드
- **총 개수**: 약 150개
- **주요 용도**: ID, HP, Gold, 각종 카운트, Tier, Level 등

### float 필드
- **총 개수**: 약 45개
- **주요 용도**: 확률(Probability), 퍼센트(Percent), 배율(Multiplier), 성장률(Growth) 등

---

## 📁 주요 Row 클래스별 상세

### MonstersRow.cs
```csharp
public int ID;        // ← BigNum으로 변환 예정
public int HP;        // ← BigNum으로 변환 예정
public string Name;
public string Image;
```

### WeaponsRow.cs
```csharp
public int ID;                      // ← BigNum
public int Rank;                    // ← BigNum
public int autoAttackDamage;        // ← BigNum
public int autoAttackDamageGrowth;  // ← BigNum
public int tapDamage;               // ← BigNum
public int tapDamageGrowth;         // ← BigNum
public int growthCost;              // ← BigNum
public int growthCostChange;        // ← BigNum
public float specialAbilityValue;   // ← 그대로 유지
public string Name;
public string Desc;
public string Icon;
public string IconRank;
public string specialAbilityType;
```

### StagesRow.cs
```csharp
public int ID;            // ← BigNum
public int Gold;          // ← BigNum (중요! 골드는 큰 숫자 가능)
public int monsterID;     // ← BigNum
public int isBoss;        // ← BigNum
public int bossTime;      // ← BigNum
public int HPMultiplyer;  // ← BigNum
public int ItemDrops;     // ← BigNum
public string Name;
public string Background;
```

### GoldDungeonRow.cs
```csharp
public int ID;              // ← BigNum
public int Tier;            // ← BigNum
public int Monster;         // ← BigNum
public int HPMultiplyer;    // ← BigNum
public int RewardItem;      // ← BigNum
public int RewardAmount;    // ← BigNum (중요! 보상은 큰 숫자 가능)
public int Time;            // ← BigNum
public int EnteranceItem;   // ← BigNum
public int EntranceCost;    // ← BigNum
public string Name;
public string Type;
```

### ArtifactsRow.cs
```csharp
public int ID;                        // ← BigNum
public int costGrowth;                // ← BigNum
public int maxLevel;                  // ← BigNum
public float damageMultiplier;        // ← 그대로 유지 (퍼센트/배율)
public float damageMultipleGrowth;    // ← 그대로 유지
public float Value;                   // ← 그대로 유지
public float valueGrowth;             // ← 그대로 유지
public StatType statType;
public string Name;
public string Desc;
public string Icon;
```

---

## 🎯 변환 규칙

### BigNum으로 변환될 필드 (정수)
- ✅ 모든 `public int` 필드
- 예: ID, HP, Gold, Damage, Cost, Count, Tier, Level, Amount 등

### 그대로 유지될 필드
- ✅ `public float` - 소수점 값 (확률, 퍼센트, 배율)
- ✅ `public string` - 문자열
- ✅ `public bool` - 불린
- ✅ `public StatType` - Enum

---

## 📝 예상 변환 결과

### 변환 전 (현재)
```csharp
public class WeaponsRow {
    public int autoAttackDamage;       // int 범위: ~21억
    public float specialAbilityValue;  // float (소수점)
}
```

### 변환 후 (예상)
```csharp
public class WeaponsRow {
    public BigNum autoAttackDamage;    // BigNum: 무한대
    public float specialAbilityValue;  // float 유지
}
```

---

## ⚠️ 중요 필드 (큰 숫자 예상)

게임 후반부에서 int 범위를 초과할 가능성이 높은 필드들:

1. **Gold** (StagesRow, 각종 Reward)
2. **HP** (MonstersRow)
3. **Damage** (WeaponsRow, SkillsRow)
4. **Cost** (각종 성장 비용)
5. **RewardAmount** (보상 수량)
6. **Experience** (경험치)

이 필드들이 BigNum으로 바뀌면 **무한 성장** 가능!

---

## 🔍 다음 단계

1. ✅ 현재 상태 기록 완료
2. ⏳ parser2에서 XLSX → JSON/Row 파싱 실행
3. ⏳ 파싱 후 Row 클래스 확인
4. ⏳ int → BigNum 변환 검증
5. ⏳ float 필드 유지 확인
