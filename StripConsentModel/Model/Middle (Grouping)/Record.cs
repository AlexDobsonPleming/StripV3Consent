﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StripV3Consent.Model
{
    /// <summary>
    /// A single record from one of the csv files
    /// </summary>
    public class Record
    {
        public string[] DataRecord;

        public DataFile OriginalFile;

        public Record(string[] dataRecord, DataFile originalFile)
        {
            DataRecord = dataRecord;
            OriginalFile = originalFile;
        }

        public string this[int index]
        {
            get
            {
                return DataRecord[index];
            }
            set
            {
                DataRecord[index] = value;
            }
        }

        public string CompositeIdentifier
        {
            get
            {
                StringBuilder CompositeIdentifier = new StringBuilder();
                foreach(string IdentifierCode in Specification.DataSubmissionSpecification.IdentifierCodes)
                {
                    string CurrentValue = DataRecord[Spec2021K.Specification.PositionOfInEveryFile(IdentifierCode)];

                    //Special logic for NHS Numbers on comparison. This allows the matching of 123456890 and 123 456 7890
                    //Ideally we would have a list of datatypes instead of the list of strings that we have with Record
                    //And the NHS Number datatype would have a comparison operator that handles this
                    //But right now I do not have the time to encode all 1800 list data entries from 2021K and I doubt I ever will
                    if (IdentifierCode == "IBD01")
                    {
                        CurrentValue = CurrentValue.Replace(" ", "");
                    }

                    CompositeIdentifier.Append(CurrentValue);
                }
                return CompositeIdentifier.ToString();
            }
        }

        /// <summary>
        /// Returns a value from the record corresponding to it's Data Item Code in 2021K
        /// </summary>
        /// <param name="DataItemCode">the data itemcode, i.e. IBD01 for NHS Number</param>
        /// <returns></returns>
        public string GetValueByDataItemCode(string DataItemCode)
        {
            return this[this.OriginalFile.SpecificationFile.Fields.FindIndex(f => f.DataItemCode == DataItemCode)];
        }

        /// <summary>
        /// Do these records belong to the same patient
        /// </summary>
        /// <param name="OtherRecord">Other Record to compare against the current one</param>
        /// <returns>true/false if they're the same patient or not</returns>
        public bool Compare(Record OtherRecord)
        {
            int[] IdentifierLocations = Spec2021K.Specification.IdentiferLocations();

            //if any of the identifier values don't match return false
            if (IdentifierLocations.Any(i => DataRecord[i] != OtherRecord[i]))
            {
                return false;
            } else
            {
                return true;
            }
        }

        public static implicit operator string[](Record r) => r.DataRecord;

        public override string ToString()
        {
            return $"{OriginalFile.Name} file";
        }
    }
}
