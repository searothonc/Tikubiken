/* ********************************************************************** *\
 * Tikubiken binary patch updater ver 1.0.0
 * User defined runtime error exceptions
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
using System;
using System.Windows.Forms;

namespace Tikubiken
{
	//============================================================
	// Base class of user defined exception
	//============================================================
    class URTException : Exception
	{
		private static string _title;
		public static string MsgBoxTitle { get => _title ?? ""; set => _title=value; }

		public static string LineBraek { get => Environment.NewLine; }

		//--------------------------------------------------------
		// Constructors
		//--------------------------------------------------------
		public URTException() : base() {}
		public URTException(string msg) : base(msg) {}
		public URTException(string msg, Exception e) : base(msg,e) {}

		//--------------------------------------------------------
		// Methods
		//--------------------------------------------------------

		// Base operation of ToString()
		public override string ToString() => 
			InnerException == null ? this.Message : $"[{this.Message}]{InnerException.Message}";

		// Base operation of ToString()
		public void MsgBox(string title=null)
			=> MessageBox.Show( 
					this.ToString(), 
					title ?? URTException.MsgBoxTitle, 
					MessageBoxButtons.OK, 
					MessageBoxIcon.Error
				);
	}

	//============================================================
	// Validation failure error
	//============================================================
	class ErrorValidationFailed : URTException
	{
		private string file;
		public ErrorValidationFailed(System.Xml.Schema.XmlSchemaException e, string f)
			 : base("XML Validation failure",e) => file = f;


		public override string ToString() 
			=> base.ToString() + LineBraek + this.file;
	}

	//============================================================
	// XML exception error
	//============================================================
	class ErrorXmlException : URTException
	{
		public ErrorXmlException(System.Xml.XmlException e)
			 : base("XML exception",e) {}
	}

	//============================================================
	// Coder exception
	//============================================================
	class CoderException : URTException
	{
		public CoderException(string reason) : base($"[Decode failed]{reason}") {}
	}
}

