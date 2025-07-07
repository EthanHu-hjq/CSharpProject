using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TestCore.Data;
using TestCore;
using TestCore.Abstraction.Base;

namespace Toucan_WPF.ViewModels
{
    public class VM_Limit : DependencyObject
    {
        public string Index { get; }
        public TF_Limit Limit => NestLimit?.Element;
        public string USL { get => Limit?.USL?.ToString(); set => Limit.USL = value.StringToIComparable(Limit.LimitType); }
        public string LSL { get => Limit?.LSL?.ToString(); set => Limit.LSL = value.StringToIComparable(Limit.LimitType); }

        public VM_Limit Parent { get; set; }

        public Nest<TF_Limit> NestLimit { get; }

        public bool IsRemoved
        {
            get { return (bool)GetValue(IsRemovedProperty); }
            set { SetValue(IsRemovedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsRemoved.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsRemovedProperty =
            DependencyProperty.Register("IsRemoved", typeof(bool), typeof(VM_Limit), new PropertyMetadata(false));



        public bool IsAdded
        {
            get { return (bool)GetValue(IsAddedProperty); }
            set { SetValue(IsAddedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAdded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAddedProperty =
            DependencyProperty.Register("IsAdded", typeof(bool), typeof(VM_Limit), new PropertyMetadata(false));

        public bool IsChanged
        {
            get { return (bool)GetValue(IsChangedProperty); }
            set { SetValue(IsChangedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsChanged.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsChangedProperty =
            DependencyProperty.Register("IsChanged", typeof(bool), typeof(VM_Limit), new PropertyMetadata(false));

        public bool IsMoved
        {
            get { return (bool)GetValue(IsMovedProperty); }
            set { SetValue(IsMovedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMoved.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMovedProperty =
            DependencyProperty.Register("IsMoved", typeof(bool), typeof(VM_Limit), new PropertyMetadata(false));

        //public VM_Limit(string index, TF_Limit limit, VM_Limit parent)
        //{
        //    Index = index;
        //    Limit = limit;
        //    Parent = parent;
        //}

        public VM_Limit(string index, Nest<TF_Limit> limit, VM_Limit parent)
        {
            Index = index;
            Parent = parent;
            NestLimit = limit;
        }


        public bool IsSameName(VM_Limit baselimit)
        {
            if (Limit?.Name == baselimit.Limit?.Name)
            {
                if (Parent is null)
                {
                    if (baselimit.Parent is null)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (baselimit.Parent is null)
                    {
                        return false;
                    }
                    return Parent.IsSameName(baselimit.Parent);
                }
            }
            else
            {
                return false;
            }
        }

        public static void FlattenSpecLimit(Nest<TF_Limit> limits, ICollection<VM_Limit> flat, string index, VM_Limit vm_p = null)
        {
            int subidx = 1;
            foreach (var sub in limits)
            {
                var idx = index is null ? subidx.ToString() : string.Format("{0}.{1}", index, subidx);
                var vm = new VM_Limit(idx, sub, vm_p);
                flat.Add(vm);

                FlattenSpecLimit(sub, flat, idx, vm);
                subidx++;
            }
        }

        public static INest<TF_Limit> FetchLimit(INest<TF_Limit> source, VM_Limit limit)
        {
            List<TF_Limit> limitlist = new List<TF_Limit>();


            do
            {
                limitlist.Add(limit.Limit);
                limit = limit.Parent;
            }
            while (limit != null);

            limitlist.Reverse();

            INest<TF_Limit> rtn = source;

            foreach (var templimit in limitlist)
            {
                rtn = rtn.FirstOrDefault(x => x.Element.Name == templimit.Name);

                if (rtn is null)
                {
                    break;
                }
            }

            return rtn;
        }

        public string GetDefectName()
        {
            List<string> d = new List<string>();
            var node = this;
            while (node != null)
            {
                d.Add(node.Limit.Name);
                node = node.Parent;
            }
            d.Reverse();
            return string.Join(TF_StepData.NameSeparator, d);
        }
    }
}
