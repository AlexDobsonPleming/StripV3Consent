﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StripV3Consent.Model
{
    public class ImportFile : DataFile
    {
        public ImportFile(string path) : base(path)
        {
        }

        public FileValidationState IsValid
        {
            get
            {
                FileValidationState ReturnValue = new FileValidationState();

                if (SpecificationFile is Specification.RegistryFile)
                {
                    ReturnValue.Organisation = FileOrganisation.Registry;

                } else if (SpecificationFile is Specification.NationalOptOutFile)
                {
                    ReturnValue.Organisation = FileOrganisation.NHS;
                    ReturnValue.IsValid = ValidState.Good;
                    ReturnValue.Message = "National Opt-Out file";
                } else
                {
                    ReturnValue.Organisation = FileOrganisation.Unknown;
                    ReturnValue.IsValid = ValidState.None;
                    ReturnValue.Message = "Unknown file type";

                    return ReturnValue;
                }


                if (IsSpecificationFileAlreadyImported(this) == true)
                {
                    ReturnValue.IsValid = ValidState.Error;
                    ReturnValue.Message = "File already imported";
                    return ReturnValue;
                }

                if (IsCommaDelimited() == true)
                {
                    ReturnValue.IsValid = ValidState.Good;
                    ReturnValue.Message = "File passed validation checks";
                }
                else
                {
                    ReturnValue.IsValid = ValidState.Error;
                    ReturnValue.Message = "CSV file not comma separated";

                }
                
                
                return ReturnValue;
            }
        }

        private enum IsWellFormed
        {
            NotWellFormed,
            JaggedRows,

        }
        /// <summary>
        /// Returns whether the file has the correct number of columns and that each row contains all of those columns
        /// </summary>
        /// <param name="SuspectedFile">Specification.File object representing the file you think it is to get the correct column form for</param>
        /// <returns></returns>
        private bool IsFileWellFormed(Specification.File SuspectedFile)
        {
            File2DArray Contents = SplitInto2DArray();

            if (Contents.Content.GroupBy<string[], int>(Row => Row.Count()).Count() > 1)
            {
                var test = Contents.Content.GroupBy<string[], int>(Row => Row.Count()).Count();
                return false;
            }
            return true;
        }

        private bool ContainsHeaders()
        {
            String TopLeftValue = null;
            const uint CutOffValue = 32;
            using (StreamReader StreamReader = new StreamReader(this.Path))
            {
                StringBuilder TopLeftValueBuilder = new StringBuilder();
                while ((char)StreamReader.Peek() != ',')
                {
                    TopLeftValueBuilder.Append((char)StreamReader.Read());
                        
                    if (TopLeftValueBuilder.Length >= CutOffValue)
                    {
                        break;
                    }
                }
                TopLeftValue = TopLeftValueBuilder.ToString();
            }

            if (TopLeftValue.StartsWith("HEADER_"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsCommaDelimited()
        {
            //Crudely tries to find if the file is comma delimited by seeing what is the most common char out of common delimiters
            //Especially tab as excel loves to swap commas for tabs in csv files
            char[] CommonDelimiters = new char[] {',','\t', '|' };

            string FileContents = null;
            using (StreamReader StreamReader = new StreamReader(this.Path))
            {
                FileContents = StreamReader.ReadToEnd();
            }
            int[] AppearanceCounts = CommonDelimiters.Select(Delimiter => FileContents.Count(f => f == Delimiter)).ToArray();

            if (AppearanceCounts[Array.IndexOf(CommonDelimiters, ',')] == AppearanceCounts.Max())
            {
                return true;
            } else
            {
                return false;
            }
        }

        /// <summary>
        /// Set by parent class, returns whether the specification file has already been dropped into the import pane
        /// </summary>
        public Predicate<ImportFile> IsSpecificationFileAlreadyImported;

        private string LineEndingsInFile()
        {

            using (StreamReader StreamReader = new StreamReader(this.Path))
            {
                while (StreamReader.EndOfStream == false)
                {
                    if ((char)StreamReader.Read() == '\r') //If carraige return is encountered followed by line feed then Windows
                    {
                        if ((char)StreamReader.Read() == '\n')
                        {
                            return "\r\n";
                        }
                    }
                    else if ((char)StreamReader.Read() == '\n')   //If line feed is encountered first then Unix
                    {
                        return "\n";
                    }
                }

                //Neither line ending found, doesn't really matter what we return but we'll return line feed
                return "\n";
            }

        }

        private File2DArray SplitIntoBoxed2DArrayWithHeaders(string File)
        {
            string RowSeparator = LineEndingsInFile();       
            char ColumnSeparator = ',';

            string[][] TwoDList;
            string[] ListOfLines = File.Split(RowSeparator.ToCharArray());

            TwoDList = ListOfLines.Select(x => x.Split(ColumnSeparator)).ToArray();

            File2DArray Return2DArray = new File2DArray();

            string[][] EmptyRows = (from string[] element in TwoDList
                                    where element.IsEmpty()
                                    select element).ToArray();

            List<string[]> RowstoRemove = new List<string[]>();
            RowstoRemove.AddRange(EmptyRows);

            if (ContainsHeaders())
            {
                RowstoRemove.Add(TwoDList[0]);  //Remove headers from content
                Return2DArray.Headers = TwoDList[0];
            }
            Return2DArray.Content = TwoDList.Except(RowstoRemove).ToArray();


            return Return2DArray;
        }

        public File2DArray SplitInto2DArray()
        {
            string FileContent = null;
            using (StreamReader reader = new StreamReader(this.Path))
            {
                FileContent = reader.ReadToEnd();
            }

            return SplitIntoBoxed2DArrayWithHeaders(FileContent);
        }
    }

    public static class StringArrayExtension
    {
        public static bool IsEmpty(this string[] Array)
        {
            if (Array.Length == 1 & Array[0] == "") { return true; }

            if (Array.All(element => element == Array[0])) { return true; }

            return false;
        }
    }

    
}
