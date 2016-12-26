﻿#region ======================== Namespaces

using System;
using System.IO;
using System.Text;
using System.Xml;

#endregion

namespace mxd.ChangelogMaker
{
	class Program
	{
		#region ======================== Constants

		private const string SEPARATOR = "--------------------------------------------------------------------------------";

		#endregion

		#region ======================== Main

		static int Main(string[] args)
		{
			Console.WriteLine("Changelog Maker v02 by MaxED");
			if(args.Length != 4)
			{
				return Fail("USAGE: ChangelogMaker.exe input output author revision_number\n" +
							"input: xml file generated by 'svn log --xml' command\n" +
							"output: directory to store Changelog.txt in\n" +
							"author: commit author\n" +
							"revision_number: latest revision number", 1);
			}

			string input = args[0];
			string output = args[1];
			string author = args[2];
			int revnum;
			if(!int.TryParse(args[3], out revnum)) return Fail("Unable to parse revision number from string '" + revnum + "'.", 4);

			if(!File.Exists(input)) return Fail("Input file '" + input + "' does not exist.", 2);
			if(!Directory.Exists(output)) return Fail("Output folder '" + output + "' does not exist.", 3);

			//Replace bracket placeholders, because git log command doesn't escape xml-unfriendly chars like < or >...
			string inputtext = File.ReadAllText(input);
			inputtext = inputtext.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;").Replace("[OB]", "<").Replace("[CB]", ">");
			
			XmlDocument log = new XmlDocument();
			using(MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(inputtext)))
			{
				log.Load(stream);
			}

			StringBuilder result = new StringBuilder(1000);
			foreach(XmlNode node in log.ChildNodes) 
			{
				if(node.ChildNodes.Count == 0) continue;

				foreach (XmlNode logentry in node.ChildNodes)
				{
					string commit = (logentry.Attributes != null ? logentry.Attributes.GetNamedItem("commit").Value : "unknown");
					DateTime date = DateTime.Now;
					string message = string.Empty;
					bool skip = false;

					// Add revision info...
					if(logentry.Attributes != null)
					{
						var revinfo = log.CreateAttribute("revision");
						revinfo.Value = revnum.ToString();
						logentry.Attributes.SetNamedItem(revinfo);
					}

					foreach(XmlNode value in logentry.ChildNodes)
					{
						switch(value.Name)
						{
							case "author":
								if(value.InnerText != author) skip = true;
								break;

							case "date":
								date = Convert.ToDateTime(value.InnerText); 
								break;

							case "msg":
								message = value.InnerText;
								break;
						}
					}

					if(!skip)
					{
						result.Append("R" + revnum)
						      .Append(" | ")
							  .Append(commit)
							  .Append(" | ")
						      .Append(date.ToShortDateString())
						      .Append(", ")
						      .Append(date.ToShortTimeString())
						      .Append(Environment.NewLine)
							  .AppendLine(SEPARATOR)
						      .Append(message)
						      .Append(Environment.NewLine)
                              .Append(Environment.NewLine)
							  .Append(Environment.NewLine);
					}

					// Decrease revision number...
					revnum--;
				}
				break;
			}

			// Save modified xml
			log.Save(input);

			//Save result
			string outputpath = Path.Combine(output, "Changelog.txt");
			File.WriteAllText(outputpath, result.ToString());
			Console.WriteLine("Saved '" + outputpath + "'");

			//All done
			return 0;
		}

		#endregion

		private static int Fail(string message, int exitcode)
		{
			Console.WriteLine(message + "\nPress any key to quit");
			Console.ReadKey();
			return exitcode;
		}
	}
}