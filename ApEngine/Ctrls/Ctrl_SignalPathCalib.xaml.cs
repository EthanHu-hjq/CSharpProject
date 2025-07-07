using ApEngine.Base;
using AudioPrecision.API;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TestCore;

namespace ApEngine.Ctrls
{
    /// <summary>
    /// Interaction logic for Ctrl_SignalPathCalib.xaml
    /// </summary>
    public partial class Ctrl_SignalPathCalib : TabControl
    {
        //public ObservableCollection<VM_EqFile> EqFiles
        //{
        //    get { return (ObservableCollection<VM_EqFile>)GetValue(EqFilesProperty); }
        //    set { SetValue(EqFilesProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for EqFiles.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty EqFilesProperty =
        //    DependencyProperty.Register("EqFiles", typeof(ObservableCollection<VM_EqFile>), typeof(Ctrl_SignalPathCalib), new PropertyMetadata(new ObservableCollection<VM_EqFile>()));



        //public VM_EqFile[] EqFiles
        //{
        //    get { return (VM_EqFile[])GetValue(EqFilesProperty); }
        //    set { SetValue(EqFilesProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for EqFiles.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty EqFilesProperty =
        //    DependencyProperty.Register("EqFiles", typeof(VM_EqFile[]), typeof(Ctrl_SignalPathCalib), new PropertyMetadata(null));



        //public DelegateCommand ChangeFile { get; }

        public Ctrl_SignalPathCalib()
        {
            //ChangeFile = new DelegateCommand(cmd_ChangeFile);

            InitializeComponent();
            DataContextChanged += Ctrl_SignalPathCalib_DataContextChanged;
        }

        private string cmd_ChangeFile(object obj)
        {
            if (obj is null) return null;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            if (obj is EqTableFile eqtab)
            {
                ofd.Filter = "EQ File|*.csv";
                if (ofd.ShowDialog() == true)
                {
                    eqtab.EqFile = ofd.FileName;
                    return ofd.FileName;
                }
            }
            else if (obj is EqCalibData eqcalib)
            {
                ofd.Filter = "EQ File|*.xlsx;*.xls";
                if (ofd.ShowDialog() == true)
                {
                    eqcalib.EqPath = ofd.FileName;
                    return ofd.FileName;
                }
            }
            else if (obj is Calib_ImpedanceThieleSmall calib_imp)
            {
                ofd.Filter = "EQ Data File|*.xlsx;*.xls;*.csv";
                if (ofd.ShowDialog() == true)
                {
                    calib_imp.CorrectionCurve = ofd.FileName;
                }
            }
            else if (obj is Calib_LoudspeakerProductionTest calib_lspt)
            {
                ofd.Filter = "EQ Data File|*.xlsx;*.xls;*.csv";
                if (ofd.ShowDialog() == true)
                {
                    calib_lspt.CorrectionCurve = ofd.FileName;
                }
            }
            return null;
        }

        private void Ctrl_SignalPathCalib_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SignalPathCalibData spcd)
            {
                //EqFiles = spcd.EqTableFiles.Select(x => new VM_EqFile(x)).ToArray();
                //spcd.ImpedanceThieleSmalls
            }
        }

        private void ChangeFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var rtn = cmd_ChangeFile(btn.DataContext);

                if (btn.Tag is TextBox tb && rtn != null)
                {
                    tb.Text = rtn;
                }
                else if (btn.Tag is DataGrid dg)
                {
                    dg.Items.Refresh();
                }
            }
        }
    }

    //public class VM_EqFile : DependencyObject
    //{
    //    public DelegateCommand ChangeFile { get; }
    //    public string Name
    //    {
    //        get { return (string)GetValue(NameProperty); }
    //        set { SetValue(NameProperty, value); }
    //    }

    //    // Using a DependencyProperty as the backing store for Name.  This enables animation, styling, binding, etc...
    //    public static readonly DependencyProperty NameProperty =
    //        DependencyProperty.Register("Name", typeof(string), typeof(VM_EqFile), new PropertyMetadata(null));


    //    public EQType EqType
    //    {
    //        get { return (EQType)GetValue(EqTypeProperty); }
    //        set { SetValue(EqTypeProperty, value); }
    //    }

    //    // Using a DependencyProperty as the backing store for EqType.  This enables animation, styling, binding, etc...
    //    public static readonly DependencyProperty EqTypeProperty =
    //        DependencyProperty.Register("EqType", typeof(EQType), typeof(VM_EqFile), new PropertyMetadata(EQType.None));

    //    public string EqFile
    //    {
    //        get { return (string)GetValue(EqFileProperty); }
    //        set { SetValue(EqFileProperty, value); }
    //    }

    //    // Using a DependencyProperty as the backing store for EqFile.  This enables animation, styling, binding, etc...
    //    public static readonly DependencyProperty EqFileProperty =
    //        DependencyProperty.Register("EqFile", typeof(string), typeof(VM_EqFile), new PropertyMetadata(null, EqfileChanged));

    //    private static void EqfileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
            
    //        if (d is VM_EqFile vm)
    //        {
    //            if (e.NewValue is string path)
    //            {
    //                try
    //                {
    //                    vm.eqtablefile.EqFile = path;
    //                }
    //                catch
    //                { }
    //            }
    //        }
    //    }

    //    EqTableFile eqtablefile;
    //    public VM_EqFile(EqTableFile file)
    //    {
    //        if (file is null) return; // TODO
    //        Name = file.StepName;
    //        EqType = file.EqType;
    //        EqFile = file.EqFile;
    //        eqtablefile = file;

    //        ChangeFile = new DelegateCommand(cmd_ChangeFile);
    //    }

    //    private void cmd_ChangeFile(object obj)
    //    {
    //        try
    //        {
    //            OpenFileDialog openFileDialog = new OpenFileDialog();

    //            openFileDialog.Filter = "EQ Table File|*.csv";
    //            openFileDialog.Title = $"Select a eq table file for {Name}";
    //            openFileDialog.Multiselect = false;

    //            if (openFileDialog.ShowDialog() == true)
    //            {
    //                if (File.Exists(openFileDialog.FileName))
    //                {
    //                    var dest = System.IO.Path.Combine(ApxEngine.CalibrationBase, System.IO.Path.GetFileName(openFileDialog.FileName));
    //                    //if (File.Exists(dest)) File.Delete(dest);
    //                    File.Copy(openFileDialog.FileName, dest, true);

    //                    eqtablefile.EqFile = EqFile = openFileDialog.FileName;
    //                }
    //            }
    //        }
    //        catch  // Permission Exception
    //        {
    //            MessageBox.Show("Change Eq File Failed");
    //        }
    //    }
    //}
}
