/* ********************************************************************** *\
 * Tikubiken binary patch updater ver 1.0.0
 * Coder interface and others shared among encoding and decoding
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Tikubiken
{
	//============================================================
	// Enum
	//============================================================

	public enum DeltaFormat
	{
		// Delta Encoding
		Encoding_Mask					= 0xFF,
		Encoding_VCDiff					= 0x01,
		//Encoding_BSDiff				= 0x02,
		Encoding_BsPlus					= 0x03,

		// VCDiff checksum
		Compress_VCDiff_ChecksumMask	= 0x0F00,
		Compress_VCDiff_None			= 0x0000,		// The compress byte for VCDiff
		Compress_VCDiff_Interleaved		= 0x8000,		// stores checksum format or option 
		Compress_VCDiff_SDCH			= 0x0100,		// in form of bit flags 
		Compress_VCDiff_XDelta3			= 0x0200,		// that are able to combine.

		// BSDiff compress
		//Compress_None					= 0x0000,
		//Compress_BZIP2					= 0x0100,

		// BsPlus compress
		Compress_BsPlus_Mask			= 0xFF00,
		Compress_BsPlus_Optimal			= 0x0000,	// = 0x100 * CompressionLevel.Optimal
		Compress_BsPlus_Fastest			= 0x0100,	// = 0x100 * CompressionLevel.Fastest
		Compress_BsPlus_NoCompress		= 0x0200,	// = 0x100 * CompressionLevel.NoCompress

		// VCDiff complete
		VCDiff				= Encoding_VCDiff | Compress_VCDiff_None,
		VCDiff_Google		= Encoding_VCDiff | Compress_VCDiff_Interleaved | Compress_VCDiff_SDCH,
		VCDiff_XDelta3		= Encoding_VCDiff | Compress_VCDiff_XDelta3,

		// BSDiff complete
		//Raw				= 0,
		//BSDiff			= Encoding_BSDiff | Compress_BZIP2,

		// BsPlus complete
		BsPlus_Optimal		= Encoding_BsPlus | Compress_BsPlus_Optimal,
		BsPlus_Fastest		= Encoding_BsPlus | Compress_BsPlus_Fastest,
		BsPlus_NoCompress	= Encoding_BsPlus | Compress_BsPlus_NoCompress,
	};

	//============================================================
	// Interface: ICoder
	//============================================================

	public interface ICoder
	{
		public DeltaFormat Format { get; set; }

		public Task DoAsync( byte[] binSource, 			// old file for both enc and dec
							 byte[] binTarget, 			// new file for enc/delta file for dec
							 FileInfo fiOutput, 		// delta file for enc/rebuilt file for dec
							 CancellationToken cToken,	// Cancellation token
							 Progress<float> progress	// where progress will be reported to as a value of 0.0f - 1.0f
							);
	}

	//============================================================
	// Static methods for asynchronous operations
	//============================================================

	static class Ext
	{
		// Asynchrous operations
		public static void Advance( IProgress<float> Progress, float position ) => Progress?.Report( position );
		public static void AcceptCancel(CancellationToken  cToken) => cToken.ThrowIfCancellationRequested();
		public static void AcceptCancel(CancellationToken? cToken) => cToken?.ThrowIfCancellationRequested();

		// File extensions, headers
		public static readonly string DeltaExt	= @".tbdelta";
		public static readonly string PatchExt	= @".tbp";

		// Filename for internal patch xml
		public static readonly string PatchXML	= "patch.xml";

		// Extension methods for TBPHeader
		//------------------------------

		// [Extension method]Load structure from stream
		// 		TPBHeader header = streamObj.ReadTBPHeader();
		public static TBPHeader ReadTBPHeader( this Stream source )
		{
			TBPHeader result;
			using (var reader = new BinaryReader(source,Encoding.UTF8,true))
			{
				var buf = reader.ReadBytes(TBPHeader.Size);
				var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);

				try
				{
					result = (TBPHeader) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(TBPHeader));
				}
				finally
				{
					handle.Free();
				}
			}
			return result;
		}

		// Save structure to stream
		public static void WriteTBPHeader( this Stream target, TBPHeader header )
		{
			using (var writer = new BinaryWriter(target,Encoding.UTF8,true))
			{
				var buf = new byte[TBPHeader.Size];
				var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);

				try
				{
					Marshal.StructureToPtr(header, handle.AddrOfPinnedObject(), false);
				}
				finally
				{
					handle.Free();
				}
				writer.Write(buf);
			}
		}
	}

	//============================================================
	// Header structure for archived patch file
	//============================================================
	 [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct TBPHeader
	{
		// Constants
		public const int OverlapLength	= 4;		// Overlap length with zip header
		public const Int32 SignatureTBP	= 0x01504254;	// "TBP\1"
		public const Int16 VersionTBP	= 0x0100;
		public const Int16 AlignmentTBP	= 16;

		// Members
		private Int32	_Signature;
		private Int16	_Version;
		private Int16	_HeaderSize;
		private Int16	_DeltaEncodingCompression;
		private Int16	_ReservedWORD;
		private Int32	_ReservedDWORD;
		private Int32	_ZipOffset;
		private Int32	_ZipLength;
		private Int32	_HeadOffset;
		private Int32	_TailOffset;
		private Int16	_Alignment;
		private Int16	_HeaderLength;

		// Constructor
		//------------------------------
		public TBPHeader(DeltaFormat encoding)
		{
			this._Signature					= TBPHeader.SignatureTBP;
			this._Version					= TBPHeader.VersionTBP;
			this._HeaderSize				= (Int16) TBPHeader.Size;
			this._DeltaEncodingCompression	= (Int16) encoding;
			this._ReservedWORD				= 0;
			this._ReservedDWORD				= 0;
			this._ZipOffset					= 0;
			this._ZipLength					= 0;
			this._HeadOffset				= 0;
			this._TailOffset				= 0;
			this._Alignment					= TBPHeader.AlignmentTBP;
			this._HeaderLength				= this._HeaderSize;
		}

		// Static methods
		//------------------------------

		// Calculate structure size
		public static int Size { get => Marshal.SizeOf(typeof(TBPHeader)); }

		// Calcurate padding to next alignment
		public static int PaddingLength( int offset ) 
			=> (TBPHeader.AlignmentTBP - (offset % TBPHeader.AlignmentTBP)) % TBPHeader.AlignmentTBP;

		// Properties
		//------------------------------
		public DeltaFormat DeltaEncoding
		{
			get => (DeltaFormat) _DeltaEncodingCompression; 
			set => this._DeltaEncodingCompression = (Int16) value;
		}
		public Int32 ZipOffset		{ get => this._ZipOffset;	set => this._ZipOffset	= value;	}
		public Int32 ZipLength		{ get => this._ZipLength;	set => this._ZipLength	= value;	}
		public Int32 HeadOffset	{ get => this._HeadOffset;	set => this._HeadOffset	= value;	}
		public Int32 TailOffset	{ get => this._TailOffset;	set => this._TailOffset	= value;	}

		// Member methods
		//------------------------------

		// Signature check
		public bool HasValidSignature() => this._Signature != TBPHeader.SignatureTBP;
	}
}

