using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using System.Windows;
using TestCore.Data;
using TestCore;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using Toucan_WPF.UIs;
using TestCore.Services;
using TestCore.Configuration;
using ToucanCore.Engine;
using ToucanCore.Abstraction.Engine;
using ScottPlot.Statistics.Interpolation;

namespace Toucan_WPF.ViewModels
{
    public class VM_SpecEditor : DependencyObject
    {
        public TF_Spec Spec
        {
            get { return (TF_Spec)GetValue(SpecProperty); }
            set { SetValue(SpecProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Spec.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpecProperty =
            DependencyProperty.Register("Spec", typeof(TF_Spec), typeof(VM_SpecEditor), new PropertyMetadata(null));

        public ObservableCollection<VM_Limit> Limits
        {
            get { return (ObservableCollection<VM_Limit>)GetValue(LimitsProperty); }
            set { SetValue(LimitsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LimitsProperty =
            DependencyProperty.Register("Limits", typeof(ObservableCollection<VM_Limit>), typeof(VM_SpecEditor), new PropertyMetadata(null));



        public string GradeTag
        {
            get { return (string)GetValue(GradeTagProperty); }
            set { SetValue(GradeTagProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GradeTag.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GradeTagProperty =
            DependencyProperty.Register("GradeTag", typeof(string), typeof(VM_SpecEditor), new PropertyMetadata(null));



        public bool IsReadOnly { get; private set; } = true;
        

        public DelegateCommand OpenFile { get; }
        public DelegateCommand OpenSpec { get; }
        public DelegateCommand SaveFile { get; }
        public DelegateCommand SaveAsFile { get; }
        public DelegateCommand ExportAsCsv { get; }
        public DelegateCommand ExportAsResultTemplate { get; }

        public DelegateCommand OpenGoldenSampleSpec { get; }
        public DelegateCommand OpenSecondarySampleSpec { get; }

        string SourcePath { get; set; }

        public IScript Script { get; }
        public IAuthService AuthService { get; }
        public ITimeService TimeService { get; }

        public SpecType SpecType { get; }

        public VM_SpecEditor()
        {
            OpenFile = new DelegateCommand(new Action<object>(cmd_OpenFile));
            OpenSpec = new DelegateCommand(new Action<object>(cmd_OpenSpec));
            SaveFile = new DelegateCommand(new Action<object>(cmd_SaveFile));
            SaveAsFile = new DelegateCommand(new Action<object>(cmd_SaveAsFile));
            ExportAsCsv = new DelegateCommand(new Action<object>(cmd_ExportAsCsv));
            ExportAsResultTemplate = new DelegateCommand(new Action<object>(cmd_ExportAsResultTemplate));

            OpenGoldenSampleSpec = new DelegateCommand(cmd_OpenGoldenSampleSpec);
            OpenSecondarySampleSpec = new DelegateCommand(cmd_OpenSecondarySampleSpec);

            Limits = new ObservableCollection<VM_Limit>();

            Limits.CollectionChanged += LimitsCollectionChanged;
        }

        public VM_SpecEditor(IAuthService auth, ITimeService time, IScript script, SpecType type = SpecType.Normal) : this()
        {
            Script = script;

            AuthService = auth;
            TimeService = time;

            if (AuthService?.CurrentAuthType >= AuthType.Engineer)
            {
                IsReadOnly = false;
            }

            SpecType = type;
        }

        Timer Timer;

        private void LimitsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (VM_Limit item in e.OldItems)
                {
                    if (item.Parent is null)
                    {
                        Spec.Limit.Remove(item.NestLimit);
                    }
                    else
                    {
                        item.Parent.NestLimit.Remove(item.NestLimit);
                    }
                }

                if (Timer is null)
                {
                    Timer = new Timer();
                    Timer.Elapsed += UpdateUI;
                }

                Timer.Start();
            }
        }

        private void UpdateUI(object sender, ElapsedEventArgs e)
        {
            Timer?.Stop();
            Dispatcher.Invoke(() =>
            {
                Limits.Clear();

                VM_Limit.FlattenSpecLimit(Spec.Limit, Limits, null);
            });

        }

        private void cmd_ExportAsCsv(object obj)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = System.IO.Path.GetDirectoryName(SourcePath);
                sfd.Title = "Save Spec As Csv...";
                sfd.DefaultExt = "*.csv";
                sfd.Filter = "CSV|*.csv";
                sfd.AddExtension = true;

                if (sfd.ShowDialog() == true)
                {
                    using (StreamWriter sw = new StreamWriter(sfd.FileName))
                    {
                        sw.WriteLine($"Spec,{Spec.Name},Version,{Spec.Version}");
                        sw.WriteLine($"Author,{Spec.Author},Time,{Spec.Time}");
                        sw.WriteLine($"Note,{Spec.Note}");
                        sw.WriteLine($"Chk,{Spec.CheckValue}");

                        sw.WriteLine("Index,Name,USL,LSL,Unit,Format,Skip,Sfc,DefectName,DefectCode");
                        foreach (var limit in Limits)
                        {
                            var defectname = limit.GetDefectName();
                            sw.WriteLine($"{limit.Index},{limit.Limit.Name},{limit.Limit.USL},{limit.Limit.LSL},{limit.Limit.Unit},{limit.Limit.Format},{limit.Limit.Skip},{limit.Limit.Sfc},{defectname},{limit.Limit.Defect}");
                        }

                        sw.Flush();
                        sw.Close();
                        sw.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save File As Failed");
            }
        }

        private void cmd_ExportAsResultTemplate(object obj)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = System.IO.Path.GetDirectoryName(SourcePath);
                sfd.Title = "Save Result Csv Template As...";
                sfd.DefaultExt = "*.csv";
                sfd.Filter = "CSV|*.csv";
                sfd.AddExtension = true;

                if (sfd.ShowDialog() == true)
                {
                    TF_Result rs = new TF_Result(Spec);

                    rs.ExportTestDataCSV(System.IO.Path.GetDirectoryName(sfd.FileName), System.IO.Path.GetFileName(sfd.FileName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Result Template As Failed");
            }
        }


        private void cmd_SaveAsFile(object obj)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = System.IO.Path.GetDirectoryName(SourcePath);
                sfd.Title = "Save Spec As...";
                sfd.Filter = SpecFilter;
                sfd.DefaultExt = "*.xml";
                sfd.AddExtension = true;

                if (sfd.ShowDialog() == true)
                {
                    var temp = new TF_Spec(Spec.Name, Spec.Version, Spec.Limit)
                    {
                        Author = AuthService.UserName,
                        Time = TimeService.CurrentTime,
                    };

                    temp.XmlSerialize().Save(sfd.FileName);

                    FileInfo fi = new FileInfo(sfd.FileName);
                    fi.IsReadOnly = true;

                    MessageBox.Show($"Save Spec As {sfd.FileName}", "Save File OK");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save File As Failed");
            }
        }

        const string SpecFilter = "Spec XML|*.xml|Any File|*.*";
        private void cmd_SaveFile(object obj)
        {
            try
            {
                if (SpecType == SpecType.Secondary && string.IsNullOrEmpty(GradeTag))
                {
                    MessageBox.Show("Secondary Spec require a grade tag", "Warning");
                    return;
                }

                Spec.Author = AuthService.UserName;
                Spec.Time = TimeService.CurrentTime;

                if (SpecType == SpecType.Secondary)
                {
                    if (string.IsNullOrEmpty(SourcePath))
                    {
                        SourcePath = Path.Combine(Script.BaseDirectory, $"{GradeTag}_Spec.xml");
                    }
                }
                else if(SpecType == SpecType.GoldenSample)
                {
                    if (string.IsNullOrEmpty(SourcePath))
                    {
                        SourcePath = Path.Combine(Script.BaseDirectory, $"GS_Spec.xml");
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(SourcePath))
                    {
                        SourcePath = Path.Combine(Script.BaseDirectory, $"Spec.xml");
                    }
                }

                FileInfo fi = null;
                if (File.Exists(SourcePath))
                {
                    fi = new FileInfo(SourcePath);
                    fi.IsReadOnly = false;
                    Spec.XmlSerialize().Save(SourcePath);
                }
                else
                {
                    Spec.XmlSerialize().Save(SourcePath);
                    fi = new FileInfo(SourcePath);
                }

                fi.IsReadOnly = true;
                Spec.Grade = GradeTag;
                Spec.FilePath = SourcePath;

                if (SpecType == SpecType.GoldenSample)
                {
                    if (SourcePath.Contains(Script.BaseDirectory))
                    {
                        Script.SystemConfig.General.GolderSampleSpec = SourcePath.Substring(Script.BaseDirectory.Length + 1);
                    }
                    else
                    {
                        Script.SystemConfig.General.GolderSampleSpec = SourcePath;
                    }

                    Script.SystemConfig.Save();

                    MessageBox.Show($"Save Golden Sample Spec into {SourcePath}. It will be effecitive after you restart execution", "Save File OK");
                }
                else if (SpecType == SpecType.Normal)
                {
                    if (Script.SystemConfig.General.RestrictLimit == false)
                    {
                        if(MessageBox.Show("Current Restrict Limit is false, Press Yes to Continue Saving and make it True, Press No to stop saving", "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        {
                            return;
                        }

                        Script.SystemConfig.General.RestrictLimit = true;
                    }

                    if (Spec.Secondary is null)
                    {
                        Script.SystemConfig.General.SecondarySpecs.Clear();
                        Script.SystemConfig.Save();

                        MessageBox.Show($"Save Spec into {SourcePath}. It will be effecitive after you restart execution", "Save File OK");
                    }
                    else
                    {
                        if(string.IsNullOrEmpty(GradeTag))
                        {
                            MessageBox.Show("Secondary Sepc Detected. Please Input Tag for Current Spec");
                            return;
                        }

                        Script.SystemConfig.General.SecondarySpecs.Clear();
                        Script.SystemConfig.General.SecondarySpecs.Add(string.Empty, GradeTag);

                        var sec = Spec.Secondary;

                        while(sec != null)
                        {
                            if(sec.FilePath.StartsWith(Script.BaseDirectory))
                            {
                                Script.SystemConfig.General.SecondarySpecs.Add(sec.FilePath.Substring(Script.BaseDirectory.Length + 1), sec.Grade);
                            }
                            else
                            {
                                Script.SystemConfig.General.SecondarySpecs.Add(sec.FilePath, sec.Grade);
                            }
                            
                            sec = sec.Secondary;
                        }
                        Script.SystemConfig.Save();
                        MessageBox.Show($"Save Spec into {SourcePath} with {Script.SystemConfig.General.SecondarySpecs.Count-1} Secondary spec OK", "Save File OK");
                    }
                }
                else
                {
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save File Failed");
            }
        }

        private void cmd_OpenFile(object obj)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = SpecFilter;
            if (ofd.ShowDialog() == true)
            {
                var spec = System.Xml.Linq.XDocument.Load(ofd.FileName);
                var specobj = TF_Spec.XmlDeserializeWithoutCheckValue(spec.Root) as TF_Spec;

                cmd_OpenSpec(specobj);

                SourcePath = ofd.FileName;
            }
        }

        private void cmd_OpenSpec(object obj)
        {
            if (obj is TF_Spec spec)
            {
                Spec = spec;
                Limits.Clear();
                GradeTag = Spec.Grade;
                VM_Limit.FlattenSpecLimit(Spec.Limit, Limits, null);
            }
        }

        public void cmd_OpenGoldenSampleSpec(object obj)
        {
            if (SpecType == SpecType.Normal && obj is IEnumerable<VM_Limit> vmlimits)
            {
                if (Script is null)
                {
                    MessageBox.Show("Can not init Golden Sample spec in pure spec, Golden Sample spec Only works in project", "Error");
                    return;
                }

                TF_Spec gsspec;
                
                if (Script.SystemConfig.General.GolderSampleSpec is null)   // For this UI might be opened when no execution
                {
                    if (vmlimits.Count() <= 0)
                    {
                        MessageBox.Show("Please select target test items at first", "Error");
                        return;
                    }

                    Nest<TF_Limit> secondaryspec = new Nest<TF_Limit>(new TF_Limit("GoldenSampleSpec"));
                    foreach (var vmlimit in vmlimits)
                    {
                        List<TF_Limit> temp = new List<TF_Limit>();
                        var tempvm = vmlimit;
                        temp.Add(tempvm.Limit);
                        while (tempvm.Parent != null)
                        {
                            temp.Add(tempvm.Parent.Limit);
                            tempvm = tempvm.Parent;
                        }

                        Nest<TF_Limit> last = new Nest<TF_Limit>(temp[temp.Count - 1].Clone() as TF_Limit);
                        secondaryspec.Add(last);
                        for (int i = 1; i < temp.Count - 1; i++)  // Add parent
                        {
                            var newnode = new Nest<TF_Limit>(temp[temp.Count - i - 1].Clone() as TF_Limit);
                            last.Add(newnode);
                            last = newnode;
                        }

                        last.Add(new Nest<TF_Limit>(vmlimit.Limit.Clone() as TF_Limit)); // Add self

                        foreach (var item in vmlimit.NestLimit)  // Add child
                        {
                            last?.Add(item.Run((x) => { return x.Clone() as TF_Limit; }));
                        }
                    }

                    gsspec = new TF_Spec("", Spec.Version, secondaryspec);
                }
                else
                {
                    if (Path.IsPathRooted(Script.SystemConfig.General.GolderSampleSpec))
                    {
                        gsspec = TF_Spec.LoadFromXml(Script.SystemConfig.General.GolderSampleSpec);
                    }
                    else
                    {
                        gsspec = TF_Spec.LoadFromXml(Path.Combine(Script.BaseDirectory, Script.SystemConfig.General.GolderSampleSpec));
                    }
                }

                SpecEditor se = new SpecEditor(gsspec, AuthService, TimeService, Script, SpecType.GoldenSample);
                se.Title = $"Spec Editor -- Golden Sample";
                se.ShowDialog();
            }
            else
            {
                return;
            }
        }

        public void cmd_OpenSecondarySampleSpec(object obj)
        {
            if (obj is IEnumerable<VM_Limit> vmlimits)
            {
                if (Script is null)
                {
                    MessageBox.Show("Can not init secondary spec in pure spec, secondary spec Only works in project", "Error");
                    return;
                }

                TF_Spec gsspec;
                if (Spec.Secondary is null)
                {
                    if (vmlimits.Count() <= 0)
                    {
                        MessageBox.Show("Please select target test items at first", "Error");
                        return;
                    }
                    
                    Nest<TF_Limit> secondaryspec = new Nest<TF_Limit>(new TF_Limit("SecondarySpec"));
                    foreach (var vmlimit in vmlimits)
                    {
                        List<TF_Limit> temp = new List<TF_Limit>();
                        var tempvm = vmlimit;
                        temp.Add(tempvm.Limit);
                        while (tempvm.Parent != null)
                        {
                            temp.Add(tempvm.Parent.Limit);
                            tempvm = tempvm.Parent;
                        }

                        Nest<TF_Limit> last = new Nest<TF_Limit>(temp[temp.Count - 1].Clone() as TF_Limit);
                        secondaryspec.Add(last);
                        for (int i = 1; i < temp.Count - 1; i++)  // Add parent
                        {
                            var newnode = new Nest<TF_Limit>( temp[temp.Count - i - 1].Clone() as TF_Limit);
                            last.Add(newnode);
                            last = newnode;
                        }

                        last.Add(new Nest<TF_Limit>(vmlimit.Limit.Clone() as TF_Limit)); // Add self

                        foreach (var item in vmlimit.NestLimit)  // Add child
                        {
                            last?.Add(item.Run((x) => { return x.Clone() as TF_Limit; }));
                        }
                    }

                    gsspec = new TF_Spec("", Spec.Version, secondaryspec);
                }
                else
                {
                    gsspec = Spec.Secondary;
                }

                SpecEditor se = new SpecEditor(gsspec, AuthService, TimeService, Script, SpecType.Secondary);
                se.Title = $"Spec Editor -- Secondary";
                se.ShowDialog();
                
                if (!string.IsNullOrEmpty(gsspec.FilePath))
                {
                    Spec.Secondary = gsspec;

                    ScriptUtilities.AttachSecondaryLimit(Spec);
                }
            }
            else
            {
                return;
            }
        }
    }

    public enum SpecType
    {
        Normal,
        GoldenSample,
        Secondary,
    }
}
