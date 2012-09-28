using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Newtonsoft.Json;
using System.Web.UI;
using RiseWork.Models;
using RiseWork.Code;

namespace Rise_work.Controllers
{
	public class GoogleAnalyticsController : Controller
	{
		RiseWorkEntities _db = new RiseWorkEntities();
		private DateTime _start;

		private static Dictionary<string, List<Tuple<string, string>>> _projects;
		private static Dictionary<string, List<Tuple<string, string>>> Projects
		{
			get
			{
				if (_projects == null)
				{
					_projects = new Dictionary<string, List<Tuple<string, string>>>();
					var xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(RiseWork.Properties.Resources.Projects);
					var projects = xmlDoc.ChildNodes.Cast<XmlNode>().Single(d => d.Name == "Projects");
					foreach (var projectsType in projects.ChildNodes.Cast<XmlNode>().Where(d => d.Name == "ProjectsType"))
					{
						var tables = new List<Tuple<string, string>>();
						foreach (var project in projectsType.ChildNodes.Cast<XmlNode>().Where(d => d.Name == "Project"))
							tables.Add(new Tuple<string, string>(project.Attributes["name"].Value, project.Attributes["tableId"].Value));
						_projects.Add(projectsType.Attributes["name"].Value, tables);
					}
				}
				return _projects;
			}
		}

		private Dictionary<string, Int64> GetVisitors(List<string> tablesId, DateTime start, DateTime end)
		{
			var answer = new Dictionary<string, Int64>();
			foreach (var id in tablesId)
			{
				long p = 0;
				int i = 0;
				do
				{
					p = DataFeedLoader.GetVisitors(id, start, end);
					System.Threading.Thread.Sleep(200);
					if (p<0)
						System.Threading.Thread.Sleep(15000);
					i++;
				}
				while (p < 0 && i<5);
				if (p < 0) p = 0;
				answer.Add(id, p);
				
			}
			return answer;
		}

		private Dictionary<string, Int64> GetVisitorsByGroup(List<string> tablesId, DateTime start, DateTime end)
		{
			var answer = new Dictionary<string, Int64>();
			foreach (var projectType in Projects)
			{
				answer.Add(projectType.Key, GetVisitors(projectType.Value.Select(p => p.Item2).ToList(), start, end).Sum(v => v.Value));
				System.Threading.Thread.Sleep(1000);
			}
			return answer;
		}
		public JsonResult GetVisitorsByGroups()
		{
			var answer = new Dictionary<string, Dictionary<DateTime, Int64>>();
			var currentDate = DateTime.Now;
			foreach (var projectType in Projects)
				answer.Add(projectType.Key, new Dictionary<DateTime, Int64>());
			while (_start < currentDate)
			{
				var newCurrentDate = currentDate.Add(new TimeSpan(-7, 0, 0, 0));
				var newWeek = GetVisitorsByGroup(answer.Keys.ToList(), newCurrentDate, currentDate);
				foreach (var projectTypeWeek in newWeek)
					answer[projectTypeWeek.Key].Add(newCurrentDate.Date, projectTypeWeek.Value);
				currentDate = newCurrentDate;
			}
			return Json(JsonConvert.SerializeObject(answer), JsonRequestBehavior.AllowGet);
		}
		private string GetProjectNameById(string id)
		{
			return Projects.SelectMany(p => p.Value).Single(p => p.Item2 == id).Item1;
		}
		private string GetProjectIdByName(string id)
		{
			return Projects.SelectMany(p => p.Value).Single(p => p.Item1 == id).Item2;
		}

		public JsonResult GetVisitorsByProjects()
		{
			var answer = new Dictionary<string, Dictionary<DateTime, Int64>>();
			var currentDate = DateTime.Now;
			foreach (var projectType in Projects)
				foreach (var project in projectType.Value)
					answer.Add(project.Item1, new Dictionary<DateTime, Int64>());
			while (_start < currentDate)
			{
				System.Threading.Thread.Sleep(1000);
				var newCurrentDate = currentDate.Add(new TimeSpan(-7, 0, 0, 0));
				var newWeek = GetVisitors(answer.Keys.ToList(), newCurrentDate, currentDate);
				foreach (var projectTypeWeek in newWeek)
					answer[GetProjectNameById(projectTypeWeek.Key)].Add(newCurrentDate.Date, projectTypeWeek.Value);
				currentDate = newCurrentDate;
			}
			return Json(JsonConvert.SerializeObject(answer), JsonRequestBehavior.AllowGet);
		}

		private void SaveProjectToDb()
		{
			foreach (var t in Projects)
				foreach (var p in t.Value)
				{
					var site = _db.Sites.FirstOrDefault(o => o.Site_name == p.Item1);
					if (site == null)
					{
						_db.Sites.Add(new Sites() { Site_name = p.Item1 });
						_db.SaveChanges();
					}
				}
		}
		private long GetSiteIdByName(string name)
		{
			return _db.Sites.FirstOrDefault(o => o.Site_name == name).Site_id;
		}
		[OutputCache(Duration=3600, Location=OutputCacheLocation.Server)]
		public JsonResult GetVisitors()
		{
			var answer = new Dictionary<string, Dictionary<string, Dictionary<DateTime, float>>>();
			answer.Add("Projects", new Dictionary<string, Dictionary<DateTime, float>>());
			answer.Add("ProjectGroups", new Dictionary<string, Dictionary<DateTime, float>>());
			var currentDate = DateTime.Now;
			foreach (var projectType in Projects)
				foreach (var project in projectType.Value)
					answer["Projects"].Add(project.Item1, new Dictionary<DateTime, float>());
			answer["Projects"] = answer["Projects"].Reverse().ToDictionary(o => o.Key, o => o.Value);
			foreach (var projectType in Projects)
				answer["ProjectGroups"].Add(projectType.Key, new Dictionary<DateTime, float>());
			SaveProjectToDb();
			foreach (var projectType in Projects)
				foreach (var p in projectType.Value)
				{
					System.Threading.Thread.Sleep(1000);
					var siteId = GetSiteIdByName(p.Item1);
					var data = _db.Analytics.Where(o => o.Sites_id == siteId).ToList();
					if (data.Count == 0)
						_start = new DateTime(2010, 10, 4);
					else
						_start = data[data.Count - 1].Analytics_date.AddDays(7);
					foreach (var i in data)
					{
						answer["Projects"][p.Item1].Add(i.Analytics_date, i.Analytics_value);
					}
					while (_start <= DateTime.Now.Date)
					{
						DateTime nextDate;
						if (DateTime.Now - _start >= TimeSpan.FromDays(6))
							nextDate = _start.AddDays(6);
						else
							nextDate = DateTime.Now;
						string projId = GetProjectIdByName(p.Item1);
						var newWeek = GetVisitors(new List<string> { projId }, _start, nextDate);
						System.Threading.Thread.Sleep(300);
						foreach (var projectWeek in newWeek)
						{
							answer["Projects"][p.Item1].Add(nextDate.Date, projectWeek.Value);
							if (_start < DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1))
							{
								_db.Analytics.Add(new Analytics()
								{
									Sites_id = GetSiteIdByName(p.Item1),
									Analytics_value = (int)projectWeek.Value,
									Analytics_date = _start
								});
								_db.SaveChanges();
							}
						}
						_start = nextDate.AddDays(1);
					}
					answer["Projects"][p.Item1] = answer["Projects"][p.Item1].OrderByDescending
						(o => o.Key).ToDictionary(o => o.Key, o => o.Value);
				}
			foreach (var projectType in Projects)
			{
				foreach (var p in projectType.Value)
				{
					foreach (var i in answer["Projects"][p.Item1])
						if (answer["ProjectGroups"][projectType.Key].ContainsKey(i.Key))
							answer["ProjectGroups"][projectType.Key][i.Key] += i.Value;
						else answer["ProjectGroups"][projectType.Key].Add(i.Key, i.Value);

				}
				answer["ProjectGroups"][projectType.Key] = answer["ProjectGroups"][projectType.Key].
					OrderByDescending(o => o.Key).ToDictionary(o => o.Key, o => o.Value);
				//CalcAverage(answer["ProjectGroups"], 4, "Средняя 4");
				CalcAverage(answer["ProjectGroups"], 8, "Средняя 8");
			}
			return Json(JsonConvert.SerializeObject(answer), JsonRequestBehavior.AllowGet);
		}
		private void CalcAverage(Dictionary<string, Dictionary<DateTime, float>> data, int n, string name)
		{
			var input = data.First().Value;
			var result = new Dictionary<DateTime, float>();
			for (int i = 0; i < input.Values.Count - n; i++)
			{
				float avr = 0;
				for (int j = i, k = 0; k < n; k++, j++)
					avr += input.Values.ElementAt(j);
				result.Add(input.Keys.ElementAt(i), avr / n);
			}
			for (int i = input.Values.Count - n; i < input.Values.Count; i++)
				result.Add(input.Keys.ElementAt(i), 0);
			result = result.OrderByDescending(o => o.Key).ToDictionary(o => o.Key, o => o.Value);
			data.Add(name, result);
		}
		public ActionResult Index()
		{
			return View();
		}
	}
}
