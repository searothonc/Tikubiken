/* ********************************************************************** *\
 * Tikubiken binary patch updater ver 1.0.0
 * Processor class for diff
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Tikubiken
{
    public sealed class Processor : IDisposable
	{
		//--------------------------------------------------------
		// Constants
		//--------------------------------------------------------

		// Block size of file operation(at least >= TPHeader.AlignmentTBP(=16)
		private const int c_blockSize		= 65536;		// 64KB

		// Resource names
		//------------------------------

		// Resouce ID for diff format DTD
		private const string c_ridDTDPatch			= @"Tikubiken.Resources.tikubiken_patch100.dtd";

		//--------------------------------------------------------
		// Fields
		//--------------------------------------------------------

		// URI for DTD
		private readonly Uri uriDTD		= new Uri( @"http://raw.githubusercontent.com/searothonc/Tikubiken/master/dtd/tikubiken_patch100.dtd");
		private readonly Uri uriDTD_s	= new Uri(@"https://raw.githubusercontent.com/searothonc/Tikubiken/master/dtd/tikubiken_patch100.dtd");

		// CancellationTokenSource for asyncronous processing
		private CancellationTokenSource		ctSource;

		// Progress<T>
		private IProgress<ProgressState>	m_progress;

		// Progress state
		private ProgressState				currentProgress;

		// Working objects
		private XDocument					xmlDoc;
		private List<Batch>					listBatch;

		//--------------------------------------------------------
		// Properties
		//--------------------------------------------------------

		// XElement connected to target directory
		public XElement IdentityElement		{ private set; get; }

		// XElement connected to target directory
		public XElement PatchElement		{ private set; get; }

		// File paths
		//------------------------------

		// Directory where unpacked patch data stored
		public DirectoryInfo PatchDir		{ private set; get; }

		// Source directory to update
		public DirectoryInfo SourceDir		{ private set; get; }

		// Result values
		//------------------------------
		public bool IsNewestFound			{ private set; get; }

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
			// Initialise properties
			IdentityElement	= null;
			PatchElement	= null;
			PatchDir		= null;
			SourceDir		= null;
			IsNewestFound	= false;

			// Initialyze Progress<T> and state container
			InitProgressState(handler);

			// Create cancellation token source
			ctSource = new CancellationTokenSource();

			// Operation(file) list
			listBatch = new List<Batch>();

			// Load XML with DTD validation, user errors unchecked yet
			LoadXML();
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
			currentProgress.Count	= 0;
		}

		///	<summary>Dispose objects</summary>
		public void Dispose()
		{
			// Progress<T> and state container
			m_progress = null;

			// Dispose cancellation token source
			ctSource?.Dispose();
			ctSource = null;
		}

		//--------------------------------------------------------
		// Diagnosis
		//--------------------------------------------------------

		// Batch list as text
		public string ReportBatch()
		{
			string nowtime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
			string strReport = $"[Recorded: {nowtime}]" + System.Environment.NewLine;

			foreach ( var b in listBatch )
			{
				// Operation
				strReport += b.Operation.ToString();

				// Size
				strReport += "\t";
				strReport += b.SizeKB.ToString("#,0");

				strReport += "\t";
				strReport += b.RelativePath;

				strReport += System.Environment.NewLine;
			}

			return strReport;
		}

		//--------------------------------------------------------
		// [Initialise] Load XML
		//--------------------------------------------------------

		// Loading source XML
		private void LoadXML()
		{
			// Assembly currently running
			var asm = Assembly.GetExecutingAssembly();

			// Source XML file
			FileInfo fiSourceXML = Unpacked.GetInstance().PatchXML;

			// Settings for XML Reader
			var settings = new XmlReaderSettings();
			settings.DtdProcessing					= DtdProcessing.Parse;
			settings.IgnoreComments					= true;
			settings.IgnoreProcessingInstructions	= true;
			settings.IgnoreWhitespace				= true;
			//settings.LineNumberOffset				= 1;
			//settings.LinePositionOffset				= 1;
			settings.ValidationType					= ValidationType.DTD;

			string docDTD;
			using ( var stream = asm.GetManifestResourceStream(c_ridDTDPatch) )
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
		// XML utils
		//--------------------------------------------------------

		// Select the element matches to system locale language by "lang" attribute
		private XElement LanguageMatchElement(IEnumerable<XElement> elms)
		{
			string cName = System.Globalization.CultureInfo.CurrentCulture.Name.ToUpper();
			XElement match = (	from e in elms
								where e.Attribute("lang")?.Value is string s ? cName.StartsWith(s.ToUpper()) : false
								// ^above^
								// When e.Attribute("lang") is null, left side of '?' operator will result as false
								// so the expression will be evaluated as false (at left of ':' operator)
								// otherwise e.Attribute("lang").Value is always string type, 
								// so 's' will reffer e.Attribute("lang") as string, as a result of 'is' operator,
								// and finally the expression will be evaluated by String.StartsWith(...)
								select e
							).FirstOrDefault();		// null if not found
			return match ?? ( from e in elms where e.Attribute("lang")==null select e ).FirstOrDefault();
		}

		// Element content as singie line text
		public string ElementTextInSingleLine( XElement elm )
			=> elm==null ? null : string.Join( null, 
							from e in elm.Nodes()
							select e is XText t ? t.Value.Trim() : " "
						);

		// Element content as multi line text with <br/> element for line break
		public string ElementTextInMultiLine( XElement elm )
			=> elm==null ? null : string.Join( null, 
							from e in elm.Nodes()
							select e is XText t ? t.Value.Trim() : System.Environment.NewLine
						);

		// Short form of same name elements under specified element
		private IEnumerable<XElement> EnumerateChildren(string parentElmName, string nameFilter = null)
			=> xmlDoc.Descendants(parentElmName).First().Elements(nameFilter);

		//--------------------------------------------------------
		// <updater>
		//--------------------------------------------------------

		// Localised cover image, or null
		public System.Drawing.Image GetCoverImage()
		{
			var elm = LanguageMatchElement(EnumerateChildren("updater", "cover"));
			if (elm == null) return null;
			return Unpacked.GetInstance().LoadCoverImage(elm.Attribute("image").Value);
		}

		// Localised title bar text, or null
		public string GetTitleCaption()
			=> LanguageMatchElement(EnumerateChildren("updater", "title"))?.Attribute("text")?.Value;

		// Localised UI label, or null
		public string GetLocalisedUI( string uiType )
			=> ElementTextInSingleLine( LanguageMatchElement(
					from e in EnumerateChildren("updater", "localise")
					where e.Attribute("target")?.Value == uiType
					select e
				) );

		//--------------------------------------------------------
		// <text>
		//--------------------------------------------------------

		// Get message text by type name
		public string GetMessageTextByTypeName(string typeName)
			=>	ElementTextInMultiLine(  LanguageMatchElement(
						from e in IdentityElement.Descendants("text")
						where e.Attribute("type")?.Value==typeName
						select e
				) );

		//--------------------------------------------------------
		// <patch>
		//--------------------------------------------------------

		// <patch> element associated to current <identity>
		private XElement GetPatchElement()
		{
			if ( this.PatchElement != null ) return PatchElement;

			XElement elm = IdentityElement.Element("patchref");
			if ( elm != null )
			{
				// if <patchref> exists, solve reffernce
				var attr = elm.Attribute("version");
				elm = (	from e in xmlDoc.Descendants("patch")
						where e.Attribute("version").Value == attr.Value
						select e).First();
			}
			else
			{
				elm = IdentityElement.Element("patch");
			}

			this.PatchElement = elm;

			return elm;
		}

		// Get balance from <patch> element
		public long GetPatchBalance()
			=> long.Parse(GetPatchElement().Attribute("balance").Value);

		//--------------------------------------------------------
		// Find update source
		//--------------------------------------------------------

		/// <summary>Search for the directory where the patch will be applied to.</summary>
		public async Task DetermineLocation()
		{
			this.IsNewestFound = false;
			foreach ( var elmInstall in xmlDoc.Descendants("install") )
			{
				SourceDir = null;
				XElement elm = elmInstall.Element("registry");
				if ( elm != null ) SourceDir = GetInstallDirectory(elm);
#if	DEBUG
				string projectRoot = @"..\..\..\..";
				SourceDir = new DirectoryInfo(Ext.JoinPath(projectRoot, @"test_data\min_branch") );
#endif
				SourceDir ??= Unpacked.GetInstance().ExeDir;

				// Verify identity
				elm = IdentityElement = elmInstall.Element("identity");
				FileInfo fiSource = new FileInfo(Ext.JoinPath(SourceDir.FullName, elm.Attribute("path").Value));

				if ( !fiSource.Exists ) continue;
				string digest = await Ext.CaclDigestAsync(fiSource, elm.Attribute("method").Value);
				if ( digest == elm.Attribute("destination").Value ) this.IsNewestFound = true;
				if ( digest != elm.Attribute("source").Value ) continue;
				ReportComplete(ProgressState.Phase.Prestart, true);		// Post completion as success
				return;						// and exit thread
			}
			ReportComplete(ProgressState.Phase.Prestart, false);		// Post completion as failed
		}

		// Get installed directory from registry
		DirectoryInfo GetInstallDirectory( XElement elmRegistry )
		{
			string str = elmRegistry.Attribute("root").Value;
			var rootKey = str switch
					{
						"HKCR"					=> Microsoft.Win32.Registry.ClassesRoot,
						"HKCU"					=> Microsoft.Win32.Registry.CurrentUser,
						"HKLM"					=> Microsoft.Win32.Registry.LocalMachine,
						"HKCC"					=> Microsoft.Win32.Registry.CurrentConfig,
						"HKU"					=> Microsoft.Win32.Registry.Users,
						"HKEY_CLASSES_ROOT"		=> Microsoft.Win32.Registry.ClassesRoot,
						"HKEY_CURRENT_USER"		=> Microsoft.Win32.Registry.CurrentUser,
						"HKEY_LOCAL_MACHINE"	=> Microsoft.Win32.Registry.LocalMachine,
						"HKEY_CURRENT_CONFIG"	=> Microsoft.Win32.Registry.CurrentConfig,
						"HKEY_USERS"			=> Microsoft.Win32.Registry.Users,
						_	=>	throw new URTException($"No such registry key:{str}")
					};
			str = elmRegistry.Attribute("subkey").Value;
			using (var regKey = rootKey.OpenSubKey(str))
			{
				if ( regKey == null ) return null;	// Key does not exist
				str = elmRegistry.Attribute("value").Value;
				str = regKey.GetValue(str) as string;	// null if value type is not match as string
			}

			if ( str == null ) return null;

			return new DirectoryInfo(str);
		}

		//--------------------------------------------------------
		// Inter-thread communications
		//--------------------------------------------------------

		/// <summary>/// Cancel running task./// </summary>
		public void Cancel() => ctSource?.Cancel();

		// Throw exception when cancel
		private void ThrowIfCancellationRequested() => ctSource?.Token.ThrowIfCancellationRequested();

		// Post progress to UI
		private void PostReport()
		{
			if ( m_progress == null ) return;
			m_progress.Report(currentProgress);
		}

		// Send task complete message
		private void ReportComplete(ProgressState.Phase ph, bool b_success)
		{
			currentProgress.Usage = b_success ? ProgressState.Op.Sucess : ProgressState.Op.Failed;
			currentProgress.Size = (int) ph;
			PostReport();
		}

		// Set anchor and size for report delta by rate
		private void SetRportRateAnchor(int sizeKB)
		{
			// Set current progress to anchor
			currentProgress.Anchor = currentProgress.Value;
			currentProgress.Size   = sizeKB;
		}

		// Report progress as a percentage in the file
		private void ReportDeltaRate(float rate)
		{
			int newValue = currentProgress.Anchor + (int)( rate * (float) currentProgress.Size );
			if ( currentProgress.Value > newValue ) return;
			currentProgress.Value = newValue;
			currentProgress.Usage	= ProgressState.Op.Progress;
			PostReport();
		}

		private void ReportDeltaRateFull(bool count_up=false)
		{
			currentProgress.Value = currentProgress.Anchor + currentProgress.Size;
			currentProgress.Usage = ProgressState.Op.Progress;
			if ( count_up )
			{
				currentProgress.Usage |= ProgressState.Op.Count;
				currentProgress.Count++;
			}
			PostReport();
		}

		private void ReportCountUp()
		{
			currentProgress.Usage = ProgressState.Op.Count;
			currentProgress.Count++;
			PostReport();
		}

		//--------------------------------------------------------
		// Sync task has to be done bofore async
		//--------------------------------------------------------

		/// <summary>Operations having to be done before async task</summary>
		///	<param name="dirBase">(DirectoryInfo)Directory where all the patch bundle unpacked into.</param>
		///	<returns>(int count, int size)Count of operations and their total file size(in KB).</returns>
		public (int count, int size) Ready(DirectoryInfo dirBase)
		{
			// Patch directory
			var elmPatch = GetPatchElement();
			this.PatchDir = new DirectoryInfo( Ext.JoinPath(dirBase.FullName, elmPatch.Attribute("version").Value) );

			// Check files and register operatons recursively
			AggregateFiles();

			// Add remove to task list from <patch> element
			foreach ( var elm in elmPatch.Elements("remove") ) listBatch.Add( new Batch(elm) );

			// Sum all file size
			var sizeList = from x in listBatch select x;
			int count = sizeList.Count();
			int size  = sizeList.Sum( x => x.SizeKB );
			// Patch operations advance progress twice
			count += sizeList.Count( x => x.IsPatch() );
			size  += sizeList.Sum( x => x.IsPatch() ? x.SizeKB : 0 );

			// Initialyze progress state
			currentProgress.Usage = ProgressState.Op.None;
			currentProgress.Min		= 0;
			currentProgress.Max		= size;
			currentProgress.Value	= 0;
			currentProgress.Text	= "";
			currentProgress.Anchor	= 0;
			currentProgress.Size	= 0;
			currentProgress.Count	= 0;

			return (count, size);
		}

		// Check files and register operatons recursively
		public void AggregateFiles(string relativeDir="")
		{
			DirectoryInfo dir   = new DirectoryInfo(Ext.JoinPath(PatchDir.FullName,  relativeDir));

			relativeDir += String.IsNullOrEmpty(relativeDir) ? "" : @"\";

			// Recurse into sub-directories
			foreach ( var d in dir.EnumerateDirectories() ) AggregateFiles( relativeDir + d.Name );

			// Files in this directory
			foreach ( var f in dir.EnumerateFiles() )
			{
				int size = (int) ((f.Length + 1023L) / 1024L);	// size in KB
				DeltaFormat fmt = GetDeltaFormatFromFile(f);
				if ( fmt != DeltaFormat.None )
				{
					FileInfo fiSource = new FileInfo(Ext.JoinPath(SourceDir.FullName,relativeDir,Path.GetFileNameWithoutExtension(f.Name)));
					size += (int) ((fiSource.Length + 1023L) / 1024L);	// in KB, again
				}
				listBatch.Add( new Batch(relativeDir + f.Name, fmt, size) );
			}
		}

		// File format from extension and header
		private DeltaFormat GetDeltaFormatFromFile(FileInfo file)
		{
			DeltaFormat format = DeltaFormat.None;

			// Extension is not match
			if ( Path.GetExtension(file.Name).ToLower() != Ext.DeltaExt ) return format;

			// Load file header
			byte[] buf = new byte[System.Runtime.InteropServices.Marshal.SizeOf(BsPlus.BsPlus.c_fileSignature)];
			using (var fs = file.OpenRead()) fs.Read(buf,0,buf.Length);

			// Signature in number
			Int32 signatureInt  = BitConverter.ToInt32(buf,0);
			Int64 signatureLong = BitConverter.ToInt64(buf,0);

			if ( signatureInt == 0x00C4C3D6 ) format = DeltaFormat.Encoding_VCDiff;		// any of VCDiff
			if ( signatureInt == 0x53C4C3D6 ) format = DeltaFormat.Encoding_VCDiff;
			if ( signatureLong == BsPlus.BsPlus.c_fileSignature ) format = DeltaFormat.Encoding_BsPlus;	// any of BsPlus

			return format;
		}
		//--------------------------------------------------------
		// Async task: apply patches to files
		//--------------------------------------------------------

		///	<summary>Apply patches to files</summary>
		public async Task RunAsync()
		{
			try
			{
				// Applying patches in unzipped directory
				foreach ( var b in listBatch )
					if ( b.IsPatch() ) 
					{
						await ApplyPatchAsync(b);
						ThrowIfCancellationRequested();
					}
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

		//--------------------------------------------------------
		// Async task: patch apply for a file
		//--------------------------------------------------------
		private async Task ApplyPatchAsync(Batch op)
		{
			// Set current progress to anchor
			SetRportRateAnchor(op.SizeKB);		// first half of progression in this file

			// Files
			var fiSource = op.FileAtRelativePath(SourceDir, true);
			var fiDelta  = op.FileAtRelativePath(PatchDir);
			var fiOutput = op.FileAtRelativePath(PatchDir, true);

			// Decode
			ICoder coder = op.Operation switch
							{
								Batch.Op.VCDiff	=> new VCPatchCoder(),
								Batch.Op.BsPlus	=> new BsPatchCoder(),
								_ => throw new CoderException("Invalid delta format")
							};
			await coder.DoAsync(fiSource, fiDelta, fiOutput, ctSource.Token, new Progress<float>(ReportDeltaRate) );

			// Delete delta file after the updated file restored
			fiDelta.Delete();

			ReportDeltaRateFull(true);	// progress bar advances at full file size and count increases
		}

		//--------------------------------------------------------
		// Async task: overwrite result
		//--------------------------------------------------------

		public async Task WriteResultAsync()
		{
			try
			{
				// Remove directories and files no longer exists on new version
				foreach (var b in listBatch) if ( b.IsRemove() ) RemoveFile(b);

				// Copy result files to target
				foreach (var b in listBatch) 
				{
					if ( !b.IsCopyOrPatch() ) continue;

					// Set current progress to anchor, last half of progression in the case of patch
					SetRportRateAnchor(b.SizeKB);

					// Copy asynchronously
					await CopyFilesAsync(b);

					// Advance progress and count, then accept cancel
					ThrowIfCancellationRequested();
				}

				// Set progress bar to maximum
				currentProgress.Value = currentProgress.Max;
				currentProgress.Usage = ProgressState.Op.Progress;
				PostReport();
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

		// Remove file synchronously
		private void RemoveFile(Batch op)
		{
			// Set current progress to anchor
			SetRportRateAnchor(0);

			// path to remove
			string path = op.MakeRelativePath(SourceDir);

			FileInfo fi = new FileInfo(path);
			DirectoryInfo di = new DirectoryInfo(path);

			// If the path points to file, FileInfo will be valid
			if ( fi.Exists )
			{
				// Delete file
				fi.Delete();
			}
			// If the path points to directory, DirectoryInfo will be valid
			else if ( di.Exists ) 
			{
				// delete with all directory contents
				di.Delete(true);
			}
			else
			{
				// otherwise, it's an error
				throw new FileNotFoundException("File not found", path);
			}

			// Advance progress and count, then accept cancel
			ReportCountUp();
			ThrowIfCancellationRequested();
		}

		// Copy file asynchronously
		private async Task CopyFilesAsync(Batch op)
		{
			// Files
			var fiDst = op.FileAtRelativePath(SourceDir, op.IsPatch());
			var fiSrc = op.FileAtRelativePath(PatchDir,  op.IsPatch());

			// Buffer
			int len = 0;
			byte[] buf = new byte[c_blockSize];

			// Create directory and sub-directory needed, if they don't exist
			Directory.CreateDirectory(fiDst.DirectoryName);

			// File copy asynchronously
			using (var fsRead  = fiSrc.Open(FileMode.Open,   FileAccess.Read,  FileShare.Read))
			using (var fsWrite = fiDst.Open(FileMode.Create, FileAccess.Write, FileShare.None))
			{
				while ( fsRead.Position < fsRead.Length )
				{
					len = await fsRead.ReadAsync(buf, 0, c_blockSize, ctSource.Token);
					await fsWrite.WriteAsync(buf, 0, len, ctSource.Token);
					ReportDeltaRate( (float)len / (float)fiSrc.Length );
					ThrowIfCancellationRequested();
				}
			}

			// progress bar advances at full file size and count increases
			ReportDeltaRateFull(true);
		}

	}	//** public sealed class Processor **********************************************************

	//============================================================
	// Container of operation
	//============================================================
	class Batch
	{
		public enum Op
		{
			None,
			VCDiff,
			BsPlus,
			Copy,
			Remove,
		};

		// Properties
		//------------------------------
		public Op					Operation		{ get; protected set; }
		public string				RelativePath	{ get; protected set; }
		public int					SizeKB			{ get; protected set; }

		// Constructors
		//------------------------------

		// Copy file or apply patch from file path and size
		public Batch(string relativeFile, DeltaFormat format, int size)
		{
			format &= DeltaFormat.Encoding_Mask;
			this.Operation = format switch 
					{
						DeltaFormat.None			=> Op.Copy,
						DeltaFormat.Encoding_VCDiff	=> Op.VCDiff,
						DeltaFormat.Encoding_BsPlus	=> Op.BsPlus,
						_ => throw new ArgumentException("Invalid delta format")
					};
			this.RelativePath = relativeFile;
			this.SizeKB = size;
		}

		// Remove file from XElement
		public Batch( XElement elmRemove )
		{
			this.Operation		= Op.Remove;
			this.RelativePath	= elmRemove.Attribute("path").Value;
			this.SizeKB			= 0;
		}

		// Methods
		//------------------------------

		// Whether the operation is patch applying
		public bool IsPatch()	=>	this.Operation==Op.VCDiff || this.Operation==Op.BsPlus;

		// Whether the operation is copy at last
		public bool IsCopyOrPatch()	=>	new[]{Op.VCDiff, Op.BsPlus, Op.Copy}.Contains(this.Operation);

		// Whether the operation is patch applying
		public bool IsRemove()	=>	this.Operation==Op.Remove;

		/// <summary>Make a path relative to specified directory</summary>
		/// <param name="dir">(DirectoryInfo)Base directory where relative path will be connected to</param>
		/// <param name="trim">(bool)Whether to trim extension or not</param>
		public string MakeRelativePath(DirectoryInfo dir, bool trim=false)
		{
			string str = Ext.JoinPath(dir.FullName, this.RelativePath);
			if ( trim )
				str = Ext.JoinPath( Path.GetDirectoryName(str), Path.GetFileNameWithoutExtension(str) );
			return str;
		}

		/// <summary>Same as MakeRelativePath() but returns FileInfo instance</summary>
		/// <param name="dir">(DirectoryInfo)Base directory where relative path will be connected to</param>
		/// <param name="trim">(bool)Whether to trim extension or not</param>
		public FileInfo FileAtRelativePath(DirectoryInfo dir, bool trim=false)
			=> new FileInfo(MakeRelativePath(dir, trim));
	}


	//============================================================
	// Interface: ICoder
	//============================================================

	public interface ICoder
	{
		public Task DoAsync( FileInfo fiSource, 		// old file for both enc and dec
							 FileInfo fiDelta, 			// delta file for decpding
							 FileInfo fiOutput, 		// rebuilt file by decoding
							 CancellationToken cToken,	// Cancellation token
							 Progress<float> progress	// where progress will be reported to as a value of 0.0f - 1.0f
						);
	}


	// BsDiff
	//------------------------------
	class BsPatchCoder : ICoder
	{
		//--------------------------------------------------------
		// Constructors
		//--------------------------------------------------------
		public BsPatchCoder() {}

		//--------------------------------------------------------
		// Implements of conventions
		//--------------------------------------------------------
		public async Task DoAsync( FileInfo fiSource, FileInfo fiDelta, 	FileInfo fiOutput, 
		CancellationToken cToken, Progress<float> progress )
		{
			using ( var fsSource = fiSource.OpenRead() )
			using ( var fsOutput = fiOutput.Create() )
			{
				await BsPlus.BsPlus.ApplyAsync(fsSource, ()=>fiDelta.OpenRead(), fsOutput, cToken, progress);
			}
		}
	}

	// VCDiff
	//------------------------------
	class VCPatchCoder : ICoder
	{
		//--------------------------------------------------------
		// Constructors
		//--------------------------------------------------------
		public VCPatchCoder() {}

		//--------------------------------------------------------
		// Implements of conventions
		//--------------------------------------------------------
		public async Task DoAsync( FileInfo fiSource, FileInfo fiDelta, 	FileInfo fiOutput, 
		CancellationToken cToken, Progress<float> progress )
		{
			using ( var fsSource = fiSource.OpenRead() )
			using ( var fsDelta  = fiDelta.OpenRead()  )
			using ( var fsOutput = fiOutput.Create()   )
			{
				// Decoder instance
				var decoder = new VCDiff.Decoders.VcDecoder(fsSource, fsDelta, fsOutput);

				// Encode asyncronously, returns VCDiff.Includes.VCDiffResult
				(var result, long bytesWritten) = await decoder.DecodeAsync(cToken, progress);
				if ( result != VCDiff.Includes.VCDiffResult.SUCCESS ) 
					throw new CoderException(result.ToString());
			}
		}
	}
}
