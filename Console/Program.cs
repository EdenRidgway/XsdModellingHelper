namespace XsdHelper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Schema;

    using NDesk.Options;

    public class Program
    {
        public static void Main(string[] args)
        {
            string sourceFileName = String.Empty;
            string targetFileName = String.Empty;
            string rootNodeName = String.Empty;
            bool showHelp = false;
            List<string> nodesToSkip = new List<string>();
            List<string> attributesToAdd = new List<string>();

            var optionSet = new OptionSet() 
            {
                { "s|source=", "The {SOURCE} xsd file to use to generate the XPaths (required).",   v => sourceFileName = v },
                { "t|target=", "The {TARGET} csv file containing the extracted XPaths (required).",  v => targetFileName = v },
                { "r|root:", "The {ROOT ELEMENT} to start from (optional). If not specified this defaults to the first root element found.",  v => rootNodeName = v },
                { "i|ignore:", "The {ELEMENT} to ignore when generating the XPaths (optional).",  nodesToSkip.Add },
                { "a|add:", "An additional {ATTRIBUTE} to extract from the element when generating the csv list (optional).",  attributesToAdd.Add },
                { "h|?|help", "Show this message and exit.", v => showHelp = v != null },
            };

            try
            {
                List<string> extraParameters = optionSet.Parse(args);
                if (extraParameters.Count > 0)
                {
                    Console.WriteLine("XsdHelper: The following extra parameters are being ignored:\n {0}", String.Join("\n", extraParameters.ToArray()));
                }
            }
            catch (OptionException e)
            {
                Console.Write("XsdHelper: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `XsdHelper --help' for more information.");
                return;
            }

            showHelp |= String.IsNullOrEmpty(sourceFileName) || String.IsNullOrEmpty(targetFileName);

            if (showHelp)
            {
                ShowHelp(optionSet);
                return;
            }

            ValidateSuppliedFileArguments(sourceFileName, targetFileName);

            using (StreamWriter csvXPathFileWriter = new StreamWriter(targetFileName, false))
            {
                Console.WriteLine("Loading schemas...");
                SchemaParser parser = new SchemaParser(sourceFileName, csvXPathFileWriter);

                Console.WriteLine(String.Format("{0} files loaded", parser.LoadedSchemas.Count));

                foreach (KeyValuePair<string, XmlSchema> loadedSchema in parser.LoadedSchemas.OrderBy(s => s.Key))
                {
                    Console.WriteLine(String.Format("File: {0}, Namespace: {1}", loadedSchema.Key, loadedSchema.Value.TargetNamespace));
                }

                parser.ExtractXPaths(nodesToSkip, attributesToAdd, rootNodeName);
                csvXPathFileWriter.Close();
            }
        }

        private static void ValidateSuppliedFileArguments(string sourceFileName, string targetFileName)
        {
            if (!File.Exists(sourceFileName))
            {
                Console.Write(String.Format("Missing source file specified: '{0}'", sourceFileName));
                Environment.Exit(1); 
            }

            FileInfo targetFileInfo = new FileInfo(targetFileName);
            if (targetFileInfo.Directory != null && !targetFileInfo.Directory.Exists)
            {
                Console.Write(String.Format("Missing target directory specified: '{0}'", targetFileInfo.Directory.FullName));
                Environment.Exit(1); 
            }

            if (!targetFileInfo.Exists)
            {
                return;
            }
            if (targetFileInfo.IsReadOnly)
            {
                Console.Write(String.Format("Target file '{0}' is read only. Please make it writable.", targetFileInfo.Name));
                Environment.Exit(1);
            }

            if (IsFileInUse(targetFileInfo.FullName))
            {
                Console.Write(String.Format("The file '{0}' is currently locked. Please close the application locking the file before running the command.", targetFileInfo.Name));
                Environment.Exit(1);
            }
        }

        private static void ShowHelp(OptionSet optionSet)
        {
            Console.WriteLine("Usage: XsdHelper [OPTIONS]");
            Console.WriteLine("Example: XsdHelper -s=Source.Xml -t=XPaths.csv -i=MetaData -i=Error -a=annotation:deprecated -a=annotation:deprecated-reason");
            Console.WriteLine("Extracts XPaths for each element in the source document and creates a CSV file with the node name and parent as well as full XPath.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
        }

        /// <summary>
        /// Nasty way of determining if the file is currently in use (e.g. Open in Excel)
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>true if it is in use and false if it is not.</returns>
        private static bool IsFileInUse(string path)
        {
            try
            {
                using (new FileStream(path, FileMode.OpenOrCreate)) { }

                return false;
            }
            catch (IOException ex)
            {
                if (ex.Message.Contains("The process cannot access the file"))
                {
                    return true;
                }
                
                throw;
            }
        }

    }
}