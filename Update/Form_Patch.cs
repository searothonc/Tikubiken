using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Tikubiken
{
	public partial class Form_Patch : Form
	{
		public Form_Patch()
		{
			InitializeComponent();
		}

		private void button_start_Click(object sender, EventArgs e)
		{
			long totalsize = 0;
			string text = "";
			var asm = System.Reflection.Assembly.GetExecutingAssembly();
			var list = asm.GetFiles();
			foreach (var f in list)
			{
				totalsize += f.Length;
				text += f.Name;
				text += ":";
				text += f.Length.ToString();
				text += Environment.NewLine;
			}
			text += "Total:" + totalsize.ToString();
			text += Environment.NewLine;

			text += "[System.Reflection.Assembly.GetExecutingAssembly().Location]";
			text += Environment.NewLine;
			text += System.Reflection.Assembly.GetExecutingAssembly().Location;
			text += Environment.NewLine;

			text += "[System.Reflection.Assembly.GetEntryAssembly().Location]";
			text += Environment.NewLine;
			text += System.Reflection.Assembly.GetEntryAssembly().Location;
			text += Environment.NewLine;

			text += "[System.Reflection.Assembly.GetExecutingAssembly().CodeBase]";
			text += Environment.NewLine;
			text += System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
			text += Environment.NewLine;

			text += "[System.Reflection.Assembly.GetCallingAssembly().Location]";
			text += Environment.NewLine;
			text += System.Reflection.Assembly.GetCallingAssembly().Location;
			text += Environment.NewLine;

			text += "[Application.ExecutablePath]";
			text += Environment.NewLine;
			text += Application.ExecutablePath;
			text += Environment.NewLine;

			text += "[System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase]";
			text += Environment.NewLine;
			text += System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
			text += Environment.NewLine;

			text += "[System.AppDomain.CurrentDomain.BaseDirectory]";
			text += Environment.NewLine;
			text += System.AppDomain.CurrentDomain.BaseDirectory;
			text += Environment.NewLine;

			text += "[System.Environment.CurrentDirectory]";
			text += Environment.NewLine;
			text += System.Environment.CurrentDirectory;
			text += Environment.NewLine;

			//string filename = Application.StartupPath;
			string filename = @"E:\skyro\VisualStudio\source\repos\Tikubiken\images";
			filename += "\\" + "log.txt";
			try
			{
				File.WriteAllText(filename, text, Encoding.UTF8);
			}
			catch (Exception err)
			{
				MessageBox.Show(err.Message, "Error");
			}
			this.label_message.Text = filename + Environment.NewLine + text;

			filename = @"E:\skyro\VisualStudio\source\repos\Tikubiken\images\\Update.exe";
			totalsize = 85578608;
			byte[] buff = new byte[totalsize];
			using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				fs.Seek(0x0265EF0F, SeekOrigin.Begin);
				fs.Read(buff, 0, (int)totalsize - 0x0265EF0F);
			}

			filename = @"E:\skyro\VisualStudio\source\repos\Tikubiken\images\\1.zip";
			using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
			{
				fs.Write(buff, 0, (int)totalsize - 0x0265EF0F);
				fs.Flush();
			}

			filename = @"E:\skyro\VisualStudio\source\repos\Tikubiken\images\\Update.exe";
			using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				fs.Seek(0x0265FFEF, SeekOrigin.Begin);
				fs.Read(buff, 0, (int)totalsize - 0x0265FFEF);
			}

			filename = @"E:\skyro\VisualStudio\source\repos\Tikubiken\images\\2.zip";
			using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
			{
				fs.Write(buff, 0, (int)totalsize - 0x0265FFEF);
				fs.Flush();
			}
		}

		private void Form_Patch_Load(object sender, EventArgs e)
		{
			this.pictureBox.Image = System.Drawing.Image.FromFile(@"E:\skyro\Dropbox\Searothonc\SG001A1\英語版withHabisain\tikubiken\cover_images\cover-640x160-jp.png");
		}
	}
}
