using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RiseWork.Models;

namespace Rise_work.Controllers
{
	[Authorize]
	public class ProjectsController : Controller
	{
		private RiseWorkEntities db = new RiseWorkEntities();

		public ViewResult Index()
		{
			return View(db.Projects.ToList());
		}

		public ActionResult Create()
		{
			return View();
		}

		[HttpPost]
		public ActionResult Create(Projects projects)
		{
			if (ModelState.IsValid)
			{
				db.Projects.Add(projects);
				db.SaveChanges();
				return RedirectToAction("Index");
			}

			return View(projects);
		}

		public ActionResult Edit(long id)
		{
			Projects projects = db.Projects.Single(p => p.Project_id == id);
			return View(projects);
		}

		[HttpPost]
		public ActionResult Edit(Projects projects)
		{
			if (ModelState.IsValid)
			{
				db.Projects.Attach(projects);
				db.ChangeTracker.DetectChanges();
				db.SaveChanges();
				return RedirectToAction("Index");
			}
			return View(projects);
		}

		//public ActionResult Delete(long id)
		//{
		//    Projects projects = db.Projects.Single(p => p.Project_id == id);
		//    return View(projects);
		//}

		//[HttpPost, ActionName("Delete")]
		//public ActionResult DeleteConfirmed(long id)
		//{
		//    Projects projects = db.Projects.Single(p => p.Project_id == id);
		//    db.Projects.DeleteObject(projects);
		//    db.SaveChanges();
		//    return RedirectToAction("Index");
		//}

		public string CheckIn(CheckInModel model)
		{
			string userName = User.Identity.Name.Split('\\')[1];
			var user = db.User.SingleOrDefault(x => x.User_name == userName);
			if (user == null)
			{
				db.User.Add(new User() { User_name = userName });
				db.SaveChanges();
				user = db.User.SingleOrDefault(x => x.User_name == userName);
			}
			UserProjectsM2M m2m = new UserProjectsM2M()
			{
				Projects_id = model.ProjectId,
				User_id = user.User_id,
				UserProjectsM2M_hours = model.Hours,
				UserProjectsM2M_date = model.Date,
				UserProjectsM2M_comment = model.Comment
			};
			db.UserProjectsM2M.Add(m2m);
			db.SaveChanges();
			return "ok";
		}

		protected override void Dispose(bool disposing)
		{
			db.Dispose();
			base.Dispose(disposing);
		}
	}
}