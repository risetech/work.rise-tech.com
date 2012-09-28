using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RiseWork.Models;

namespace Rise_work.Controllers
{
	public class HomeController : Controller
	{
		private RiseWorkEntities _db = new RiseWorkEntities();

		public ActionResult Index()
		{
			return View(_db.UserProjectsM2M);
		}

		public ActionResult About()
		{
			return View();
		}
	}
}
