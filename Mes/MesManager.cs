using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using TestCore.Abstraction.Process;

namespace Mes
{
    public sealed class MesManager
    {
        private static Dictionary<Type, IMes> _MesDictionary;
        public static IReadOnlyDictionary<Type, IMes> MesDictionary
        {
            get
            {
                if (_MesDictionary is null)
                {
                    LoadMesLib();
                }
                return _MesDictionary;
            }
        }

        public static IMes GetMesInstance(TestCore.Location location, string vendorname = null)
        {
            IMes mes = null;
            switch (location)
            {
                case Location.TYDG:
                case Location.TYHZ:
                case Location.TYDC:
                case Location.TYTH:
                    mes = MesDictionary[typeof(TYMSFC)];
                    if (mes is null)
                    {
                        mes = _MesDictionary[typeof(TYMSFC)] = new TYMSFC();
                    }
                    break;

                case Location.PRIMAX:
                    mes = MesDictionary[typeof(PrimaxSFC)];
                    if (mes is null)
                    {
                        mes = _MesDictionary[typeof(PrimaxSFC)] = new PrimaxSFC();
                    }
                    break;

                case TestCore.Location.Vendor:

                    var mestype = MesDictionary.Keys.FirstOrDefault(x => x.Name.StartsWith(vendorname));
                    if (mestype is null)
                    {
                        if (vendorname.Equals("PRIMAX TH", StringComparison.OrdinalIgnoreCase))
                        {
                            mes = _MesDictionary[typeof(PrimaxWebApi)] = new PrimaxWebApi() { Root_URL = "http://10.80.1.27:9527/SMT/api/" };
                        }
                        else
                        {
                            TF_Base.StaticLog($"MesManager: Get Mes Type Failed. No MES Driver for {vendorname}");
                            mes = null;
                        }
                    }
                    else
                    {
                        mes = MesDictionary[mestype];
                        if (mes is null)
                        {
                            mes = _MesDictionary[mestype] = Activator.CreateInstance(mestype) as IMes;
                        }
                    }
                    break;

                default:
                    mes = null;
                    TF_Base.StaticLog($"MesManager: Get Mes Type Failed. No MES Driver for {location}");
                    break;
            }

            return mes;
        }

        private static void LoadMesLib()
        {
            _MesDictionary = new Dictionary<Type, IMes>
            {
                { typeof(TYMSFC), null },
                { typeof(PrimaxSFC), null },
                { typeof(PrimaxWebApi), null }
            };

            var path = Path.Combine(AppContext.BaseDirectory, "Mes");

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.dll");
                foreach(var f in files)
                {
                    TF_Utility.GetPlugIn<IMes>(f, out List<Type> mesplugins);

                    foreach (var mes in mesplugins)
                    {
                        _MesDictionary.Add(mes, null);
                    }
                }
                
            }
        }
    }
}
