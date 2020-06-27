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
			string file = null;
			
			if (args.Length==0 || args[0].ToLower().Equals("help"))
			{
				Help();
				return;
			}
			else
			{
				file = args[0];
			}

			Parser fileParser = new Parser(file);

			for (int i=1;i<args.Length;i++)
			{
				string arg = args[i];
				switch (arg)
				{
					case "-q":
					case "-quiet":
						Parser.consolePrint = false;
						break;
					case "-con":
						i++;
						if (i < args.Length)
							Parser.connectionString = args[i];
						else
							Console.WriteLine("Connection string not supplied");
						break;
					case "-t":
						i++;
						if (i < args.Length)
							fileParser.setTable(args[i]);
						else
							Console.WriteLine("Table name not supplied");
						break;
					default:
						Console.WriteLine("Unknown argument \"{0}\"", arg);
						break;
				}

				Console.WriteLine(arg);	
			}

			if (!File.Exists(file))
			{
				Console.WriteLine("File \"{0}\" does not exist", file);
				Console.WriteLine("Current Directory: \"{0}\"",Directory.GetCurrentDirectory());
				return;
			}

			fileParser.StartParsing();
		}

		//print out command options
		private static void Help()
		{
			Console.WriteLine("Arguments:");
			Console.WriteLine("file [-i] [-con connection_string] [-t table_name]\n");
			Console.WriteLine("file    The file to load into the database, writing \"help\" will open this menu instead");
			Console.WriteLine("-q      Stops output excluding error messages");
			Console.WriteLine("-con    replaces the database connection string in config");
			Console.WriteLine("-t	   replaces the default table to hold the values");
		}
	}
}
