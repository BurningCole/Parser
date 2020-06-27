using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.IO;
using System.Configuration;

namespace Parser
{
	class Parser
	{
		//the file to read from
		private string inputFile = "";
		private SqlConnection connection;

		//years to count between, set in file header
		private int minYear = 2000;
		private int maxYear = 2000;

		//default database table is set in config file
		private string table = ConfigurationManager.AppSettings["DefaultTable"];

		//wether to print non-error messages
		public static bool consolePrint = true;

		//when this is set it replaces the connection string defined in the config file
		public static string connectionString = null;

		//create a parser for a specific file
		public Parser(string inputFile)
		{
			
			this.inputFile = inputFile;
			this.connection = new SqlConnection(
				connectionString ?? ConfigurationManager.AppSettings["ConnectionArgument"]
			);
		}


		//changes database connection string to new string
		public void ChangeDBConfig(string configString)
		{
			connection.ConnectionString = configString;
			connectionString = configString;
		}

		//change database table to put parsed results in
		public void setTable(string table) {
			this.table=table;
		}

		//start parsing the file
		public void StartParsing()
		{
			StreamReader stream = new StreamReader(inputFile);
			ReadHeader(stream);


			try
			{
				connection.Open();

				while (ReadBlock(stream))
				{
					WriteLine("Block added");
				}
			}
			catch(SqlException e)
			{
				Console.WriteLine("Database Error occurred: "+e.Message);
			}
			finally
			{
				connection.Close();
			}

		}

		//read the header of the file and set variables based on the results
		private void ReadHeader(StreamReader stream)
		{
			while (stream.Peek() != '[')//go through initial data
			{
				string line = stream.ReadLine();
			}

			while (stream.Peek() == '[')//go through boxed configurations
			{
				string line = stream.ReadLine();
				line = line.Replace("]", "");
				string[] options = line.Split('[');
				foreach (string option in options)
				{
					string[] configuration = option.Split('=');

					// set year gap
					if (configuration[0].ToLower().Equals("years"))
					{
						string[] yearBounds = configuration[1].Trim().Split('-');
						try
						{
							minYear = int.Parse(yearBounds[0]);
							maxYear = int.Parse(yearBounds[1]);
							WriteLine("Year bounds set: {0} to {1}", minYear, maxYear);
						}
						catch (FormatException)
						{
							WriteLine("Years \"{0}\" formatted incorrectly.",configuration[1]);
						}

					}

				}
			}
			//create table if it doesn't exist
			try
			{
				SqlCommand command = new SqlCommand(
					"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + table + "') " +
					"CREATE TABLE " + table + " (Xref INT NOT NULL, Yref INT NOT NULL, Date DATE NOT NULL, Value INT NOT NULL);"
					, connection
				);
				connection.Open();
				command.BeginExecuteNonQuery();
			}
			finally
			{
				connection.Close();
			}

		}

		//read next block
		private bool ReadBlock(StreamReader stream)
		{
			int[] gridRef = new int[2];
			string line;

			//ensure starting at beginning of block
			do
			{
				line = stream.ReadLine();

				//stop scanning if at end of file
				if (line == null) return false;
			} while (!line.StartsWith("Grid-ref"));

			string mergedGridRef = line.Split('=')[1];
			string[] gridRefStr = mergedGridRef.Split(',');
			try
			{
				//set grid ref data
				gridRef[0] = int.Parse(gridRefStr[0].Trim());
				gridRef[1] = int.Parse(gridRefStr[1].Trim());
				WriteLine("Grid Block: {0} to {1}", gridRef[0], gridRef[1]);
			}
			catch (FormatException)
			{
				WriteLine("Grid-ref formatted incorrectly. {0}",mergedGridRef);
				return true;
			}

			string sqlCommandString = "INSERT INTO " + table + " (Xref, Yref, Date, Value) VALUES (@xRef, @yRef, @date, @value);";
			SqlCommand command = new SqlCommand(sqlCommandString, connection);
			command.Parameters.Add("@xRef", System.Data.SqlDbType.Int);
			command.Parameters.Add("@yRef", System.Data.SqlDbType.Int);
			command.Parameters.Add("@date", System.Data.SqlDbType.DateTime);
			command.Parameters.Add("@value", System.Data.SqlDbType.Int);

			command.Parameters["@xRef"].Value = gridRef[0];
			command.Parameters["@yRef"].Value = gridRef[1];

			for (int year = minYear; year <= maxYear; year++)
			{
				if (stream.Peek() == 'G')
				{
					WriteLine("Block missing years {0} to {1}", year, maxYear);
					break;
				}
				line = stream.ReadLine();
				if (line == null) return false; //end of file
				ReadYear(line, command, year);
			}
			
			return true;
		}

		//take line and calculate yearly data for grid position
		private void ReadYear(string line, SqlCommand command, int year)
		{
			//split line into 12 substrings of identical length
			int valueSize = line.Length / 12;
			for (int month = 1; month <= 12; month++)
			{
				string result = line.Substring((month - 1) * valueSize, valueSize);
				command.Parameters["@date"].Value = new DateTime(year, month, 1);
				try
				{
					//set grid ref data
					int resultValue = int.Parse(result.Trim());
					command.Parameters["@value"].Value = resultValue;
					command.ExecuteNonQuery();
				}
				catch (FormatException e)
				{
					WriteLine("Value \"{0}\" is not an integer", result);
					continue;
				}
			}
		}

		//print out values from a select command
		public static void printValues(SqlCommand command)
		{
			using (SqlDataReader reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					int fields = reader.VisibleFieldCount;
					for (int i = 0; i < fields; i++)
					{
						Console.Write("|{0}", reader.GetString(i));
					}
					Console.WriteLine("|");
				}
			}
		}

		//print if quiet flag not raised
		public static void WriteLine(string str, params object[] args)
		{
			if (consolePrint) Console.WriteLine(str, args);
		}
	}
}
