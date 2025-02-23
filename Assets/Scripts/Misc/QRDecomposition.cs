using System;
using UnityEngine;

// Token: 0x020005EB RID: 1515
internal class QRDecomposition
{
    // Token: 0x06002F0F RID: 12047 RVA: 0x000236FB File Offset: 0x000218FB
    public QRDecomposition(Matrix4x4 a)
    {
        this.A = a;
        this.CalculateDecomposition();
    }

    // Token: 0x17000834 RID: 2100
    // (get) Token: 0x06002F10 RID: 12048 RVA: 0x00023710 File Offset: 0x00021910
    public bool DecomposedSuccessfully
    {
        get
        {
            return this.decomposedSuccessfully;
        }
    }

    // Token: 0x17000835 RID: 2101
    // (get) Token: 0x06002F11 RID: 12049 RVA: 0x00023718 File Offset: 0x00021918
    public float ScaleX
    {
        get
        {
            if (this.decomposedSuccessfully)
            {
                return this.R.m00;
            }
            return this.scaleX;
        }
    }

    // Token: 0x17000836 RID: 2102
    // (get) Token: 0x06002F12 RID: 12050 RVA: 0x00023737 File Offset: 0x00021937
    public float ScaleY
    {
        get
        {
            if (this.decomposedSuccessfully)
            {
                return this.R.m11;
            }
            return this.scaleY;
        }
    }

    // Token: 0x17000837 RID: 2103
    // (get) Token: 0x06002F13 RID: 12051 RVA: 0x00023756 File Offset: 0x00021956
    public Matrix4x4 Rotation
    {
        get
        {
            return this.Q;
        }
    }

    // Token: 0x06002F14 RID: 12052 RVA: 0x000C93A8 File Offset: 0x000C75A8
    private void CalculateDecomposition()
    {
        int num = 0;
        for (int i = 0; i < 2; i++)
        {
            int num2 = 0;
            while (num2 < 2 && num < 2)
            {
                if (Math.Abs(this.A[i, num2]) > 0.001f)
                {
                    num++;
                }
                num2++;
            }
        }
        if (num < 2)
        {
            this.decomposedSuccessfully = false;
            this.ExtractWHFromMatrix();
            return;
        }
        Matrix4x4 matrix4x = default(Matrix4x4);
        this.Q = default(Matrix4x4);
        matrix4x.SetColumn(0, this.A.GetColumn(0));
        for (int j = 1; j < 4; j++)
        {
            Vector4 vector = default(Vector4);
            for (int k = 0; k < j; k++)
            {
                Vector4 b = Vector4.Project(this.A.GetColumn(j), matrix4x.GetColumn(j - k - 1));
                vector += b;
            }
            Vector4 v = this.A.GetColumn(j) - vector;
            matrix4x.SetColumn(j, v);
        }
        for (int l = 0; l < 4; l++)
        {
            float magnitude = matrix4x.GetColumn(l).magnitude;
            for (int m = 0; m < 4; m++)
            {
                this.Q[m, l] = matrix4x[m, l] / magnitude;
            }
        }
        this.R = this.Q.transpose * this.A;
        this.decomposedSuccessfully = true;
    }

    // Token: 0x06002F15 RID: 12053 RVA: 0x000C9548 File Offset: 0x000C7748
    private void ExtractWHFromMatrix()
    {
        this.scaleX = 0f;
        this.scaleY = 0f;
        if (Math.Abs(this.A.m00) > 0.001f)
        {
            this.scaleX = this.A.m00;
        }
        else if (Math.Abs(this.A.m01) > 0.001f)
        {
            this.scaleX = this.A.m01;
        }
        if (Math.Abs(this.A.m11) > 0.001f)
        {
            this.scaleY = this.A.m11;
        }
        else if (Math.Abs(this.A.m10) > 0.001f)
        {
            this.scaleY = this.A.m10;
        }
        this.Q = Matrix4x4.identity;
    }

    // Token: 0x06002F16 RID: 12054 RVA: 0x000C962C File Offset: 0x000C782C
    public bool ContainsSkew()
    {
        return this.decomposedSuccessfully && (Math.Abs(this.R.m10) > 0.001f || Math.Abs(this.R.m01) > 0.001f);
    }

    // Token: 0x040017BB RID: 6075
    public const float delta = 0.001f;

    // Token: 0x040017BC RID: 6076
    private Matrix4x4 A;

    // Token: 0x040017BD RID: 6077
    private Matrix4x4 Q;

    // Token: 0x040017BE RID: 6078
    private Matrix4x4 R;

    // Token: 0x040017BF RID: 6079
    private bool decomposedSuccessfully;

    // Token: 0x040017C0 RID: 6080
    private float scaleX;

    // Token: 0x040017C1 RID: 6081
    private float scaleY;
}

