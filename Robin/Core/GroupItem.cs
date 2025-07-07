using Robin.UIs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using TestCore;
using TestCore.Base;

namespace Robin.Core
{
    public interface IGroupItem
    {
        ///// <summary>
        ///// Source for the group items
        ///// </summary>
        //string Source { get; set; }

        /// <summary>
        /// the Generator source which for generator the Group Data
        /// </summary>
        string Generator { get; set; }

        string Note { get; }

        /// <summary>
        /// Load items from Source
        /// </summary>
        /// <returns></returns>
        int Load();

        /// <summary>
        /// Generate the Source.
        /// </summary>
        /// <returns></returns>
        int Generate();

        /// <summary>
        /// Save items into Source
        /// </summary>
        /// <returns></returns>
        int Save();
    }

    /// <summary>
    /// Group Item is for app implement a customizable options for Users 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [XmlRoot("GroupItem")]
    public class GroupItem<T> : IGroupItem, IReadOnlyDictionary<string, T>, System.Xml.Serialization.IXmlSerializable
    {
        public static string FilePath { get; set; }
        public static string Workbase { get; set; }

        protected Dictionary<string, T> _Items = new Dictionary<string, T>();
        public IReadOnlyDictionary<string, T> Items { get => _Items; }

        public T this[string key] => ((IReadOnlyDictionary<string, T>)_Items)[key];

        public string Generator { get; set; }
        public string Note { get; set; }

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, T>)_Items).Keys;

        public IEnumerable<T> Values => ((IReadOnlyDictionary<string, T>)_Items).Values;

        public int Count => ((IReadOnlyCollection<KeyValuePair<string, T>>)_Items).Count;

        public bool ContainsKey(string key)
        {
            return ((IReadOnlyDictionary<string, T>)_Items).ContainsKey(key);
        }

        public int Generate() { return 1; }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, T>>)_Items).GetEnumerator();
        }

        public int Load()
        {
            _Items.Clear();

            string path = null;
            if (FilePath.Contains(":"))
            {
                path = FilePath;
            }
            else
            {
                path = Path.Combine(Workbase, FilePath);
            }

            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    var d = XmlSerializerHelper.Deserialize(sr.ReadToEnd(), typeof(GroupItem<T>)) as GroupItem<T>;

                    foreach (var item in d.Items)
                    {
                        _Items.Add(item.Key, item.Value);
                    }
                }
            }
            else
            {
                return 0;
            }

            return 1;
        }

        public int Save()
        {
            string path = null;
            if (FilePath.Contains(":"))
            {
                path = FilePath;
            }
            else
            {
                path = Path.Combine(Workbase, FilePath);
            }

            var content = XmlSerializerHelper.Serialize(this);

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(content);
                }
            }
            catch
            {
                return 0;
            }

            return 1;
        }


        public bool TryGetValue(string key, out T value)
        {
            return ((IReadOnlyDictionary<string, T>)_Items).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_Items).GetEnumerator();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool isEmpty = reader.IsEmptyElement;

            if (reader.Name != nameof(GroupItem)) return;

            Generator = reader.GetAttribute("generator");
            Note = reader.GetAttribute("note");

            reader.ReadStartElement(nameof(GroupItem)); // no matter the element is empty, need reader the element,

            if (!isEmpty)
            {
                reader.ReadStartElement(nameof(Items));

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    var key = reader.GetAttribute("key");

                    XmlSerializer valueserializer = null;
                    var temptype = typeof(T);

                    if (temptype.IsAbstract || temptype.IsInterface)
                    {
                        var type = reader.GetAttribute("type");
                        var assembles = AppDomain.CurrentDomain.GetAssemblies();

                        temptype = Type.GetType(type);
                        foreach (var asm in assembles)
                        {
                            temptype = asm.GetType(type);
                            if (temptype != null)
                            {
                                break;
                            }
                        }
                    }

                    reader.ReadStartElement("Item");

                    valueserializer = new XmlSerializer(temptype);
                    var val = valueserializer.Deserialize(reader);

                    _Items.Add(key, (T)val);

                    reader.ReadEndElement();
                }

                reader.ReadEndElement();
                
                reader.ReadEndElement();   // if the element is empty, the reader start element will make the element goto parent. So do not read end.
            }
            
            reader.MoveToContent();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (Generator != null)
            {
                writer.WriteAttributeString(nameof(Generator).ToLower(), Generator);
            }
            if(Note != null)
            {
                writer.WriteAttributeString(nameof(Note).ToLower(), Note);
            }

            if (Items.Count > 0)
            {
                writer.WriteStartElement(nameof(Items));

                foreach (var item in Items)
                {
                    writer.WriteStartElement("Item");
                    writer.WriteAttributeString("key", item.Key);

                    var temptype = typeof(T);

                    if (temptype.IsAbstract || temptype.IsInterface)
                    {
                        writer.WriteAttributeString("type", item.Value.GetType().FullName);
                    }

                    XmlSerializer valueserializer = new XmlSerializer(item.Value.GetType());
                    valueserializer.Serialize(writer, item.Value);

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }
    }

    public class ObservableGroupItem<T> : DependencyObject
    {
        public GroupItem<T> GroupItem { get; }

        public string Generator
        {
            get { return (string)GetValue(GeneratorProperty); }
            set { SetValue(GeneratorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Generator.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GeneratorProperty =
            DependencyProperty.Register("Generator", typeof(string), typeof(ObservableGroupItem<T>), new PropertyMetadata(null));

        public string Note
        {
            get { return (string)GetValue(NoteProperty); }
            set { SetValue(NoteProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Note.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NoteProperty =
            DependencyProperty.Register("Note", typeof(string), typeof(ObservableGroupItem<T>), new PropertyMetadata(null));



        public ObservableCollection<KeyValueObject<string, T>> Items
        {
            get { return (ObservableCollection<KeyValueObject<string, T>>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("ItemsProperty", typeof(ObservableCollection<KeyValueObject<string, T>>), typeof(ObservableGroupItem<T>), new PropertyMetadata(null));

        public DelegateCommand Save { get; }
        public DelegateCommand Generate { get; }

        public ObservableGroupItem(GroupItem<T> item)
        {
            GroupItem = item;
            Generate = new DelegateCommand(cmd_Generate);
            Save = new DelegateCommand(cmd_Save);

            Generator = item.Generator;
            Note = item.Note;
            Items = new ObservableCollection<KeyValueObject<string, T>>();

            foreach (var i in item)
            {
                Items.Add(new KeyValueObject<string, T>() { Key = i.Key, Value = i.Value });
            }
        }

        private void cmd_Save(object obj)
        {
            try
            {
                GroupItem.Generator = Generator;
                GroupItem.Note = Note;

                var dict = GroupItem.Items as Dictionary<string, T>;

                dict.Clear();

                foreach (var item in Items)
                {
                    dict.Add(item.Key, item.Value);
                }
            }
            catch
            { }
        }

        private void cmd_Generate(object obj)
        {
            MessageBox.Show("Reserved");
        }
    }
}
