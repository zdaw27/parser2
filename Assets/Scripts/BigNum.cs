using System;
using System.Globalization;
#if FIREBASE_AVAILABLE
using Firebase.Firestore;
#endif

[Serializable]
#if FIREBASE_AVAILABLE
[FirestoreData]
#endif
public struct BigNum : IComparable<BigNum>, IEquatable<BigNum>
{
#if FIREBASE_AVAILABLE
    [FirestoreProperty]
#endif
    public double m;  // 1 ≤ m < 10000 (0이면 e4=0)

#if FIREBASE_AVAILABLE
    [FirestoreProperty]
#endif
    public int e4;    // 0=원, 1=만, 2=억, 3=조, 4=경 ...

    public BigNum(double m, int e4)
    {
        this.m = m;
        this.e4 = e4;
        Normalize(ref this.m, ref this.e4);
    }

    // ─────────────────────────────────────────────────────────────
    // 정규화: 1 ≤ |m| < 10000 범위로 맞춤
    // ─────────────────────────────────────────────────────────────
    public static void Normalize(ref double m, ref int e4)
    {
        if (m == 0 || double.IsNaN(m) || double.IsInfinity(m))
        {
            m = 0;
            e4 = 0;
            return;
        }

        const double BASE = 10000.0; // 10^4
        double absm = Math.Abs(m);

        // 한 번에 스케일 맞춤
        int k = (int)Math.Floor(Math.Log10(absm) / 4.0);
        if (k != 0)
        {
            m *= Math.Pow(BASE, -k); // m /= BASE^k
            e4 += k;
            absm = Math.Abs(m);
        }

        // 미세 조정
        if (absm >= BASE)
        {
            m /= BASE;
            e4++;
        }
        else if (absm > 0 && absm < 1.0)
        {
            m *= BASE;
            e4--;
        }

        if (m == 0) e4 = 0;
    }

    public void Normalize()
    {
        double tempM = m;
        int tempE4 = e4;
        Normalize(ref tempM, ref tempE4);
        m = tempM;
        e4 = tempE4;
    }

    // ─────────────────────────────────────────────────────────────
    // 변환: 기본 타입 → BigNum
    // ─────────────────────────────────────────────────────────────
    public static BigNum FromInt(int v)
    {
        return new BigNum(v, 0);
    }

    public static BigNum FromLong(long v)
    {
        return new BigNum(v, 0);
    }

    public static BigNum FromDouble(double v)
    {
        return new BigNum(v, 0);
    }

    // 테이블 숫자(원 단위 문자열) → BigNum
    public static BigNum FromRawString(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return new BigNum(0, 0);

        s = s.Replace(",", "").Replace(" ", "").Trim();

        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
            return new BigNum(val, 0);

        return new BigNum(0, 0);
    }

    // ─────────────────────────────────────────────────────────────
    // 변환: BigNum → 기본 타입
    // ─────────────────────────────────────────────────────────────
    public bool TryToInt(out int v)
    {
        v = 0;
        if (e4 > 0 || e4 < -1) return false;

        double result = m * Math.Pow(10000, e4);
        if (result > int.MaxValue || result < int.MinValue) return false;

        v = (int)Math.Round(result);
        return true;
    }

    public bool TryToLong(out long v)
    {
        v = 0;
        if (e4 > 1 || e4 < -1) return false;

        double result = m * Math.Pow(10000, e4);
        if (result > long.MaxValue || result < long.MinValue) return false;

        v = (long)Math.Round(result);
        return true;
    }

    public double ToDouble()
    {
        return m * Math.Pow(10000, e4);
    }

    // 안전하지 않은 변환 (오버플로우 무시)
    public int ToIntUnsafe()
    {
        return (int)Math.Round(ToDouble());
    }

    public long ToLongUnsafe()
    {
        return (long)Math.Round(ToDouble());
    }

    // 안전한 변환 (범위 초과 시 기본값 반환)
    public int ToIntSafe(int defaultValue = 0)
    {
        return TryToInt(out int result) ? result : defaultValue;
    }

    public long ToLongSafe(long defaultValue = 0)
    {
        return TryToLong(out long result) ? result : defaultValue;
    }

    // ─────────────────────────────────────────────────────────────
    // 비교
    // ─────────────────────────────────────────────────────────────
    public int CompareTo(BigNum other)
    {
        // 부호 체크
        int signThis = Math.Sign(m);
        int signOther = Math.Sign(other.m);

        if (signThis != signOther) return signThis.CompareTo(signOther);
        if (signThis == 0) return 0; // 둘 다 0

        // e4 비교 (큰 지수가 더 큼)
        if (e4 != other.e4)
        {
            // 음수일 때는 반대
            return signThis > 0 ? e4.CompareTo(other.e4) : other.e4.CompareTo(e4);
        }

        // e4 같으면 m 비교
        return m.CompareTo(other.m);
    }

    public bool Equals(BigNum other)
    {
        return CompareTo(other) == 0;
    }

    public override bool Equals(object obj)
    {
        return obj is BigNum other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(m, e4);
    }

    // ─────────────────────────────────────────────────────────────
    // 사칙연산
    // ─────────────────────────────────────────────────────────────
    public static BigNum operator +(BigNum a, BigNum b)
    {
        // 같은 지수로 맞춤
        if (a.e4 > b.e4)
        {
            int diff = a.e4 - b.e4;
            double bAdjusted = b.m / Math.Pow(10000, diff);
            return new BigNum(a.m + bAdjusted, a.e4);
        }
        else if (b.e4 > a.e4)
        {
            int diff = b.e4 - a.e4;
            double aAdjusted = a.m / Math.Pow(10000, diff);
            return new BigNum(aAdjusted + b.m, b.e4);
        }
        else
        {
            return new BigNum(a.m + b.m, a.e4);
        }
    }

    public static BigNum operator -(BigNum a, BigNum b)
    {
        return a + new BigNum(-b.m, b.e4);
    }

    public static BigNum operator *(BigNum a, BigNum b)
    {
        return new BigNum(a.m * b.m, a.e4 + b.e4);
    }

    public static BigNum operator /(BigNum a, BigNum b)
    {
        if (b.m == 0) throw new DivideByZeroException("BigNum division by zero");
        return new BigNum(a.m / b.m, a.e4 - b.e4);
    }

    public static BigNum operator -(BigNum a)
    {
        return new BigNum(-a.m, a.e4);
    }

    // ─────────────────────────────────────────────────────────────
    // 비교 연산자
    // ─────────────────────────────────────────────────────────────
    public static bool operator ==(BigNum a, BigNum b) => a.Equals(b);
    public static bool operator !=(BigNum a, BigNum b) => !a.Equals(b);
    public static bool operator <(BigNum a, BigNum b) => a.CompareTo(b) < 0;
    public static bool operator >(BigNum a, BigNum b) => a.CompareTo(b) > 0;
    public static bool operator <=(BigNum a, BigNum b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BigNum a, BigNum b) => a.CompareTo(b) >= 0;

    // ─────────────────────────────────────────────────────────────
    // int/long과의 연산자 오버로딩 (암묵적 사용 편의성)
    // ─────────────────────────────────────────────────────────────

    // BigNum과 int 비교
    public static bool operator ==(BigNum a, int b) => a == FromInt(b);
    public static bool operator !=(BigNum a, int b) => a != FromInt(b);
    public static bool operator <(BigNum a, int b) => a < FromInt(b);
    public static bool operator >(BigNum a, int b) => a > FromInt(b);
    public static bool operator <=(BigNum a, int b) => a <= FromInt(b);
    public static bool operator >=(BigNum a, int b) => a >= FromInt(b);

    public static bool operator ==(int a, BigNum b) => FromInt(a) == b;
    public static bool operator !=(int a, BigNum b) => FromInt(a) != b;
    public static bool operator <(int a, BigNum b) => FromInt(a) < b;
    public static bool operator >(int a, BigNum b) => FromInt(a) > b;
    public static bool operator <=(int a, BigNum b) => FromInt(a) <= b;
    public static bool operator >=(int a, BigNum b) => FromInt(a) >= b;

    // BigNum과 long 비교
    public static bool operator ==(BigNum a, long b) => a == FromLong(b);
    public static bool operator !=(BigNum a, long b) => a != FromLong(b);
    public static bool operator <(BigNum a, long b) => a < FromLong(b);
    public static bool operator >(BigNum a, long b) => a > FromLong(b);
    public static bool operator <=(BigNum a, long b) => a <= FromLong(b);
    public static bool operator >=(BigNum a, long b) => a >= FromLong(b);

    public static bool operator ==(long a, BigNum b) => FromLong(a) == b;
    public static bool operator !=(long a, BigNum b) => FromLong(a) != b;
    public static bool operator <(long a, BigNum b) => FromLong(a) < b;
    public static bool operator >(long a, BigNum b) => FromLong(a) > b;
    public static bool operator <=(long a, BigNum b) => FromLong(a) <= b;
    public static bool operator >=(long a, BigNum b) => FromLong(a) >= b;

    // BigNum과 int 산술연산
    public static BigNum operator +(BigNum a, int b) => a + FromInt(b);
    public static BigNum operator +(int a, BigNum b) => FromInt(a) + b;
    public static BigNum operator -(BigNum a, int b) => a - FromInt(b);
    public static BigNum operator -(int a, BigNum b) => FromInt(a) - b;
    public static BigNum operator *(BigNum a, int b) => a * FromInt(b);
    public static BigNum operator *(int a, BigNum b) => FromInt(a) * b;
    public static BigNum operator /(BigNum a, int b) => a / FromInt(b);
    public static BigNum operator /(int a, BigNum b) => FromInt(a) / b;

    // BigNum과 long 산술연산
    public static BigNum operator +(BigNum a, long b) => a + FromLong(b);
    public static BigNum operator +(long a, BigNum b) => FromLong(a) + b;
    public static BigNum operator -(BigNum a, long b) => a - FromLong(b);
    public static BigNum operator -(long a, BigNum b) => FromLong(a) - b;
    public static BigNum operator *(BigNum a, long b) => a * FromLong(b);
    public static BigNum operator *(long a, BigNum b) => FromLong(a) * b;
    public static BigNum operator /(BigNum a, long b) => a / FromLong(b);
    public static BigNum operator /(long a, BigNum b) => FromLong(a) / b;

    // ─────────────────────────────────────────────────────────────
    // 유틸리티
    // ─────────────────────────────────────────────────────────────
    public static BigNum Max(BigNum a, BigNum b)
    {
        return a > b ? a : b;
    }

    public static BigNum Min(BigNum a, BigNum b)
    {
        return a < b ? a : b;
    }

    public static BigNum Abs(BigNum a)
    {
        return new BigNum(Math.Abs(a.m), a.e4);
    }

    public static BigNum Floor(BigNum a)
    {
        return new BigNum(System.Math.Floor(a.m), a.e4);
    }

    public static BigNum Ceil(BigNum a)
    {
        return new BigNum(System.Math.Ceiling(a.m), a.e4);
    }

    // ─────────────────────────────────────────────────────────────
    // 문자열 변환 (한국 단위 표시)
    // ─────────────────────────────────────────────────────────────
    private static readonly string[] KoreanUnits =
    {
        "",     // e4=0: 원 (단위 없음)
        "만",   // e4=1: 10^4
        "억",   // e4=2: 10^8
        "조",   // e4=3: 10^12
        "경",   // e4=4: 10^16
        "해",   // e4=5: 10^20
        "자",   // e4=6: 10^24
        "양",   // e4=7: 10^28
        "구",   // e4=8: 10^32
        "간",   // e4=9: 10^36
        "정",   // e4=10: 10^40
        "재",   // e4=11: 10^44
        "극",   // e4=12: 10^48
        "항하사", // e4=13: 10^52
        "아승기", // e4=14: 10^56
        "나유타", // e4=15: 10^60
        "불가사의", // e4=16: 10^64
        "무량대수"  // e4=17: 10^68
    };

    public string ToKoreanString()
    {
        if (m == 0) return "0";

        bool negative = m < 0;
        double absM = Math.Abs(m);

        // e4가 음수면 0에 가까운 소수
        if (e4 < 0) return "0";

        // 단위 표시
        string unit = "";
        if (e4 < KoreanUnits.Length)
            unit = KoreanUnits[e4];
        else
            unit = $"×10^{e4 * 4}"; // 범위 초과 시 과학 표기법

        // 소수점 자릿수 결정
        string format = absM >= 1000 ? "F0" : absM >= 100 ? "F1" : "F2";
        string valueStr = absM.ToString(format);

        return $"{(negative ? "-" : "")}{valueStr}{unit}";
    }

    public override string ToString()
    {
        return ToKoreanString();
    }

    // 디버그/개발용 상세 표시
    public string ToDebugString()
    {
        return $"{m:F4} × 10000^{e4} (= {ToKoreanString()})";
    }

    // 과학 표기법 표시
    public string ToScientificString()
    {
        if (m == 0) return "0";
        return $"{m:F2}e{e4 * 4}";
    }

    // ─────────────────────────────────────────────────────────────
    // Zero/One 상수
    // ─────────────────────────────────────────────────────────────
    public static BigNum Zero => new BigNum(0, 0);
    public static BigNum One => new BigNum(1, 0);

    // ─────────────────────────────────────────────────────────────
    // 편의 메서드
    // ─────────────────────────────────────────────────────────────
    public bool IsZero => m == 0;
    public bool IsPositive => m > 0;
    public bool IsNegative => m < 0;
}
