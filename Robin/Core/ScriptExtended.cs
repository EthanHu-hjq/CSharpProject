using ApEngine;
using AudioPrecision.API;
using ScottPlot.Control.EventProcess.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using TestCore;
using TestCore.Base;
using TestCore.Data;

namespace Robin.Core
{
    [Serializable]
    public class ScriptExtended : TF_Base, System.Xml.Serialization.IXmlSerializable, INotifyPropertyChanged
    {
        public const string FileExt = "sxt";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private DateTime scripttime;
        public DateTime ScriptTime 
        {
            get { return scripttime; }
            set { scripttime = value; OnPropertyChanged("ScriptTime"); }
        }
        public bool ShowInputSn { get; set; } = true;
        public bool CheckStartReady { get; set; } = false;
        public bool MergeSequenceReport { get; set; } = true;

        public List<SequenceExtended> SequenceExtendeds { get; } = new List<SequenceExtended>();

        [XmlIgnore]
        public Script Script { get; set; }

        public ScriptExtended()
        {
            
        }

        public void Analyze()
        {
            ScriptTime = Script.Time;

            var activeseq = Script.ActiveSequence;
            foreach (Sequence seq in Script.Sequences)
            {
                seq.Analyze();
            }

            Script.Activate(activeseq.Name);
        }

        public static ScriptExtended AnalyzeScript(Script script)
        {
            ScriptExtended se = new ScriptExtended();

            se.Script = script;
            se.ScriptTime = script.Time;

            var activeseq = script.ActiveSequence;
            foreach(Sequence seq in script.Sequences)
            {
                se.SequenceExtendeds.Add(new SequenceExtended(seq));
            }

            script.Activate(activeseq.Name);

            return se;
        }

        public static ScriptExtended FromScript(Script script)
        {
            var extfile = Path.Combine(Path.GetDirectoryName(script.FilePath), $"{Path.GetFileNameWithoutExtension(script.FilePath)}.{FileExt}");
            if (File.Exists(script.FilePath))
            {
                if(File.Exists(extfile))
                {
                    var se =  FromFile(extfile);

                    if (se.ScriptTime.ToString() == script.Time.ToString())
                    {
                        se.Script = script;
                        foreach (Sequence seq in script.Sequences)
                        {
                            if (se.SequenceExtendeds.FirstOrDefault(x => x.Name == seq.Name) is SequenceExtended seqex)
                            {
                                seqex.Sequence = seq;
                                seq.Spec = seqex.Spec;
                            }
                        }
                        script.Activate((Sequence)script.ActiveSequence);

                        return se;
                    }
                }
            }

            var rtn = AnalyzeScript(script);
            rtn.Save(extfile);

            return rtn;
        }

        public static ScriptExtended FromFile(string path)
        { 
            using(StreamReader sr = new StreamReader(path))
            {
                return XmlSerializerHelper.Deserialize(sr.ReadToEnd(), typeof(ScriptExtended)) as ScriptExtended;
            }
        }

        public const string CUSTOMIZED_TAG = "[Robin]";

        public int Save(string path = null)
        {
            if(path is null)
            {
                path = Path.Combine(Path.GetDirectoryName(Script.FilePath), $"{Path.GetFileNameWithoutExtension(Script.FilePath)}.{FileExt}");
            }

            bool dataexport_specsheet_first = true;
            bool dataexport_database_first = true;
            bool dataexport_vacs_first = true;

            if (Script != null)
            {
                foreach (var se in SequenceExtendeds)
                {
                    Script.Activate(se.Sequence);
                    
                    foreach (ISignalPath sp in ApxEngine.ApRef.Sequence)
                    {
                        if (!sp.Checked) continue;
                        var des = se.DataExportConfigs.Where(x => x.SignalPath == sp.Name);

                        foreach(ISequenceMeasurement meas in sp)
                        {
                            if (!meas.Checked) continue;

                            var ders = des.Where(x => x.Measurement == meas.Name);

                            if (ders.Count() == 0) continue;

                            //foreach(ISequenceStep step in meas.SequenceSteps.ExportResultDataSteps)
                            //{
                            //    if (step is null) continue;
                            //    if(step.Name.StartsWith(CUSTOMIZED_TAG))
                            //    step.Delete();
                            //}

                            foreach (ISequenceStep step in meas.SequenceSteps)
                            {
                                if (step is null) continue;
                                if (step.Name.StartsWith(CUSTOMIZED_TAG))
                                    step.Delete();
                            }

                            foreach (var dec in ders)
                            {
                                if(dec.Channels_Database.Count != 0)
                                {
                                    if(!string.IsNullOrEmpty(dec.ExportDataSpec_Database))
                                    {
                                        if (App.GroupSetting.GlobalDefinitionGroups[GlobalDefinitionGroupName.Export_Data_Specification] is GroupItem<string> ds)
                                        {
                                            if(dec.ExportDataSpec_Database == "N/A")
                                            {
                                            }
                                            else 
                                            {
                                                var erds = meas.SequenceSteps.ExportResultDataSteps.Add();
                                                erds.Name = $"{CUSTOMIZED_TAG}[ER_Database]";
                                                erds.ResultName = dec.Result;
                                                erds.SheetPerChannel = true;
                                                erds.DataType = SourceDataType.Measured;
                                                
                                                if(dataexport_database_first)
                                                {
                                                    erds.Append = false;
                                                    dataexport_database_first = false;
                                                }
                                                else
                                                {
                                                    erds.Append = true;
                                                }

                                                if (ds.Keys.Contains(dec.ExportDataSpec_Database))
                                                {
                                                    var specpath = Path.Combine(App.CommonFileDir, ds[dec.ExportDataSpec_Database]);

                                                    if (!File.Exists(specpath))
                                                    {
                                                        throw new FileNotFoundException($"Apply Data Spec Failed. File {specpath} does not exist for {dec.ExportDataSpec_Database}");
                                                    }

                                                    erds.LoadExportSpecification(specpath, true);

                                                    //try
                                                    //{
                                                    //    erds.ExportSpecification = ds[dec.ExportDataSpec_Database];
                                                    //}
                                                    //catch (AudioPrecision.API.APException) // No import yet;
                                                    //{
                                                        
                                                    //}
                                                }
                                                else
                                                {
                                                    erds.ExportSpecification = dec.ExportDataSpec_Database;
                                                }

                                                for (int i = 0; i < 16; i++)
                                                {
                                                    erds.SetChannelEnabled(i, false);
                                                }

                                                foreach (var ch in dec.Channels_Database)
                                                {
                                                    erds.SetChannelEnabled(int.Parse(ch.Substring(2)) - 1, true);  // Don't know if it is start from 0 or 1,
                                                }

                                                erds.FileName = "$(DataFolder)\\$(SUT_Model)\\$(SUT_Model_Option)\\$(ProjectName)\\$(SUT_Model)_$(SUT_Model_Option)_$(SUT_ID)_$(Date)_Database.xlsx";
                                            }
                                        }
                                    }
                                }

                                if(dec.Channels_SpecSheet.Count != 0)
                                {
                                    if (!string.IsNullOrEmpty(dec.ExportDataSpec_SpecSheet))
                                    {
                                        if (App.GroupSetting.GlobalDefinitionGroups[GlobalDefinitionGroupName.Export_Data_Specification] is GroupItem<string> ds)
                                        {
                                            if (dec.ExportDataSpec_SpecSheet == "N/A")
                                            {

                                            }
                                            else
                                            {
                                                var erds = meas.SequenceSteps.ExportResultDataSteps.Add();
                                                erds.Name = $"{CUSTOMIZED_TAG}[ER_SpecSheet]";
                                                erds.ResultName = dec.Result;
                                                erds.SheetPerChannel = true;
                                                erds.DataType = SourceDataType.Measured;
                                                if (dataexport_specsheet_first)
                                                {
                                                    erds.Append = false;
                                                    dataexport_specsheet_first = false;
                                                }
                                                else
                                                {
                                                    erds.Append = true;
                                                }

                                                if (ds.Keys.Contains(dec.ExportDataSpec_SpecSheet))
                                                {
                                                    var specpath = Path.Combine(App.CommonFileDir, ds[dec.ExportDataSpec_SpecSheet]);

                                                    if (!File.Exists(specpath))
                                                    {
                                                        throw new FileNotFoundException($"Apply Data Spec Failed. File {specpath} does not exist for {dec.ExportDataSpec_SpecSheet}");
                                                    }

                                                    erds.LoadExportSpecification(specpath, true);

                                                    //try
                                                    //{
                                                    //    erds.ExportSpecification = ds[dec.ExportDataSpec_SpecSheet];   // this exception will make AP disconnect
                                                    //}
                                                    //catch (AudioPrecision.API.APException) // No import yet;
                                                    //{
                                                        
                                                    //}
                                                }
                                                else
                                                {
                                                    erds.ExportSpecification = dec.ExportDataSpec_SpecSheet;
                                                }

                                                for (int i = 0; i < 16; i++)
                                                {
                                                    erds.SetChannelEnabled(i, false);
                                                }

                                                foreach (var ch in dec.Channels_SpecSheet)
                                                {
                                                    erds.SetChannelEnabled(int.Parse(ch.Substring(2)) - 1, true);  // Don't know if it is start from 0 or 1,
                                                }

                                                erds.FileName = "$(DataFolder)\\$(SUT_Model)\\$(SUT_Model_Option)\\$(ProjectName)\\$(SUT_Model)_$(SUT_Model_Option)_$(SUT_ID)_$(Date)_Specsheet.xlsx";   // make ProjectName instead of sequence name
                                            }
                                        }
                                    }
                                }

                                if(dec.Channels_Vacs.Count != 0)
                                {
                                    if(dec.ExportAsVacsCartesian || dec.ExportAsVacsContour)
                                    {
                                        if (!string.IsNullOrEmpty(dec.ExportDataSpec_Vacs))
                                        {
                                            if (App.GroupSetting.GlobalDefinitionGroups[GlobalDefinitionGroupName.Export_Data_Specification] is GroupItem<string> ds)
                                            {
                                                if (dec.ExportDataSpec_Vacs == "N/A")
                                                {

                                                }
                                                else
                                                {
                                                    var erds = meas.SequenceSteps.ExportResultDataSteps.Add();
                                                    erds.Name = $"{CUSTOMIZED_TAG}[ER_Vacs]";
                                                    erds.ResultName = dec.Result;
                                                    erds.SheetPerChannel = true;
                                                    if (dataexport_vacs_first)
                                                    {
                                                        erds.Append = false;
                                                        dataexport_vacs_first = false;
                                                    }
                                                    else
                                                    {
                                                        erds.Append = true;
                                                    }

                                                    erds.DataType = SourceDataType.Measured;

                                                    if (ds.Keys.Contains(dec.ExportDataSpec_Vacs))
                                                    {
                                                        var specpath = Path.Combine(App.CommonFileDir, ds[dec.ExportDataSpec_Vacs]);

                                                        if (!File.Exists(specpath))
                                                        {
                                                            throw new FileNotFoundException($"Apply Data Spec Failed. File {specpath} does not exist for {dec.ExportDataSpec_Vacs}");
                                                        }

                                                        erds.LoadExportSpecification(specpath, true);
                                                        //try
                                                        //{
                                                        //    erds.ExportSpecification = ds[dec.ExportDataSpec_Vacs];
                                                        //}
                                                        //catch (AudioPrecision.API.APException) // No import yet;
                                                        //{
                                                            
                                                        //}
                                                    }
                                                    else
                                                    {
                                                        erds.ExportSpecification = dec.ExportDataSpec_Vacs;
                                                    }

                                                    for (int i = 0; i < 16; i++)
                                                    {
                                                        erds.SetChannelEnabled(i, false);
                                                    }

                                                    foreach (var ch in dec.Channels_Vacs)
                                                    {
                                                        erds.SetChannelEnabled(int.Parse(ch.Substring(2)) - 1, true);  // Don't know if it is start from 0 or 1,
                                                    }

                                                    erds.FileName = "$(DataFolder)\\$(SUT_Model)\\$(SUT_Model_Option)\\$(ProjectName)\\$(SUT_Model)_$(SUT_Model_Option)_$(SUT_ID)_$(Date)_VACS.xlsx";

                                                    if (dec.ExportAsVacsCartesian)
                                                    {
                                                        var cmd = meas.SequenceSteps.ProgramSteps.Add();
                                                        cmd.Arguments = "\"$(DataFolder)\\$(SUT_Model)\\$(SUT_Model_Option)\\$(ProjectName)\\$(SUT_Model)_$(SUT_Model_Option)_$(SUT_ID)_$(Date)_$(Hour)h_$(Minute)m_VACS.csv\" Cartesian $(VACS_Data)";
                                                        cmd.Command = "$(APxDir)\\Utilities\\Polar Plot\\APxToVacs.exe";
                                                        cmd.Name = $"{CUSTOMIZED_TAG}[VACS_Cartesian][{dec.Result}]";
                                                    }

                                                    if (dec.ExportAsVacsContour)
                                                    {
                                                        var cmd = meas.SequenceSteps.ProgramSteps.Add();
                                                        cmd.Arguments = "\"$(DataFolder)\\$(SUT_Model)\\$(SUT_Model_Option)\\$(ProjectName)\\$(SUT_Model)_$(SUT_Model_Option)_$(SUT_ID)_$(Date)_$(Hour)h_$(Minute)m_VACS.csv\" Contour $(VACS_Data)";
                                                        cmd.Command = "$(APxDir)\\Utilities\\Polar Plot\\APxToVacs.exe";
                                                        cmd.Name = $"{CUSTOMIZED_TAG}[VACS_Contour][{dec.Result}]";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    ApxEngine.ApRef.Sequence.Report.Checked = se.EnableReport;
                    ApxEngine.ApRef.Sequence.Report.AutoSaveReport = se.EnableReport;
                    ApxEngine.ApRef.Sequence.Report.AutoSaveReportFileLocation = "$(DataFolder)\\$(SUT_Model)\\$(SUT_Model_Option)\\$(ProjectName)\\";
                    ApxEngine.ApRef.Sequence.Report.AutoSaveReportFileNamePrefix = "$(SUT_Model)_$(SUT_Model_Option)_$(SUT_ID)_$(Date)_$(Hour)h_$(Minute)m_$(SequenceName)_Report";
                    ApxEngine.ApRef.Sequence.Report.AutoSaveReportFileNameType = AutoSaveReportFileNameType.CustomPrefix;
                    ApxEngine.ApRef.Sequence.Report.AutoSaveReportFileFormat = ReportExportFormat.Text;
                    ApxEngine.ApRef.Sequence.DataOutput.WriteMeterReadingsToCsvFile = se.EnableDataOutput;
                }

                //var efs = Script.ExportFiles;

                Script.Save();
                Analyze();
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (StreamWriter sw = new StreamWriter(path))
            {
                XmlSerializer xml = new XmlSerializer(GetType());
                xml.Serialize(sw, this);
                sw.Close();
            }
            return 1;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var time = reader.GetAttribute(nameof(ScriptTime).ToLower());
            if(DateTime.TryParse(time, out DateTime readtime))
            {
                ScriptTime = readtime;
            }
            else
            {
                ScriptTime = default(DateTime);
            }

            var showinputsn = reader.GetAttribute(nameof(ShowInputSn).ToLower());
            if(!string.IsNullOrEmpty(showinputsn))
            {
                ShowInputSn = TRUE_STRING.Contains(showinputsn);
            }

            var checkready = reader.GetAttribute(nameof(CheckStartReady).ToLower());
            if (!string.IsNullOrEmpty(checkready))
            {
                CheckStartReady = TRUE_STRING.Contains(checkready);
            }
            var mergereport = reader.GetAttribute(nameof(MergeSequenceReport).ToLower());
            if (!string.IsNullOrEmpty(mergereport))
            {
                MergeSequenceReport = TRUE_STRING.Contains(mergereport);
            }

            reader.Read();

            var isempty = reader.IsEmptyElement;
            reader.ReadStartElement(nameof(SequenceExtendeds));
            
            if(!isempty)
            {
                SequenceExtendeds.Clear();
                XmlSerializer xml = new XmlSerializer(typeof(SequenceExtended));
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    var se = xml.Deserialize(reader) as SequenceExtended;
                    SequenceExtendeds.Add(se);
                }

                //reader.ReadEndElement();
            }

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(ScriptTime).ToLower(), ScriptTime.ToString());
            if (ShowInputSn) writer.WriteAttributeString(nameof(ShowInputSn).ToLower(), ShowInputSn.ToString());
            if (CheckStartReady) writer.WriteAttributeString(nameof(CheckStartReady).ToLower(), CheckStartReady.ToString());
            if (!MergeSequenceReport) writer.WriteAttributeString(nameof(MergeSequenceReport).ToLower(), MergeSequenceReport.ToString());

            writer.WriteStartElement(nameof(SequenceExtendeds));
            XmlSerializer xml = new XmlSerializer(typeof(SequenceExtended));
            foreach(var se in SequenceExtendeds)
            {
                xml.Serialize(writer, se);
            }

            writer.WriteEndElement();
        }
    }    
}
