using System;
using System.Diagnostics;

namespace VoiceCraft.Core.Audio.Filters
{
	internal class WebRtcUtil
	{
		public static event Action<string> WriteDebugMessageEventHandler = null;

		[Conditional("MEDIADEBUG")]
		internal static void WriteDebugMessage(string message)
		{
			if (WriteDebugMessageEventHandler != null)
			{
				WriteDebugMessageEventHandler(message);
			}
		}

		internal static int WEBRTC_SPL_SHIFT_W16(int x, int c)
		{
			return (((c) >= 0) ? ((x) << (c)) : ((x) >> (-(c))));
		}

		internal static int WEBRTC_SPL_SHIFT_W32(int x, int c)
		{
			return (((c) >= 0) ? ((x) << (c)) : ((x) >> (-(c))));
		}

		internal static int WEBRTC_SPL_MUL_16_16_RSFT(int a, int b, int c)
		{
			int m = ((((short)(a)) * ((short)(b))));

			return ((m) >> (c));
		}

		internal static short WEBRTC_SPL_LSHIFT_W16(int x, int n)
		{
			return (short)(x << n);
		}

		internal static short WEBRTC_SPL_RSHIFT_W16(int x, int n)
		{
			return (short)(x >> n);
		}

		internal static int WEBRTC_SPL_RSHIFT_W32(int x, int n)
		{
			return x >> n;
		}

		internal static int WebRtcSpl_DivW32W16(int num, int den)
		{
			// Guard against division with 0
			if (den != 0)
			{
				return (num / den);
			}
			return 0x7FFFFFFF;
		}

		internal static int WEBRTC_SPL_MUL_16_16(int a, int b)
		{
			return (a * b);
		}

		internal static int WEBRTC_SPL_LSHIFT_W32(int x, int n)
		{
			return x << n;
		}

		internal static int WEBRTC_SPL_SAT(int a, int b, int c)
		{
			return (b > a ? a : b < c ? c : b);
		}

		internal static short WEBRTC_SPL_MIN(short a, short b)
		{
			return (a < b ? a : b); // Get min value
		}

		internal static float WEBRTC_SPL_MIN(float a, float b)
		{
			return (a < b ? a : b); // Get min value
		}

		internal static short WEBRTC_SPL_MAX(short a, short b)
		{
			return (a > b ? a : b); // Get max value
		}

		internal static float WEBRTC_SPL_MAX(float a, float b)
		{
			return (a > b ? a : b); // Get max value
		}

		internal static int WEBRTC_SPL_MIN(int a, int b)
		{
			return (a < b ? a : b); // Get min value
		}

		internal static int WEBRTC_SPL_MAX(int a, int b)
		{
			return (a > b ? a : b); // Get max value
		}
	}
}
