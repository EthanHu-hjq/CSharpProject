using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using TestCore.Data;
using TestCore;
using Microsoft.Win32;
using TestCore.Services;
using System.Security.Cryptography;
using System.Windows.Shell;

namespace Toucan_WPF.ViewModels
{
    public class VM_SpecMerge : DependencyObject
    {
        public DelegateCommand OpenBase { get; set; }
        public DelegateCommand OpenTarget { get; set; }
        public DelegateCommand SaveTargetAs { get; set; }
        public DelegateCommand MergeAndSaveTargetAs { get; set; }

        public TF_Spec BaseSpec
        {
            get { return (TF_Spec)GetValue(BaseSpecProperty); }
            set { SetValue(BaseSpecProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BaseSpec.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseSpecProperty =
            DependencyProperty.Register("BaseSpec", typeof(TF_Spec), typeof(VM_SpecMerge), new PropertyMetadata(null, BaseSpecChanged));

        private static void BaseSpecChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_SpecMerge merge)
            {
                if (e.NewValue is TF_Spec spec)
                {
                    merge.BaseLimits.Clear();
                    VM_Limit.FlattenSpecLimit(spec.Limit, merge.BaseLimits, null);
                }
            }
        }

        public string BasePath
        {
            get { return (string)GetValue(BasePathProperty); }
            set { SetValue(BasePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BasePath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BasePathProperty =
            DependencyProperty.Register("BasePath", typeof(string), typeof(VM_SpecMerge), new PropertyMetadata("*"));

        public string BaseFileName
        {
            get { return (string)GetValue(BaseFileNameProperty); }
            set { SetValue(BaseFileNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BaseFileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseFileNameProperty =
            DependencyProperty.Register("BaseFileName", typeof(string), typeof(VM_SpecMerge), new PropertyMetadata("*"));


        public TF_Spec TargetSpec
        {
            get { return (TF_Spec)GetValue(TargetSpecProperty); }
            set { SetValue(TargetSpecProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetSpec.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetSpecProperty =
            DependencyProperty.Register("TargetSpec", typeof(TF_Spec), typeof(VM_SpecMerge), new PropertyMetadata(null, TargetSpecChanged));

        private static void TargetSpecChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_SpecMerge merge)
            {
                if (e.NewValue is TF_Spec spec)
                {
                    merge.TargetLimits.Clear();
                    VM_Limit.FlattenSpecLimit(spec.Limit, merge.TargetLimits, null);

                    merge.Compare(merge.BaseLimits, merge.TargetLimits);
                }
            }
        }

        public string TargetPath
        {
            get { return (string)GetValue(TargetPathProperty); }
            set { SetValue(TargetPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetPathProperty =
            DependencyProperty.Register("TargetPath", typeof(string), typeof(VM_SpecMerge), new PropertyMetadata("*"));


        public string TargetFileName
        {
            get { return (string)GetValue(TargetFileNameProperty); }
            set { SetValue(TargetFileNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetFileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetFileNameProperty =
            DependencyProperty.Register("TargetFileName", typeof(string), typeof(VM_SpecMerge), new PropertyMetadata("*"));




        public ObservableCollection<VM_Limit> BaseLimits
        {
            get { return (ObservableCollection<VM_Limit>)GetValue(BaseLimitsProperty); }
            set { SetValue(BaseLimitsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BaseLimit.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseLimitsProperty =
            DependencyProperty.Register("BaseLimits", typeof(ObservableCollection<VM_Limit>), typeof(VM_SpecMerge), new PropertyMetadata(new ObservableCollection<VM_Limit>()));

        public ObservableCollection<VM_Limit> TargetLimits
        {
            get { return (ObservableCollection<VM_Limit>)GetValue(TargetLimitsProperty); }
            set { SetValue(TargetLimitsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetLimit.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetLimitsProperty =
            DependencyProperty.Register("TargetLimits", typeof(ObservableCollection<VM_Limit>), typeof(VM_SpecMerge), new PropertyMetadata(new ObservableCollection<VM_Limit>()));

        public string Prefix
        {
            get { return (string)GetValue(PrefixProperty); }
            set { SetValue(PrefixProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Prefix.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register("Prefix", typeof(string), typeof(VM_SpecMerge), new PropertyMetadata(null));



        public static IAuthService AuthService = ServiceStatic.ToolboxService().GetService<IAuthService>();
        public static ITimeService TimeService = ServiceStatic.ToolboxService().GetService<ITimeService>();

        public VM_SpecMerge()
        {
            OpenBase = new DelegateCommand(new Action<object>(cmd_OpenBase));
            OpenTarget = new DelegateCommand(new Action<object>(cmd_OpenTarget));
            SaveTargetAs = new DelegateCommand(new Action<object>(cmd_SaveTargetAs));
            MergeAndSaveTargetAs = new DelegateCommand(new Action<object>(cmd_MergeIntoBaseAs));
        }

        public VM_SpecMerge(TF_Spec original, TF_Spec target, string prefix) : this()
        {
            cmd_OpenBase(original);
            cmd_OpenTarget(target);

            if (string.IsNullOrEmpty(prefix))
            {
                Prefix = TestCore.Configuration.GlobalConfiguration.Default.General.Prefix_DefectCode;
            }
            else
            {
                Prefix = prefix;
            }
        }

        private void cmd_MergeIntoBaseAs(object obj)
        {
            //foreach (var limit in TargetLimits)
            //{
            //    if (limit.IsAdded)
            //    {
            //        if (limit.Parent is null)
            //        {
            //            BaseSpec.Limit.Add(limit.Limit);
            //        }
            //        else
            //        {
            //            var d = VM_Limit.FetchLimit(BaseSpec.Limit, limit);
            //            d.Add(limit.Limit);
            //        }
            //    }
            //}

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = System.IO.Path.GetDirectoryName(TargetPath ?? BaseSpec.FilePath);
            //sfd.FileName = System.IO.Path.GetFileName(TargetPath);
            sfd.Title = "Save Spec As...";
            sfd.Filter = "XML|*.xml";
            sfd.DefaultExt = "*.xml";
            sfd.AddExtension = true;
            sfd.FileName = Path.GetFileName(TargetPath ?? BaseSpec.FilePath);

            var exist = File.Exists(BaseSpec.FilePath);
            string temppath = null;
            if (exist)
            {
                temppath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                FileInfo fi = new FileInfo(BaseSpec.FilePath);
                fi.IsReadOnly = false;
                File.Move(BaseSpec.FilePath, temppath);
            }

            if (sfd.ShowDialog() == true)
            {
                var mergespec = TF_Spec.Merge(BaseSpec, TargetSpec, Prefix);
                mergespec.Author = AuthService?.Name ?? "PTE";
                mergespec.Time = TimeService?.CurrentTime ?? DateTime.Now;
                var xml = mergespec.XmlSerialize();

                xml.Add(new XComment("Merged Spec"));

                xml.Save(sfd.FileName);

                FileInfo fi = new FileInfo(sfd.FileName);
                fi.IsReadOnly = true;
            }
            else
            {
                if (exist)
                {
                    File.Move(temppath, BaseSpec.FilePath);
                    FileInfo fi = new FileInfo(BaseSpec.FilePath);
                    fi.IsReadOnly = true;
                }
            }
        }

        private void cmd_SaveTargetAs(object obj)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = System.IO.Path.GetDirectoryName(TargetPath);
            sfd.FileName = System.IO.Path.GetFileName(TargetPath);
            sfd.Title = "Save Spec As...";
            sfd.Filter = "XML|*.xml";
            sfd.DefaultExt = "*.xml";
            sfd.AddExtension = true;

            if (sfd.ShowDialog() == true)
            {
                TargetSpec.Author = AuthService?.Name ?? "PTE";
                TargetSpec.Time = TimeService?.CurrentTime ?? DateTime.Now;
                TargetSpec.XmlSerialize().Save(sfd.FileName);
            }
        }

        private void cmd_OpenTarget(object obj)
        {
            if (obj is TF_Spec spec)
            {
                TargetSpec = spec;

                TargetPath = spec.FilePath;
                TargetFileName = System.IO.Path.GetFileName(spec.FilePath);
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Spec XML|*.xml|Any FIle|*.*";
                if (ofd.ShowDialog() == true)
                {
                    try
                    {
                        var specxml = System.Xml.Linq.XDocument.Load(ofd.FileName);
                        TargetSpec = TF_Spec.XmlDeserializeWithoutCheckValue(specxml.Root) as TF_Spec;
                        //TargetSpec = TF_Spec.LoadFromXml(ofd.FileName);

                        TargetPath = ofd.FileName;
                        TargetFileName = System.IO.Path.GetFileName(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Load Target Spec Failed", ex.ToString());
                    }
                }
            }
        }

        private void cmd_OpenBase(object obj)
        {
            if (obj is TF_Spec spec)
            {
                BaseSpec = spec;

                BasePath = spec.FilePath;
                BaseFileName = System.IO.Path.GetFileName(spec.FilePath);
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Spec XML|*.xml|Any FIle|*.*";

                if (ofd.ShowDialog() == true)
                {
                    try
                    {
                        BaseSpec = TF_Spec.LoadFromXml(ofd.FileName);

                        BasePath = ofd.FileName;
                        BaseFileName = System.IO.Path.GetFileName(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Load Base Spec Failed", ex.ToString());
                    }
                }
            }
        }

        private void Compare(IList<VM_Limit> origin, IList<VM_Limit> target)
        {
            int init_idx = 0;
            for (int i = 0; i < origin.Count(); i++)
            {
                bool found = false;

                var tempb = origin.ElementAt(i);
                for (int j = init_idx; j < target.Count(); j++)
                {
                    var tempt = target.ElementAt(j);
                    if (tempt.IsSameName(tempb))
                    {
                        found = true;

                        if (tempt?.Limit?.Defect != tempb?.Limit?.Defect)
                        {
                            origin.ElementAt(i).IsChanged = true;
                            target.ElementAt(j).IsChanged = true;
                            //target.ElementAt(j).Limit.Defect = tempb?.Limit?.Defect;
                        }

                        for (int k = init_idx; k < j; k++)
                        {
                            target.ElementAt(k).IsAdded = true;
                            origin.Insert(i, new VM_Limit(null, null, target.ElementAt(k).Parent));
                            i++;
                        }

                        init_idx = j + 1;
                        break;
                    }
                }

                if (!found)
                {
                    target.Insert(i, new VM_Limit(null, null, origin.ElementAt(i).Parent));

                    for (int j = 0; j < init_idx - 1; j++)
                    {
                        var tempt = target.ElementAt(j);
                        if (tempt.IsSameName(tempb))
                        {
                            tempt.IsMoved = true;
                            found = true;
                        }
                    }

                    if (found)
                    {
                        tempb.IsMoved = true;
                    }
                    else
                    {
                        tempb.IsRemoved = true;
                    }

                    init_idx++;
                }
            }
        }

        private TF_Spec Merge(IList<VM_Limit> origin, IList<VM_Limit> target)
        {

            return null;
        }
    }
}
