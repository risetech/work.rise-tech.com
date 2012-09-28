using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RiseWork.Models
{
	public class CheckInModel
	{
		public long ProjectId { get; set; }
		public long Hours { get; set; }
		public DateTime Date { get; set; }
		public string Comment { get; set; }
	}
}