﻿using Cinteros.Crm.Utils.Common;
using Cinteros.Crm.Utils.Shuffle;
using System;
using System.Management.Automation;
using System.Xml;

namespace Cinteros.Crm.Utils.CI.Cmdlets
{
    [Cmdlet(VerbsData.Import, "CrmShuffle")]
    [OutputType(typeof(ShuffleImportResult))]
    public class ImportCrmShuffleCmdlet : XrmCommandBase
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Provide a valid Shuffle definition, as an XMLDocument"
        )]
        [Alias("Def", "D")]
        public XmlDocument Definition { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Provide Shuffle XML-data, as an XMLDocument"
        )]
        public XmlDocument DataXml { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Provide Shuffle CSV-data, as a string"
        )]
        public string DataCsv { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Working folder for relative paths, solution zips etc."
        )]
        [Alias("F")]
        public string Folder { get; set; }

        private readonly ProgressRecord progress = new ProgressRecord(0, "Shuffle", "Idle");

        protected override void ProcessRecord()
        {
            var Data = DataXml;
            if (Data == null && DataCsv != null)
            {
                try
                {
                    WriteVerbose("Xmlifying CSV data");
                    Data = ShuffleHelper.StringToXmlSerialized(DataCsv);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "StringToXmlSerialized", ErrorCategory.ReadError, Definition));
                }
            }
            try
            {
                WriteDebug("Importing");
                var result = Shuffler.QuickImport(Definition, Data, ShuffleListener, Container, Folder, true);
                var output = new ShuffleImportResult
                {
                    Created = result.Item1,
                    Updated = result.Item2,
                    Skipped = result.Item3,
                    Deleted = result.Item4,
                    Failed = result.Item5
                };
                WriteObject(output);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "QuickImport", ErrorCategory.ReadError, Definition));
            }
        }

        private void ShuffleListener(object sender, ShuffleEventArgs e)
        {
            if (e.Counters != null && !string.IsNullOrWhiteSpace(e.Counters.Block))
            {
                progress.StatusDescription = $"Importing {e.Counters.Block}";
            }
            if (e.Counters.Blocks > 0)
            {
                progress.PercentComplete = (e.Counters.BlockNo * 100) / e.Counters.Blocks;
            }
            WriteProgress(progress);
            if (!string.IsNullOrWhiteSpace(e.Message))
            {
                WriteVerbose(e.Message);
            }
        }
    }

    public class ShuffleImportResult
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public int Deleted { get; set; }
        public int Failed { get; set; }
    }
}
