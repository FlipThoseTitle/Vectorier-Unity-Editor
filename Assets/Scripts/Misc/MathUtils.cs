using System;
using UnityEngine;

    // Token: 0x02000613 RID: 1555
    public static class MathUtils
    {
        // Token: 0x06003103 RID: 12547 RVA: 0x00025111 File Offset: 0x00023311
        public static float Round(float Value, float Pow)
        {
            return Mathf.Floor(Value * Pow + 0.5f) / Pow;
        }

        // Token: 0x06003104 RID: 12548 RVA: 0x000D0594 File Offset: 0x000CE794
        public static float AngleBetweenPoints(Vector2 p_a, Vector2 p_b)
        {
            float x = p_b.x - p_a.x;
            float y = p_b.y - p_a.y;
            return Mathf.Atan2(y, x) * 57.29578f;
        }

        // Token: 0x06003105 RID: 12549 RVA: 0x00025123 File Offset: 0x00023323
        public static float NormalizeAngle(float p_angle)
        {
            return p_angle % 360f + (float)((p_angle >= 0f) ? 0 : 360);
        }

        // Token: 0x06003106 RID: 12550 RVA: 0x000D05D0 File Offset: 0x000CE7D0
        public static bool LineRectIntersection(Vector2 p_lineStartPoint, Vector2 p_lineEndPoint, Rect p_rectangle, ref Vector2 p_result)
        {
            Vector2 vector = (p_lineStartPoint.x > p_lineEndPoint.x) ? p_lineEndPoint : p_lineStartPoint;
            Vector2 vector2 = (p_lineStartPoint.x > p_lineEndPoint.x) ? p_lineStartPoint : p_lineEndPoint;
            Vector2 vector3 = (p_lineStartPoint.y > p_lineEndPoint.y) ? p_lineEndPoint : p_lineStartPoint;
            Vector2 vector4 = (p_lineStartPoint.y > p_lineEndPoint.y) ? p_lineStartPoint : p_lineEndPoint;
            float xMax = p_rectangle.xMax;
            float xMin = p_rectangle.xMin;
            float yMax = p_rectangle.yMax;
            float yMin = p_rectangle.yMin;
            if (vector.x <= xMax && xMax <= vector2.x)
            {
                float num = (vector2.y - vector.y) / (vector2.x - vector.x);
                float num2 = (xMax - vector.x) * num + vector.y;
                if (vector3.y <= num2 && num2 <= vector4.y && yMin <= num2 && num2 <= yMax)
                {
                    p_result = new Vector2(xMax, num2);
                    return true;
                }
            }
            if (vector.x <= xMin && xMin <= vector2.x)
            {
                float num3 = (vector2.y - vector.y) / (vector2.x - vector.x);
                float num4 = (xMin - vector.x) * num3 + vector.y;
                if (vector3.y <= num4 && num4 <= vector4.y && yMin <= num4 && num4 <= yMax)
                {
                    p_result = new Vector2(xMin, num4);
                    return true;
                }
            }
            if (vector3.y <= yMax && yMax <= vector4.y)
            {
                float num5 = (vector4.x - vector3.x) / (vector4.y - vector3.y);
                float num6 = (yMax - vector3.y) * num5 + vector3.x;
                if (vector.x <= num6 && num6 <= vector2.x && xMin <= num6 && num6 <= xMax)
                {
                    p_result = new Vector2(num6, yMax);
                    return true;
                }
            }
            if (vector3.y <= yMin && yMin <= vector4.y)
            {
                float num7 = (vector4.x - vector3.x) / (vector4.y - vector3.y);
                float num8 = (yMin - vector3.y) * num7 + vector3.x;
                if (vector.x <= num8 && num8 <= vector2.x && xMin <= num8 && num8 <= xMax)
                {
                    p_result = new Vector2(num8, yMin);
                    return true;
                }
            }
            return false;
        }
    }

