using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ToucanCore.Misc
{
    public class EngineHelper
    {
        public static string SnRuleToRegularExString(string rule)
        {
            if (rule.Contains(@"\"))
            {
                if (!rule.StartsWith("^"))
                {
                    if (rule.EndsWith("$"))
                    {
                        rule = $"^{rule}";
                    }
                    else
                    {
                        rule = $"^{rule}$";
                    }
                }

                return rule;
            }
            else if (int.TryParse(rule, out int len))
            {
                return $"^(\\w{{{len}}}|\\w{{2}})$";
            }
            else
            {
                return $"^(\\w{{2}}|{rule}\\w+)$";
            }
        }
    }
}
