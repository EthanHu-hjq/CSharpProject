using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualMVVM.Models
{
    public class UserModel
    {
		private int _id;

		public int Id
		{
			get { return _id; }
			set { _id = value; }
		}

		private string? _nsme;

		public string Name
		{
			get { return _nsme??""; }
			set
			{
				if(string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Name cannot be null or empty");
                }
                _nsme = value;
            }
		}

        public UserModel()
        {
            
        }

        public UserModel(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
