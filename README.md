# Ravel
**Ravel is a tool that can split (import) and merge (publish) Twine 2 HTML files.**

You can run build.cmd to build Ravel if you do not have Visual Studio
installed. This requires Microsoft.NET Framework v4.0 or higher.

The **helloworld** folder contains an example Ravel workspace.

Check **readme.txt** for information on basic usage.

## Known Issues
The function for importing files currently has a questionable implementation,
causing it to fail when the global story CSS or JS contains &lt;style&gt; or &lt;script&gt;
tags. A good implementation would use a proper HTML interpreter to decompose
the story markup.