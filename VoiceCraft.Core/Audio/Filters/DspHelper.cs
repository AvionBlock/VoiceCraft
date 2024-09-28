using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Audio.Filters
{
	public class DspHelper
	{

		public static double GetAverage(short[] array)
		{
			long sum = 0;
			for (int i = 0; i < array.Length; i++)
			{
				sum += Math.Abs((int)array[i]);
			}
			return (sum / (double)array.Length);
		}

		public static double GetStandardDeviation(short[] array)
		{
			long sum = 0;
			long sumOfDerivation = 0;
			long totalLength = 0;
			totalLength += array.Length;
			for (int i = 0; i < array.Length; i++)
			{
				sum += array[i];
				sumOfDerivation += (array[i] * array[i]);
			}
			double average = sum / (double)totalLength;
			double sumOfDerivationAverage = sumOfDerivation / (double)totalLength;
			return Math.Sqrt(sumOfDerivationAverage - (average * average));
		}

		public static double GetStandardDeviation(IList<int> array)
		{
			long sum = 0;
			long sumOfDerivation = 0;
			long totalLength = 0;
			totalLength += array.Count;
			for (int i = 0; i < array.Count; i++)
			{
				sum += array[i];
				sumOfDerivation += (array[i] * array[i]);
			}
			double average = sum / (double)totalLength;
			double sumOfDerivationAverage = sumOfDerivation / (double)totalLength;
			return Math.Sqrt(sumOfDerivationAverage - (average * average));
		}

		public static double GetRootMeanSquare(short[] array)
		{
			long sum = 0;
			long totalLength = 0;
			totalLength += array.Length;
			for (int i = 0; i < array.Length; i++)
			{
				sum += (array[i] * array[i]);
			}
			double average = sum / (double)totalLength;
			return Math.Sqrt(average);
		}

	}
}
