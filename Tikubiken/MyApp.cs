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
using System;
using System.Collections.Generic;
using System.Text;
using Sgry.Ini;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Reflection;

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

		// Command line options
		public string	OptCmdReport	{ get; protected set; }
		public bool		OptSaveXML		{ get; protected set; }

		//------------------------------------------------------------
		// Constructors
		//------------------------------------------------------------
		public MyApp()
		{
			// Path name for .INI file
			this.ExeDir = System.Environment.CurrentDirectory;
			this.IniPath = this.ExeDir + Path.DirectorySeparatorChar + INI_name;

			// Default values of command line options
			this.OptCmdReport	= null;
			this.OptSaveXML		= false;
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
			string[] args = Environment.GetCommandLineArgs();
			for ( int i=1 ; i<args.Length ; ++i )
			{
				// [/ReportCmd=filename] RepoprtCmd option that report commands list in text file
				if ( Regex.Match(args[i], @"(?i)^\/ReportCmd=").Success )
				{
					string path = args[i].Substring(11);
					if ( path[1]!=':' ) path = Path.GetFullPath( Path.Join(this.ExeDir,path) );
					this.OptCmdReport = path;
				}
				// [/SaveXML] Duplicate patch.xml out of result .exe or .tbp file.
				if ( Regex.Match(args[i], @"(?i)^\/SaveXML$").Success) this.OptSaveXML = true;
			}
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

			// Returns document if succeed
			return ini;
		}

		// Save to INI file
		public void IniSave()
		{
			iniDoc.Set( "Status", "LastDir", this.LastDir );
			iniDoc.Set( "Status", "LastOut", this.LastOut );

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
