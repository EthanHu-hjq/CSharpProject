using NationalInstruments.TestStand.Interop.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace TsEngine.UIs
{
    /// <summary>
    /// Interaction logic for Variables_Browser.xaml
    /// </summary>
    public partial class Variables_Browser : Window
    {        
        public ObservableCollection<Variable> Variables
        {
            get { return (ObservableCollection<Variable>)GetValue(VariablesProperty); }
            set { SetValue(VariablesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Variables.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VariablesProperty =
            DependencyProperty.Register("Variables", typeof(ObservableCollection<Variable>), typeof(Variables_Browser), new PropertyMetadata(new ObservableCollection<Variable>()));



        public Variables_Browser()
        {
            InitializeComponent();
        }

        public void Refresh(Execution exec, int slot)
        {
            Dispatcher.Invoke(() =>
            {
                SequenceContext sc = exec.SlotSequenceContexts[slot];
                Title = $"{exec.Name} Slot {slot}";
                Variables.Clear();
                var d = sc.FileGlobals.GetSubProperties("", 0);

                for (int i = 0; i < d.Length; i++)
                {
                    var name = d[i].Name;
                    var type = d[i].Type.ValueType;

                    switch (type)
                    {
                        case PropertyValueTypes.PropValType_String:
                            Variables.Add(new Variable() { Name = name, Type = "string", Value = d[i].GetValString("", 0) });
                            break;

                        case PropertyValueTypes.PropValType_Boolean:
                            Variables.Add(new Variable() { Name = name, Type = "Boolean", Value = d[i].GetValBoolean("", 0) });
                            break;
                        case PropertyValueTypes.PropValType_Number:
                            Variables.Add(new Variable() { Name = name, Type = "Number", Value = d[i].GetValNumber("", 0) });
                            break;

                        case PropertyValueTypes.PropValType_Enum:
                            Variables.Add(new Variable() { Name = name, Type = "string", Value = d[i].GetValueDisplayName("", 0) });
                            break;
                    }

                    var value = d[i].GetValString("", 0);
                }
            });
        }
    }

    public class Variable
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
    }
}
