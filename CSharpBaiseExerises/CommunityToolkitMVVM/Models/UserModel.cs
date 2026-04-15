using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkitMVVM.Models
{
    public class UserModel : ObservableObject
    {
		private int _id;

		public int Id
		{
			get { return _id; }
			set 
			{ 
				SetProperty(ref _id, value);
			}
		}

		private string _name;

		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}
	}
}
