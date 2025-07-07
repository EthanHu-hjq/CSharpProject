using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TestCore.Data;

namespace ToucanCore.Engine
{
    //public class InjectVariable<T> : CheckableVariable<T[]>
    //{
    //    public string Name { get; set; }
    //    public bool IsMes { get; set; }
    //    public T[] Value { get; set; }

    //    public InjectVariable() : this("", null, false) { }

    //    public InjectVariable(string name, T[] value, bool isMes)
    //    {
    //        Name = name;
    //        Value = value;
    //        IsMes = isMes;
    //    }

    //    public InjectVariable(string name, T[] value) : this(name, value, false) { }
    //}

    //public sealed class InjectedVariableTable : List<CheckableVariable<string[]>>
    //{
    //    public const string DefaultName = "InjectedVariableTable";
    //    public string FilePath { get; private set; }

    //    public int SocketCount { get; }

    //    public InjectedVariableTable() { }
    //    public InjectedVariableTable(int socketcount) : this() 
    //    {
    //        SocketCount = socketcount;
    //        //Add("VarName", new string[socketcount]);
    //    }

    //    public static InjectedVariableTable LoadFromCsv(string path)
    //    {
    //        InjectedVariableTable keyValuePairs = null;

    //        using (StreamReader sr = new StreamReader(path))
    //        {
    //            var firstline = sr.ReadLine().Split(',');

    //            if (firstline.Contains("IsMes"))
    //            {
    //                var socketcount = int.Parse(firstline[3]);
    //                keyValuePairs = new InjectedVariableTable(socketcount);
    //                while (!sr.EndOfStream)
    //                {
    //                    var line = sr.ReadLine().Split(',');
    //                    var data = line.Skip(3).ToArray();
    //                    if (data.Length != socketcount)
    //                    {
    //                        throw new InvalidDataException("Injected Variable Table Data Count does not match declaration. \r\nProbably there is comma(,) in value, if so please delete the csv and save a xml one");
    //                    }
    //                    else
    //                    {
    //                        keyValuePairs.Add(new CheckableVariable<string[]>(line[0], data, TestCore.TF_Base.TRUE_STRING.Contains(line[1])));
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                var socketcount = int.Parse(firstline[2]);
    //                keyValuePairs = new InjectedVariableTable(socketcount);
    //                while (!sr.EndOfStream)
    //                {
    //                    var line = sr.ReadLine().Split(',');
    //                    var data = line.Skip(2).ToArray();
    //                    if (data.Length != socketcount)
    //                    {
    //                        throw new InvalidDataException("Injected Variable Table Data Count does not match declaration. \r\nProbably there is comma(,) in value, if so please delete the csv and save a xml one");
    //                    }
    //                    else
    //                    {
    //                        keyValuePairs.Add(new CheckableVariable<string[]>(line[0], data));
    //                    }
    //                }
    //            }
    //        }
    //        keyValuePairs.FilePath = path;
    //        return keyValuePairs;
    //    }

    //    public void SaveAsCsv(string path)
    //    {
    //        using(StreamWriter sw = new StreamWriter(path))
    //        {
    //            sw.WriteLine($"Name,Type,IsMes,{SocketCount}");

    //            foreach(var item in this)
    //            {
    //                if (string.IsNullOrWhiteSpace(item.Name)) continue;
    //                sw.WriteLine($"{item.Name},,{item.IsChecked},{string.Join(",", item.Value)}");
    //            }

    //            sw.Flush();
    //            sw.Close();

    //            FilePath = path;
    //        }
    //    }

    //    public static InjectedVariableTable LoadFromXml(string path)
    //    {
            
    //        XDocument doc = XDocument.Load(path);
    //        var elem = doc.Element(nameof(InjectedVariableTable));

    //        var count = int.Parse(elem.Attribute("socketcount")?.Value);
    //        InjectedVariableTable keyValuePairs = new InjectedVariableTable(count);
    //        var itemelems = elem.Elements("Item");
    //        foreach (var itemelem in itemelems)
    //        {
    //            var name = itemelem.Attribute("name").Value;
    //            var ismesstr = itemelem.Attribute("mes")?.Value;
    //            bool ismes = false;
    //            if (TestCore.TF_Base.TRUE_STRING.Contains(ismesstr))
    //            {
    //                ismes = true;
    //            }

    //            var slotvalues = itemelem.Elements("Value");
    //            //if(keyValuePairs.ContainsKey(name))
    //            //{
    //            //    keyValuePairs[name] = new string[count];

    //            //    for (int i = 0; i < count; i++)
    //            //    {
    //            //        keyValuePairs[name][i] = slotvalues.ElementAt(i).Value;
    //            //    }
    //            //}
    //            //else
    //            //{
    //                var slotval = new string[count];
    //                for (int i = 0; i < count; i++)
    //                {
    //                    slotval[i] = slotvalues.ElementAt(i).Value;
    //                }

    //                keyValuePairs.Add(new CheckableVariable<string[]>(name, slotval, ismes));
    //            //}
    //        }

    //        keyValuePairs.FilePath = path;
    //        return keyValuePairs;
    //    }

    //    public void SaveAsXml(string path)
    //    {
    //        XElement element = new XElement(nameof(InjectedVariableTable));

    //        element.Add(new XAttribute("socketcount", SocketCount));

    //        foreach(var item in this)
    //        {
    //            if (string.IsNullOrWhiteSpace(item.Name)) continue;
    //            var elemitem = new XElement("Item");
    //            elemitem.Add(new XAttribute("name", item.Name));
    //            if(item.IsChecked) elemitem.Add(new XAttribute("mes", item.IsChecked));
    //            foreach (var val in item.Value)
    //            {
    //                elemitem.Add(new XElement("Value", val));
    //            }

    //            element.Add(elemitem);
    //        }

    //        element.Save(path);

    //        FilePath = path;
    //    }

    //    public static InjectedVariableTable Load(string path)
    //    {
    //        if(path.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
    //        {
    //            return LoadFromCsv(path);
    //        }
    //        else
    //        {
    //            return LoadFromXml(path);
    //        }
    //    }

    //    public static InjectedVariableTable GetFromWorkbase(string dir)
    //    {
    //        var path = Path.Combine(dir, $"{DefaultName}.xml");

    //        if(File.Exists(path))
    //        {
    //            return LoadFromXml(path);
    //        }
    //        else
    //        {
    //            path = Path.Combine(dir, $"{DefaultName}.csv");
    //            if (File.Exists(path))
    //            {
    //                return LoadFromCsv(path);
    //            }
    //            return null;
    //        }
    //    }
    //}
}
