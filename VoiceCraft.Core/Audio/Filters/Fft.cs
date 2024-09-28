using System;

namespace VoiceCraft.Core.Audio.Filters
{
	public class FFT
	{
		public FFT(int size)
		{
			this.size = size;
			ip = new int[(int)Math.Sqrt(size) + 2];
			w = new float[size];
		}

		private int size;
		private readonly int[] ip;
		private readonly float[] w;

		private static void cft1st(int n, float[] a, float[] w)
		{
			int j, k1, k2;
			float wk1r, wk1i, wk2r, wk2i, wk3r, wk3i;
			float x0r, x0i, x1r, x1i, x2r, x2i, x3r, x3i;

			x0r = a[0] + a[2];
			x0i = a[1] + a[3];
			x1r = a[0] - a[2];
			x1i = a[1] - a[3];
			x2r = a[4] + a[6];
			x2i = a[5] + a[7];
			x3r = a[4] - a[6];
			x3i = a[5] - a[7];
			a[0] = x0r + x2r;
			a[1] = x0i + x2i;
			a[4] = x0r - x2r;
			a[5] = x0i - x2i;
			a[2] = x1r - x3i;
			a[3] = x1i + x3r;
			a[6] = x1r + x3i;
			a[7] = x1i - x3r;
			wk1r = w[2];
			x0r = a[8] + a[10];
			x0i = a[9] + a[11];
			x1r = a[8] - a[10];
			x1i = a[9] - a[11];
			x2r = a[12] + a[14];
			x2i = a[13] + a[15];
			x3r = a[12] - a[14];
			x3i = a[13] - a[15];
			a[8] = x0r + x2r;
			a[9] = x0i + x2i;
			a[12] = x2i - x0i;
			a[13] = x0r - x2r;
			x0r = x1r - x3i;
			x0i = x1i + x3r;
			a[10] = wk1r * (x0r - x0i);
			a[11] = wk1r * (x0r + x0i);
			x0r = x3i + x1r;
			x0i = x3r - x1i;
			a[14] = wk1r * (x0i - x0r);
			a[15] = wk1r * (x0i + x0r);
			k1 = 0;
			for (j = 16; j < n; j += 16)
			{
				k1 += 2;
				k2 = 2 * k1;
				wk2r = w[k1];
				wk2i = w[k1 + 1];
				wk1r = w[k2];
				wk1i = w[k2 + 1];
				wk3r = wk1r - 2 * wk2i * wk1i;
				wk3i = 2 * wk2i * wk1r - wk1i;
				x0r = a[j] + a[j + 2];
				x0i = a[j + 1] + a[j + 3];
				x1r = a[j] - a[j + 2];
				x1i = a[j + 1] - a[j + 3];
				x2r = a[j + 4] + a[j + 6];
				x2i = a[j + 5] + a[j + 7];
				x3r = a[j + 4] - a[j + 6];
				x3i = a[j + 5] - a[j + 7];
				a[j] = x0r + x2r;
				a[j + 1] = x0i + x2i;
				x0r -= x2r;
				x0i -= x2i;
				a[j + 4] = wk2r * x0r - wk2i * x0i;
				a[j + 5] = wk2r * x0i + wk2i * x0r;
				x0r = x1r - x3i;
				x0i = x1i + x3r;
				a[j + 2] = wk1r * x0r - wk1i * x0i;
				a[j + 3] = wk1r * x0i + wk1i * x0r;
				x0r = x1r + x3i;
				x0i = x1i - x3r;
				a[j + 6] = wk3r * x0r - wk3i * x0i;
				a[j + 7] = wk3r * x0i + wk3i * x0r;
				wk1r = w[k2 + 2];
				wk1i = w[k2 + 3];
				wk3r = wk1r - 2 * wk2r * wk1i;
				wk3i = 2 * wk2r * wk1r - wk1i;
				x0r = a[j + 8] + a[j + 10];
				x0i = a[j + 9] + a[j + 11];
				x1r = a[j + 8] - a[j + 10];
				x1i = a[j + 9] - a[j + 11];
				x2r = a[j + 12] + a[j + 14];
				x2i = a[j + 13] + a[j + 15];
				x3r = a[j + 12] - a[j + 14];
				x3i = a[j + 13] - a[j + 15];
				a[j + 8] = x0r + x2r;
				a[j + 9] = x0i + x2i;
				x0r -= x2r;
				x0i -= x2i;
				a[j + 12] = -wk2i * x0r - wk2r * x0i;
				a[j + 13] = -wk2i * x0i + wk2r * x0r;
				x0r = x1r - x3i;
				x0i = x1i + x3r;
				a[j + 10] = wk1r * x0r - wk1i * x0i;
				a[j + 11] = wk1r * x0i + wk1i * x0r;
				x0r = x1r + x3i;
				x0i = x1i - x3r;
				a[j + 14] = wk3r * x0r - wk3i * x0i;
				a[j + 15] = wk3r * x0i + wk3i * x0r;
			}
		}

		private void cftmdl(int n, int l, float[] a)
		{
			int j, j1, j2, j3, k, k1, k2, m, m2;
			float wk1r, wk1i, wk2r, wk2i, wk3r, wk3i;
			float x0r, x0i, x1r, x1i, x2r, x2i, x3r, x3i;

			m = l << 2;
			for (j = 0; j < l; j += 2)
			{
				j1 = j + l;
				j2 = j1 + l;
				j3 = j2 + l;
				x0r = a[j] + a[j1];
				x0i = a[j + 1] + a[j1 + 1];
				x1r = a[j] - a[j1];
				x1i = a[j + 1] - a[j1 + 1];
				x2r = a[j2] + a[j3];
				x2i = a[j2 + 1] + a[j3 + 1];
				x3r = a[j2] - a[j3];
				x3i = a[j2 + 1] - a[j3 + 1];
				a[j] = x0r + x2r;
				a[j + 1] = x0i + x2i;
				a[j2] = x0r - x2r;
				a[j2 + 1] = x0i - x2i;
				a[j1] = x1r - x3i;
				a[j1 + 1] = x1i + x3r;
				a[j3] = x1r + x3i;
				a[j3 + 1] = x1i - x3r;
			}
			wk1r = w[2];
			for (j = m; j < l + m; j += 2)
			{
				j1 = j + l;
				j2 = j1 + l;
				j3 = j2 + l;
				x0r = a[j] + a[j1];
				x0i = a[j + 1] + a[j1 + 1];
				x1r = a[j] - a[j1];
				x1i = a[j + 1] - a[j1 + 1];
				x2r = a[j2] + a[j3];
				x2i = a[j2 + 1] + a[j3 + 1];
				x3r = a[j2] - a[j3];
				x3i = a[j2 + 1] - a[j3 + 1];
				a[j] = x0r + x2r;
				a[j + 1] = x0i + x2i;
				a[j2] = x2i - x0i;
				a[j2 + 1] = x0r - x2r;
				x0r = x1r - x3i;
				x0i = x1i + x3r;
				a[j1] = wk1r * (x0r - x0i);
				a[j1 + 1] = wk1r * (x0r + x0i);
				x0r = x3i + x1r;
				x0i = x3r - x1i;
				a[j3] = wk1r * (x0i - x0r);
				a[j3 + 1] = wk1r * (x0i + x0r);
			}
			k1 = 0;
			m2 = 2 * m;
			for (k = m2; k < n; k += m2)
			{
				k1 += 2;
				k2 = 2 * k1;
				wk2r = w[k1];
				wk2i = w[k1 + 1];
				wk1r = w[k2];
				wk1i = w[k2 + 1];
				wk3r = wk1r - 2 * wk2i * wk1i;
				wk3i = 2 * wk2i * wk1r - wk1i;
				for (j = k; j < l + k; j += 2)
				{
					j1 = j + l;
					j2 = j1 + l;
					j3 = j2 + l;
					x0r = a[j] + a[j1];
					x0i = a[j + 1] + a[j1 + 1];
					x1r = a[j] - a[j1];
					x1i = a[j + 1] - a[j1 + 1];
					x2r = a[j2] + a[j3];
					x2i = a[j2 + 1] + a[j3 + 1];
					x3r = a[j2] - a[j3];
					x3i = a[j2 + 1] - a[j3 + 1];
					a[j] = x0r + x2r;
					a[j + 1] = x0i + x2i;
					x0r -= x2r;
					x0i -= x2i;
					a[j2] = wk2r * x0r - wk2i * x0i;
					a[j2 + 1] = wk2r * x0i + wk2i * x0r;
					x0r = x1r - x3i;
					x0i = x1i + x3r;
					a[j1] = wk1r * x0r - wk1i * x0i;
					a[j1 + 1] = wk1r * x0i + wk1i * x0r;
					x0r = x1r + x3i;
					x0i = x1i - x3r;
					a[j3] = wk3r * x0r - wk3i * x0i;
					a[j3 + 1] = wk3r * x0i + wk3i * x0r;
				}
				wk1r = w[k2 + 2];
				wk1i = w[k2 + 3];
				wk3r = wk1r - 2 * wk2r * wk1i;
				wk3i = 2 * wk2r * wk1r - wk1i;
				for (j = k + m; j < l + (k + m); j += 2)
				{
					j1 = j + l;
					j2 = j1 + l;
					j3 = j2 + l;
					x0r = a[j] + a[j1];
					x0i = a[j + 1] + a[j1 + 1];
					x1r = a[j] - a[j1];
					x1i = a[j + 1] - a[j1 + 1];
					x2r = a[j2] + a[j3];
					x2i = a[j2 + 1] + a[j3 + 1];
					x3r = a[j2] - a[j3];
					x3i = a[j2 + 1] - a[j3 + 1];
					a[j] = x0r + x2r;
					a[j + 1] = x0i + x2i;
					x0r -= x2r;
					x0i -= x2i;
					a[j2] = -wk2i * x0r - wk2r * x0i;
					a[j2 + 1] = -wk2i * x0i + wk2r * x0r;
					x0r = x1r - x3i;
					x0i = x1i + x3r;
					a[j1] = wk1r * x0r - wk1i * x0i;
					a[j1 + 1] = wk1r * x0i + wk1i * x0r;
					x0r = x1r + x3i;
					x0i = x1i - x3r;
					a[j3] = wk3r * x0r - wk3i * x0i;
					a[j3 + 1] = wk3r * x0i + wk3i * x0r;
				}
			}
		}

		private void cftfsub(int n, float[] a)
		{
			int j, j1, j2, j3, l;
			float x0r, x0i, x1r, x1i, x2r, x2i, x3r, x3i;

			l = 2;
			if (n > 8)
			{
				cft1st(n, a, w);
				l = 8;
				while ((l << 2) < n)
				{
					cftmdl(n, l, a);
					l <<= 2;
				}
			}
			if ((l << 2) == n)
			{
				for (j = 0; j < l; j += 2)
				{
					j1 = j + l;
					j2 = j1 + l;
					j3 = j2 + l;
					x0r = a[j] + a[j1];
					x0i = a[j + 1] + a[j1 + 1];
					x1r = a[j] - a[j1];
					x1i = a[j + 1] - a[j1 + 1];
					x2r = a[j2] + a[j3];
					x2i = a[j2 + 1] + a[j3 + 1];
					x3r = a[j2] - a[j3];
					x3i = a[j2 + 1] - a[j3 + 1];
					a[j] = x0r + x2r;
					a[j + 1] = x0i + x2i;
					a[j2] = x0r - x2r;
					a[j2 + 1] = x0i - x2i;
					a[j1] = x1r - x3i;
					a[j1 + 1] = x1i + x3r;
					a[j3] = x1r + x3i;
					a[j3 + 1] = x1i - x3r;
				}
			}
			else
			{
				for (j = 0; j < l; j += 2)
				{
					j1 = j + l;
					x0r = a[j] - a[j1];
					x0i = a[j + 1] - a[j1 + 1];
					a[j] += a[j1];
					a[j + 1] += a[j1 + 1];
					a[j1] = x0r;
					a[j1 + 1] = x0i;
				}
			}
		}

		private void makect(int nc, int wOffset)
		{
			ip[1] = nc;
			if (nc > 1)
			{
				int nch = nc >> 1;
				float delta = (float)Math.Atan(1.0f) / nch;
				w[wOffset + 0] = (float)Math.Cos(delta * nch);
				w[wOffset + nch] = 0.5f * w[wOffset + 0];
				int j;
				for (j = 1; j < nch; j++)
				{
					w[wOffset + j] = 0.5f * (float)Math.Cos(delta * j);
					w[wOffset + nc - j] = 0.5f * (float)Math.Sin(delta * j);
				}
			}
		}

		private void bitrv2(int n, int ipOffset, float[] a)
		{
			int j, j1, k, k1, l, m, m2;
			float xr, xi, yr, yi;

			ip[ipOffset + 0] = 0;
			l = n;
			m = 1;
			while ((m << 3) < l)
			{
				l >>= 1;
				for (j = 0; j < m; j++)
				{
					ip[ipOffset + m + j] = ip[ipOffset + j] + l;
				}
				m <<= 1;
			}
			m2 = 2 * m;
			if ((m << 3) == l)
			{
				for (k = 0; k < m; k++)
				{
					for (j = 0; j < k; j++)
					{
						j1 = 2 * j + ip[ipOffset + k];
						k1 = 2 * k + ip[ipOffset + j];
						xr = a[j1];
						xi = a[j1 + 1];
						yr = a[k1];
						yi = a[k1 + 1];
						a[j1] = yr;
						a[j1 + 1] = yi;
						a[k1] = xr;
						a[k1 + 1] = xi;
						j1 += m2;
						k1 += 2 * m2;
						xr = a[j1];
						xi = a[j1 + 1];
						yr = a[k1];
						yi = a[k1 + 1];
						a[j1] = yr;
						a[j1 + 1] = yi;
						a[k1] = xr;
						a[k1 + 1] = xi;
						j1 += m2;
						k1 -= m2;
						xr = a[j1];
						xi = a[j1 + 1];
						yr = a[k1];
						yi = a[k1 + 1];
						a[j1] = yr;
						a[j1 + 1] = yi;
						a[k1] = xr;
						a[k1 + 1] = xi;
						j1 += m2;
						k1 += 2 * m2;
						xr = a[j1];
						xi = a[j1 + 1];
						yr = a[k1];
						yi = a[k1 + 1];
						a[j1] = yr;
						a[j1 + 1] = yi;
						a[k1] = xr;
						a[k1 + 1] = xi;
					}
					j1 = 2 * k + m2 + ip[ipOffset + k];
					k1 = j1 + m2;
					xr = a[j1];
					xi = a[j1 + 1];
					yr = a[k1];
					yi = a[k1 + 1];
					a[j1] = yr;
					a[j1 + 1] = yi;
					a[k1] = xr;
					a[k1 + 1] = xi;
				}
			}
			else
			{
				for (k = 1; k < m; k++)
				{
					for (j = 0; j < k; j++)
					{
						j1 = 2 * j + ip[ipOffset + k];
						k1 = 2 * k + ip[ipOffset + j];
						xr = a[j1];
						xi = a[j1 + 1];
						yr = a[k1];
						yi = a[k1 + 1];
						a[j1] = yr;
						a[j1 + 1] = yi;
						a[k1] = xr;
						a[k1 + 1] = xi;
						j1 += m2;
						k1 += m2;
						xr = a[j1];
						xi = a[j1 + 1];
						yr = a[k1];
						yi = a[k1 + 1];
						a[j1] = yr;
						a[j1 + 1] = yi;
						a[k1] = xr;
						a[k1 + 1] = xi;
					}
				}
			}
		}

		private void makewt(int nw)
		{
			int j, nwh;
			float delta, x, y;

			ip[0] = nw;
			ip[1] = 1;
			if (nw > 2)
			{
				nwh = nw >> 1;
				delta = (float)Math.Atan(1.0f) / nwh;
				w[0] = 1;
				w[1] = 0;
				w[nwh] = (float)Math.Cos(delta * nwh);
				w[nwh + 1] = w[nwh];
				if (nwh > 2)
				{
					for (j = 2; j < nwh; j += 2)
					{
						x = (float)Math.Cos(delta * j);
						y = (float)Math.Sin(delta * j);
						w[j] = x;
						w[j + 1] = y;
						w[nw - j] = y;
						w[nw - j + 1] = x;
					}
					bitrv2(nw, 2, w);
				}
			}
		}

		private void rftbsub(int n, float[] a, int nc, int wOffset)
		{
			int j, k, kk, ks, m;
			float wkr, wki, xr, xi, yr, yi;

			a[1] = -a[1];
			m = n >> 1;
			ks = 2 * nc / m;
			kk = 0;
			for (j = 2; j < m; j += 2)
			{
				k = n - j;
				kk += ks;
				wkr = 0.5f - w[wOffset + nc - kk];
				wki = w[wOffset + kk];
				xr = a[j] - a[k];
				xi = a[j + 1] + a[k + 1];
				yr = wkr * xr + wki * xi;
				yi = wkr * xi - wki * xr;
				a[j] -= yr;
				a[j + 1] = yi - a[j + 1];
				a[k] += yr;
				a[k + 1] = yi - a[k + 1];
			}
			a[m + 1] = -a[m + 1];
		}

		private void rftfsub(int n, float[] a, int nc, int wOffset)
		{
			int j, k, kk, ks, m;
			float wkr, wki, xr, xi, yr, yi;

			m = n >> 1;
			ks = 2 * nc / m;
			kk = 0;
			for (j = 2; j < m; j += 2)
			{
				k = n - j;
				kk += ks;
				wkr = 0.5f - w[wOffset + nc - kk];
				wki = w[wOffset + kk];
				xr = a[j] - a[k];
				xi = a[j + 1] + a[k + 1];
				yr = wkr * xr - wki * xi;
				yi = wkr * xi + wki * xr;
				a[j] -= yr;
				a[j + 1] -= yi;
				a[k] += yr;
				a[k + 1] -= yi;
			}
		}

		private void cftbsub(int n, float[] a)
		{
			int j, j1, j2, j3, l;
			float x0r, x0i, x1r, x1i, x2r, x2i, x3r, x3i;

			l = 2;
			if (n > 8)
			{
				cft1st(n, a, w);
				l = 8;
				while ((l << 2) < n)
				{
					cftmdl(n, l, a);
					l <<= 2;
				}
			}
			if ((l << 2) == n)
			{
				for (j = 0; j < l; j += 2)
				{
					j1 = j + l;
					j2 = j1 + l;
					j3 = j2 + l;
					x0r = a[j] + a[j1];
					x0i = -a[j + 1] - a[j1 + 1];
					x1r = a[j] - a[j1];
					x1i = -a[j + 1] + a[j1 + 1];
					x2r = a[j2] + a[j3];
					x2i = a[j2 + 1] + a[j3 + 1];
					x3r = a[j2] - a[j3];
					x3i = a[j2 + 1] - a[j3 + 1];
					a[j] = x0r + x2r;
					a[j + 1] = x0i - x2i;
					a[j2] = x0r - x2r;
					a[j2 + 1] = x0i + x2i;
					a[j1] = x1r - x3i;
					a[j1 + 1] = x1i - x3r;
					a[j3] = x1r + x3i;
					a[j3 + 1] = x1i + x3r;
				}
			}
			else
			{
				for (j = 0; j < l; j += 2)
				{
					j1 = j + l;
					x0r = a[j] - a[j1];
					x0i = -a[j + 1] + a[j1 + 1];
					a[j] += a[j1];
					a[j + 1] = -a[j + 1] - a[j1 + 1];
					a[j1] = x0r;
					a[j1 + 1] = x0i;
				}
			}
		}

		/// <summary>
		/// Perform an in-place fast-fourier transform
		/// </summary>
		/// <param name="a">The vector to be transformed</param>
		/// <param name="n">The size of the data</param>
		public void Transform(float[] a, int n)
		{
			// Initialize the working tables if necessary (typically only on the first call).
			int nw = ip[0];
			if (n > (nw << 2))
			{
				nw = n >> 2;
				makewt(nw);
			}
			int nc = ip[1];
			if (n > (nc << 2))
			{
				nc = n >> 2;
				makect(nc, nw);
			}

			if (n > 4)
			{
				bitrv2(n, 2, a);
				cftfsub(n, a);
				rftfsub(n, a, nc, nw);
			}
			else if (n == 4)
			{
				cftfsub(n, a);
			}
			float xi = a[0] - a[1];
			a[0] += a[1];
			a[1] = xi;

		}

		/// <summary>
		/// Perform an in-place fast-fourier transform
		/// </summary>
		/// <param name="a">The vector to be transformed</param>
		/// <param name="n">The size of the data</param>
		public void ReverseTransform(float[] a, int n)
		{
			// Initialize the working tables if necessary (typically only on the first call).
			int nw = ip[0];
			if (n > (nw << 2))
			{
				nw = n >> 2;
				makewt(nw);
			}
			int nc = ip[1];
			if (n > (nc << 2))
			{
				nc = n >> 2;
				makect(nc, nw);
			}

			a[1] = 0.5f * (a[0] - a[1]);
			a[0] -= a[1];
			if (n > 4)
			{
				rftbsub(n, a, nc, nw);
				bitrv2(n, 2, a);
				cftbsub(n, a);
			}
			else if (n == 4)
			{
				cftfsub(n, a);
			}
		}


	}


}
