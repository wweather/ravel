Ravel is a tool that can split and merge Twine 2 HTML files.
License: Public Domain
Contact: whimsical.weather@gmail.com

Run build.cmd to build Ravel. This requires Microsoft.NET Framework v4.0 or
higher. Once Ravel is built, you can run ravel.exe from a command prompt.
Ravel currently supports 2 commands:

  ravel import <filename> <workspace>
Imports and splits a Twine 2 HTML file specified by <filename> to a new
workspace folder specified by <workspace>. Once the import is complete, you
must copy your format.js file into the workspace folder. Story javascript and
style are imported to script.js and style.css, respectively. The story name and
start passage are stored to story.json as name and startnode, respectively, and
can be edited with any text editor.
Note that the Twine 2 passage positions are not preserved. If you import a
Ravel-published story back into Twine 2, the passages will be ordered
alphabetically.

  ravel publish <workspace> [<filename>]
Publishes a workspace to a Twine 2-compatible HTML file using the provided
format.js. If <filename> is omitted, the resulting HTML file will named after
the workspace.
Note that this will overwrite any previous file without warning.

An example Ravel project is included in the helloworld folder. Try copying in a
format.js and run "ravel publish helloworld".