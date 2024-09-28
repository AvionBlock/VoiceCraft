
namespace VoiceCraft.Core.Audio.Filters
{
	public static class JaggedArrayHelper
	{
		public static T[][] Create2DJaggedArray<T>(int len1, int len2)
		{
			var array = new T[len1][];
			for (int i = 0; i < len1; i++)
			{
				array[i] = new T[len2];
			}
			return array;
		}

		public static T[][][] Create3DJaggedArray<T>(int len1, int len2, int len3)
		{
			var array = new T[len1][][];
			for (int i = 0; i < len1; i++)
			{
				array[i] = Create2DJaggedArray<T>(len2, len3);
			}
			return array;
		}

		public static T[][][][] Create4DJaggedArray<T>(int len1, int len2, int len3, int len4)
		{
			var array = new T[len1][][][];
			for (int i = 0; i < len1; i++)
			{
				array[i] = Create3DJaggedArray<T>(len2, len3, len4);
			}
			return array;
		}

		public static T[][][][][] Create5DJaggedArray<T>(int len1, int len2, int len3, int len4, int len5)
		{
			var array = new T[len1][][][][];
			for (int i = 0; i < len1; i++)
			{
				array[i] = Create4DJaggedArray<T>(len2, len3, len4, len5);
			}
			return array;
		}

		public static T[][][][][][] Create6DJaggedArray<T>(int len1, int len2, int len3, int len4, int len5, int len6)
		{
			var array = new T[len1][][][][][];
			for (int i = 0; i < len1; i++)
			{
				array[i] = Create5DJaggedArray<T>(len2, len3, len4, len5, len6);
			}
			return array;
		}

	}
}
