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
using System.Reflection;

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
			public Batch(FileInfo from, FileInfo to) => (fsiFrom,fsiTo,Operation) = (from,to,Op.CopyFile);
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
		}

		/// <summary>Validation error</summary>
		/// <remarks>Also a wrapper class of XmlSchemaException.</summary>
		public class ErrorValidationFailed : Error
		{
			public string File { protected set; get; }
			public ErrorValidationFailed(System.Xml.Schema.XmlSchemaException e, string file) : base("An error found in validaing XML document",e)
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
			public ErrorXmlException(XmlException e) : base("An exception occured in parsing XML document",e) {}
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

		//--------------------------------------------------------
		// Properties
		//--------------------------------------------------------

		// File paths
		public FileInfo fiSourceXML	{ private set; get; }
		public FileInfo fiOutput	{ private set; get; }

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

			// Create work directory
			dirWork = dirTmp.CreateSubdirectory(Path.GetFileNameWithoutExtension(fiOutput.Name));

			CreateTemporaryDirectory();
			LoadXML();

			// Maximum value of progress range
			return currentProgress.Max;
		}

		// Create temprary directory that has unique path name
		void CreateTemporaryDirectory()
		{
			if ( dirTmp != null ) DeleteTemporaryDirectory();

			string path = Path.GetTempFileName();
			File.Delete(path);		// GetTempFileName() creates temporary file

			// Create directory info and actual directory of unique temporary name.
			dirTmp = new DirectoryInfo(path);
			dirTmp.Create();
		}

		// Delete temporary directory and all its contents
		void DeleteTemporaryDirectory()
		{
			if ( dirTmp == null ) return;
			if ( dirTmp.Exists ) dirTmp.Delete(true);	// Delete directory with all its contents
			dirTmp = null;
		}

		// Loading source XML
		void LoadXML()
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
						var doc = XDocument.Load( XmlReader.Create(streamXML, settings) );
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
