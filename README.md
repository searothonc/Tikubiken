## Overview

Tikubiken is a binary delta encoding application that creates 
self-decoding, executable update programs. Tikubiken compares 
the differences throughout entire folders between versions of software 
consisting of a number of files, and outputs an executable file (*.exe)
containing the delta that updates the old version to the new version.


## How to use Tikubiken

### Making an executable file of the update program

 1. Prepare a source XML file. Define the paths to the folders that 
    contain the old version and the new version, as well as 
    the messages to be displayed in this file.
 2. Run Tikubiken.exe, set the source XML file to be loaded, the output 
    file name, etc., and click the [Start] button.
 3. When the process is complete, the update program will be output with
    the specified file name (only if the file extension is ".exe").
 4. Distribute the output executable file to users who need to update 
    the older version to the newer one, with appropriate documentation.

### Applying update patch

Most of the behavior is determined by the definitions in the source XML 
file. See the comments in sample_(lang).xml for details.

Assuming that Update.exe is the executable file output by Tikubiken.exe 
as described above. When a user launches Update.exe, it usually uses 
the registry information set during software installation for locating 
where to apply the update. For software that does not use the registry, 
the folder where Update.exe is placed will be used as the destination 
for the update.

Once the update destination is determined, simply click the [Start] 
button to complete the update.

In short, the user only needs to do the following two things:

1. Launch Update.exe
2. Click the [Start] button


## Licenses

* Tikubiken.exe is available on a subscription basis, and is licensed 
  in each of the crowdfunding plans hosted by Searothonc. For more 
  information, please refer to the description page of 
  the crowdfunding plan.

* The update program output by Tikubiken.exe is licensed under 
  a [2-clause BSD license](https://opensource.org/licenses/BSD-2-Clause) 
  that is free to copy and distribute. The license terms are required 
  to attach for copying and redistribution, but it is automatically 
  output when the program is first started, so you can distribute 
  the output file only.


## FAQ

### What does 'Tikubiken' mean?
All of the world-renowned Japanese HENTAI gentlemen know that this 
unfamiliar-sounding name 'Tikubiken' comes from a minor Japanese slang 
meaning a ticket to uncensor blanked-out or laser-hidden nipples 
depictations. It is said that the legendary ticket called 'chikubi-ken' 
-- 'tikubi-ken' is a paraphrase of that -- alleged to be issued by 
editorial division when the manga with nude scenes is compiled into 
a volume to recover nipples that had been blanked during magazine 
serialization to boobs. We adopted this name because of the similarity 
of lifting the ban on all-ages versions of doujin games to 
adult versions.

### Which delta encoding format should I use?
At any rate, you will not have any trouble as long as you use one of 
the VCDiffs. We have implemented BsDiff, but even though it takes about 
10 times longer to encode than VCDiff, there is hardly any advantage in
compression size. BsDiff is faster than VCDiff in decompression speed, 
but it is only about twice as fast, so you may not need it except for 
those cases where the size of each file is small but the number of 
files is many and the entire size is large.

### A file named LICENSE.md was output, what is this?
This is a file that contains the rights notice and license terms of 
the library being used. In order to use the library, it is required 
to be included when redistributing it, so it is automatically output 
at the first startup. In the same way, the update program you made 
by Tikubiken also outputs LICENSE.md, so there is no legal problem 
if you distribute the .exe file only.

### Does the user need to keep LICENSE.md?
Not required. The license terms are only relevant in cases such as 
distributing copies, and there are no restrictions on personal use. 
When a user asks you the same thing, just say "You can delete it".
