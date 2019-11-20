using System;
using System.Collections;
using System.Collections.Generic;

public class Model
{
	public Model() { }

    private static List<float> sigmoid(List<float> A)
    {
        List<float> values = new List<float>();
        for(int i = 0; i < A.Count; ++i)
            values.Add((float)(1 / (1 + Math.Exp(-A[i]))));
        return values;
    }

	// Neural Network prediction (3 layer)
	public static List<float> NNPredict(List<List<float>> A, List<List<float>> B, List<float> X)
	{
		X.Insert(0, 1); // bias
		List<float> result1 = new List<float>();
		List<float> result2 = new List<float>();

		float c = 0;

		for (int j = 0; j < A[0].Count; ++j)
		{
			for (int i = 0; i < X.Count; ++i)
			{
				c = c + X[i] * A[i][j];
			}

			result1.Add(c);
			c = 0;
		}

		result1.Insert(0, 1);

		c = 0;
		for (int j = 0; j < B[0].Count; ++j)
		{
			for (int i = 0; i < result1.Count; ++i)
				c = c + result1[i] * B[i][j];

			result2.Add(c);
			c = 0;
		}

		// remove bias
		X.RemoveAt(0);
		return sigmoid(result2);
	}

	// Least-Squares prediction
	public static List<float> LSPredict(List<List<float>> A, List<float> X)
	{
		List<float> result = new List<float>();

		float c = 0;
		
		for (int j = 0; j < A[0].Count; ++j)
		{
			for (int i = 0; i < X.Count; ++i)
			{
				c = c + X[i] * A[i][j];
			}

			result.Add(c);
			c = 0;
		}

		return result;
	}

	// create polynomial features
	public static List<float> PolyFeatures(List<float> X, int p)
	{
		List<float> Xpoly = new List<float>();
		Xpoly.Add(1);

		for (int i = 0; i < p; ++i)
		{
			for (int j = 0; j < X.Count; ++j)
			{
				float exp = (float)(i + 1);
				Xpoly.Add((float)System.Math.Pow(X[j], exp));
			}
		}

		return Xpoly;
	}

	public static List<float> DecodeAngle(List<float> NNprediction)
	{
		double angleX = System.Math.Atan2(NNprediction[0], NNprediction[1]) * (180 / System.Math.PI);
		double angleY = System.Math.Atan2(NNprediction[2], NNprediction[3]) * (180 / System.Math.PI);
		double angleZ = System.Math.Atan2(NNprediction[4], NNprediction[5]) * (180 / System.Math.PI);
		return new List<float>() { (float)angleX, (float)angleY, (float)angleZ };
	}
}
