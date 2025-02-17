using System;
using UnityEngine;

// Token: 0x02000022 RID: 34
public class AffineDecomposition
{
    // Token: 0x0600003C RID: 60 RVA: 0x0003B998 File Offset: 0x00039B98
    public AffineDecomposition(Matrix4x4 p_matrix)
    {
        float num = p_matrix[0, 0] * p_matrix[1, 1] - p_matrix[0, 1] * p_matrix[1, 0];
        float num2 = Mathf.Sqrt(p_matrix[0, 0] * p_matrix[0, 0] + p_matrix[0, 1] * p_matrix[0, 1]);
        if (num2 == 0f)
        {
            return;
        }
        float scaleY = num / num2;
        float num3 = (p_matrix[0, 0] * p_matrix[1, 0] + p_matrix[0, 1] * p_matrix[1, 1]) / num;
        float f = Mathf.Atan2(p_matrix[0, 1], p_matrix[0, 0]);
        float num4 = num3 / 2f;
        float num5 = 0.5f * Mathf.Atan(num4);
        float f2 = -0.7853982f - num5;
        float num6 = 0.7853982f - num5;
        float num7 = Mathf.Sqrt(num4 * num4 + 1f);
        float scaleX = num7 - num4;
        float scaleY2 = num7 + num4;
        Matrix4x4 identity = Matrix4x4.identity;
        identity[0, 0] = Mathf.Cos(f2);
        identity[0, 1] = Mathf.Sin(f2);
        identity[1, 0] = -identity[0, 1];
        identity[1, 1] = identity[0, 0];
        Matrix4x4 identity2 = Matrix4x4.identity;
        identity2[0, 0] = Mathf.Cos(f);
        identity2[0, 1] = Mathf.Sin(f);
        identity2[1, 0] = -identity2[0, 1];
        identity2[1, 1] = identity2[0, 0];
        Matrix4x4 matrix4x = identity2 * identity;
        float num8 = Mathf.Atan2(matrix4x[0, 1], matrix4x[0, 0]);
        this.ScaleX1 = num2;
        this.ScaleX2 = scaleX;
        this.ScaleY1 = scaleY;
        this.ScaleY2 = scaleY2;
        this.Angle1 = 57.29578f * num6;
        this.Angle2 = 57.29578f * num8;
    }

    // Token: 0x1700000B RID: 11
    // (get) Token: 0x0600003D RID: 61 RVA: 0x00005A2C File Offset: 0x00003C2C
    public float ScaleMultyX
    {
        get
        {
            return this.ScaleX1 * this.ScaleX2;
        }
    }

    // Token: 0x1700000C RID: 12
    // (get) Token: 0x0600003E RID: 62 RVA: 0x00005A3B File Offset: 0x00003C3B
    public float ScaleMultyY
    {
        get
        {
            return this.ScaleY1 * this.ScaleY2;
        }
    }

    // Token: 0x1700000D RID: 13
    // (get) Token: 0x0600003F RID: 63 RVA: 0x00005A4A File Offset: 0x00003C4A
    public float AngleSum
    {
        get
        {
            return this.Angle1 + this.Angle2;
        }
    }

    // Token: 0x0400004D RID: 77
    public float ScaleX1;

    // Token: 0x0400004E RID: 78
    public float ScaleY1;

    // Token: 0x0400004F RID: 79
    public float ScaleX2;

    // Token: 0x04000050 RID: 80
    public float ScaleY2;

    // Token: 0x04000051 RID: 81
    public float Angle1;

    // Token: 0x04000052 RID: 82
    public float Angle2;
}
