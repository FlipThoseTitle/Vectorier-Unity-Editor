using System;
using System.Drawing;
using System.Xml;
using UnityEditor;
using UnityEngine;

// Token: 0x02000960 RID: 2400
public class Vector3f
{
    // Token: 0x06004C33 RID: 19507 RVA: 0x00037B9B File Offset: 0x00035D9B
    public Vector3f(float p_x = 0f, float p_y = 0f, float p_z = 0f)
    {
        this._X = p_x;
        this._Y = p_y;
        this._Z = p_z;
    }

    // Token: 0x06004C34 RID: 19508 RVA: 0x00037BB8 File Offset: 0x00035DB8
    public Vector3f(Vector3f p_vector)
    {
        this._X = p_vector.X;
        this._Y = p_vector.Y;
        this._Z = p_vector.Z;
    }

    // Token: 0x06004C35 RID: 19509 RVA: 0x00037BE4 File Offset: 0x00035DE4
    public Vector3f(Vector3 p_vector)
    {
        this._X = p_vector.x;
        this._Y = p_vector.y;
        this._Z = p_vector.z;
    }

    // Token: 0x17000D66 RID: 3430
    // (get) Token: 0x06004C36 RID: 19510 RVA: 0x00037C13 File Offset: 0x00035E13
    // (set) Token: 0x06004C37 RID: 19511 RVA: 0x00037C1B File Offset: 0x00035E1B
    public float X
    {
        get
        {
            return this._X;
        }
        set
        {
            this._X = value;
        }
    }

    // Token: 0x17000D67 RID: 3431
    // (get) Token: 0x06004C38 RID: 19512 RVA: 0x00037C24 File Offset: 0x00035E24
    // (set) Token: 0x06004C39 RID: 19513 RVA: 0x00037C2C File Offset: 0x00035E2C
    public float Y
    {
        get
        {
            return this._Y;
        }
        set
        {
            this._Y = value;
        }
    }

    // Token: 0x17000D68 RID: 3432
    // (get) Token: 0x06004C3A RID: 19514 RVA: 0x00037C35 File Offset: 0x00035E35
    // (set) Token: 0x06004C3B RID: 19515 RVA: 0x00037C3D File Offset: 0x00035E3D
    public float Z
    {
        get
        {
            return this._Z;
        }
        set
        {
            this._Z = value;
        }
    }

    // Token: 0x17000D69 RID: 3433
    // (get) Token: 0x06004C3C RID: 19516 RVA: 0x00037C46 File Offset: 0x00035E46
    public float Length
    {
        get
        {
            return Mathf.Sqrt(this._X * this._X + this._Y * this._Y + this._Z * this._Z);
        }
    }

    // Token: 0x17000D6A RID: 3434
    // (get) Token: 0x06004C3D RID: 19517 RVA: 0x00037C76 File Offset: 0x00035E76
    public float LengthXY
    {
        get
        {
            return Mathf.Sqrt(this._X * this._X + this._Y * this._Y);
        }
    }

    // Token: 0x17000D6B RID: 3435
    // (get) Token: 0x06004C3E RID: 19518 RVA: 0x00037C98 File Offset: 0x00035E98
    public static Vector3f Right
    {
        get
        {
            return new Vector3f(1f, 0f, 0f);
        }
    }

    // Token: 0x17000D6C RID: 3436
    // (get) Token: 0x06004C3F RID: 19519 RVA: 0x00037CAE File Offset: 0x00035EAE
    public static Vector3f Up
    {
        get
        {
            return new Vector3f(0f, 1f, 0f);
        }
    }

    // Token: 0x17000D6D RID: 3437
    // (get) Token: 0x06004C40 RID: 19520 RVA: 0x00037CAE File Offset: 0x00035EAE
    public static Vector3f Forward
    {
        get
        {
            return new Vector3f(0f, 1f, 0f);
        }
    }

    // Token: 0x17000D6E RID: 3438
    // (get) Token: 0x06004C41 RID: 19521 RVA: 0x00037CC4 File Offset: 0x00035EC4
    public static Vector3f Zero
    {
        get
        {
            return new Vector3f(0f, 0f, 0f);
        }
    }

    // Token: 0x17000D6F RID: 3439
    // (get) Token: 0x06004C42 RID: 19522 RVA: 0x00037CDA File Offset: 0x00035EDA
    public static Vector3f One
    {
        get
        {
            return new Vector3f(1f, 1f, 1f);
        }
    }

    // Token: 0x06004C43 RID: 19523 RVA: 0x00037CF0 File Offset: 0x00035EF0
    public static float Distance(Vector3f p_vector1, Vector3f p_vector2)
    {
        return (p_vector2 - p_vector1).Length;
    }

    // Token: 0x06004C44 RID: 19524 RVA: 0x00037CFE File Offset: 0x00035EFE
    public float Distance(Vector3f p_vector)
    {
        return (this - p_vector).Length;
    }

    // Token: 0x06004C45 RID: 19525 RVA: 0x00037D0C File Offset: 0x00035F0C
    public Vector3f Add(float p_value)
    {
        this._X += p_value;
        this._Y += p_value;
        this._Z += p_value;
        return this;
    }

    // Token: 0x06004C46 RID: 19526 RVA: 0x00037D39 File Offset: 0x00035F39
    public Vector3f Add(float p_x, float p_y, float p_z)
    {
        this._X += p_x;
        this._Y += p_y;
        this._Z += p_z;
        return this;
    }

    // Token: 0x06004C47 RID: 19527 RVA: 0x00037D66 File Offset: 0x00035F66
    public Vector3f Add(Vector3f p_vector)
    {
        this._X += p_vector.X;
        this._Y += p_vector.Y;
        this._Z += p_vector.Z;
        return this;
    }

    // Token: 0x06004C48 RID: 19528 RVA: 0x00127BB0 File Offset: 0x00125DB0
    public Vector3f Add(Vector3f p_vector, float p_multy)
    {
        this._X += p_vector.X * p_multy;
        this._Y += p_vector.Y * p_multy;
        this._Z += p_vector.Z * p_multy;
        return this;
    }

    // Token: 0x06004C49 RID: 19529 RVA: 0x00037DA2 File Offset: 0x00035FA2
    public Vector3f Add(Point p_point)
    {
        this._X += p_point.X;
        this._Y += p_point.Y;
        return this;
    }

    // Token: 0x06004C4A RID: 19530 RVA: 0x00037DCB File Offset: 0x00035FCB
    public Vector3f Subtract(float p_value)
    {
        this._X -= p_value;
        this._Y -= p_value;
        this._Z -= p_value;
        return this;
    }

    // Token: 0x06004C4B RID: 19531 RVA: 0x00037DF8 File Offset: 0x00035FF8
    public Vector3f Subtract(float p_x, float p_y, float p_z)
    {
        this._X -= p_x;
        this._Y -= p_y;
        this._Z -= p_z;
        return this;
    }

    // Token: 0x06004C4C RID: 19532 RVA: 0x00037E25 File Offset: 0x00036025
    public Vector3f Subtract(Vector3f p_vector)
    {
        this._X -= p_vector.X;
        this._Y -= p_vector.Y;
        this._Z -= p_vector.Z;
        return this;
    }

    // Token: 0x06004C4D RID: 19533 RVA: 0x00037E61 File Offset: 0x00036061
    public Vector3f Subtract(Point p_point)
    {
        this._X -= p_point.X;
        this._Y -= p_point.Y;
        return this;
    }

    // Token: 0x06004C4E RID: 19534 RVA: 0x00037E8A File Offset: 0x0003608A
    public Vector3f Multiply(float p_value)
    {
        this._X *= p_value;
        this._Y *= p_value;
        this._Z *= p_value;
        return this;
    }

    // Token: 0x06004C4F RID: 19535 RVA: 0x00037EB7 File Offset: 0x000360B7
    public Vector3f Multiply(float p_x, float p_y, float p_z)
    {
        this._X *= p_x;
        this._Y *= p_y;
        this._Z *= p_z;
        return this;
    }

    // Token: 0x06004C50 RID: 19536 RVA: 0x00127C00 File Offset: 0x00125E00
    public Vector3f Cross(Vector3f p_vector)
    {
        return new Vector3f(this._Y * p_vector.Z - this._Z * p_vector.Y, this._Z * p_vector.X - this._X * p_vector.Z, this._X * p_vector.Y - this._Y - p_vector.X);
    }

    // Token: 0x06004C51 RID: 19537 RVA: 0x00127C64 File Offset: 0x00125E64
    public static Vector3f Cross(Vector3f p_point1, Vector3f p_point2, Vector3f p_point3, Vector3f p_point4)
    {
        float num = (p_point2.Y - p_point1.Y) * (p_point3.X - p_point4.X) - (p_point3.Y - p_point4.Y) * (p_point2.X - p_point1.X);
        float num2 = (p_point2.Y - p_point1.Y) * (p_point3.X - p_point1.X) - (p_point3.Y - p_point1.Y) * (p_point2.X - p_point1.X);
        float num3 = (p_point3.Y - p_point1.Y) * (p_point3.X - p_point4.X) - (p_point3.Y - p_point4.Y) * (p_point3.X - p_point1.X);
        if ((double)num == 0.0 && (double)num2 == 0.0 && (double)num3 == 0.0)
        {
            return null;
        }
        if ((double)num == 0.0)
        {
            return null;
        }
        float num4 = num2 / num;
        float num5 = num3 / num;
        float p_x = p_point1.X + (p_point2.X - p_point1.X) * num5;
        float p_y = p_point1.Y + (p_point2.Y - p_point1.Y) * num5;
        if (0f < num4 && num4 < 1f && (0f < num5 & num5 < 1f))
        {
            return new Vector3f(p_x, p_y, 0f);
        }
        return null;
    }

    // Token: 0x06004C52 RID: 19538 RVA: 0x00037EE4 File Offset: 0x000360E4
    public static Vector3f Middle(Vector3f p_point1, Vector3f p_point2)
    {
        return p_point1 + (p_point2 - p_point1) * 0.5f;
    }

    // Token: 0x06004C53 RID: 19539 RVA: 0x00127DD8 File Offset: 0x00125FD8
    public static void Middle(Vector3f p_point1, Vector3f p_point2, Vector3f p_result)
    {
        p_result._X = p_point1._X + (p_point2._X - p_point1._X) * 0.5f;
        p_result._Y = p_point1._Y + (p_point2._Y - p_point1._Y) * 0.5f;
        p_result._Z = p_point1._Z + (p_point2._Z - p_point1._Z) * 0.5f;
    }

    // Token: 0x06004C54 RID: 19540 RVA: 0x00127E48 File Offset: 0x00126048
    public static Vector3f Closest(Vector3f p_point, Vector3f p_linePoint, Vector3f p_lineDirection)
    {
        Vector3f p_vector = p_point - p_linePoint;
        float num = p_vector * p_lineDirection;
        float num2 = p_lineDirection * p_lineDirection;
        float p_value = 0f;
        if (num2 != 0f)
        {
            p_value = num / num2;
        }
        return p_linePoint + p_lineDirection * p_value;
    }

    // Token: 0x06004C55 RID: 19541 RVA: 0x00037EFD File Offset: 0x000360FD
    public void Reset()
    {
        this._X = 0f;
        this._Y = 0f;
        this._Z = 0f;
    }

    // Token: 0x06004C56 RID: 19542 RVA: 0x00037F20 File Offset: 0x00036120
    public static Vector3f Round(Vector3f p_vector, float p_pow)
    {
        p_vector.X = MathUtils.Round(p_vector.X, p_pow);
        p_vector.Y = MathUtils.Round(p_vector.Y, p_pow);
        p_vector.Z = MathUtils.Round(p_vector.Z, p_pow);
        return p_vector;
    }

    // Token: 0x06004C57 RID: 19543 RVA: 0x00037F59 File Offset: 0x00036159
    public Vector3f Round(float p_pow)
    {
        this._X = MathUtils.Round(this._X, p_pow);
        this._Y = MathUtils.Round(this._Y, p_pow);
        this._Z = MathUtils.Round(this._Z, p_pow);
        return this;
    }

    // Token: 0x06004C58 RID: 19544 RVA: 0x00127E90 File Offset: 0x00126090
    public Vector3f Normalize()
    {
        float num = this.Length;
        if (num != 0f)
        {
            num = 1f / num;
        }
        this._X *= num;
        this._Y *= num;
        this._Z *= num;
        return this;
    }

    // Token: 0x06004C59 RID: 19545 RVA: 0x00037F92 File Offset: 0x00036192
    public static Vector3f Normal(Vector3f p_vector1, Vector3f p_vector2)
    {
        return (p_vector1 - p_vector2).Cross(Vector3f.Forward).Normalize();
    }

    // Token: 0x06004C5A RID: 19546 RVA: 0x00037FAA File Offset: 0x000361AA
    public static float Factor(Vector3f p_impulse, Vector3f p_start, Vector3f p_end)
    {
        p_start.Z = 0f;
        p_impulse.Z = 0f;
        p_end.Z = 0f;
        return Vector3f.Distance(p_start, p_end) / Vector3f.Distance(p_start, p_impulse);
    }

    // Token: 0x06004C5B RID: 19547 RVA: 0x00037FDC File Offset: 0x000361DC
    public Vector3f Clone()
    {
        return new Vector3f(this._X, this._Y, this._Z);
    }

    // Token: 0x06004C5C RID: 19548 RVA: 0x00037FF5 File Offset: 0x000361F5
    public Vector3f Set(Vector3f p_vector)
    {
        this._X = p_vector.X;
        this._Y = p_vector.Y;
        this._Z = p_vector.Z;
        return this;
    }

    // Token: 0x06004C5D RID: 19549 RVA: 0x0003801C File Offset: 0x0003621C
    public Vector3f Set(Vector3 p_vector)
    {
        this._X = p_vector.x;
        this._Y = p_vector.y;
        this._Z = p_vector.z;
        return this;
    }

    // Token: 0x06004C5E RID: 19550 RVA: 0x00038046 File Offset: 0x00036246
    public Vector3f Set(float p_x = 0f, float p_y = 0f, float p_z = 0f)
    {
        this._X = p_x;
        this._Y = p_y;
        this._Z = p_z;
        return this;
    }

    // Token: 0x06004C5F RID: 19551 RVA: 0x0003805E File Offset: 0x0003625E
    public void RoundToInt()
    {
        this._X = (float)((int)this._X);
        this._Y = (float)((int)this._Y);
        this._Z = (float)((int)this._Z);
    }

    // Token: 0x06004C60 RID: 19552 RVA: 0x00127EE4 File Offset: 0x001260E4
    public static Vector3f Create(XmlNode p_node)
    {
        if (p_node == null)
        {
            return null;
        }
        Vector3f vector3f = new Vector3f(0f, 0f, 0f);
        try
        {
            vector3f._X = float.Parse(p_node.Attributes["X"].Value);
        }
        catch
        {
            throw new Exception("Error : parse X eeror type");
        }
        try
        {
            vector3f._Y = float.Parse(p_node.Attributes["Y"].Value);
        }
        catch
        {
            throw new Exception("Error : parse Y eeror type");
        }
        return vector3f;
    }

    // Token: 0x06004C61 RID: 19553 RVA: 0x00127F98 File Offset: 0x00126198
    public override string ToString()
    {
        return string.Concat(new object[]
        {
            "X=",
            this._X,
            " Y=",
            this._Y,
            " Z=",
            this._Z
        });
    }

    // Token: 0x06004C62 RID: 19554 RVA: 0x0003808A File Offset: 0x0003628A
    public Vector3 ToVector2()
    {
        return new Vector2((float)((int)this._X), (float)((int)this._Y));
    }

    // Token: 0x06004C63 RID: 19555 RVA: 0x000380A6 File Offset: 0x000362A6
    public Vector3 ToVector3()
    {
        return new Vector3((float)((int)this._X), (float)((int)this._Y), (float)((int)this._Z));
    }

    // Token: 0x06004C64 RID: 19556 RVA: 0x000380C5 File Offset: 0x000362C5
    public static Vector3f operator +(Vector3f p_vector, float p_value)
    {
        return new Vector3f(p_vector.X + p_value, p_vector.Y + p_value, p_vector.Z + p_value);
    }

    // Token: 0x06004C65 RID: 19557 RVA: 0x000380E4 File Offset: 0x000362E4
    public static Vector3f operator +(Vector3f p_vector1, Vector3f p_vector2)
    {
        return new Vector3f(p_vector1.X + p_vector2.X, p_vector1.Y + p_vector2.Y, p_vector1.Z + p_vector2.Z);
    }

    // Token: 0x06004C66 RID: 19558 RVA: 0x00038112 File Offset: 0x00036312
    public static Vector3f operator -(Vector3f p_vector, float p_value)
    {
        return new Vector3f(p_vector.X - p_value, p_vector.Y - p_value, p_vector.Z - p_value);
    }

    // Token: 0x06004C67 RID: 19559 RVA: 0x00038131 File Offset: 0x00036331
    public static Vector3f operator -(Vector3f p_vector1, Vector3f p_vector2)
    {
        return new Vector3f(p_vector1.X - p_vector2.X, p_vector1.Y - p_vector2.Y, p_vector1.Z - p_vector2.Z);
    }

    // Token: 0x06004C68 RID: 19560 RVA: 0x0003815F File Offset: 0x0003635F
    public static Vector3f operator *(Vector3f p_vector, float p_value)
    {
        return new Vector3f(p_vector.X * p_value, p_vector.Y * p_value, p_vector.Z * p_value);
    }

    // Token: 0x06004C69 RID: 19561 RVA: 0x0003817E File Offset: 0x0003637E
    public static float operator *(Vector3f p_vector1, Vector3f p_vector2)
    {
        return p_vector1.X * p_vector2.X + p_vector1.Y * p_vector2.Y + p_vector1.Z * p_vector2.Z;
    }

    // Token: 0x06004C6A RID: 19562 RVA: 0x000381A9 File Offset: 0x000363A9
    public static implicit operator Vector3(Vector3f p_vector)
    {
        return new Vector3(p_vector.X, p_vector.Y, p_vector.Z);
    }

    // Token: 0x06004C6B RID: 19563 RVA: 0x000381C2 File Offset: 0x000363C2
    public static implicit operator Vector3f(Vector3 p_vector)
    {
        return new Vector3f(p_vector.x, p_vector.y, p_vector.z);
    }

    // Token: 0x0400288B RID: 10379
    private float _X;

    // Token: 0x0400288C RID: 10380
    private float _Y;

    // Token: 0x0400288D RID: 10381
    private float _Z;
}
