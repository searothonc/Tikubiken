/* ********************************************************************** *\
 * BsPlus: bsdiff/bspatch library with asynchronous support
 * Common in namespace
 * Copyright (c) 2021 Searothonc
\* ********************************************************************** */
/*
(Project overview)
BsPlus is a binary delta encoding library, based on bsdiff.net 
(https://github.com/LogosBible/bsdiff.net) by Logos Bible Software
which is a .NET port of the original bsdiff c-code by Colin Percival, 
and improved for our own purposes.

Major changes are as follows:

  * Replaced compression algorism from bzip2 to brotli which has
    faster decompression speed and does not require extra dll 
    such as ICSharpCode.SharpZipLib.dll because of being 
    contained within .NET as a part of the standard implement.

  * Added support of the asynchronous mechanism. And also added 
    async version methods that wraps the original method to 
    start a task in a worker thread.

  * Splitted the patch-creating code and the patch-applying code 
    into separate source files to make possible to merge indivisually 
    at the source code level.

(Licenses)
BsPlus itself is distributed under the license known as 
'2-clause BSD License'.

bsdiff.net(https://github.com/LogosBible/bsdiff.net) by Logos Bible 
Software is distributed under the license known as 'The MIT License'.

The original bsdiff.c source code (http://www.daemonology.net/bsdiff/)
by Colin Percival is distributed under the 2-clause BSD License too.

The full terms of the license for each work are as follows:

** BsPlus **************************************************************

Coyright (c) 2021 Searothonc

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions 
are met:

1.  Redistributions of source code must retain the above copyright 
    notice, this list of conditions and the following disclaimer.

2.  Redistributions in binary form must reproduce the above copyright 
    notice, this list of conditions and the following disclaimer in the 
    documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR 
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

** bsdiff.net **********************************************************
The original C# port version bsdiff.net source code 
(https://github.com/LogosBible/bsdiff.net) is distributed under 
the following license:

Copyright 2010 Logos Bible Software

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

** bsdiff.c ************************************************************
The original bsdiff.c source code (http://www.daemonology.net/bsdiff/) is
distributed under the following license:

Copyright 2003-2005 Colin Percival
All rights reserved

Redistribution and use in source and binary forms, with or without
modification, are permitted providing that the following conditions 
are met:
1. Redistributions of source code must retain the above copyright
	notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
	notice, this list of conditions and the following disclaimer in the
	documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Threading;

namespace BsPlus
{
	public partial class BsPlus
	{
		//--------------------------------------------------------
		// Constants
		//--------------------------------------------------------
		//const long c_fileSignature = 0x3034464649445342L;		// "BSDIFF40"
		const long c_fileSignature = 0x30312B5342424B54L;		// "TKBBS+10"
		const int c_headerSize = 32;

		// Expose protected const values as static readonly
		public static readonly long FileSignature		= c_fileSignature;
		public static readonly int HeaderSize			= c_headerSize;

		//--------------------------------------------------------
		// Common methods
		//--------------------------------------------------------
		//private static void Advance( CancellationToken? cToken, IProgress<float> Progress, int pos, long max )
		//	=> Advance(cToken, Progress, (float)pos / (float)max);
/*
		private static void Advance( CancellationToken? cToken, IProgress<float> Progress, float position )
		{
			if ( Progress != null ) Progress.Report( position );
			AcceptCancel(cToken);
		}

		private static void AcceptCancel(CancellationToken? cToken) => cToken?.ThrowIfCancellationRequested();
*/
	}
}
