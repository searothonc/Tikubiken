/* ********************************************************************** *\
 * Tikubiken binary patch updater ver 1.0.0
 * Exception classes
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
using System;
using System.Xml;

namespace Tikubiken
{
    /// <summary>Base for user exception sub-classes in this class</summary>
    public class Error : System.Exception
		{
			// line break sugar
			public static string lineBreak => System.Environment.NewLine;

			public Error() : base() {}
			public Error(String s) : base(s) {}
			//public Error(SerializationInfo si, StreamingContext sc) : base(si,sc) {}
			public Error(String s, Exception e) : base(s,e) {}

			// Error messages
			//------------------------------
			// Just in case making multilingual and loading resources are needed one day.
			public static string TypeDOCTYPE		= "The URI to DTD in System ID of DOCTYPE is not correct";
			public static string TypeValidation		= "An error found in validaing XML document";
			public static string TypeException		= "An exception occured in parsing XML document";
			public static string TypeAnalysis		= "An error found in analyzing source XML";
			public static string TypeEncoding		= "Delta encoder has returned an error";
			public static string TypeInternal		= "An internal error occured";

			public static string Msg_DupLangNull	= "Duplication of the element with no \x22lang\x22 attribute is not allowed.";
			public static string Msg_DupLangValue	= "Duplication of the element having the same value of the \x22lang\x22 attribute is not allowed.";
			public static string Msg_DupNoRegistry	= "<install> element witout <registry> element is able to exist only 1 simaltaneously.";
			public static string Msg_NoReferenceID	= "Referenced IDs do not exist.";
			public static string Msg_AbsolutePath	= "Absolute path can not be specified here.";

			//public static string Msg_FileNotExists	= "File not exists.";

			public static string Msg_UniqSibling	= "Cannot be duplicated in sibling elements";
			public static string Msg_AttrNumber		= "Attribute must be a number";
		}

		/// <summary>DOCTYPE error</summary>
		/// <remarks>System ID(URI to DTD) in &lt;!DOCTYPE&gt; is wrong.</summary>
		public class ErrorDOCTYPE : Error
		{
			public ErrorDOCTYPE() : base(TypeDOCTYPE) {}
		}

		/// <summary>Validation error</summary>
		/// <remarks>Also a wrapper class of XmlSchemaException.</summary>
		public class ErrorValidationFailed : Error
		{
			public string File { protected set; get; }
			public ErrorValidationFailed(System.Xml.Schema.XmlSchemaException e, string file) : base(TypeValidation,e)
			{
				this.File = file;
			}
			public override string ToString()
			{
				System.Xml.Schema.XmlSchemaException e = (System.Xml.Schema.XmlSchemaException)this.InnerException;
				return					$"[{this.Message}]" +
					$"{lineBreak}" +	$"{e.Message}" + 
					$"{lineBreak}" +	$"in Line {e.LineNumber}, Position {e.LinePosition} of \x22{this.File}\x22" +
#if DEBUG
					$"{lineBreak}" +	$"Source:{e.Source}" +
					$"{lineBreak}" +	$"SourceUri:{e.SourceUri}" +
					$"{lineBreak}" +	$"SourceSchemaObject:{e.SourceSchemaObject}" +
#endif
					 "";
			}
		}

		/// <summary>Miscellaneous XML errors</summary>
		/// <remarks>Also a wrapper class of XmlException.</summary>
		public class ErrorXmlException : Error
		{
			public ErrorXmlException(XmlException e) : base(TypeException,e) {}
			public override string ToString()
			{
				XmlException e = (XmlException)this.InnerException;
				return					$"[{this.Message}]" +
					$"{lineBreak}" +	$"{e.Message}" + 
					$"{lineBreak}" +	$"in {e.SourceUri}." + 
					 "";
			}
		}

		/// <summary>Error in analysis of XML data and file structure</summary>
		public class ErrorAnalysis : Error
		{
			private string detailMessage;
			public ErrorAnalysis(string s) : base(TypeAnalysis) => detailMessage = s;
			public override string ToString()
			{
				XmlException e = (XmlException)this.InnerException;
				return					$"[{this.Message}]" +
					$"{lineBreak}" +	$"{detailMessage}" + 
					 "";
			}
		}

		/// <summary>ErrorDeltaEncodingFailed</summary>
		/// <remarks>Delta encoding library has failed and returned error code.</summary>
		public class ErrorDeltaEncodingFailed : Error
		{
			public string Encoder { protected set; get; }
			public ErrorDeltaEncodingFailed(string encoder) : base(TypeEncoding)
			{
				this.Encoder = encoder;
			}
			public override string ToString()
			{
				System.Xml.Schema.XmlSchemaException e = (System.Xml.Schema.XmlSchemaException)this.InnerException;
				return					$"[{this.Message}]" +
					$"{lineBreak}" +	$"{e.Message}" + 
					$"{lineBreak}" +	$"in Line {e.LineNumber}, Position {e.LinePosition}, Encoder=\x22{this.Encoder}\x22" +
					 "";
			}
		}

		/// <summary>Miscellaneous internal error</summary>
		public class ErrorInternal : Error
		{
			private string detailMessage;
			public ErrorInternal(string s) : base(TypeInternal) => detailMessage = s;
			public override string ToString()
			{
				return					$"[{this.Message}]" +
					$"{lineBreak}" +	$"{detailMessage}" + 
					 "";
			}
		}

}	// ==== namespace Tikubiken ============================================================
