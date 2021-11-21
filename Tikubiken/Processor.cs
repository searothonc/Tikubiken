/* ********************************************************************** *\
 * Tikubiken binary patch updater ver 1.0.0
 * Processor class for diff
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tikubiken
{
	public sealed class Processor : IDisposable
	{
		//============================================================
		// Inner class: ProgressState
		//============================================================

		/// <summary>
		/// Value container used in async message of Progress<T>
		/// adaptable for both text logging and Progress control
		/// </summary>
		public class ProgressState
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
			private int		_value;
			private string	_text;

			// Propeerties
			//------------------------------
			public int		Min		{ get; set; }
			public int		Max		{ get; set; }
			public int		Value	{ get; set; }
			public string	Text	{ get; set; }
			public Op		Usage	{ get; set; }

			// Initalyzation & Cleaning up
			//------------------------------
			/// <summary>Constructor</summary>
			public ProgressState()
			{
				Min		= 0;
				Max		= 100;
				Value	= 0;
				Text	= "";
				Usage	= Op.None;
			}

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
		// Body of Processor class
		//============================================================

		//--------------------------------------------------------
		// Fields
		//--------------------------------------------------------

		// Progress<T>
		private IProgress<ProgressState>	m_progress;

		// CancellationTokenSource for asyncronous processing
		private CancellationTokenSource		ctSource;

		//--------------------------------------------------------
		// Properties
		//--------------------------------------------------------

		// Progress state
		public ProgressState CurrentProgress { private set; get; }

		// File paths
		public string SourceXML		{ private set; get; }
		public string OutputFile	{ private set; get; }

		//--------------------------------------------------------
		// Constructors
		//--------------------------------------------------------
		///	<summary>Constructor</summary>
		public Processor()
		{
			m_progress = null;

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
			CurrentProgress = new ProgressState();
		}

		///	<summary>Dispose objects</summary>
		public void Dispose()
		{
			// Progress<T> and state container
			m_progress = null;
			CurrentProgress = null;
			

			// Dispose cancellation token source
			if ( ctSource != null ) ctSource.Dispose();
			ctSource = null;

			// for this class itself
			// delete all files under temporary directory
		}

		//--------------------------------------------------------
		// Async task: public
		//--------------------------------------------------------

		public void Ready()
		{
		}

		///	<summary>
		///	Entry point of async task
		///	</summary>
		// 	<param name="handler">(Action{ProgressState})
		///	Callback method to operate progress by retrieving 
		//	newest state via ProgressState object as parametor.
		/// </param>
		///	<remark>
		///	Usually caller need dispose using block to dipose Processor such as:
		///		// Triggering UI disable codes here
		///		using ( var processor = new Processor() )
		///		{
		///			await processor.RunAsync(handler);
		///		}
		///		// Triggering UI enable codes here
		///	</remark>
		public async Task RunAsync(Action<ProgressState> handler)
		{
			// Initialyze Progress<T> and state container
			InitProgressState(handler);

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
			//		ctSource.Token.ThrowIfCancellationRequested();
			//	</code>
			// If UI thread has posted cancel request,
			// OperationCanceledException will be thrown there,
			// and immediately get out to the catch clause 
			// in the caller method this.RunAsync().

			Test1();
		}

		private void Report(string text)
		{
			CurrentProgress.Text	= text;
			CurrentProgress.Usage	= ProgressState.Op.Log;
			PostReport();
		}

		private void Report(int value)
		{
			CurrentProgress.Value	= value;
			CurrentProgress.Usage	= ProgressState.Op.Progress;
			PostReport();
		}

		private void Report(string text, int value)
		{
			CurrentProgress.Text	= text;
			CurrentProgress.Value	= value;
			CurrentProgress.Usage	= ProgressState.Op.Both;
			PostReport();
		}

		private void PostReport()
		{
			m_progress.Report(CurrentProgress);
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
		}

		private void Test2()
		{
			// Count one second
			for ( int i=0 ; i<10 ; ++i )
			{
				Thread.Sleep(100);		// 0.1 seconds
				// check cancellation every 0.1 seconds
				ctSource.Token.ThrowIfCancellationRequested();
				Report( CurrentProgress.Value + 1 );	// progress 1 tick
			}
		}
	}
}
