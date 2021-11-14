using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tikubiken.Properties;

namespace TikubiDiff
{
	public partial class Form_Diff : Form
	{
		private Tikubiken.MyApp myapp;

		public Form_Diff()
		{
			InitializeComponent();
		}

		private void button_source_Click(object sender, EventArgs e)
		{
			using ( OpenFileDialog dlg = new OpenFileDialog() ) {
				// dlg.Title = "Open File";
				dlg.InitialDirectory = myapp.lastDir;
				dlg.Filter = Resources.ofd_filter;

				if ( dlg.ShowDialog() == DialogResult.OK ) {
					// dialog closed by [OK] button
					textBox_source.Text = dlg.FileName;
					myapp.lastDir = System.IO.Path.GetDirectoryName( dlg.FileName );
				} else {
					// dialog closed by [Cancel] button
				}
			}
		}

		private void Form_Diff_Load(object sender, EventArgs e)
		{
			// アプリケーションオブジェクト
			myapp = new Tikubiken.MyApp();
		}

		private void Form_Diff_FormClosed(object sender, FormClosedEventArgs e)
		{
			// iniファイルに変更を保存
			myapp.IniSave();
		}
	}
}
