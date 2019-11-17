using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matrix3x3
{
	Vector3 r1, r2, r3;

	public Matrix3x3() { }
	public Matrix3x3(Vector3 c1, Vector3 c2, Vector3 c3)
	{
		r1 = c1;
		r2 = c2;
		r3 = c3;
	}

	public Matrix3x3 Transpose()
	{
		Vector3 _r1, _r2, _r3;

		_r1.x = r1.x;
		_r2.x = r1.y;
		_r3.x = r1.z;
		_r1.y = r2.x;
		_r2.y = r2.y;
		_r3.y = r2.z;
		_r1.z = r3.x;
		_r2.z = r3.y;
		_r3.z = r3.z;

		return new Matrix3x3(_r1, _r2, _r3);
	}

	public Matrix3x3 Inverse()
	{
		float d = r1.x * r2.y * r3.z + r1.y * r2.z * r3.x +
		r1.z * r3.y * r2.x - r1.z * r2.y * r3.x -
		r1.y * r2.x * r3.z - r1.x * r3.y * r2.z;	// determinent

		if (d == 0.0f)
			Debug.Log("inverse of singular Matrix3x3");

		Vector3 _r1, _r2, _r3;
		_r1.x = (r2.y * r3.z - r2.z * r3.y) / d;
		_r1.y = (r1.z * r3.y - r1.y * r3.z) / d;
		_r1.z = (r1.y * r2.z - r1.z * r2.y) / d;
		_r2.x = (r2.z * r3.x - r2.x * r3.z) / d;
		_r2.y = (r1.x * r3.z - r1.z * r3.x) / d;
		_r2.z = (r1.z * r2.x - r1.x * r2.z) / d;
		_r3.x = (r2.x * r3.y - r2.y * r3.x) / d;
		_r3.y = (r1.y * r3.x - r1.x * r3.y) / d;
		_r3.z = (r1.x * r2.y - r1.y * r2.x) / d;

		return new Matrix3x3(_r1, _r2, _r3);
	}

	public Vector3 GetRow(int i)
	{
		switch(i)
		{
			case 0:
				return r1;
			case 1:
				return r2;
			case 2:
				return r3;
			default:
				return r1;
		}
	}

	public Vector3 GetColumn(int i)
	{
		switch (i)
		{
			case 0:
				return new Vector3(r1.x, r2.x, r3.x);
			case 1:
				return new Vector3(r1.y, r2.y, r3.y);
			case 2:
				return new Vector3(r1.z, r2.z, r3.z);
			default:
				return new Vector3(r1.x, r2.x, r3.x);
		}
	}

	public void SetRow(int i, Vector3 val)
	{
		switch (i)
		{
			case 0:
				r1 = val;
				break;
			case 1:
				r2 = val;
				break;
			case 2:
				r3 = val;
				break;
		}
	}

	public static Vector3 operator *(Matrix3x3 S1, Vector3 S2)
	{
		return new Vector3(Vector3.Dot(S1.GetRow(0), S2), Vector3.Dot(S1.GetRow(1), S2), Vector3.Dot(S1.GetRow(2), S2));
	}

	public static Matrix3x3 operator *(Matrix3x3 S1, Matrix3x3 S2)
	{
		Vector3 _r1 = new Vector3(Vector3.Dot(S1.GetRow(0), S2.GetColumn(0)), Vector3.Dot(S1.GetRow(0), S2.GetColumn(1)), Vector3.Dot(S1.GetRow(0), S2.GetColumn(2)));
		Vector3 _r2 = new Vector3(Vector3.Dot(S1.GetRow(1), S2.GetColumn(0)), Vector3.Dot(S1.GetRow(1), S2.GetColumn(1)), Vector3.Dot(S1.GetRow(1), S2.GetColumn(2)));
		Vector3 _r3 = new Vector3(Vector3.Dot(S1.GetRow(2), S2.GetColumn(0)), Vector3.Dot(S1.GetRow(2), S2.GetColumn(1)), Vector3.Dot(S1.GetRow(2), S2.GetColumn(2)));
		return new Matrix3x3(_r1, _r2, _r3);
	}
}
