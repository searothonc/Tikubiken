/* ********************************************************************** *\
 * BsPlus: bsdiff/bspatch library with asynchronous support
 * Diff
 * Modified by Searothonc
\\* ********************************************************************** */
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Diagnostics;

namespace BsPlus
{
	public partial class BsPlus
	{
		public static async System.Threading.Tasks.Task CreateAsync(byte[] oldData, byte[] newData, Stream output,
		CancellationToken cToken, CompressionLevel compLv=CompressionLevel.Optimal, IProgress<float> Progress=null) 
			=> await System.Threading.Tasks.Task.Run( ()=> Create(oldData, newData, output, compLv, cToken, Progress), cToken );


		/// <summary>
		/// Creates a binary patch (in <a href="http://www.daemonology.net/bsdiff/">bsdiff</a> format) that can be used
		/// (by <see cref="Apply"/>) to transform <paramref name="oldData"/> into <paramref name="newData"/>.
		/// </summary>
		/// <param name="oldData">The original binary data.</param>
		/// <param name="newData">The new binary data.</param>
		/// <param name="output">A <see cref="Stream"/> to which the patch will be written.</param>
		/// <param name="compLv">Complession level enum value in type of System.IO.Compression.CompressionLevel. 
		/// No compression is available at a value as 'NoCompression'. Default value is 'Optimal' if omitted.</param>
		/// <param name="cToken">Cancellation token. Or null if not needed to cancel.</param>
		/// <param name="Progress">Progress reporting action. Or null if not needed to report progress.</param>
		//public static void Create(byte[] oldData, byte[] newData, Stream output)
		public static void Create(byte[] oldData, byte[] newData, Stream output, CompressionLevel compLv=CompressionLevel.Optimal, 
		CancellationToken cToken=default, IProgress<float> Progress=null)
		{
			// check arguments
			if (oldData == null)
				throw new ArgumentNullException("oldData");
			if (newData == null)
				throw new ArgumentNullException("newData");
			if (output == null)
				throw new ArgumentNullException("output");
			if (!output.CanSeek)
				throw new ArgumentException("Output stream must be seekable.", "output");
			if (!output.CanWrite)
				throw new ArgumentException("Output stream must be writable.", "output");

			/* Header is
				0	8	 "BSDIFF40"
				8	8	length of bzip2ed ctrl block
				16	8	length of bzip2ed diff block
				24	8	length of new file */
			/* File is
				0	32	Header
				32	??	Bzip2ed ctrl block
				??	??	Bzip2ed diff block
				??	??	Bzip2ed extra block */
			byte[] header = new byte[c_headerSize];
			WriteInt64(c_fileSignature, header, 0); // "BSDIFF40"
			WriteInt64(0, header, 8);
			WriteInt64(0, header, 16);
			WriteInt64(newData.Length, header, 24);

			long startPosition = output.Position;
			output.Write(header, 0, header.Length);

			int[] I = SuffixSort(oldData, cToken, Progress);

			byte[] db = new byte[newData.Length];
			byte[] eb = new byte[newData.Length];

			int dblen = 0;
			int eblen = 0;

			using ( Stream bz2Stream = new BrotliStream(output, compLv, true) )
			{
				// compute the differences, writing ctrl as we go
				int scan = 0;
				int pos = 0;
				int len = 0;
				int lastscan = 0;
				int lastpos = 0;
				int lastoffset = 0;
				while (scan < newData.Length)
				{
					int oldscore = 0;
					for (int scsc = scan += len; scan < newData.Length; scan++)
					{
						// Accept cancel without progress report
						Tikubiken.Ext.AcceptCancel(cToken);

						len = Search(I, oldData, newData, scan, 0, oldData.Length, out pos);

						for (; scsc < scan + len; scsc++)
						{
							// Accept cancel without progress report
							Tikubiken.Ext.AcceptCancel(cToken);

							if ((scsc + lastoffset < oldData.Length) && (oldData[scsc + lastoffset] == newData[scsc]))
								oldscore++;
						}

						if ((len == oldscore && len != 0) || (len > oldscore + 8))
							break;

						if ((scan + lastoffset < oldData.Length) && (oldData[scan + lastoffset] == newData[scan]))
							oldscore--;
					}

					// Report progress
					Tikubiken.Ext.Advance( Progress, (0.5f/7)*1 + 0.5f);

					if (len != oldscore || scan == newData.Length)
					{
						int s = 0;
						int sf = 0;
						int lenf = 0;
						for (int i = 0; (lastscan + i < scan) && (lastpos + i < oldData.Length); )
						{
							// Accept cancel without progress report
							Tikubiken.Ext.AcceptCancel(cToken);

							if (oldData[lastpos + i] == newData[lastscan + i])
								s++;
							i++;
							if (s * 2 - i > sf * 2 - lenf)
							{
								sf = s;
								lenf = i;
							}
						}

						// Report progress
						Tikubiken.Ext.Advance( Progress, (0.5f/7)*2 + 0.5f);

						int lenb = 0;
						if (scan < newData.Length)
						{
							s = 0;
							int sb = 0;
							for (int i = 1; (scan >= lastscan + i) && (pos >= i); i++)
							{
								// Accept cancel without progress report
								Tikubiken.Ext.AcceptCancel(cToken);

								if (oldData[pos - i] == newData[scan - i])
									s++;
								if (s * 2 - i > sb * 2 - lenb)
								{
									sb = s;
									lenb = i;
								}
							}
						}

						// Report progress
						Tikubiken.Ext.Advance( Progress, (0.5f/7)*3 + 0.5f);

						if (lastscan + lenf > scan - lenb)
						{
							int overlap = (lastscan + lenf) - (scan - lenb);
							s = 0;
							int ss = 0;
							int lens = 0;
							for (int i = 0; i < overlap; i++)
							{
								// Accept cancel without progress report
								Tikubiken.Ext.AcceptCancel(cToken);

								if (newData[lastscan + lenf - overlap + i] == oldData[lastpos + lenf - overlap + i])
									s++;
								if (newData[scan - lenb + i] == oldData[pos - lenb + i])
									s--;
								if (s > ss)
								{
									ss = s;
									lens = i + 1;
								}
							}

							lenf += lens - overlap;
							lenb -= lens;
						}

						// Report progress
						Tikubiken.Ext.Advance( Progress, (0.5f/7)*4 + 0.5f);

						for (int i = 0; i < lenf; i++)
						{
							// Accept cancel without progress report
							Tikubiken.Ext.AcceptCancel(cToken);

							db[dblen + i] = (byte) (newData[lastscan + i] - oldData[lastpos + i]);
						}
						// Report progress
						Tikubiken.Ext.Advance( Progress, (0.5f/7)*5 + 0.5f);
						for (int i = 0; i < (scan - lenb) - (lastscan + lenf); i++)
						{
							// Accept cancel without progress report
							Tikubiken.Ext.AcceptCancel(cToken);

							eb[eblen + i] = newData[lastscan + lenf + i];
						}

						// Report progress
						Tikubiken.Ext.Advance( Progress, (0.5f/7)*6 + 0.5f);

						dblen += lenf;
						eblen += (scan - lenb) - (lastscan + lenf);

						byte[] buf = new byte[8];
						WriteInt64(lenf, buf, 0);
						bz2Stream.Write(buf, 0, 8);

						WriteInt64((scan - lenb) - (lastscan + lenf), buf, 0);
						bz2Stream.Write(buf, 0, 8);

						WriteInt64((pos - lenb) - (lastpos + lenf), buf, 0);
						bz2Stream.Write(buf, 0, 8);

						lastscan = scan - lenb;
						lastpos = pos - lenb;
						lastoffset = pos - scan;
					}
				}
			}

			// compute size of compressed ctrl data
			long controlEndPosition = output.Position;
			WriteInt64(controlEndPosition - startPosition - c_headerSize, header, 8);

			// Accept cancel without progress report
			Tikubiken.Ext.AcceptCancel(cToken);

			// write compressed diff data
			using ( Stream bz2Stream = new BrotliStream(output, compLv, true) )
			{
				bz2Stream.Write(db, 0, dblen);
			}

			// Report progress
			Tikubiken.Ext.Advance( Progress, (0.5f/7)*6.5f + 0.5f);

			// compute size of compressed diff data
			long diffEndPosition = output.Position;
			WriteInt64(diffEndPosition - controlEndPosition, header, 16);

			// write compressed extra data
			using ( Stream bz2Stream = new BrotliStream(output, compLv, true) )
			{
				bz2Stream.Write(eb, 0, eblen);
			}

			// seek to the beginning, write the header, then seek back to end
			long endPosition = output.Position;
			output.Position = startPosition;
			output.Write(header, 0, header.Length);
			output.Position = endPosition;

			// Report progress
			Tikubiken.Ext.Advance( Progress, 1.0f );
		}

		private static int CompareBytes(byte[] left, int leftOffset, byte[] right, int rightOffset)
		{
			for (int index = 0; index < left.Length - leftOffset && index < right.Length - rightOffset; index++)
			{
				int diff = left[index + leftOffset] - right[index + rightOffset];
				if (diff != 0)
					return diff;
			}
			return 0;
		}

		private static int MatchLength(byte[] oldData, int oldOffset, byte[] newData, int newOffset)
		{
			int i;
			for (i = 0; i < oldData.Length - oldOffset && i < newData.Length - newOffset; i++)
			{
				if (oldData[i + oldOffset] != newData[i + newOffset])
					break;
			}
			return i;
		}

		private static int Search(int[] I, byte[] oldData, byte[] newData, int newOffset, int start, int end, out int pos)
		{
			if (end - start < 2)
			{
				int startLength = MatchLength(oldData, I[start], newData, newOffset);
				int endLength = MatchLength(oldData, I[end], newData, newOffset);

				if (startLength > endLength)
				{
					pos = I[start];
					return startLength;
				}
				else
				{
					pos = I[end];
					return endLength;
				}
			}
			else
			{
				int midPoint = start + (end - start) / 2;
				return CompareBytes(oldData, I[midPoint], newData, newOffset) < 0 ?
					Search(I, oldData, newData, newOffset, midPoint, end, out pos) :
					Search(I, oldData, newData, newOffset, start, midPoint, out pos);
			}
		}

		private static void Split(int[] I, int[] v, int start, int len, int h)
		{
			if (len < 16)
			{
				int j;
				for (int k = start; k < start + len; k += j)
				{
					j = 1;
					int x = v[I[k] + h];
					for (int i = 1; k + i < start + len; i++)
					{
						if (v[I[k + i] + h] < x)
						{
							x = v[I[k + i] + h];
							j = 0;
						}
						if (v[I[k + i] + h] == x)
						{
							Swap(ref I[k + j], ref I[k + i]);
							j++;
						}
					}
					for (int i = 0; i < j; i++)
						v[I[k + i]] = k + j - 1;
					if (j == 1)
						I[k] = -1;
				}
			}
			else
			{
				int x = v[I[start + len / 2] + h];
				int jj = 0;
				int kk = 0;
				for (int i2 = start; i2 < start + len; i2++)
				{
					if (v[I[i2] + h] < x)
						jj++;
					if (v[I[i2] + h] == x)
						kk++;
				}
				jj += start;
				kk += jj;

				int i = start;
				int j = 0;
				int k = 0;
				while (i < jj)
				{
					if (v[I[i] + h] < x)
					{
						i++;
					}
					else if (v[I[i] + h] == x)
					{
						Swap(ref I[i], ref I[jj + j]);
						j++;
					}
					else
					{
						Swap(ref I[i], ref I[kk + k]);
						k++;
					}
				}

				while (jj + j < kk)
				{
					if (v[I[jj + j] + h] == x)
					{
						j++;
					}
					else
					{
						Swap(ref I[jj + j], ref I[kk + k]);
						k++;
					}
				}

				if (jj > start)
					Split(I, v, start, jj - start, h);

				for (i = 0; i < kk - jj; i++)
					v[I[jj + i]] = kk - 1;
				if (jj == kk - 1)
					I[jj] = -1;

				if (start + len > kk)
					Split(I, v, kk, start + len - kk, h);
			}
		}

		private static int[] SuffixSort(byte[] oldData, CancellationToken cToken, IProgress<float> Progress)
		{
			int[] buckets = new int[256];

			foreach (byte oldByte in oldData)
				buckets[oldByte]++;
			for (int i = 1; i < 256; i++)
				buckets[i] += buckets[i - 1];
			for (int i = 255; i > 0; i--)
				buckets[i] = buckets[i - 1];
			buckets[0] = 0;

			int[] I = new int[oldData.Length + 1];
			for (int i = 0; i < oldData.Length; i++)
			{
				// Accept cancel without progress report
				Tikubiken.Ext.AcceptCancel(cToken);

				I[++buckets[oldData[i]]] = i;
			}

			// Report progress
			Tikubiken.Ext.Advance( Progress, (0.5f/4)*1);

			int[] v = new int[oldData.Length + 1];
			for (int i = 0; i < oldData.Length; i++)
			{
				// Accept cancel without progress report
				Tikubiken.Ext.AcceptCancel(cToken);

				v[i] = buckets[oldData[i]];
			}

			for (int i = 1; i < 256; i++)
			{
				if (buckets[i] == buckets[i - 1] + 1)
					I[buckets[i]] = -1;
			}
			I[0] = -1;

			// Report progress
			Tikubiken.Ext.Advance( Progress, (0.5f/4)*2);

			for (int h = 1; I[0] != -(oldData.Length + 1); h += h)
			{
				int len = 0;
				int i = 0;
				while (i < oldData.Length + 1)
				{
					// Accept cancel without progress report
					Tikubiken.Ext.AcceptCancel(cToken);

					if (I[i] < 0)
					{
						len -= I[i];
						i -= I[i];
					}
					else
					{
						if (len != 0)
							I[i - len] = -len;
						len = v[I[i]] + 1 - i;
						Split(I, v, i, len, h);
						i += len;
						len = 0;
					}
				}

				if (len != 0)
					I[i - len] = -len;
			}

			// Report progress
			Tikubiken.Ext.Advance( Progress, (0.5f/4)*3);

			for (int i = 0; i < oldData.Length + 1; i++)
			{
				// Accept cancel without progress report
				Tikubiken.Ext.AcceptCancel(cToken);

				I[v[i]] = i;
			}

			// Report progress
			Tikubiken.Ext.Advance( Progress, 0.5f);

			return I;
		}

		private static void Swap(ref int first, ref int second)
		{
			int temp = first;
			first = second;
			second = temp;
		}

		private static void WriteInt64(long value, byte[] buf, int offset)
		{
			long valueToWrite = value < 0 ? -value : value;

			for (int byteIndex = 0; byteIndex < 8; byteIndex++)
			{
				buf[offset + byteIndex] = unchecked((byte) valueToWrite);
				valueToWrite >>= 8;
			}

			if (value < 0)
				buf[offset + 7] |= 0x80;
		}

	}
}
