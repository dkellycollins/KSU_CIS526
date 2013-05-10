﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Demo.Models;
using Demo.Repositories;
using System.ComponentModel.DataAnnotations;
using Demo.Filters;

namespace Demo.Models
{
    public class TaskViewModel
    {
        [Display(Name="Milestones")]
        public List<Task> MileStones { get; set; }

        [Display(Name="Tasks")]
        public List<Task> Tasks { get; set; }
    }

    public class TaskCompleteViewModel
    {
        public string TaskName {get; set;}
        public string TaskDescription { get; set;}
        public string TaskToken { get; set; }
    }
}

namespace Demo.Controllers
{
    /// <summary>
    /// Handles creating pages for tasks.
    /// </summary>
    public class TaskController : Controller
    {
        private IRepository<UserProfile> _userRepo;
        private IRepository<Task> _taskRepo;

        public TaskController()
        {
            _taskRepo = new BasicRepo<Task>();
            _userRepo = new BasicRepo<UserProfile>();
        }

        //
        // GET: /Task/
        public ActionResult Index()
        {
            TaskViewModel tvm = new TaskViewModel()
            {
                MileStones = _taskRepo.GetAll().Where(tr => tr.IsMilestone == true).ToList(),
                Tasks = _taskRepo.GetAll().Where(tr=>tr.IsMilestone == false).ToList()
            };

            return View(tvm);
        }

        //
        // GET: /Task/Detials/{id}
        public ActionResult Details(int id)
        {
            return View(_taskRepo.Get(id));
        }

        //Admin Task
        // GET: /Task/Create/
        [CasAuthorize]
        public ActionResult Create()
        {
            return View();
        }

        //Admin Task
        // POST: /Task/Create/
        [CasAuthorize]
        public ActionResult Create(Task item)
        {
            if(ModelState.IsValid)
            {
                item.Token = new Guid().ToString();
                _taskRepo.Create(item);
            }

            return RedirectToAction("Index");
        }

        //
        // GET: /Task/Update/
        [CasAuthorize]
        public ActionResult Update()
        {
            return View();
        }

        //
        // POST: /Task/Update/
        [CasAuthorize]
        public ActionResult Update(Task item)
        {
            if(ModelState.IsValid)
            {
                _taskRepo.Create(item);
            }

            return RedirectToAction("Index");
        }

        //
        // GET: /Task/Delete/{id}
        [HttpGet]
        [CasAuthorize]
        public ActionResult Delete(int id)
        {
            return View(_taskRepo.Get(id));
        }

        //
        // POST: /Task/Delete/{id}
        [HttpPost]
        [CasAuthorize]
        public ActionResult DeleteComfirmed(int id)
        {
            _taskRepo.Delete(id);

            return RedirectToAction("Index");
        }

        //
        // GET: /Task/CompleteTask/{id}
        [HttpGet]
        public ActionResult CompleteTask(int id)
        {
            Task task = _taskRepo.Get(id);
            if (task == null)
                return new HttpNotFoundResult();
            return View(createTaskViewModel(task));
        }

        //
        // POST: /Task/CompleteTask/
        [HttpPost]
        //public ActionResult CompeleteTask(TaskCompletePacket packet)
        public ActionResult CompleteTask(FormCollection formCollection)
        {
            string status = null;
            
            //Find user and task.
            int userID;
            UserProfile user = null;
            if(Int32.TryParse(formCollection["UserID"], out userID))
                user = _userRepo.Get(userID);
            string taskToken = formCollection["TaskToken"];
            Task task = _taskRepo.Get((t) => t.Token == taskToken).FirstOrDefault();
            
            if (user == null)
            { //Ensure we have a user.
                status = "User not found";
            }
            else if (task == null)
            { //Ensure we have a task.
                status = "Task not found";
            }
            else if (DateTime.Now < task.StartTime || DateTime.Now > task.EndTime)
            { //Verify that this task can be completed.
                status = "Task is not availible at this time";
            }
            else
            { //Complete the task.
                if (!string.IsNullOrEmpty(task.Solution))
                {
                    if (formCollection["Solution"] == task.Solution)
                    {
                        addTaskToUser(user, task);
                        status = "Correct!";
                    }
                    else
                    {
                        status = "Incorrect solution.";
                    }
                }
                else
                {
                    addTaskToUser(user, task);
                    status = "Task Completed";
                }
            }

            ViewBag.StatusMessage = status;
            return RedirectToAction("Index", "Scoreboard");
        }

        private TaskCompleteViewModel createTaskViewModel(Task task)
        {
            TaskCompleteViewModel tcvm = new TaskCompleteViewModel()
            {
                TaskName = task.Name,
                TaskDescription = task.Description,
                TaskToken = task.Token
            };

            return tcvm;
        }

        private void addTaskToUser(UserProfile user, Task task)
        {
            CompletedTask cTask = new CompletedTask()
            {
                Task = task,
                TaskID = task.ID,
                UserProfile = user,
                UserProfileID = user.ID,
                CompletedDate = DateTime.Now,
            };

            if (task.CompletedBy.Count < task.MaxBonusAwards)
                cTask.AwardedPoints = task.Points + task.BonusPoints;
            else
                cTask.AwardedPoints = task.Points;

            user.CompletedTask.Add(cTask);
            task.CompletedBy.Add(cTask);
            _taskRepo.Update(task);
            _userRepo.Update(user);
        }
    }
}
