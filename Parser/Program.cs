using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Parser
{
	class Program
	{
		static void Main(string[] args)
		{
			//read arguments
			Console.WriteLine("Reading arguments");
			string file = null;
			
			if (args.Length==0 || args[0].Equals("help"))
			{
				Help();
				return;
			}
			else
			{
				file = args[0];
			}

			for (int i=1;i<args.Length;i++)
			{
				string arg = args[i];
				
				Console.WriteLine(arg);	
			}
			Console.WriteLine("args read");

			if (!File.Exists(file))
			{
				Console.WriteLine("File \"{0}\" does not exist", file);
				Console.WriteLine("Current Directory: \"{0}\"",Directory.GetCurrentDirectory());
				return;
			}

			Parser fileParser = new Parser(file);

			fileParser.StartParsing();
		}

		//print out command options //TODO
		private static void Help()
		{
			Console.WriteLine("Arguments:");
			Console.WriteLine("file\n");
			Console.WriteLine("file The file to load into the database");
			
		}
	}
}
