using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenRecordTool.Models
{
    public class ScreenModel
    {
		private string _btnIcon = string.Empty;

		public string BtnIcon
		{
			get => _btnIcon;
			set
			{
				if(string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("BtnIcon", "Button icon path cannot be null or empty.");
                }
                else
                {
                    _btnIcon = value;
                }
            }
		}

		private string? _btnContent;

		public string? BtnContent
		{
			get { return _btnContent; }
			set
			{
				if(string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("BtnContent", "Button content cannot be null or empty.");
                }
                else
                {
                    _btnContent = value;
                }
            }
		}

        public ScreenModel()
        {
            
        }

        public ScreenModel(string? btnIcon,string? btnContent)
        {
            _btnIcon = btnIcon ?? throw new ArgumentNullException("btnIcon", "Button icon path cannot be null.");
            _btnContent = btnContent ?? throw new ArgumentNullException("btnContent", "Button content cannot be null.");
        }

    }
}
