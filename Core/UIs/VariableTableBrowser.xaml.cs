using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
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
using TestCore.Data;
using ToucanCore.Engine;
using ToucanCore.HAL;
using ToucanCore.Abstraction.Engine;

namespace ToucanCore.UIs
{
    /// <summary>
    /// VariableTable.xaml 的交互逻辑
    /// </summary>
    public partial class VariableTableBrowser : Window
    {
        public ObservableCollection<CheckableVariable<string[]>> VariableItems
        {
            get { return (ObservableCollection<CheckableVariable<string[]>>)GetValue(VariableItemsProperty); }
            set { SetValue(VariableItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VariableItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VariableItemsProperty =
            DependencyProperty.Register("VariableItems", typeof(ObservableCollection<CheckableVariable<string[]>>), typeof(VariableTableBrowser), new PropertyMetadata(null));

        public InjectedVariableTable VariableTable { get; private set; }

        public string Workbase { get; set; }

        public int SocketCount { get; }

        public VariableTableBrowser(InjectedVariableTable svt, int socketcount)
        {
            InitializeComponent();

            VariableTable = svt;

            dg.Columns.Add(new DataGridTextColumn() { Header = "Name", IsReadOnly = false, Binding = new Binding("Name") });
            dg.Columns.Add(new DataGridCheckBoxColumn { Header = "MES", Binding = new Binding("IsChecked") });
            //dg.Columns.Add(new DataGridTextColumn() { Header = "Value", IsReadOnly = true, Binding = new Binding($"Value") });
            SocketCount = socketcount;
            for (int i = 0; i < SocketCount; i++)
            {
                dg.Columns.Add(new DataGridTextColumn() { Header = i.ToString(), IsReadOnly = false, Binding = new Binding($"Value[{i}]") });
            }

            VariableItems = new ObservableCollection<CheckableVariable<string[]>>();

            LoadVariableTable(svt);
        }

        public VariableTableBrowser(InjectedVariableTable svt) : this(svt, svt.SocketCount)
        {
        }

        public VariableTableBrowser(int socketcount, string path = null) : this(new InjectedVariableTable(socketcount))
        {
        }

        public void LoadVariableTable(InjectedVariableTable svt)
        {
            VariableItems.Clear();

            if(SocketCount == svt.SocketCount)
            {
                if (svt.Count == 0)
                {
                    VariableItems.Add(new CheckableVariable<string[]>("VarName", new string[SocketCount]));
                }
                else
                {
                    foreach (var item in svt)
                    {
                        VariableItems.Add(item);
                    }
                }
                VariableTable = svt;
            }
            else
            {
                var cnt = Math.Min(SocketCount, svt.SocketCount);

                InjectedVariableTable newsvt = new InjectedVariableTable(SocketCount);

                if (svt.Count == 0)
                {
                    VariableItems.Add(new CheckableVariable<string[]>("VarName", new string[SocketCount]));
                }
                else
                {
                    foreach (var item in svt)
                    {
                        var newitem = new CheckableVariable<string[]>(item.Name, new string[SocketCount], item.IsChecked);

                        for (int i = 0; i < cnt; i++)
                        {
                            newitem.Value[i] = item.Value[i];
                        }

                        newsvt.Add(newitem);
                        VariableItems.Add(newitem);
                    }
                }

                VariableTable = newsvt;
            }
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(VariableTable.FilePath))
            {
                Button_SaveAs_Click(sender, e);
                return;
            };

            VariableTable.Clear();
            foreach (var item in VariableItems)
            {
                VariableTable.Add(item);
                //if(VariableTable.ContainsKey(item.Name))
                //{
                //    VariableTable[item.Name] = item.Value;
                //}
                //else
                //{
                //    VariableTable.Add(item.Name, item.Value);
                //}
            }

            if (VariableTable.FilePath.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
            {
                VariableTable.SaveAsCsv(VariableTable.FilePath);
            }
            else
            {
                VariableTable.SaveAsXml(VariableTable.FilePath);
            }
        }

        private void Button_SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "csv";
            sfd.Filter = Filter;
            sfd.FileName = InjectedVariableTable.DefaultName;

            if(Directory.Exists(Workbase))
            {
                sfd.InitialDirectory = Workbase;
            }

            if (sfd.ShowDialog() == true)
            {
                VariableTable.Clear();
                foreach (var item in VariableItems)
                {
                    VariableTable.Add(item);
                }

                if (sfd.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    foreach(var item in VariableTable)
                    {
                        if (item.Name.Contains(',') || item.Value.Contains(","))
                        {
                            MessageBox.Show($"Comma in {item.Name} -> {item.Value} in invalid in CSV format. Please save it as XML format", "Error");
                            return;
                        }
                    }

                    VariableTable.SaveAsCsv(sfd.FileName);
                }
                else
                {
                    VariableTable.SaveAsXml(sfd.FileName);
                }

                MessageBox.Show($"Variable Table save as {sfd.FileName} ok", "Info");
            }
        }

        private void dg_AddNewItem(object sender, AddingNewItemEventArgs e)
        {
            if(sender is DataGrid)
            {
                e.NewItem = new CheckableVariable<string[]>("", new string[VariableTable.SocketCount]);
            }
        }

        const string Filter = "CSV File|*.csv|XML File|*.xml";
        private void Button_Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = Filter;
                ofd.Multiselect = false;

                if (ofd.ShowDialog() == true)
                {
                    if (ofd.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                    {
                        var vt = InjectedVariableTable.LoadFromCsv(ofd.FileName);
                        LoadVariableTable(vt);
                    }
                    else
                    {
                        var vt = InjectedVariableTable.LoadFromXml(ofd.FileName);
                        LoadVariableTable(vt);
                    }
                }
            }
            catch { }
        }
    }

    //public class VariableItem
    //{
    //    public string Name { get; set; }
    //    public bool IsMes { get; set; }
    //    public string[] Value { get; set; }
    //}
}
