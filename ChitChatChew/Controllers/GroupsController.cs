using ChitChatChew.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ChitChatChew.Controllers
{
    public class TempGroup
    {
        public string groupName { get; set; }
        public string createdBy { get; set; }
        public string[] Users { get; set; }
    }
    public class GroupsController : Controller
    {
        private MainModels db = new MainModels();

        //POST: Users/Groups/Create
        [HttpPost]
        public ActionResult Create(TempGroup requestGroup)
        {
            //check if group already exist
            var groupCreatorID = Convert.ToInt32(requestGroup.createdBy);
            var existingGroup = db.Groups.Where(x => x.groupName == requestGroup.groupName && x.createdBy == groupCreatorID).FirstOrDefault();
            if(existingGroup == null)
            {
                //Create a new group (name, createdBy, isPublic)
                Group newGroup = new Group();
                newGroup.groupName = setGroupName(requestGroup.groupName);
                if (requestGroup.createdBy != null && requestGroup.createdBy.Trim() != "")
                {
                    newGroup.createdBy = Convert.ToInt32(requestGroup.createdBy);
                }
                
                //add users
                for (int i = 0; i < requestGroup.Users.Length; i++)
                {
                    var addedUserId = Convert.ToInt32(requestGroup.Users[i]);
                    var addUser = db.Users.Where(x => x.id == addedUserId).FirstOrDefault();
                    if (addUser != null)
                    {
                        newGroup.Users.Add(addUser);
                    }
                }
                //if the creator is not in already then add creator
                if (!newGroup.Users.Contains(db.Users.Where(x => x.id == groupCreatorID).FirstOrDefault()))
                {
                    newGroup.Users.Add(db.Users.Where(x => x.id == groupCreatorID).FirstOrDefault());
                }
                db.Groups.Add(newGroup);
                db.SaveChangesAsync();
                var isGroupActive = false;
                foreach(var user in newGroup.Users)
                {
                    if(user.id != newGroup.createdBy && user.isActive == true)
                    {
                        isGroupActive = true;
                        break;
                    }
                }
                return Json(new { groupId = newGroup.id, groupName = newGroup.groupName, isActive = isGroupActive }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var isGroupActive = false;
                foreach (var user in existingGroup.Users)
                {
                    if (user.id != existingGroup.createdBy && user.isActive == true)
                    {
                        isGroupActive = true;
                        break;
                    }
                }
                return Json(new { groupId = existingGroup.id, groupName = existingGroup.groupName, isActive = isGroupActive }, JsonRequestBehavior.AllowGet);
            }
        }

        //GET: Users/Groups/List
        [Route("users/groups/list")]
        public ActionResult getUserGroups()
        {
            var ult = new Utilities();
            if (!ult.isLoggedIn())
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                var currentSessionUser = (User)Session["currentUser"];
                var currentUser = db.Users.Where(x => x.id == currentSessionUser.id).FirstOrDefault();
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Users");
                }
                else
                {
                    var joinedGroups = currentUser.InGroups.Select(x => new { id = x.id, groupName = x.groupName, users = x.Users, messages = x.Messages}).ToList();
                    List<Object> resultGroups = new List<object>();
                    foreach (var g in joinedGroups)
                    {
                        var isGroupActive = false;
                        foreach (var u in g.users)
                        {
                            if (u.isActive == true && u.id != currentUser.id)
                            {
                                isGroupActive = true;
                                break;
                            }
                        }
                        if(g.messages.Count > 0)
                        {
                            var latestMessageText = g.messages.OrderByDescending(x => x.createdAt).FirstOrDefault().messageText;
                            var latestMessageTime = g.messages.OrderByDescending(x => x.createdAt).FirstOrDefault().createdAt;
                            var timeString = "";
                            if(latestMessageTime != null)
                            {
                                var now = DateTime.Now;
                                var msgTime = latestMessageTime.Value;
                                if(now.Year > msgTime.Year)
                                {
                                    timeString = latestMessageTime?.ToString("dd/MM/yyyy");
                                }
                                else
                                {
                                    int dowNow, dowMsg;
                                    TimeSpan span = now.Subtract(msgTime);
                                    dowNow = (int)now.DayOfWeek == 0 ? 7 : (int)now.DayOfWeek;
                                    dowMsg = (int)msgTime.DayOfWeek == 0 ? 7 : (int)msgTime.DayOfWeek;
                                    bool isSameWeek = (span.Days > (dowNow - dowMsg)) ? false : true;
                                    if (isSameWeek == true)
                                    {
                                        if (span.Days == 0)
                                        {
                                            timeString = latestMessageTime?.ToString("HH:mm");
                                        }
                                        else
                                        {
                                            timeString = latestMessageTime?.ToString("ddd");
                                        }
                                    }
                                    else
                                    {
                                        timeString = latestMessageTime?.ToString("dd MMM");
                                    }
                                }
                            }
                            resultGroups.Add(new { id = g.id, groupName = g.groupName, isActive = isGroupActive, latestMessageText = latestMessageText, latestMessageTime = timeString });
                        }
                        else
                        {
                            resultGroups.Add(new { id = g.id, groupName = g.groupName, isActive = isGroupActive, latestMessageText = "", latestMessageTime = "" });
                        }
                    }
                    return Json(resultGroups, JsonRequestBehavior.AllowGet);
                }
            }
        }

        private string setGroupName(string groupName)
        {
            if(groupName == null || groupName.Trim() == "")
            {
                Random r = new Random();
                groupName = "GroupChat" + r.Next(0, 9999).ToString();
            }
            return groupName;
        }

    }

}