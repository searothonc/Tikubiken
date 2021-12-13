/* ********************************************************************** *\
 * Tikubiken binary patch updater ver 1.0.0
 * Processor class for diff
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Tikubiken.Properties;

namespace Tikubiken
{
    public sealed class Processor : IDisposable
	{
		//--------------------------------------------------------
		// Constants
		//--------------------------------------------------------

		// Block size of file operation(at least >= TPHeader.AlignmentTBP(=16)
		private const int c_blockSize		= 65536;		// 64KB

		// line break sugar
		private static string lineBreak => System.Environment.NewLine;

		// Resource names
		// Resouce ID for diff format DTD
		private const string c_ridDTDdiff			= @"Tikubiken.Resources.tikubiken_diff100.dtd";
		// Resouce ID for Update.exe
		private const string c_ridUpdateExe		= @"Tikubiken.Resources.Update.exe";

		// System ID URIs of DTD for patch
		private const string c_uriDTDpatch		= @"https://raw.githubusercontent.com/searothonc/Tikubiken/master/dtd/tikubiken_patch100.dtd";

		//============================================================
		// Batch operation
		//============================================================

		/// <summary>Container to store file operations</summary>
		private class Batch
		{
			public enum Op
			{
				None,
				CreateDir,
				CopyFile,
				Diff,
				DigestMD5,
				DigestSHA1,
			}

			// Fields
			//------------------------------
			private FileSystemInfo	fsiFrom;
			private FileSystemInfo	fsiTo;

			// Properties
			//------------------------------
			public Op				Operation	{ get; protected set; }
			public int				SizeKB		{ get; set; }
			public FileInfo			FileFrom	{ get => fsiFrom as FileInfo;		set => fsiFrom = value; }
			public FileInfo			FileTo		{ get => fsiTo   as FileInfo;		set => fsiTo   = value; }
			public FileInfo			FileDiff	{ get; protected set; }
			public DirectoryInfo	DirFrom		{ get => fsiFrom as DirectoryInfo;	set => fsiFrom = value; }
			public DirectoryInfo	DirTo		{ get => fsiTo   as DirectoryInfo;	set => fsiTo   = value; }
			public string			Id			{ get; protected set; }
			public string			DigestSrc	{ get; set; }
			public string			DigestDst	{ get; set; }

			// Constructtors
			//------------------------------

			// Operation = create a directory
			public Batch(DirectoryInfo dirCreate) 
				=> (this.fsiFrom,this.fsiTo,this.Operation) = (null,dirCreate,Op.CreateDir);
			// The equivalent as above from a string
			public Batch(string dirCreate) 
				=> (this.fsiFrom,this.fsiTo,this.Operation) = (null, new DirectoryInfo(dirCreate), Op.CreateDir);

			// Operation = copy a file
			public Batch(FileInfo fileFrom, FileInfo fileTo) => init(fileFrom, fileTo);
			// The equivalent as above from strings
			public Batch(string fileFrom, string fileTo) => init(new FileInfo(fileFrom), new FileInfo(fileTo));

			// Operation = take diff between fileFrom and fileTo and save the delta to fileDiff
			public Batch(FileInfo fileFrom, FileInfo fileTo, FileInfo fileDiff) => init(fileFrom, fileTo, fileDiff);
			// The equivalent as above from strings
			public Batch(string fileFrom, string fileTo, string fileDiff) 
				=> init(new FileInfo(fileFrom), new FileInfo(fileTo), new FileInfo(fileDiff));

			/*
						public Batch(FileInfo fileFrom, XElement e, XAttribute a)
							=> (this.fsiFrom,this.fsiTo,this.Id,this.Operation,this.SizeKB) 
								= (fileFrom, null, 
									e.Attribute("version").Value, (a.Value == "MD5") ? Op.DigestMD5 : Op.DigestSHA1, 
									(int)(fileFrom.Length/1024));
			*/
			public Batch(FileInfo fileFrom, FileInfo fileTo, XElement e, XAttribute a)
				=> (this.fsiFrom, this.fsiTo, this.Id, this.Operation, this.SizeKB)
					= (fileFrom, fileTo,
						e.Attribute("version").Value, (a.Value == "MD5") ? Op.DigestMD5 : Op.DigestSHA1,
						(int)(fileFrom.Length / 1024L) + (int)(fileTo.Length / 1024L)   //considering precision matter
					);

			// Initialyze in case of CopyFile
			private void init(FileInfo fileFrom, FileInfo fileTo)
			{
				( this.fsiFrom, this.fsiTo, this.Operation ) = ( fileFrom, fileTo, Op.CopyFile );

				// File existence check
				//if ( !this.fsiFrom.Exists ) throw new ErrorAnalysis( Error.Msg_FileNotExists + lineBreak + this.fsiFrom.FullName );
				if ( !this.fsiFrom.Exists ) throw new FileNotFoundException(null,this.fsiFrom.FullName);

				// Calcurate working size
				this.SizeKB = (int)( this.FileFrom.Length / 1024 );		// unit is KB
				if ( this.SizeKB < 1 ) this.SizeKB = 1;		// at least 1KB
			}

			// Initialyze in case of Diff
			private void init(FileInfo fileFrom, FileInfo fileTo, FileInfo fileDiff) 
			{
				( this.fsiFrom, this.fsiTo, this.FileDiff, this.Operation ) = ( fileFrom, fileTo, fileDiff, Op.Diff );

				// File existence check
				//if ( !this.fsiFrom.Exists ) throw new ErrorAnalysis( Error.Msg_FileNotExists + lineBreak + this.fsiFrom.FullName );
				if ( !this.fsiFrom.Exists ) throw new FileNotFoundException(null, this.fsiFrom.FullName);
				//if ( !this.fsiTo.Exists   ) throw new ErrorAnalysis( Error.Msg_FileNotExists + lineBreak + this.fsiTo.FullName );
				if ( !this.fsiTo.Exists   ) throw new FileNotFoundException(null, this.fsiTo.FullName);

				// Calcurate working size
				long sum = this.FileFrom.Length + this.FileTo.Length;
				sum /= 1024;											// unit is KB
				if ( sum < 1 ) sum = 1;									// at least 1KB
				this.SizeKB = (int) sum;								// sum of two for diff
			}
		}

		//============================================================
		// User exceptions
		//============================================================

		/// <summary>Base for user exception sub-classes in this class</summary>
		public class Error : System.Exception
		{
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

		//============================================================
		// Body of Processor class
		//============================================================

		//--------------------------------------------------------
		// Fields
		//--------------------------------------------------------

		// URI for DTD
		private readonly Uri uriDTD   = new Uri( "http://raw.githubusercontent.com/searothonc/Tikubiken/master/dtd/tikubiken_diff100.dtd");
		private readonly Uri uriDTD_s = new Uri("https://raw.githubusercontent.com/searothonc/Tikubiken/master/dtd/tikubiken_diff100.dtd");

		// Progress<T>
		private IProgress<ProgressState>	m_progress;

		// CancellationTokenSource for asyncronous processing
		private CancellationTokenSource		ctSource;

		// Progress state
		private ProgressState				currentProgress;

		// Temporary directory
		private DirectoryInfo				dirTmp;

		// Work(output image) directory
		private DirectoryInfo				dirWork;

		// Working objects
		private XDocument					xmlDoc;
		private List<Batch>					listBatch;

		//--------------------------------------------------------
		// Properties
		//--------------------------------------------------------

		// File paths
		public FileInfo fiSourceXML			{ private set; get; }
		public FileInfo fiOutput			{ private set; get; }

		// Encoding format
		public DeltaFormat EncodingFormat	{ private set; get; }

		//--------------------------------------------------------
		// Utils
		//--------------------------------------------------------

		/// <summary>Absolute path from the loaded XML file.</summary>
		/// <param>(string)relPath=Path to convert, absolute or relative.</param>
		/// <remarks>
		/// If the relPath is a relative path, it converted to the absolute path 
		/// from loaded XML source file. If relPath is absolute, done nothing.
		/// <remarks>
		public string PathFromXML(string relPath)  => IsAbsPath(relPath) ? relPath : JoinPath(fiSourceXML.Directory.FullName, relPath);

		/// <summary>Convert relative path to absolute path from the result temporary folder.</summary>
		public string PathToResult(string relPath) => JoinPath(dirWork.FullName, relPath);

		/// <summary>Concatenate multiple relative path.</summary>
		/// <remarks>
		/// If you want to make an absolute path, first path in the array must be absolute.
		/// <remarks>
		//public static string JoinPath(params string[] p) => Path.GetFullPath( Path.Join(p) );
		public static string JoinPath(params string[] p) => Ext.JoinPath(p);

		// Constants for IsAbsPath()
		private static string _IsAbsPath_ds1 = Regex.Escape( Path.DirectorySeparatorChar.ToString() );
		private static string _IsAbsPath_ds2 = Regex.Escape( Path.AltDirectorySeparatorChar.ToString() );
		private static string _IsAbsPath_vs  = Regex.Escape( Path.VolumeSeparatorChar.ToString() );
		private static Regex  _IsAbsPath_re  = new Regex( $"^[A-Za-z]{_IsAbsPath_vs}[{_IsAbsPath_ds1}{_IsAbsPath_ds2}]" );
		/// <summary>Check if the string is an absolute path.</summary>
		public static bool IsAbsPath(string pathToTest) => _IsAbsPath_re.IsMatch(pathToTest);

		//--------------------------------------------------------
		// Constructors
		//--------------------------------------------------------
		///	<summary>Constructor</summary>
		// 	<param name="handler">(Action{ProgressState})
		///	Callback method to operate progress by retrieving 
		//	newest state via ProgressState object as parametor.
		/// </param>
		public Processor(Action<ProgressState> handler)
		{
			//m_progress	= null;
			//dirTmp		= null;

			// Initialyze Progress<T> and state container
			InitProgressState(handler);

			// Create cancellation token source
			ctSource = new CancellationTokenSource();
		}

		//--------------------------------------------------------
		// Initialyzations & Cleaning ups
		//--------------------------------------------------------

		// Initialyze Progress<T> and state container
		private void InitProgressState(Action<ProgressState> handler)
		{
			m_progress = new Progress<ProgressState>(handler);
			currentProgress.Usage	= ProgressState.Op.None;
			currentProgress.Min		= 0;
			currentProgress.Max		= 200;
			currentProgress.Value	= 0;
			currentProgress.Text	= "";
		}

		///	<summary>Dispose objects</summary>
		public void Dispose()
		{
			// Progress<T> and state container
			m_progress = null;

			// Dispose cancellation token source
			ctSource?.Dispose();
			ctSource = null;

			// for this class itself
			DeleteTemporaryDirectory();
		}

		//--------------------------------------------------------
		// Diagnosis
		//--------------------------------------------------------

		// Batch list as text
		public string ReportBatch()
		{
			string nowtime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
			string strReport = $"[Recorded: {nowtime}]" + lineBreak;

			foreach ( var b in listBatch )
			{
				// Operation
				strReport += b.Operation.ToString();

				// Size
				strReport += "\t";
				strReport += b.SizeKB.ToString("#,0");

				FileSystemInfo fsi;
				strReport += "\t";
				fsi = b.FileFrom;
				strReport += fsi?.FullName ?? "null";

				strReport += "\t";
				fsi = b.FileTo;
				if (b.Operation  == Batch.Op.CreateDir) fsi = b.DirTo;
				strReport += fsi?.FullName ?? "null";

				strReport += "\t";
				fsi = b.FileDiff;
				strReport += fsi?.FullName ?? "null";

				strReport += lineBreak;
			}

			return strReport;
		}

		//--------------------------------------------------------
		// Sync task done bofore async
		//--------------------------------------------------------

		/// <summary>Operations having to be done before async task</summary>
		///	<returns>(int)Maximum value of progress range.</returns>
		public (int max, int val) Ready( string sourceXML, string outputPath, DeltaFormat encodingFormat )
		{
			// Encoding format
			this.EncodingFormat = encodingFormat;

			// FileInfo for input and output file
			fiSourceXML = new FileInfo(sourceXML);
			fiOutput = new FileInfo(outputPath);

			// Check if input XML file exists
			if ( !fiSourceXML.Exists ) throw new FileNotFoundException(null,sourceXML);
			currentProgress.Max = (int) fiSourceXML.Length;

			// Create temporary work directory
			CreateTemporaryDirectory();
			dirWork = dirTmp.CreateSubdirectory(Path.GetFileNameWithoutExtension(fiOutput.Name));

			// Parse XML document
			LoadXML();
			ErrorCheckXML();
			ParseXML();

			// Set progress position to XML file size
			currentProgress.Value = currentProgress.Max;

			// Sum of all working size
			currentProgress.Max += listBatch.Sum( x => x.SizeKB );

			// Maximum value of progress range
			return (currentProgress.Max, currentProgress.Value);
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

		// Loading source XML
		private void LoadXML()
		{
			// Assembly currently running
			var asm = Assembly.GetExecutingAssembly();

			// Settings for XML Reader
			var settings = new XmlReaderSettings();
			settings.DtdProcessing					= DtdProcessing .Parse;
			settings.IgnoreComments					= true;
			settings.IgnoreProcessingInstructions	= true;
			settings.IgnoreWhitespace				= true;
			//settings.LineNumberOffset				= 1;
			//settings.LinePositionOffset				= 1;
			settings.ValidationType					= ValidationType.DTD;

			string docDTD;
			using ( var stream = asm.GetManifestResourceStream(c_ridDTDdiff) )
			using ( var reader = new StreamReader(stream) )
				docDTD = reader.ReadToEnd();

			using ( var streamXML = fiSourceXML.OpenRead() )
			{

				// In order to skip download DTD from URL,
				// create resolver from same file in resource.
				var resolver = new System.Xml.Resolvers.XmlPreloadedResolver();
				resolver.Add(uriDTD,   docDTD);		// for http://...
				resolver.Add(uriDTD_s, docDTD);		// and for https://...
				settings.XmlResolver = resolver;

				try
				{
					// Load XML document
					xmlDoc = XDocument.Load( XmlReader.Create(streamXML, settings), LoadOptions.SetLineInfo );
				}
				// Error found in validating by DTD throws System.Xml.Schema.XmlSchemaException
				catch (System.Xml.Schema.XmlSchemaException e)
				{
					// Replace with user exception
					throw new ErrorValidationFailed(e,fiSourceXML.FullName);
				}
				// Wrong DTD and other errors before validation throws XmlException
				catch (XmlException e)
				{
					// Replace with user exception
					throw new ErrorXmlException(e);
				}
			}
		}

		//--------------------------------------------------------
		// Sync task: XML error check
		//--------------------------------------------------------

		// Error check before parse XML
		private void ErrorCheckXML()
		{
			ErrorCheckXML_DistinctAttribute_lang();
			ErrorCheckXML_CheckDuplicateInstallPlacement();
			ErrorCheckXML_CheckIdentityPathRelative();
			//ErrorCheckXML_CheckPatchrefIDReference();	// already checked in DTD validaton
		}

		// Ensure "lang" attributes are not duplicated.
		private void ErrorCheckXML_DistinctAttribute_lang()
		{
			// "lang" attribute dupilication check
			// for child elements of <updeter> element.
			var elm = xmlDoc.Descendants("updater").First();
			var target = elm.Descendants("title");
			ErrorCheckXML_DistinctLanguages(target,"title");
			target = elm.Descendants("cover");
			ErrorCheckXML_DistinctLanguages(target,"cover");

			// for child <text> elements of <message> elements.
			foreach ( var elmMsg in xmlDoc.Descendants("message") )
			{
				var tx =
					from e in elmMsg.Elements("text") 
					group e by e.Attribute("type")?.Value;
				foreach ( var t in tx )
				{
					string key = t.Key == null ? $" type=\x22{t.Key}\x22" : "";
					ErrorCheckXML_DistinctLanguages(t,$"text{key}");
				}
			}
		}

		// Check the dupurication of language attribute of the specified elements
		private void ErrorCheckXML_DistinctLanguages(IEnumerable<XElement> target, string elmName)
		{
			// Duplicate elements with no lang attribute are not allowed.
			var elms = 
				from e in target
				where e.Attribute("lang") == null
				select e;
			if ( elms.Count() > 1 )
			{
				string str = "";
				var causes = 
					from IXmlLineInfo info in elms 
					where info.HasLineInfo() 
					select $"{lineBreak}<{elmName}> in Line {info.LineNumber}, Position {info.LinePosition}";
				foreach ( string s in causes ) { str += s; }
				throw new ErrorAnalysis( Error.Msg_DupLangNull + str );
			}

			// Duplicate lang attributes having the same value.
			var attvals = 
				from elm in target
				where elm.Attribute("lang") != null
				select elm.Attribute("lang") into a
				group a by a.Value into g
				where g.Count() > 1
				select g;
			if ( attvals.Count() > 1 )
			{
				string key, str = "";
				foreach ( var val in attvals )
				{
					key = val.Key;
					var causes = 
						from IXmlLineInfo info in val
						where info.HasLineInfo() 
						select $"{lineBreak}<{elmName} lang=\x22{key}\x22> in Line {info.LineNumber}, Position {info.LinePosition}";

					foreach ( string s in causes ) { str += s; }
				}
				throw new ErrorAnalysis( Error.Msg_DupLangValue + str );
			}
		}

		private void ErrorCheckXML_CheckDuplicateInstallPlacement()
		{
			var elms = 
				from e in xmlDoc.Descendants("install")
				where e.Element("registry") == null
				select e;
			if ( elms.Count() < 2 ) return;

			var causes = 
				from IXmlLineInfo info in elms
				where info.HasLineInfo() 
				select $"{lineBreak}<install> in Line {info.LineNumber}, Position {info.LinePosition}";
			throw new ErrorAnalysis( Error.Msg_DupNoRegistry + String.Join("",causes) );
		}

		private void ErrorCheckXML_CheckIdentityPathRelative()
		{
			var errs =
				from e in xmlDoc.Descendants("identity")
				select e.Attribute("path") into a
				where IsAbsPath(a.Value)
				select a;
			if ( errs.Count() < 1 ) return;

			// Build error message
			var causes = 
				from IXmlLineInfo info in errs
				where info.HasLineInfo() 
				select $"{lineBreak}<identity> in Line {info.LineNumber}, Position {info.LinePosition}";
			throw new ErrorAnalysis( Error.Msg_AbsolutePath + String.Join("",causes) );
		}

		private void ErrorCheckXML_CheckPatchrefIDReference()
		{
			// Extract IDs from <patch> element's "version" attribute defined to be required in DTD.
			var idList = from e in xmlDoc.Descendants("patch") select e.Attribute("version").Value;

			// Checku if <patchref> referencing "version"s exist in IDs above.
			string str = "";
			foreach ( var attr in (from e in xmlDoc.Descendants("patchref") select e.Attribute("version")) )
			{
				if ( idList.Contains(attr.Value) ) continue;
				IXmlLineInfo info = attr as IXmlLineInfo;
				str += $"{lineBreak}";
				str += $"<patchref version=\x22{attr.Value}\x22> in Line {info.LineNumber}, Position {info.LinePosition}";
			}
			throw new ErrorAnalysis( Error.Msg_NoReferenceID + str );
		}

		//--------------------------------------------------------
		// Sync task: Parse XML
		//--------------------------------------------------------

		// Parse loaded source XML
		private void ParseXML()
		{
			listBatch = new List<Batch>();

			// Image path from <cover> elements
			ParseXML_AggregateCovers();

			// Files to calculate hash digest from <identity> elements
			ParseXML_AggregateIdentities();

			// Folders to take differences from <patch> elements
			ParseXML_AggregatePatches();
		}

		// Aggregate <cover> elements
		private void ParseXML_AggregateCovers()
		{
			var covers = 
				from e in xmlDoc.Descendants("cover") 
				select e.Attribute("image").Value;

			FileInfo src, dst;
			foreach ( string file in covers )
			{
				src = new FileInfo( PathFromXML(file) );
				dst = new FileInfo( PathToResult(src.Name) );
				listBatch.Add( new Batch(src,dst) );
			}
		}

		// Aggregate <identity> elements
		private void ParseXML_AggregateIdentities()
		{
			foreach ( XElement elmIdentity in xmlDoc.Descendants("identity") )
			{
				// <patch> element refered in <indentity> element
				XElement elmPatch = elmIdentity.Element("patch");
				if ( elmPatch == null )
				{
					var id = elmIdentity.Element("patchref").Attribute("version").Value;
					elmPatch = (from e in xmlDoc.Descendants("patch")
								 where e.Attribute("version").Value == id
								 select e).First();
				}

				// "path" attribute value from <branch>/<target> element under <patch> element
				string pathBranch = PathFromXML(elmPatch.Element("branch").Attribute("path").Value );
				string pathTarget = PathFromXML(elmPatch.Element("target").Attribute("path").Value );

				// Relative path from <branch>/<target> path specified as <identity path="...">
				string relPath = elmIdentity.Attribute("path").Value;

				FileInfo fiBranch = new FileInfo( JoinPath(pathBranch, relPath) );
				if ( !fiBranch.Exists ) throw new FileNotFoundException(null,fiBranch.FullName);
				FileInfo fiTarget = new FileInfo( JoinPath(pathTarget, relPath) );
				if ( !fiTarget.Exists ) throw new FileNotFoundException(null,fiTarget.FullName);

				listBatch.Add( new Batch(fiBranch, fiTarget, elmPatch, elmIdentity.Attribute("method")) );
			}
		}

		// Aggregate <patch> elements
		private void ParseXML_AggregatePatches()
		{
			// Sum of file sizes for branch and target each, in KB
			int sizeBranch = 0;
			int sizeTarget = 0;

			// Go round all <patch> elements
			foreach ( var elmPatch in xmlDoc.Descendants("patch") )
			{
				// Version ID
				string strID = elmPatch.Attribute("version").Value;
				DirectoryInfo dirResult = new DirectoryInfo(PathToResult(strID));
				listBatch.Add( new Batch(dirResult) );

				// <branch> element
				var elmBranch = elmPatch.Element("branch");
				var strBranchPath = PathFromXML(elmBranch.Attribute("path").Value);
				var ignoreBranch = 
					from e in elmBranch.Elements("exclude")
					select ParseXML_PathToRegex(e);

				// <target> element
				var elmTarget = elmPatch.Element("target");
				var strTargetPath = PathFromXML(elmTarget.Attribute("path").Value);
				var ignoreTarget = 
					from e in elmTarget.Elements("exclude")
					select ParseXML_PathToRegex(e);

				// Differences of file structure between two directories.
				(sizeBranch, sizeTarget) = ParseXML_CompareDirectory(dirResult, ".", 
						elmPatch, strBranchPath, ignoreBranch, strTargetPath, ignoreTarget);

				// Write size differences between branch and target to XML (as real byte, not cluster or KB)
				elmPatch.Add( new XAttribute("balance", 
					(sizeTarget>sizeBranch ? sizeTarget-sizeBranch : 0) * TBPHeader.ClusterSize) );

				// Remove <branch><target> element from XML
				elmBranch.Remove();
				elmTarget.Remove();
			}
		}

		// Regex from path and match attribute
		private Regex ParseXML_PathToRegex(XElement elm)
		{
			string path = elm.Attribute("name").Value;
			string match = elm.Attribute("match").Value ?? "wildcard";

			// "exact" and "regex" are not or least needed to convert.
			if ( match != "regex" ) path = Regex.Escape(path);	// escape if "exact" or "wildcard"
			if ( match == "exact" ) path = $"^{path}$";			// "exact" is whole length match
			if ( match != "wildcard" ) return new Regex(path);	// return if "exact" or "regex"

			// Only "wildcard" needs conversion
			path = path.Replace( @"\?", @"." );		// 1 letter match in wildcard '?' to regex '.'
			path = path.Replace( @"\*", @".*" );	// sequence match in wildcard '*' to regex '.*'
			return new Regex(path);
		}

		// Exclude path
		private bool ParseXML_MatchExclude(FileSystemInfo fsi, Regex exclusion)
		{
			string pattern = exclusion.ToString();
			string target = fsi.Name;
			if ( pattern.Contains(Path.AltDirectorySeparatorChar) )	target = fsi.FullName;
			if ( pattern.Contains(Path.DirectorySeparatorChar) )	target = fsi.FullName;
			if ( pattern.Contains(Path.VolumeSeparatorChar) )		target = fsi.FullName;
			return exclusion.IsMatch(target);
		}

		// Compare file structures between two directories.
		private (int sizeOfBranch, int sizeOfTarget) ParseXML_CompareDirectory( DirectoryInfo dirResult, 
									string relativePath, XElement elmPatch,
									string branchBase, IEnumerable<Regex> branchesExclude, 
									string targetBase, IEnumerable<Regex> targetsExclude )
		{
			// Sum of file sizes for branch and target each, in KB
			int sizeBranch = 0;
			int sizeTarget = 0;
			int subBranch, subTarget;

			DirectoryInfo dirBranch = new DirectoryInfo( JoinPath(branchBase, relativePath) );
			DirectoryInfo dirTarget = new DirectoryInfo( JoinPath(targetBase, relativePath) );

			// Create new batch command for new directory
			if ( relativePath != "." )
			{
				string strDirResult = JoinPath(dirResult.FullName, relativePath);
				listBatch.Add( new Batch(strDirResult) );
			}

			// Enumerate directories in the branch side.
			IEnumerable<string> strBranchDirs = 
				from d in dirBranch.EnumerateDirectories()
				where !branchesExclude.Any( r => ParseXML_MatchExclude(d,r) )
				select d.Name;

			// Enumerate directories in the target side.
			IEnumerable<string> strTargetDirs = 
				from d in dirTarget.EnumerateDirectories()
				where !targetsExclude.Any( r => ParseXML_MatchExclude(d,r) )
				select d.Name;

			// Directories intersection of branches and targets
			IEnumerable<string> dirsCommonToBoth = strBranchDirs.Intersect(strTargetDirs);
			IEnumerable<string> dirsOnlyInBranch = strBranchDirs.Except(strTargetDirs);
			IEnumerable<string> dirsOnlyInTarget = strTargetDirs.Except(strBranchDirs);

			// Relatively descend into sub-directories
			foreach ( string d in dirsCommonToBoth )
			{
				(subBranch, subTarget) = ParseXML_CompareDirectory( dirResult, 
											Path.Join(relativePath,d), elmPatch, 
											branchBase, branchesExclude, 
											targetBase, targetsExclude );
				sizeBranch += subBranch;
				sizeTarget += subTarget;
			}

			// Write the directories to be removed into XML
			foreach ( string d in dirsOnlyInBranch )
			{
				elmPatch.Add( new XElement("remove",
					new XAttribute("path",Path.Join(relativePath,d)+Path.DirectorySeparatorChar)) );
			}

			// Recursively record the directories to create new
			foreach ( string d in dirsOnlyInTarget ) 
				ParseXML_CreateNewDirectory( dirResult, Path.Join(relativePath,d), targetBase, targetsExclude );

			// Enumerate files in the branch side.
			IEnumerable<string> strBranchFiles = 
				from d in dirBranch.EnumerateFiles()
				where !branchesExclude.Any( r => ParseXML_MatchExclude(d,r) )
				select d.Name;

			// Enumerate files in the target side.
			IEnumerable<string> strTargetFiles = 
				from d in dirTarget.EnumerateFiles()
				where !targetsExclude.Any( r => ParseXML_MatchExclude(d,r) )
				select d.Name;

			// Sum file sizes, for branch estimating least
			// cluster size is maximum possible value of Windows file system
			sizeBranch += (	from f in dirBranch.EnumerateFiles()
							where !branchesExclude.Any( r => ParseXML_MatchExclude(f,r) )
							select (int) f.Length / TBPHeader.ClusterSize).Sum();

			// Sum file sizes for target estimating larger 
			sizeTarget += (	from f in dirTarget.EnumerateFiles()
							where !targetsExclude.Any( r => ParseXML_MatchExclude(f,r) )
							select ((int) f.Length + TBPHeader.ClusterSize - 1) / TBPHeader.ClusterSize ).Sum();;


			// Files intersection of branches and targets
			IEnumerable<string> filesCommonToBoth = strBranchFiles.Intersect(strTargetFiles);
			IEnumerable<string> filesOnlyInBranch = strBranchFiles.Except(strTargetFiles);
			IEnumerable<string> filesOnlyInTarget = strTargetFiles.Except(strBranchFiles);

			// Write the files to be removed into XML
			foreach ( string f in filesOnlyInBranch )
				elmPatch.Add( new XElement("remove",new XAttribute("path",Path.Join(relativePath,f))) );

			// Create batch to take differences between branch and target
			foreach ( string f in filesCommonToBoth )
			{
				string diff = JoinPath( dirResult.FullName, relativePath, f ) + Ext.DeltaExt;
				listBatch.Add( new Batch(JoinPath(branchBase, relativePath, f), JoinPath(targetBase, relativePath, f), diff) );
			}

			// Create batch to copy new files from target
			foreach ( string f in filesOnlyInTarget )
			{
				string dst = JoinPath( dirResult.FullName, relativePath, f );
				listBatch.Add( new Batch(JoinPath(targetBase, relativePath, f), dst) );
			}

			return (sizeBranch, sizeTarget);
		}

		// Create new directory and copy recursively sub-directories and files
		private void ParseXML_CreateNewDirectory( DirectoryInfo dirResult, 
									string relativePath, string targetBase, IEnumerable<Regex> targetsExclude )
		{
			// Create new batch command for new directory
			string strDirResult = JoinPath(dirResult.FullName, relativePath);
			listBatch.Add( new Batch(strDirResult) );

			// Relatively descend into sub-directories
			DirectoryInfo dirTarget = new DirectoryInfo( JoinPath(targetBase, relativePath) );
			// Enumerate directories in the target side.
			IEnumerable<string> dirSub = 
				from d in dirTarget.EnumerateDirectories()
				where !targetsExclude.Any( r => ParseXML_MatchExclude(d,r) )
				select d.Name;
			foreach ( var d in dirSub )
				ParseXML_CreateNewDirectory( dirResult, Path.Join(relativePath,d), targetBase, targetsExclude );

			// Files to copy
			IEnumerable<string> files = 
				from d in dirTarget.EnumerateFiles()
				where !targetsExclude.Any( r => ParseXML_MatchExclude(d,r) )
				select d.Name;
			foreach ( var f in files )
			{
				listBatch.Add( new Batch(
						JoinPath(dirTarget.FullName, f),
						JoinPath(strDirResult, f)
					) );
			}
		}

		//--------------------------------------------------------
		// Async task: public entry point
		//--------------------------------------------------------

		///	<summary>
		///	Entry point of async task
		///	</summary>
		///	<remark>
		///	Usually caller need dispose using block to dipose Processor such as:
		///		// Triggering UI disable codes here
		///		using ( var processor = new Processor(handler) )
		///		{
		///			await processor.RunAsync();
		///		}
		///		// Triggering UI enable codes here
		///	</remark>
		public async Task RunAsync( bool dupXML )
		{
			try
			{
				await Task.Run( () => RunBody() );

				// Set progress bar to maximum
				currentProgress.Value = currentProgress.Max;
				ReportDeltaProgress(0);

				RemoveEmptyDirectories( dirWork );
				ThrowIfCancellationRequested();

				// Convert XML to patch format and save to patch.xml
				SaveXML(dupXML);
				ThrowIfCancellationRequested();

				// Save to zip archive
				ReportMessage(Resources.log_compress);
				await CreateArchive();

				// Post message to reset progress bar
				//currentProgress = new ProgressState();
				//currentProgress.Usage	= ProgressState.Op.Progress;
				//PostReport();
			}
			catch (OperationCanceledException)
			{
				// do what to do when operation has been cancelled.
				// *CAUTION!* This block does not re-throw any exception ever.
				throw;
			}
			catch
			{
				//Re-throw if the exception other than cancellation occured.
				throw;
			}
		}

		/// <summary>
		/// Cancel running task.
		/// </summary>
		public void Cancel() => ctSource?.Cancel();

		//--------------------------------------------------------
		// Async task: private
		//--------------------------------------------------------

		// Body of async processing
		private async Task RunBody()
		{
			// To print log, call ReportMessage(string text)
			// To progress indicators, call ReportDeltaProgress(int value), this method advances progress bar by parameter
			// If both UI is needed, call Report(string text, int value), this method does not advance progress

			// Eveny point where the running operation can be cancelled,
			// the method call to throw OperationCanceledException is needed.
			//	<code>
			//		ThrowIfCancellationRequested();
			//	</code>
			// If UI thread has posted cancel request,
			// OperationCanceledException will be thrown there,
			// and immediately get out to the catch clause 
			// in the caller method this.RunAsync().

			// Process every batch operation
			foreach ( Batch b in listBatch )
			{
				switch ( b.Operation )
				{
					case Batch.Op.CreateDir:
						// Create directory
						b.DirTo.Create();
						break;
					case Batch.Op.CopyFile:
						await CopyFileAsync(b);
						break;
					case Batch.Op.Diff:
						await TakeDiffAsync(b);
						break;
					case Batch.Op.DigestMD5:
					case Batch.Op.DigestSHA1:
						await CaclDigestAsync(b);
						break;
					default:
						throw new Error("Operation code is out of range.");
				}

				// Accept cancel
				ThrowIfCancellationRequested();

				// Advance progress bar
				ReportDeltaProgress( b.SizeKB );
			}
		}

		// Throw exception when cancel
		private void ThrowIfCancellationRequested() => ctSource?.Token.ThrowIfCancellationRequested();

		// Send text message to UI
		private void ReportMessage(string text)
		{
			currentProgress.Text	= text;
			currentProgress.Usage	= ProgressState.Op.Log;
			PostReport();
		}

		// Send progress to UI
		private void ReportDeltaProgress(int value)
		{
			currentProgress.Value	+= value;
			currentProgress.Usage	= ProgressState.Op.Progress;
			PostReport();
		}

		// Send text and progress to UI
		private void Report(string text, int value)
		{
			currentProgress.Text	= text;
			currentProgress.Value	= value;
			currentProgress.Usage	= ProgressState.Op.Both;
			PostReport();
		}

		// Post progress to UI
		private void PostReport()
		{
			if ( m_progress == null ) return;
			m_progress.Report(currentProgress);
		}

		// Report progress as a percentage in the file
		private void ReportDeltaRate(float rate)
		{
			int newValue = currentProgress.Anchor + (int)( rate * currentProgress.Size );
			if ( currentProgress.Value > newValue ) return;
			currentProgress.Value = newValue;
			currentProgress.Usage	= ProgressState.Op.Progress;
			PostReport();
		}
		private void ReportDeltaRateFull()
		{
			currentProgress.Value = currentProgress.Anchor + currentProgress.Size;
			currentProgress.Usage	= ProgressState.Op.Progress;
			PostReport();
		}

		//--------------------------------------------------------
		// Async task: Copy existing file
		//--------------------------------------------------------
		private async Task CopyFileAsync(Batch op)
		{
			ReportMessage(Resources.log_copyfile + op.FileFrom.Name);

			// Open file streams
			using ( var fsSrc = op.FileFrom.Open(FileMode.Open, FileAccess.Read, FileShare.Read) )
			using ( var fsDst = op.FileTo.Create() )
				// Copy file async
				await fsSrc.CopyToAsync(fsDst, ctSource.Token );
		}

		//--------------------------------------------------------
		// Async task: Take differences between two files
		//--------------------------------------------------------
		private async Task TakeDiffAsync(Batch op)
		{
			// Load files to buffers
			byte[] binFrom = await LoadFileAsync( op.FileFrom );
			ThrowIfCancellationRequested();
			byte[] binTo   = await LoadFileAsync( op.FileTo );
			ThrowIfCancellationRequested();

			// Check if the two files are the same
			if ( op.FileFrom.Length==op.FileTo.Length && await IsSameFile(binFrom, binTo) ) return;
			ReportMessage( Resources.log_diff + op.FileFrom.Name );

			// Create coder
			ICoder coder;
			switch ( this.EncodingFormat )
			{
				// VCDiff
				case DeltaFormat.VCDiff:
				case DeltaFormat.VCDiff_Google:
				case DeltaFormat.VCDiff_XDelta3:
					coder = new VCDiffCoder(this.EncodingFormat);
					break;

				// BsDiff+Brotli
				case DeltaFormat.BsPlus_Optimal:
				case DeltaFormat.BsPlus_Fastest:
					coder = new BsDiffCoder(this.EncodingFormat);
					break;

				// Invalid value
				default:
					throw new ArgumentOutOfRangeException();
			}

			// Set current progress to anchor
			currentProgress.Anchor = currentProgress.Value;
			currentProgress.Size   =op.SizeKB;

			// Take diff async
			await coder.DoAsync( binFrom, binTo, op.FileDiff, ctSource.Token, new Progress<float>(ReportDeltaRate) );

			ReportDeltaRateFull();

			// Progress is already reported
			op.SizeKB = 0;
		}

		//--------------------------------------------------------
		// Async task: Read from file at once
		//--------------------------------------------------------
		private async Task<byte[]> LoadFileAsync( FileInfo fi )
		{
			byte[] buffer = new byte[fi.Length];
			using ( FileStream fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read) )
			{
				await fs.ReadAsync(buffer, 0, (int) fi.Length, ctSource.Token);
			}
			ThrowIfCancellationRequested();
			return buffer;
		}

		//--------------------------------------------------------
		// Async task: Compare two files to check if they are the same
		//--------------------------------------------------------
		private async Task<bool> IsSameFile( byte[] binFrom, byte[] binTo )
		{
			return await Task.Run<bool>( ()=> binFrom.SequenceEqual(binTo) );

			// Cancellation supported version
/*
			return await Task.Run<bool>( () => {
					int size = binFrom.Length;
					for ( int i = 0 ; i < size ; ++i )
					{
						if ( binFrom[i] != binTo[i] ) return false;
						ThrowIfCancellationRequested();
					}

					return true;
				} );
*/
		}

		//--------------------------------------------------------
		// Async task: Calcurate digest
		//--------------------------------------------------------
		private async Task CaclDigestAsync(Batch op)
		{
			ReportMessage(Resources.log_digest + op.FileFrom.Name);
			string algorithm = (op.Operation == Batch.Op.DigestMD5) ? "MD5" : "SHA1";
			op.DigestSrc = await Ext.CaclDigestAsync(op.FileFrom, algorithm);
			op.DigestDst = await Ext.CaclDigestAsync(op.FileTo,   algorithm);
			ThrowIfCancellationRequested();
		}

		//--------------------------------------------------------
		// Sync task: Delete empty directories recursively
		//--------------------------------------------------------
		private void RemoveEmptyDirectories( DirectoryInfo dirTarget )
		{
			// Recurse into subdirectories
			foreach ( DirectoryInfo dir in dirTarget.EnumerateDirectories() )
			{
				RemoveEmptyDirectories( dir );
			}

			// Do nothing if any subdirectories or files exist
			if ( dirTarget.GetDirectories().Length + dirTarget.GetFiles().Length > 0 ) return;

			// Delete if empty
			dirTarget.Delete();
		}

		//--------------------------------------------------------
		// Sync task: Convert and save XML
		//--------------------------------------------------------

		private void SaveXML( bool dupXML )
		{
			// Replace document type
			xmlDoc.DocumentType.SystemId = c_uriDTDpatch;

			// Replace "format" attribute for root <Tikubiken> element
			xmlDoc.Element("Tikubiken").Attribute("format").Value = "patch";

			// Replace <cover image="path"> to *.tbp#root
			var attrs = from e in xmlDoc.Descendants("cover") select e.Attribute("image");
			foreach ( var a in attrs ) a.Value = Path.GetFileName(a.Value);

			// Remove <branch> and <target> from <patch>
			foreach ( var e in xmlDoc.Descendants("branch") ) e.Remove();
			foreach ( var e in xmlDoc.Descendants("target") ) e.Remove();

			// Hash digest replacement here
			foreach ( var elmIdentity in xmlDoc.Descendants("identity") )
			{
				// Id is the value of "version" attribute in child <patch> or <patchref> element
				string id = 
					(from e in elmIdentity.Elements()
					 where e.Name == "patch" || e.Name == "patchref"
					 select e.Attribute("version").Value).First();
				elmIdentity.Add( new XAttribute("source",      (from b in listBatch where b.Id==id select b.DigestSrc).First()) );
				elmIdentity.Add( new XAttribute("destination", (from b in listBatch where b.Id==id select b.DigestDst).First()) );
			}

			// Save XML to file
			string xmlPath = PathToResult(Ext.PatchXML);
			xmlDoc.Save( xmlPath );
			if ( dupXML )
			{
				// copy patch.xml to executable directory
				File.Copy(xmlPath, JoinPath(System.Environment.CurrentDirectory,Ext.PatchXML),true);
			}
		}

		//--------------------------------------------------------
		// Async task: Create archive from temporary directory
		//--------------------------------------------------------
		private async Task CreateArchive()
		{
			// File paths
			string zipPath = JoinPath( dirTmp.FullName, fiOutput.Name + @".zip" );

			// Create ZIP archive without the base directory name
			await Task.Run( () => ZipFile.CreateFromDirectory( dirWork.FullName, zipPath, 
					CompressionLevel.Optimal, false ) );
			FileInfo fiZip = new FileInfo( zipPath );

			// Header for archive section
			TBPHeader header = new TBPHeader(this.EncodingFormat);
			header.UnzipSize = SumDirectorySize( dirWork );
			header.ZipLength = (int) fiZip.Length;

			ReportMessage(Resources.log_writing + fiOutput.Name);

			// Create directories to output file
			Directory.CreateDirectory(fiOutput.DirectoryName);

			try
			{
				// Copy temporary archive to output file
				using ( var fsArchive = fiOutput.Open(FileMode.Create, FileAccess.Write, FileShare.None) )
				{
					int paddingLength;
					int len;
					byte[] buf = new byte[c_blockSize];

					// Write exe section
					if ( Path.GetExtension(fiOutput.Name).ToUpper() == @".EXE" )
					{
						var asm = Assembly.GetExecutingAssembly();
						using ( var fsRes = asm.GetManifestResourceStream(c_ridUpdateExe) )
						{
							while ( fsRes.Position < fsRes.Length )
							{
								len = await fsRes.ReadAsync(buf, 0, c_blockSize, ctSource.Token);
								await fsArchive.WriteAsync(buf, 0, len, ctSource.Token);
							}
						}
					}
					header.HeadOffset  = (int) fsArchive.Position;
					header.HeadOffset += paddingLength = TBPHeader.PaddingLength(header.HeadOffset);

					// Zero padding after exe block
					Array.Fill<byte>(buf, 0, 0, TBPHeader.AlignmentTBP);
					await fsArchive.WriteAsync(buf, 0, paddingLength, ctSource.Token);

					// Write alignment padding after exe section
					header.ZipOffset = header.HeadOffset + TBPHeader.Size - 4;
					header.TailOffset = header.ZipOffset + header.ZipLength;
					header.TailOffset += paddingLength = TBPHeader.PaddingLength(header.TailOffset);

					// Write header to file
					fsArchive.WriteTBPHeader( header );

					// Write temporary archive to output asyncronously
					using ( var fsZip = fiZip.Open(FileMode.Open, FileAccess.Read, FileShare.None) )
					{
						// The first 4 bytes overlap the Zip header.
						fsZip.Seek(TBPHeader.OverlapLength, SeekOrigin.Begin);

						// Copy across streams
						while ( fsZip.Position < fsZip.Length )
						{
							len = await fsZip.ReadAsync(buf, 0, c_blockSize, ctSource.Token);
							await fsArchive.WriteAsync(buf, 0, len, ctSource.Token);
						}

						// Zero padding after zip block
						Array.Fill<byte>(buf, 0, 0, TBPHeader.AlignmentTBP);
						await fsArchive.WriteAsync(buf, 0, paddingLength, ctSource.Token);

						// Write tail information block
						fsArchive.WriteTBPHeader( header );
					}
				}
			}
			catch
			{
				// Delete output file when failed
				fiOutput.Delete();
				throw;
			}
		}

		// Sum the size of the directory recursively
		private int SumDirectorySize(DirectoryInfo dir)
				=> ( from d in dir.EnumerateDirectories() select d).Sum( d => SumDirectorySize(d) ) +
				   ( from f in dir.EnumerateFiles() select ((int) f.Length + TBPHeader.ClusterSize - 1) / TBPHeader.ClusterSize ).Sum();
	}
	//** public sealed class Processor **********************************************************

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

	// BsDiff
	//------------------------------
	class BsDiffCoder : ICoder
	{
		public DeltaFormat Format { get; set; }

		//--------------------------------------------------------
		// Constructors
		//--------------------------------------------------------
		public BsDiffCoder(DeltaFormat fmt) => this.Format = fmt;

		//--------------------------------------------------------
		// Compression level
		//--------------------------------------------------------
		private CompressionLevel compLevel
		{get{
			return ( this.Format & DeltaFormat.Compress_BsPlus_Mask ) switch {
					DeltaFormat.Compress_BsPlus_Optimal		=>	CompressionLevel.Optimal,
					DeltaFormat.Compress_BsPlus_Fastest		=>	CompressionLevel.Fastest,
					DeltaFormat.Compress_BsPlus_NoCompress	=>	throw new NotSupportedException(),
					_ => throw new ArgumentOutOfRangeException()
				};
		}}

		//--------------------------------------------------------
		// Implements of conventions
		//--------------------------------------------------------
		public async Task DoAsync( byte[] binSource, byte[] binTarget, FileInfo fiOutput, 
		System.Threading.CancellationToken cToken, Progress<float> progress )
		{
			// Take differences when the files are not the same
			using ( FileStream fs = fiOutput.Create() )
			{
				await BsPlus.BsPlus.CreateAsync(binSource, binTarget, fs, cToken, this.compLevel, progress);
			}
		}
	}

	// VCDiff
	//------------------------------
	class VCDiffCoder : ICoder
	{
		public DeltaFormat Format { get; set; }

		//--------------------------------------------------------
		// Constructors
		//--------------------------------------------------------
		public VCDiffCoder(DeltaFormat fmt) => this.Format = fmt;

		//--------------------------------------------------------
		// Implements of conventions
		//--------------------------------------------------------
		public async Task DoAsync( byte[] binSource, byte[] binTarget, FileInfo fiOutput, 
		System.Threading.CancellationToken cToken, Progress<float> progress )
		{
			using ( var fsOutput = fiOutput.Create() )
			{
				// Encoder instance
				var encoder = new VCDiff.Encoders.VcEncoder( 
												new VCDiff.Shared.ByteBuffer(binSource),
												new MemoryStream(binTarget),
												fsOutput );

				// Encoding parameters
				VCDiff.Shared.ChecksumFormat checksumFormat = this.Format switch {
									DeltaFormat.VCDiff			=> VCDiff.Shared.ChecksumFormat.None,
									DeltaFormat.VCDiff_Google	=> VCDiff.Shared.ChecksumFormat.SDCH,
									DeltaFormat.VCDiff_XDelta3	=> VCDiff.Shared.ChecksumFormat.Xdelta3,
									_ => throw new ArgumentException ()
								};
				bool interleaved = this.Format == DeltaFormat.VCDiff_Google;

				// Encode asyncronously, returns VCDiff.Includes.VCDiffResult
				var result = await encoder.EncodeAsync(cToken, interleaved, checksumFormat, progress);
				if ( result != VCDiff.Includes.VCDiffResult.SUCCESS ) 
					throw new Processor.ErrorDeltaEncodingFailed($"VCDiff:{result.ToString()}");
			}
		}
	}
}
