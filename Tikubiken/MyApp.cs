/* ********************************************************************** *\
 * Tikubiken binary patch updator ver 1.0.0
 * Application class
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
/*
	* .ini file format *

	[Status]
	LastDir	= last used directory
*/
using Sgry.Ini;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Tikubiken
{
    class MyApp
	{
		//------------------------------------------------------------
		// Constants
		//------------------------------------------------------------
		public const string INI_name = @"Tikubiken.ini";

		private const string ridLicense = @"Tikubiken.Resources.LICENSE.md";

		//------------------------------------------------------------
		// Data
		//------------------------------------------------------------
		private IniDocument		iniDoc;

		//------------------------------------------------------------
		// Properties
		//------------------------------------------------------------

		// Envrionment paths
		public string ExeDir		{ get; }
		public string IniPath		{ get; }

		// Argument paths
		public string LastDir		{ get; set; }
		public string LastOut		{ get; set; }

		// UI last state
		public bool CheckClearLog		{ get; set; }

		// Command line options
		public string	OptCmdReport	{ get; protected set; }
		public bool		OptSaveXML		{ get; protected set; }

		public DeltaFormat	DeltaFmt	{ get; set; }

		public string SourceXMLName		{ get; private set; }
		public string OutputName		{ get; private set; }

		// Result of command line parsing
		public bool IsCUIMode			{ get => this.DeltaFmt != DeltaFormat.None; }
		public bool IsQuiet				{ get; private set; }

		//------------------------------------------------------------
		// Constructors
		//------------------------------------------------------------
		public MyApp()
		{
			// Path name for .INI file
			// Get the path to starting assembly
			var fiExe = new FileInfo( System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName );
			this.ExeDir = fiExe.DirectoryName;
			this.IniPath = this.ExeDir + Path.DirectorySeparatorChar + INI_name;

			// Default values of command line options
			this.OptCmdReport	= null;
			this.OptSaveXML		= false;
			this.DeltaFmt		= DeltaFormat.None;
			this.SourceXMLName	= null;
			this.OutputName		= null;

			this.IsQuiet		= false;

			// Parse command line parameters
			ParseCommandLine();

			// Unpack lisense document before use any libraries with lisense
			Ext.UpnackLicensesDocument(this.ExeDir, ridLicense);

			// Load INI file
			iniDoc = IniLoad();
		}

		//------------------------------------------------------------
		// Command line
		//------------------------------------------------------------
		private void ParseCommandLine()
		{
			string[] args = System.Environment.GetCommandLineArgs();
			for ( int i=1 ; i<args.Length ; ++i )
			{
				switch ( args[i] )
				{
#if	DEBUG
					// [/ReportCmd=filename] RepoprtCmd option that report commands list in text file
					case string s when Regex.Match(s, @"(?i)^\/ReportCmd=").Success:
						s = s.Substring(11);
						if ( s[1]!=Path.VolumeSeparatorChar ) s = Path.GetFullPath( Path.Join(this.ExeDir,s) );
						this.OptCmdReport = s;
						break;

					// [/SaveXML] Duplicate patch.xml out of result .exe or .tbp file.
					case string s when Regex.Match(s, @"(?i)^\/SaveXML$").Success:
						this.OptSaveXML = true;
						break;
#endif
					// [--format=VCDiff, -fv] VCDiff/RFC3284
					case string s1 when s1.ToLower() == "--format=vcdiff":
					case string s2 when s2.ToLower() == "-fv":
						this.DeltaFmt = DeltaFormat.VCDiff;
						break;

					// [--format=Google, -fo] open-vcdiff/SDCH
					case string s1 when s1.ToLower() == "--format=google":
					case string s2 when s2.ToLower() == "-fo":
						this.DeltaFmt = DeltaFormat.VCDiff_Google;
						break;

					// [--format=XDelta, -fx] VCDiff/XDelta3
					case string s1 when s1.ToLower() == "--format=xdelta":
					case string s2 when s2.ToLower() == "-fx":
						this.DeltaFmt = DeltaFormat.VCDiff_XDelta3;
						break;

					// [--format=BsDiff, -fb] BSDiff+Brotli(Optimal)
					case string s1 when s1.ToLower() == "--format=bsdiff":
					case string s2 when s2.ToLower() == "-fb":
						this.DeltaFmt = DeltaFormat.BsPlus_Optimal;
						break;

					// [--format=BsFast] BSDiff+Brotli(Fastest)
					case string s when s.ToLower() == "--format=bsfast":
						this.DeltaFmt = DeltaFormat.BsPlus_Fastest;
						break;

					// [--quiet, -q] Mute CUI messages
					case string s1 when s1.ToLower() == "--quiet":
					case string s2 when s2.ToLower() == "-q":
						this.IsQuiet = true;
						break;

					default:
						string str;
						if ( IsRelativePath(args[i]) )
						{
							// Relative to current directory
							str = Path.Join(Environment.CurrentDirectory, args[i]);
						}
						else
						{
							// Full path
							str = args[i];
						}
						str = Path.GetFullPath(str);	// force valid path

						// accept only first two file names
						if ( string.IsNullOrEmpty(this.SourceXMLName) )
						{
							this.SourceXMLName = str;
						}
						else if ( string.IsNullOrEmpty(this.OutputName) )
						{
							this.OutputName = str;
						}
						break;
				}	// switch ( args[i] )
			}

			// If two file names are both supplied, this.DeltaFmt indicates CUI mode
			if ( string.IsNullOrEmpty(this.OutputName) )
			{
				// If not CUI mode, this.DeltaFmt is DeltaFormat.None
				this.DeltaFmt = DeltaFormat.None;
			}
			else if ( this.DeltaFmt == DeltaFormat.None )
			{
				// If CUI mode, this.DeltaFmt is not DeltaFormat.None
				this.DeltaFmt = DeltaFormat.VCDiff;
			}
		}

		// Test if given path is relative path
		//	when the first letter is '\' or '/', absolute
		//	when the given string contains ":\" or ":/", absolute
		//	other than above, relative
		public static bool IsRelativePath(string path_to_test)
		{
			string testDrive1 = new string( new char[] {Path.VolumeSeparatorChar, Path.DirectorySeparatorChar} );
			string testDrive2 = new string( new char[] {Path.VolumeSeparatorChar, Path.AltDirectorySeparatorChar} );

			if ( path_to_test.Contains(Path.DirectorySeparatorChar)    ) return false;
			if ( path_to_test.Contains(Path.AltDirectorySeparatorChar) ) return false;
			if ( path_to_test.Contains(testDrive1) ) return false;
			if ( path_to_test.Contains(testDrive2) ) return false;

			return true;
		}

		//------------------------------------------------------------
		// Ini file
		//------------------------------------------------------------

		// Load INI file
		private IniDocument IniLoad()
		{
			IniDocument ini = new IniDocument();

			try
			{
				// Load INI data in a file
				using( var file = new StreamReader(this.IniPath, Encoding.UTF8) )
				{
					ini.Load( file );
				}
			}
			catch {}	// エラーが出ても無視→読み込み失敗＝新規

			// Retrieve values
			this.LastDir = ini.Get( "Status", "LastDir", this.ExeDir );
			this.LastOut = ini.Get( "Status", "LastOut", this.ExeDir );
			this.CheckClearLog = ini.Get( "Status", "ClearLog", true );

			// Returns document if succeed
			return ini;
		}

		// Save to INI file
		public void IniSave()
		{
			iniDoc.Set( "Status", "LastDir",  this.LastDir );
			iniDoc.Set( "Status", "LastOut",  this.LastOut );
			iniDoc.Set( "Status", "ClearLog", this.CheckClearLog );

			try
			{

				using(var file = new StreamWriter(this.IniPath, false, Encoding.UTF8) )
				{
					file.NewLine = Environment.NewLine;
					iniDoc.Save( file );
				}
			}
			catch {}
		}
	}
}
