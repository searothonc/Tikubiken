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

namespace Tikubiken
{
	class MyApp
	{
		//------------------------------------------------------------
		// Constants
		//------------------------------------------------------------
		public const string INI_name = @"Tikubiken.ini";

		//------------------------------------------------------------
		// Data
		//------------------------------------------------------------
		private IniDocument		iniDoc;

		//------------------------------------------------------------
		// Properties
		//------------------------------------------------------------
		public string exeDir	{ get; }
		public string iniPath	{ get; }

		public string lastDir	{ get; set; }
		public string lastOut	{ get; set; }

		//------------------------------------------------------------
		// Constructors
		//------------------------------------------------------------
		public MyApp()
		{
			// Path name for .INI file
			exeDir = System.Environment.CurrentDirectory;
			iniPath = exeDir + Path.DirectorySeparatorChar + INI_name;

			// Load INI file
			iniDoc = IniLoad();
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
				using( var file = new StreamReader(iniPath, Encoding.UTF8) )
				{
					ini.Load( file );
				}
			}
			catch {}	// エラーが出ても無視→読み込み失敗＝新規

			// Retrieve values
			lastDir = ini.Get( "Status", "LastDir", exeDir );
			lastOut = ini.Get( "Status", "LastOut", exeDir );

			// Returns document if succeed
			return ini;
		}

		// Save to INI file
		public void IniSave()
		{
			iniDoc.Set( "Status", "LastDir", lastDir );
			iniDoc.Set( "Status", "LastOut", lastOut );

			try
			{

				using(var file = new StreamWriter(iniPath, false, Encoding.UTF8) )
				{
					file.NewLine = Environment.NewLine;
					iniDoc.Save( file );
				}
			}
			catch {}
		}
	}
}
