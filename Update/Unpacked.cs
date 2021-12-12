/* ********************************************************************** *\
 * Tikubiken binary patch updater ver 1.0.0
 * Upnacked class to handle unpacked patch data
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

using Tikubiken.Properties;

namespace Tikubiken
{
    class Unpacked
	{
		//------------------------------------------------------------
		// Constants & Fields
		//------------------------------------------------------------

		// Name for licenses file
		private const string ridLicense = @"Tikubiken.Resources.LICENSE.md";

		// Filename components for error report 
		// that will be used to comunicate with users.
		private const string	errorReportName		= "trace";
		private const string	errorReportExt		= ".txt";

		// File paths backing fields
		private FileInfo		fiExe;
		private FileInfo		fiPatchXML;
		private DirectoryInfo	dirExe;
		private DirectoryInfo	dirTmp;

		//--------------------------------------------------------
		// Properties
		//--------------------------------------------------------

		public DirectoryInfo ExeDir => dirExe;
		public FileInfo PatchXML => fiPatchXML;

		// Commandline Options
		public string OptNoUnpack		{ get; protected set; }

		//--------------------------------------------------------
		// Constants: unique instance only
		//--------------------------------------------------------
		private Unpacked() => InitInstance();

		// Unique instance
		private static Unpacked uniqueInstance = null;

		// Get unique instance
		public static Unpacked GetInstance()
		{
			uniqueInstance ??= new Unpacked();
			return uniqueInstance;
		}

		//--------------------------------------------------------
		// Clean up
		//--------------------------------------------------------

		// Disposed only when applcation is closed
		~Unpacked()
		{
			DeleteTemporaryDirectory();
		}

		//--------------------------------------------------------
		// Initialyzation
		//--------------------------------------------------------

		// Setup instance
		private void InitInstance()
		{
			// Initial value for properties
			this.OptNoUnpack = null;

			// Get the path to starting assembly
			fiExe = new FileInfo( System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName );
			dirExe = fiExe.Directory;

			// Retrieve commandline options
			ParseCommandLine();

			// Unpack lisense document before use any libraries with lisense
			Ext.UpnackLicensesDocument(dirExe.FullName, ridLicense);

#if	DEBUG
			if ( this.OptNoUnpack != null )
			{
				// Commandline option /NoUnpack=(path) is specified
				dirTmp = new DirectoryInfo( this.OptNoUnpack );
			}
			else
			{
#endif
				// Create temporary directory to extract data and to work within
				CreateTemporaryDirectory();

				// Extract embedded or same-directory tbp file
				ExtractTBP();
#if	DEBUG
			}
#endif

			// Path to patch.xml
			fiPatchXML = new FileInfo( Ext.JoinPath(dirTmp.FullName, Ext.PatchXML) );
Debug.WriteLine( fiPatchXML.FullName );
			if ( !fiPatchXML.Exists ) throw new URTException(Resources.error_corruptarchive);
		}

		// Create temprary directory that has unique path name
		private void CreateTemporaryDirectory()
		{
			if ( dirTmp != null ) DeleteTemporaryDirectory();

			// Create directory info and actual directory of unique temporary name.
			dirTmp = new DirectoryInfo(Ext.GetTemporaryFileName());
			dirTmp.Create();
		}

		// Delete temporary directory and all its contents
		private void DeleteTemporaryDirectory()
		{
			if ( dirTmp == null ) return;
#if DEBUG
			Debug.WriteLine($"[Omitted to clean up temporary files]{dirTmp.FullName}");
#else
			if ( dirTmp.Exists ) dirTmp.Delete(true);	// Delete directory with all its contents
#endif
			dirTmp = null;
		}

		//------------------------------------------------------------
		// Command line
		//------------------------------------------------------------
		private void ParseCommandLine()
		{
#if	DEBUG
			// Commandline options are only available in debug build
			string[] args = System.Environment.GetCommandLineArgs();
			for ( int i=1 ; i<args.Length ; ++i )
			{
				switch ( args[i] )
				{
					case string s when Regex.Match(s, @"(?i)^\/NoUnpack=").Success:
						s = s.Substring(10);
						if ( s[1]!=':' ) s = Path.GetFullPath( Path.Join(this.ExeDir.FullName, s) );
						this.OptNoUnpack = s;
						break;
				}
			}
#endif
		}

		//--------------------------------------------------------
		// Extract
		//--------------------------------------------------------

		// Extract embedded or same-directory tbp file
		private void ExtractTBP()
		{
			// Search for executable file itself
			using ( var stream = fiExe.Open(FileMode.Open, FileAccess.Read) )
				if ( ExtractStream(stream) ) return;	// Data successfully extracted

			// Search for *.tbp files in same directory as executable file
			foreach ( FileInfo file in dirExe.EnumerateFiles($"*.{Ext.PatchExt}") )
			{
				if ( file.Length < TBPHeader.Size*2 ) continue;	// the file is too small
				using ( var stream = file.Open(FileMode.Open, FileAccess.Read) )
					if ( ExtractStream(stream) ) return;	// Data successfully extracted
			}

#if	DEBUG
			string projectRoot = @"..\..\..\..";
			FileInfo fi = new FileInfo( Ext.JoinPath(projectRoot, @"test_data\Update.tbp") );
			using ( var stream = fi.Open(FileMode.Open, FileAccess.Read) )
				if ( ExtractStream(stream) ) return;
#endif

			// If data to operate is not found, exit as error
			throw new URTException(Resources.error_extract);
		}

		/// <summary>Extract tpb file from stream</summary>
		/// <param name="stream">(Stream)Stream to try extracting.</param>
		/// <returns>(bool)Return true if successfully extracted, otherwise false when file format is not match.</returns>
		private bool ExtractStream( Stream stream )
		{
			// Validate tail infomation section
			int tailOffset = (int) stream.Length - TBPHeader.Size;
			stream.Seek( tailOffset, SeekOrigin.Begin );
			TBPHeader header = stream.ReadTBPHeader();
			if ( !header.HasValidSignature() ) return false;
			if ( header.TailOffset != tailOffset ) return false;

			// Validate header
			byte[] head = new byte[TBPHeader.Size];
			byte[] tail = new byte[TBPHeader.Size];
			stream.Seek( tailOffset, SeekOrigin.Begin );
			stream.Read( tail, 0, TBPHeader.Size );
			stream.Seek( header.HeadOffset, SeekOrigin.Begin );
			stream.Read( head, 0, TBPHeader.Size );
			for ( int i=0 ; i<TBPHeader.Size ; ++i ) if ( head[i]!=tail[i] ) return false;

			// Load zip to memory
			byte[] zip = new byte[header.ZipLength];
			stream.Seek(header.ZipOffset, SeekOrigin.Begin );
			stream.Read(zip, 0, header.ZipLength);

			// Check disk free space enough to extract zip itself and all unzipped files
			string root = dirTmp.Root.FullName;
			var drive = new DriveInfo( root );
			if ( (long) header.ZipLength + (long) header.UnzipSize * (long) header.Cluster > drive.TotalFreeSpace )
				throw new URTException( $"{Resources.error_shortdiskspace}[{root}]" );

			// Overwrite zip header
			zip[0] = 0x50;	// 'P'
			zip[1] = 0x4B;	// 'K'
			zip[2] = 0x03;
			zip[3] = 0x04;
			// Write to temporary file
			FileInfo fiZip = new FileInfo( Ext.GetTemporaryFileName() + @".zip" );
			using ( var fs = fiZip.Create() ) fs.Write(zip, 0, header.ZipLength);

			// Extract zip file
			ZipFile.ExtractToDirectory(fiZip.FullName, dirTmp.FullName);
			fiZip.Delete();		// zip file is no longer needed once extracted

			// return successfully
			return true;
		}

		//--------------------------------------------------------
		// Paths
		//--------------------------------------------------------

		// From unzipped root
		public string FromUnpackRoot(string relative)
			=> Ext.JoinPath(dirTmp.FullName, relative);

		//------------------------------------------------------------
		// Error Report
		//------------------------------------------------------------
		public void ErrorReport(string message, string exception=null)
		{
			// Find unused file name
			FileInfo fi;
			string path;
			int? num = null;
			do
			{
				path = num?.ToString() ?? "";
				if (num == null) num = 0; else num++;
				path = errorReportName + path + errorReportExt;
				fi = new FileInfo(Ext.JoinPath(this.ExeDir.FullName, path));
			} while (fi.Exists);

			// Open & Write
			exception = string.IsNullOrEmpty(exception) ? "" : $"[{exception}]" + System.Environment.NewLine;
			using (var sw = fi.CreateText()) sw.Write(exception + message);
		}

	}	// ---- class Unpacked -----------------------------------
}

