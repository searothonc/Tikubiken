/* ********************************************************************** *\
 * Tikubiken binary patch updater ver 1.0.0
 * Processor class for diff
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Tikubiken
{
	public sealed class Processor : IDisposable
	{
		//--------------------------------------------------------
		// Constants
		//--------------------------------------------------------

		// line break sugar
		private static string lineBreak => System.Environment.NewLine;

		// Resource names
		private const string ridDTD = "Tikubiken.Resources.tikubiken_diff100.dtd";

		//============================================================
		// Inner class: ProgressState
		//============================================================

		/// <summary>
		/// Value container used in async message of Progress<T>
		/// adaptable for both text logging and Progress control
		/// </summary>
		public struct ProgressState
		{
			public enum Op
			{
				None		=	0,
				Progress	=	0x01,
				Log			=	0x02,
				Both		=	Progress|Log
			}

			// Fields
			//------------------------------
			//private int		_value;
			//private string	_text;

			// Propeerties
			//------------------------------
			public Op		Usage;
			public int		Min;
			public int		Max;
			public int		Value;
			public string	Text;

			// Value availability
			//------------------------------
			/// <summary>
			/// Check if the value is available.
			/// </summary>
			public bool IsValueAvailable() => (Usage & Op.Progress) != 0;

			/// <summary>
			/// Check if the text is available.
			/// </summary>
			public bool IsTextAvailable() => (Usage & Op.Log) != 0;
		}

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
			}

			// Fields
			//------------------------------
			private FileSystemInfo	fsiFrom;
			private FileSystemInfo	fsiTo;

			// Properties
			//------------------------------
			public Op				Operation	{ get; protected set; }
			public FileInfo			FileFrom	{ get => fsiFrom as FileInfo;		set => fsiFrom = value; }
			public FileInfo			FileTo		{ get => fsiTo   as FileInfo;		set => fsiTo   = value; }
			public DirectoryInfo	DirFrom		{ get => fsiFrom as DirectoryInfo;	set => fsiFrom = value; }
			public DirectoryInfo	DirTo		{ get => fsiTo   as DirectoryInfo;	set => fsiTo   = value; }

			// Constructtors
			//------------------------------

			// Operation = create a directory
			public Batch(DirectoryInfo dirCreate) => (fsiFrom,fsiTo,Operation) = (null,dirCreate,Op.CreateDir);

			// Operation = copy a file
			public Batch(FileInfo fileFrom, FileInfo fileTo) => (fsiFrom,fsiTo,Operation) = (fileFrom,fileTo,Op.CopyFile);
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
			public static string TypeValidation		= "An error found in validaing XML document";
			public static string TypeException		= "An exception occured in parsing XML document";
			public static string TypeAnalysis		= "An error found in analyzing source XML";

			public static string Msg_DupLangNull	= "Duplication of the element with no \x22lang\x22 attribute is not allowed.";
			public static string Msg_DupLangValue	= "Duplication of the element having the same value of the \x22lang\x22 attribute is not allowed.";
			public static string Msg_DupNoRegistry	= "<install> element witout <registry> element is able to exist only 1 simaltaneously.";
			public static string Msg_NoReferenceID	= "Referenced IDs do not exist.";

			public static string Msg_FileNotExists	= "File not exists.";
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


		//============================================================
		// Body of Processor class
		//============================================================

		//--------------------------------------------------------
		// Fields
		//--------------------------------------------------------

		// URI for DTD
		private readonly Uri uriDTD = new Uri("http://raw.githubusercontent.com/searothonc/Tikubiken/master/dtd/tikubiken_diff100.dtd");

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
		public DirectoryInfo diSourceXML	{ get => fiSourceXML.Directory; }
		public FileInfo fiOutput			{ private set; get; }

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
			ctSource.Dispose();
			ctSource = null;

			// for this class itself
			DeleteTemporaryDirectory();
		}

		//--------------------------------------------------------
		// Sync task done bofore async
		//--------------------------------------------------------

		/// <summary>Operations having to be done before async task</summary>
		///	<returns>(int)Maximum value of progress range.</returns>
		public int Ready( string sourceXML, string outputPath )
		{
			// FileInfo for input and output file
			fiSourceXML = new FileInfo(sourceXML);
			fiOutput = new FileInfo(outputPath);

			// Check if input XML file exists
			if ( !fiSourceXML.Exists ) throw new FileNotFoundException(null,sourceXML);

			// Create temporary work directory
			CreateTemporaryDirectory();
			dirWork = dirTmp.CreateSubdirectory(Path.GetFileNameWithoutExtension(fiOutput.Name));

			// Parse XML document
			LoadXML();
			ErrorCheckXML();
			ParseXML();

			// Maximum value of progress range
			return currentProgress.Max;
		}

		// Create temprary directory that has unique path name
		private void CreateTemporaryDirectory()
		{
			if ( dirTmp != null ) DeleteTemporaryDirectory();

			string path = Path.GetTempFileName();
			File.Delete(path);		// GetTempFileName() creates temporary file

			// Create directory info and actual directory of unique temporary name.
			dirTmp = new DirectoryInfo(path);
			dirTmp.Create();
		}

		// Delete temporary directory and all its contents
		private void DeleteTemporaryDirectory()
		{
			if ( dirTmp == null ) return;
			if ( dirTmp.Exists ) dirTmp.Delete(true);	// Delete directory with all its contents
			dirTmp = null;
		}

		// Loading source XML
		private void LoadXML()
		{
			// Assembly currently running
			var asm = Assembly.GetExecutingAssembly();

			# if DEBUG
			// アセンブリに埋め込まれているすべてのリソースの論理名を取得して表示する
			foreach (var _rname in asm.GetManifestResourceNames()) {
				Debug.WriteLine(_rname);
			}
			#endif

			// Settings for XML Reader
			var settings = new XmlReaderSettings();
			settings.DtdProcessing					= DtdProcessing .Parse;
			settings.IgnoreComments					= true;
			settings.IgnoreProcessingInstructions	= true;
			settings.IgnoreWhitespace				= true;
			//settings.LineNumberOffset				= 1;
			//settings.LinePositionOffset				= 1;
			settings.ValidationType					= ValidationType.DTD;

			using ( var streamXML = fiSourceXML.OpenRead() )
			{
				using ( var streamDTD = asm.GetManifestResourceStream(ridDTD) )
				{
					if ( streamDTD == null ) throw new ArgumentNullException();

					// In order to skip download DTD from URL,
					// create resolver from same file in resource.
					var resolver = new System.Xml.Resolvers.XmlPreloadedResolver();
					resolver.Add(uriDTD, streamDTD);
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
		}

		//--------------------------------------------------------
		// Sync task: XML error check
		//--------------------------------------------------------

		// Error check before parse XML
		private void ErrorCheckXML()
		{
			ErrorCheckXML_DistinctAttribute_lang();
			ErrorCheckXML_CheckDuplicateInstallPlacement();
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
					group e by e.Attribute("type") == null ? "" : e.Attribute("type").Value;
				foreach ( var t in tx )
				{
					string key = t.Key.Length>0 ? $" type=\x22{t.Key}\x22" : "";
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

			string str = "";
			foreach ( string s in causes ) { str += s; }
			throw new ErrorAnalysis( Error.Msg_DupNoRegistry + str );
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
				src = new FileInfo( Path.Join(diSourceXML.FullName, file) );
				if ( !src.Exists ) throw new ErrorAnalysis( Error.Msg_FileNotExists + lineBreak + file );
				dst = new FileInfo( Path.Join(dirWork.FullName, src.Name) );
				listBatch.Add( new Batch(src,dst) );
			}
		}

		// Aggregate <patch> elements
		private void ParseXML_AggregatePatches()
		{
			// Go round all <patch> elements
			foreach ( var elmPatch in xmlDoc.Descendants("patch") )
			{
				// Version ID
				string strID = elmPatch.Attribute("version").Value;
				DirectoryInfo dirResult = new DirectoryInfo($"{dirWork.FullName}\\{strID}");
				listBatch.Add( new Batch(dirResult) );

				// <branch> element
				var elmBranch = elmPatch.Element("branch");
				var strBranchPath = elmBranch.Attribute("path").Value;
				var ignoreBranch = 
					from e in elmBranch.Elements("exclude")
					select ParseXML_PathToRegex(e);

				// <target> element
				var elmTarget = elmPatch.Element("target");
				var strTargetPath = elmBranch.Attribute("path").Value;
				var ignoreTarget = 
					from e in elmTarget.Elements("exclude")
					select ParseXML_PathToRegex(e);

				// Differences of file structure between two directories.
				ParseXML_CompareDirectory(dirResult, ".", strBranchPath, ignoreBranch,strTargetPath, ignoreTarget);
			}
		}

		// Regex from path and match attribute
		private Regex ParseXML_PathToRegex(XElement elm)
		{
			string path = elm.Attribute("path").Value;
			string match = elm.Attribute("match").Value ?? "wildcard";

			// "exact" and "regex" are not or least needed to convert.
			if ( match != "regex" ) path = Regex.Escape(path);	// escape if "exact" or "wildcard"
			if ( match != "wildcard" ) return new Regex(path);	// return if "exact" or "regex"

			// Only "wildcard" needs conversion
			path = path.Replace( @"\?", @"." );		// 1 letter match in wildcard '?' to regex '.'
			path = path.Replace( @"\*", @".*" );	// sequence match in wildcard '*' to regex '.*'
			return new Regex(path);
		}

		// Exclude path
//		private bool ParseXML_MatchExcludePattern(string path, IEnumerable<Regex> exclusions)
//			=> exclusions.Any( r => r.IsMatch(path) )

		// Compare file structures between two directories.
		private void ParseXML_CompareDirectory( DirectoryInfo dirResult, string relativePath, 
									string branchBase, IEnumerable<Regex> branchesExclude, 
									string targetBase, IEnumerable<Regex> targetsExclude )
		{
			DirectoryInfo dirBranch = new DirectoryInfo( Path.GetRelativePath(branchBase, relativePath) );
			DirectoryInfo dirTarget = new DirectoryInfo( Path.GetRelativePath(targetBase, relativePath) );

			// Enumerate in the directory of branch side.
			IEnumerable<DirectoryInfo> dirBranches = 
				from d in dirBranch.EnumerateDirectories()
				where !branchesExclude.Any( r => r.IsMatch(d.Name) )
				select d;

			string s = "";
			foreach ( var d in dirBranches ) { s += d.Name + $"{lineBreak}"; }
			throw new Error(s);
		}


		//--------------------------------------------------------
		// Async task: public
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
		public async Task RunAsync()
		{
			try
			{
				await Task.Run( () => RunBody() );
			}
			catch (OperationCanceledException)
			{
				// do what to do when operation has been cancelled.
				// *CAUTION!* This block does not re-throw any exception ever.
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
		public void Cancel()
		{
			if ( ctSource == null ) return;
			ctSource.Cancel();
		}

		//--------------------------------------------------------
		// Async task: private
		//--------------------------------------------------------

		// Body of async processing
		private void RunBody()
		{
			// To print log, call Report(string text)
			// To progress indicators, call Report(int value)
			// If both UI is needed, call Report(string text, int value)

			// Eveny point where the running operation can be cancelled,
			// the method call to throw OperationCanceledException is needed.
			//	<code>
			//		ThrowIfCancellationRequested();
			//	</code>
			// If UI thread has posted cancel request,
			// OperationCanceledException will be thrown there,
			// and immediately get out to the catch clause 
			// in the caller method this.RunAsync().

			Test1();
		}

		private void ThrowIfCancellationRequested()
		{
			if ( ctSource == null ) return;
			ctSource.Token.ThrowIfCancellationRequested();
		}

		private void Report(string text)
		{
			currentProgress.Text	= text;
			currentProgress.Usage	= ProgressState.Op.Log;
			PostReport();
		}

		private void Report(int value)
		{
			currentProgress.Value	= value;
			currentProgress.Usage	= ProgressState.Op.Progress;
			PostReport();
		}

		private void Report(string text, int value)
		{
			currentProgress.Text	= text;
			currentProgress.Value	= value;
			currentProgress.Usage	= ProgressState.Op.Both;
			PostReport();
		}

		private void PostReport()
		{
			if ( m_progress == null ) return;
			m_progress.Report(currentProgress);
		}

		//--------------------------------------------------------
		// Test work
		//--------------------------------------------------------
		private void Test1()
		{
			// count 20 unit of time(=second)
			for ( int i=0 ; i<20  ; ++i )
			{
				Test2();
				int s = i+1;
				Report( $"{s} seconds" );			// log 1 second past
			}
			Report( currentProgress.Max );			// progress completion
			Thread.Sleep(500);		// 0.5 seconds
		}

		private void Test2()
		{
			// Count one second
			for ( int i=0 ; i<10 ; ++i )
			{
				Thread.Sleep(100);		// 0.1 seconds
				// check cancellation every 0.1 seconds
				ThrowIfCancellationRequested();
				Report( currentProgress.Value + 1 );	// progress 1 tick
			}
		}
	}
}
/*
Linq to XML (XDocument) でエンティティ宣言されたものを使う | La Verda Luno
https://blog.masuqat.net/2014/06/03/linq-to-xml-with-entity-declaration/
XmlReader クラス (System.Xml) | Microsoft Docs
https://docs.microsoft.com/ja-jp/dotnet/api/system.xml.xmlreader?view=net-6.0
XDocument クラス (System.Xml.Linq) | Microsoft Docs
https://docs.microsoft.com/ja-jp/dotnet/api/system.xml.linq.xdocument?view=net-6.0
*/
/*

XML解析
通読
処理手順の再帰的コンテナクラス作成Batchとかでいい、zipでひとつにまとめる対象のワークディレクトリをひとつ作り、その中にファイルを配置する操作すべてを記録、coverとpatch.target/branchが対象、他のタグはデータだけなのでXMLドキュメントを最後に改変する
updater.cover容量計算
patch versionの唯一性確認
patchref versionの参照可能性確認
patch versionごとにフォルダ作成用データ作成→容量計算
install.identityごとに容量計算

Batchのコマンド
ディレクトリ作成してワークノードを移動
ファイルをコピー
ファイルの差分を作成

DirectoryInfo,FileInfoのメンバとbsdiffだけで作業が完了するように分解する
指定の動作を実行するExec()メソッドを持つ

プログレスバーはBatchオブジェクト全体の容量のみ、前処理、後処理は反映しないので、バーなしで作業→Batchでバーがのびる→100%状態で後処理(xml出力)
*/
