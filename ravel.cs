/*
 * Ravel is a tool that can split and merge Twine 2 HTML files.
 * License: Public Domain
 * Contact: whimsical.weather@gmail.com
 */

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Linq;

namespace Ravel
{
	class Program
	{
		const string Version = "0.1";
		static StreamWriter Log;

		static void Main(string[] args)
		{
			Log = new StreamWriter("error.log");
			try
			{
				if (!DoCommand(args))
				{
					PrintHelp();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Something bad happened. See error.log for details.");
				Log.WriteLine(e.ToString());
			}
			Log.Close();
			if((new FileInfo("error.log").Length) == 0)
			{
				File.Delete("error.log");
			}
		}

		static bool DoCommand(string[] args)
		{
			if (args.Length < 1)
			{
				return false;
			}

			switch (args[0].ToLower())
			{
				case "import":
					if (args.Length < 3)
					{
						return false;
					}
					ImportStory(args[1], args[2]);
					break;

				case "publish":
					if (args.Length < 2)
					{
						return false;
					}
					string filename = args.Length >= 3 ? args[2] : null;
					PublishStory(args[1], filename);
					break;

				default:
					return false;
			}
			return true;
		}

		static void PrintHelp()
		{
			Console.WriteLine("Ravel v{0}", Version);
			Console.WriteLine("Ravel is a tool that can split and merge Twine 2 HTML files.");
			Console.WriteLine("License: Public Domain");
			Console.WriteLine("Contact: whimsical.weather@gmail.com");
			Console.WriteLine();
			Console.WriteLine("  Usage:");
			Console.WriteLine("ravel import <filename> <workspace>");
			Console.WriteLine("ravel publish <workspace> [<filename>]");
		}

		static void ImportStory(string filename, string story)
		{
			if(Directory.Exists(story))
			{
				Console.WriteLine("import: Specified story workspace already exists.");
				return;
			}
			if(!File.Exists(filename))
			{
				Console.WriteLine("import: Specified story file doesn't exist.");
				return;
			}

			string html = File.ReadAllText(filename);

			// Prepare story for the XML parser. The valueless attribute <tw-storydata hidden> is not valid XML,
			// and the <style> and <script> contents are not appropriately encoded to be valid XML, either.
			// Switch to http://htmlagilitypack.codeplex.com/

			string startTail = " hidden>";
			string end = "</tw-storydata>";

			int startOffset = html.IndexOf("<tw-storydata");
			int startTailOffset = html.IndexOf(startTail, startOffset);
			int passageStartOffset = html.IndexOf("<tw-passagedata", startOffset);

			string xmlString = html.Substring(startOffset, startTailOffset - startOffset) + ">"
				+ html.Substring(passageStartOffset, html.LastIndexOf(end) + end.Length - passageStartOffset);

			string otherString = html.Substring(startTailOffset + startTail.Length, passageStartOffset - startTailOffset - startTail.Length);

			// These can potentially fail if more than one matching tag start/end occurs before the first <tw-passagedata>.
			Regex styleRegex = new Regex("<style[^>]*>(.*)</style>", RegexOptions.Singleline);
			string globalStyle = styleRegex.Match(otherString).Groups[1].Value;

			Regex scriptRegex = new Regex("<script[^>]*>(.*)</script>", RegexOptions.Singleline);
			string globalScript = scriptRegex.Match(otherString).Groups[1].Value;

			XDocument doc = XDocument.Parse(xmlString);
			XElement st = doc.Element("tw-storydata");

			string startnode = st.Attribute("startnode").Value;
			string startnodeName = "";

			Console.WriteLine("Importing {0}...", st.Attribute("name").Value);
			Console.WriteLine();

			DirectoryInfo dirInfo = Directory.CreateDirectory(story);

			File.WriteAllText(Path.Combine(dirInfo.FullName, "style.css"), globalStyle);
			Console.WriteLine("Story stylesheet imported.");

			File.WriteAllText(Path.Combine(dirInfo.FullName, "script.js"), globalScript);
			Console.WriteLine("Story javascript imported.");

			int errors = 0;
			int passageCount = 0;
			foreach (XElement el in st.Elements("tw-passagedata"))
			{
				string name = el.Attribute("name").Value;
				try
				{
					string pathString = Path.GetFullPath(Path.Combine(dirInfo.FullName, Path.ChangeExtension(name.Replace('/', Path.DirectorySeparatorChar), "tws")));
					if (pathString.Substring(0, dirInfo.FullName.Length) != dirInfo.FullName)
					{
						Console.WriteLine("Error: Passage name {0} resolves to a path outside the story workspace.", name);
						Log.WriteLine("Error: Passage name {0} resolves to a path outside the story workspace.", name);
						errors++;
						continue;
					}
					if (el.Attribute("pid").Value == startnode)
					{
						startnodeName = name;
					}
					Directory.CreateDirectory(Path.GetDirectoryName(pathString));
					using (StreamWriter file = new StreamWriter(pathString))
					{
						if (el.Attribute("tags").Value != "")
						{
							file.WriteLine("/* tags: " + el.Attribute("tags").Value + " */");
						}
						file.Write(el.Value);
						file.Close();
					}
					passageCount++;
				}
				catch (Exception e)
				{
					Console.WriteLine("Error importing passage {0}. Check error.log for details.", name);
					Log.WriteLine("Error importing passage {0}.", name);
					Log.WriteLine(e.ToString());
					errors++;
				}
			}
			Console.WriteLine("{0} of {1} passage{2} imported.", passageCount, passageCount + errors, (((passageCount + errors) == 1) ? "" : "s"));

			using (FileStream infoStream = new FileStream(Path.Combine(dirInfo.FullName, "story.json"), FileMode.Create))
			{
				DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(StoryInfo));
				js.WriteObject(infoStream, new StoryInfo
				{
					name = st.Attribute("name").Value,
					startnode = startnodeName,
					ifid = st.Attribute("ifid").Value
				});
				infoStream.Close();
			}

			Console.WriteLine("Story info saved.");
			Console.WriteLine();
			if (errors > 0)
			{
				Console.WriteLine("Import completed with errors. Check error.log for details.");
			}
			else
			{
				Console.WriteLine("Import complete.");
			}
		}

		static void PublishStory(string story, string outfile = null, string options = "")
		{
			if (!Directory.Exists(story))
			{
				Console.WriteLine("publish: Specified story workspace doesn't exist.");
				return;
			}
			foreach(string filename in new string[]
			{
				"format.js",
				"style.css",
				"script.js",
				"story.json"
			})
			{
				if(!File.Exists(Path.Combine(story, filename)))
				{
					Console.WriteLine("publish: {0} doesn't exist.", filename);
					return;
				}
			}

			StoryInfo storyInfo;
			using (FileStream infoStream = new FileStream(Path.Combine(story, "story.json"), FileMode.Open))
			{
				DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(StoryInfo));
				storyInfo = (StoryInfo)js.ReadObject(infoStream);
				infoStream.Close();
			}

			if(!File.Exists(Path.Combine(story, Path.ChangeExtension(storyInfo.startnode.Replace('/', Path.DirectorySeparatorChar), "tws"))))
			{
				Console.WriteLine("publish: Story startnode {0} doesn't exist.", storyInfo.startnode);
				return;
			}

			// format.js needs to be trimmed so only the JS object remains.
			StoryFormat storyFormat;
			using (MemoryStream formatStream = new MemoryStream())
			{
				string formatFile = File.ReadAllText(Path.Combine(story, "format.js"));
				int jsObjectStart = formatFile.IndexOf('{');
				StreamWriter writer = new StreamWriter(formatStream);
				writer.Write(formatFile.Substring(jsObjectStart, formatFile.LastIndexOf('}') + 1 - jsObjectStart));
				writer.Flush();
				formatStream.Position = 0;
				DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(StoryFormat));
				storyFormat = (StoryFormat)js.ReadObject(formatStream);
				formatStream.Close();
			}

			Console.WriteLine("Exporting story {0} using format {1}...", storyInfo.name, storyFormat.name);
			Console.WriteLine();

			XDocument doc = new XDocument(
				new XElement("tw-storydata",
					new XAttribute("name", storyInfo.name),
					new XAttribute("startnode", "0"),
					new XAttribute("creator", "Ravel"),
					new XAttribute("creator-version", Version),
					new XAttribute("ifid", storyInfo.ifid),
					new XAttribute("format", storyFormat.name),
					new XAttribute("options", options),
					new XAttribute("hidden", "{{RAVEL_HIDDEN}}"),

					new XElement("style",
						new XAttribute("role", "stylesheet"),
						new XAttribute("id", "twine-user-stylesheet"),
						new XAttribute("type", "text/twine-css"),
						"{{RAVEL_STYLE}}"),
					new XElement("script",
						new XAttribute("role", "script"),
						new XAttribute("id", "twine-user-script"),
						new XAttribute("type", "text/twine-javascript"),
						"{{RAVEL_SCRIPT}}")
					)
				);

			int pid = 0;
			List<string> passages = Directory.EnumerateFiles(story, "*.tws", SearchOption.AllDirectories).ToList();
			int sizeX = (int)Math.Ceiling(Math.Sqrt(passages.Count()));
			foreach (string file in passages)
			{
				pid++;
				int posX = (pid - 1) % sizeX;
				int posY = (pid - 1 - posX) / sizeX;
				int offset = 0;
				string name = Path.ChangeExtension(file, null).Substring(story.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
				string tags = "";
				string passage = File.ReadAllText(file);
				Regex tagsRegex = new Regex(@"^/\*\s*tags:\s*(.*?)\s*\*/\s*");
				Match matches = tagsRegex.Match(passage);
				if (matches.Success)
				{
					tags = matches.Groups[1].Value;
					offset = matches.Length;
				}
				if (name == storyInfo.startnode)
				{
					doc.Root.SetAttributeValue("startnode", pid);
				}
				doc.Root.Add(new XElement("tw-passagedata",
					new XAttribute("pid", pid),
					new XAttribute("name", name),
					new XAttribute("tags", tags),
					new XAttribute("position", (posX * 100).ToString() + "," + (posY * 100).ToString()),
					passage.Substring(offset)
					));
			}

			Console.WriteLine("{0} passage{1} included.", pid, ((pid == 1) ? "" : "s"));

			Regex hiddenRegex = new Regex(Regex.Escape("hidden=\"{{RAVEL_HIDDEN}}\""));
			Regex styleRegex = new Regex(Regex.Escape("{{RAVEL_STYLE}}"));
			Regex scriptRegex = new Regex(Regex.Escape("{{RAVEL_SCRIPT}}"));

			// Fix up non-XML compliant parts. Order matters to avoid erroneous replacements: hidden, script, style.
			string storyData = doc.ToString(SaveOptions.DisableFormatting);
			storyData = hiddenRegex.Replace(storyData, "hidden", 1);
			storyData = scriptRegex.Replace(storyData, File.ReadAllText(Path.Combine(story, "script.js")), 1);
			storyData = styleRegex.Replace(storyData, File.ReadAllText(Path.Combine(story, "style.css")), 1);

			string html = storyFormat.source.Replace("{{STORY_NAME}}", storyInfo.name);
			html = html.Replace("{{STORY_DATA}}", storyData);

			if(outfile == null)
			{
				outfile = Path.ChangeExtension(story, "html");
			}

			File.WriteAllText(outfile, html);

			Console.WriteLine();
			Console.WriteLine("Story published to {0}.", outfile);
		}
	}

	public class StoryInfo
	{
		public string name;
		public string startnode;
		public string ifid;
	}

	public class StoryFormat
	{
		public string name;
		public string version;
		public string description;
		public string author;
		public string image;
		public string url;
		public string license;
		public bool proofing;
		public string source;
	}
}
